using UnityEngine;
using System.Collections;
using KingdomWar.Game.Cards;
using KingdomWar.Tools;
using KingdomWar.Game.Pathfinding;
using KingdomWar.UI;
using System.Collections.Generic;
using Photon.Pun;

namespace KingdomWar.Game.Battle
{
    public class Unit : MonoBehaviour
    {
        [Header("单位属性")]
        public int ownerId;              
        public string unitName;          
        public int health;               
        public int maxHealth;            
        public int damage;               
        public float attackSpeed;        
        public float moveSpeed;          
        public float attackRange;        
        public float deployTime;         
        public GameObject projectilePrefab; 
        
        [Header("单位状态")]
        public UnitState state = UnitState.Idle; 

        [Header("网络控制")]
        public bool isNetworkControlled = false;
        
        [Header("动画组件")]
        public Animator animator;

        [Header("血条")]
        public UnitHealthBar healthBar; 
        
        private float attackTimer = 0f;  
        private Unit targetUnit;         
        private Building targetBuilding; 
        public Vector3 moveTarget;      
        public bool isMoving = false;   
        public bool isAttacking = false; 
        public bool isHit = false;       
        public bool isDead = false;      

        // Buff system fields
        private List<Buff> activeBuffs = new List<Buff>();
        public bool isFrozen { get; private set; } = false;
        public bool isStunned { get; private set; } = false;
        public float damageMultiplier { get; private set; } = 1f;
        public float speedMultiplier { get; private set; } = 1f;

        private bool hasFiredProjectile = false;
        private float projectileFireTime = 0f;
        private float projectileFireDelay = 0.3f;
        
        private GridManager pathfindingGrid;    
        private AStar pathfinding;
        private List<Vector3> path;       // 当前路径
        private int currentPathIndex;    // 当前路径索引
        private float waypointThreshold = 0.5f; // 到达路径点的阈值
        private float targetReachedThreshold = 0.8f; // 到达目标点的阈值
        private float pathRecalculationThreshold = 1.5f; // 路径重新计算阈值
        private float idleCooldown = 0.5f; // 空闲状态冷却时间
        private float lastIdleTime = 0f; // 上次进入空闲状态的时间
        private GameObject currentTargetObject = null; // 当前寻路目标对象
        
        // 初始化单位
        public void Initialize(int ownerId, string unitName, int health, int damage, float attackSpeed, float moveSpeed, float attackRange)
        {
            this.ownerId = ownerId;
            this.unitName = unitName;
            this.health = health;
            this.maxHealth = health;
            this.damage = damage;
            this.attackSpeed = attackSpeed;
            this.moveSpeed = moveSpeed;
            this.attackRange = attackRange;
            this.state = UnitState.Idle;
            
            // 初始化寻路系统
            InitializePathfinding();
            CreateHealthBar();
        }
        // 初始化单位
        public void Initialize(int ownerId, CardData cardData)
        {
            Initialize(ownerId, cardData.cardName, cardData.unitData.health, cardData.unitData.damage, cardData.unitData.attackSpeed, cardData.unitData.moveSpeed, cardData.unitData.attackRange);
            // 获取远程攻击物体预制体
            if (cardData.unitData != null)
            {
                projectilePrefab = cardData.unitData.projectilePrefab;
            }
            animator = GetComponent<Animator>();
        }
        
        /// <summary>
        /// 初始化寻路系统
        /// </summary>
        private void InitializePathfinding()
        {
            // 获取场景中的GridManager组件
            pathfindingGrid = FindObjectOfType<GridManager>();
            if (pathfindingGrid != null)
            {
                // 创建AStar实例
                pathfinding = new AStar(pathfindingGrid);
                Debug.Log($"寻路系统初始化成功: {pathfindingGrid.GridSizeX}x{pathfindingGrid.GridSizeZ}");
            }
            else
            {
                Debug.LogWarning("场景中未找到GridManager组件，寻路功能将不可用");
            }
        }        
        public void UpdateUnit()
        {
            UpdateBuffs();

            if (isNetworkControlled)
            {
                UpdateAnimation();
                return;
            }

            switch (state)
            {
                case UnitState.Idle:
                    FindTarget();
                    break;
                case UnitState.Moving:
                    MoveToTarget();
                    break;
                case UnitState.Attacking:
                    AttackTarget();
                    break;
                case UnitState.Dead:
                    Die();
                    break;
            }
            
            UpdateAnimation();
        }
        
        /// <summary>
        /// 更新动画参数
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator != null)
            {
                // 更新移动状态
                animator.SetBool("IsMoving", isMoving);
                animator.SetFloat("MoveSpeed", isMoving ? moveSpeed : 0f);
                
                // 更新攻击状态
                animator.SetBool("Attack", isAttacking);
                
                // 更新受击状态
                animator.SetBool("GetHit", isHit);
                
                // 更新死亡状态
                animator.SetBool("IsDead", isDead);
                
                // 重置受击状态
                if (isHit)
                {
                    isHit = false;
                }
            }
        }
        
        // 寻找目标
        private void FindTarget()
        {
            // 检查空闲状态冷却时间
            if (Time.time - lastIdleTime < idleCooldown)
            {
                return;
            }
            
            // 设置空闲状态
            isMoving = false;
            isAttacking = false;
            
            // 优先攻击敌方单位
            targetUnit = FindClosestEnemyUnit();
            if (targetUnit != null)
            {
                // 检查是否在攻击范围内
                if (Vector3.Distance(transform.position, targetUnit.transform.position) <= attackRange)
                {
                    state = UnitState.Attacking;
                    currentTargetObject = targetUnit.gameObject;
                }
                else
                {
                    // 检查是否与当前目标相同
                    if (targetUnit.gameObject == currentTargetObject)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[Unit] {unitName} target unchanged, skipping pathfinding");
#endif
                        return;
                    }
                    moveTarget = targetUnit.transform.position;
                    state = UnitState.Moving;
                    currentTargetObject = targetUnit.gameObject;
                }
                return;
            }
            
            // 其次攻击敌方建筑
            targetBuilding = FindClosestEnemyBuilding();
            if (targetBuilding != null)
            {
                // 检查是否在攻击范围内（考虑建筑碰撞体积）
                float distanceToBuilding = GetDistanceToBuilding(targetBuilding);
                if (distanceToBuilding <= attackRange)
                {
                    state = UnitState.Attacking;
                    currentTargetObject = targetBuilding.gameObject;
                }
                else
                {
                    // 检查是否与当前目标相同
                    if (targetBuilding.gameObject == currentTargetObject)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[Unit] {unitName} target unchanged, skipping pathfinding");
#endif
                        return;
                    }
                    moveTarget = targetBuilding.transform.position;
                    state = UnitState.Moving;
                    currentTargetObject = targetBuilding.gameObject;
                }
                return;
            }
            
            
            // 如果没有目标，重置当前目标
            currentTargetObject = null;
            
            // 如果没有目标，向敌方基地移动
            // moveTarget = GetEnemyBasePosition();
            // state = UnitState.Moving;
        }
        
        // 移动到目标
        private void MoveToTarget()
        {
            if (isFrozen || isStunned)
            {
                isMoving = false;
                return;
            }

            isMoving = true;
            isAttacking = false;
            
            // 检查是否到达目标
            if (Vector3.Distance(transform.position, moveTarget) <= targetReachedThreshold)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"{unitName} 到达目标");
#endif
                isMoving = false;
                
                // 优先检查当前目标是否可攻击
                if (currentTargetObject != null)
                {
                    Unit currentTargetUnit = currentTargetObject.GetComponent<Unit>();
                    Building currentTargetBuilding = currentTargetObject.GetComponent<Building>();
                    
                    if (currentTargetUnit != null && currentTargetUnit.health > 0)
                    {
                        float distance = Vector3.Distance(transform.position, currentTargetUnit.transform.position);
                        if (distance <= attackRange)
                        {
                            targetUnit = currentTargetUnit;
                            targetBuilding = null;
                            state = UnitState.Attacking;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log($"{unitName} 到达目标，当前目标单位在攻击范围内，切换到攻击状态");
#endif
                            return;
                        }
                    }
                    else if (currentTargetBuilding != null && currentTargetBuilding.health > 0)
                    {
                        float distance = GetDistanceToBuilding(currentTargetBuilding);
                        if (distance <= attackRange)
                        {
                            targetBuilding = currentTargetBuilding;
                            targetUnit = null;
                            state = UnitState.Attacking;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log($"{unitName} 到达目标，当前目标建筑在攻击范围内，切换到攻击状态");
#endif
                            return;
                        }
                    }
                }
                
                // 当前目标不可用，重新搜索目标
                Unit nearbyEnemy = FindClosestEnemyUnit();
                Building nearbyBuilding = FindClosestEnemyBuilding();
                if (nearbyEnemy != null && Vector3.Distance(transform.position, nearbyEnemy.transform.position) <= attackRange)
                {
                    // 有可攻击的敌方单位，切换到攻击状态
                    targetUnit = nearbyEnemy;
                    targetBuilding = null;
                    state = UnitState.Attacking;
                    currentTargetObject = targetUnit.gameObject;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"{unitName} 到达目标，发现可攻击单位，切换到攻击状态");
#endif
                }
                else if (nearbyBuilding != null && GetDistanceToBuilding(nearbyBuilding) <= attackRange)
                {
                    // 有可攻击的敌方建筑，切换到攻击状态
                    targetBuilding = nearbyBuilding;
                    targetUnit = null;
                    state = UnitState.Attacking;
                    currentTargetObject = targetBuilding.gameObject;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"{unitName} 到达目标，发现可攻击建筑，切换到攻击状态");
#endif
                }
                else
                {
                    // 没有可攻击的目标，切换到空闲状态
                    state = UnitState.Idle;
                    lastIdleTime = Time.time;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"{unitName} 到达目标，无攻击目标，切换到空闲状态");
#endif
                }
                return;
            }
            
            // 检查当前目标是否死亡，如果死亡则立即寻找新目标
            // 同时检查当前目标是否已在攻击范围内
            if (currentTargetObject != null)
            {
                Unit targetUnitCheck = currentTargetObject.GetComponent<Unit>();
                Building targetBuildingCheck = currentTargetObject.GetComponent<Building>();
                
                bool targetIsDead = false;
                bool targetInRange = false;
                
                if (targetUnitCheck != null)
                {
                    if (targetUnitCheck.health <= 0)
                    {
                        targetIsDead = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[Unit] {unitName} 当前目标单位已死亡，寻找新目标");
#endif
                    }
                    else
                    {
                        float distanceToTarget = Vector3.Distance(transform.position, targetUnitCheck.transform.position);
                        if (distanceToTarget <= attackRange)
                        {
                            targetInRange = true;
                            targetUnit = targetUnitCheck;
                            targetBuilding = null;
                        }
                    }
                }
                else if (targetBuildingCheck != null)
                {
                    if (targetBuildingCheck.health <= 0)
                    {
                        targetIsDead = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"[Unit] {unitName} 当前目标建筑已摧毁，寻找新目标");
#endif
                    }
                    else
                    {
                        float distanceToTarget = GetDistanceToBuilding(targetBuildingCheck);
                        if (distanceToTarget <= attackRange)
                        {
                            targetInRange = true;
                            targetBuilding = targetBuildingCheck;
                            targetUnit = null;
                        }
                    }
                }
                
                if (targetIsDead)
                {
                    currentTargetObject = null;
                    targetUnit = null;
                    targetBuilding = null;
                    isMoving = false;
                    state = UnitState.Idle;
                    lastIdleTime = 0f;
                    return;
                }
                
                if (targetInRange)
                {
                    isMoving = false;
                    state = UnitState.Attacking;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"[Unit] {unitName} 移动中发现当前目标已在攻击范围内，切换到攻击状态");
#endif
                    return;
                }
            }
            
            // 如果路径为空或需要重新计算路径
            if (path == null || path.Count == 0 || ShouldRecalculatePath())
            {
                // 计算新路径
                CalculatePath();
            }
            
            // 如果路径存在，沿路径移动
            if (path != null && path.Count > 0)
            {
                // 移动到当前路径点
                Vector3 currentWaypoint = path[currentPathIndex];
                transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, moveSpeed * Time.deltaTime);
                transform.LookAt(currentWaypoint);
                
                // 检查是否到达当前路径点
                if (Vector3.Distance(transform.position, currentWaypoint) <= waypointThreshold)
                {
                    // 移动到下一个路径点
                    currentPathIndex++;
                    if (currentPathIndex >= path.Count)
                    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"{unitName} 到达路径终点!");
#endif
                        // 到达路径终点
                        isMoving = false;
                        
                        // 优先检查当前目标是否可攻击
                        if (currentTargetObject != null)
                        {
                            Unit currentTargetUnit = currentTargetObject.GetComponent<Unit>();
                            Building currentTargetBuilding = currentTargetObject.GetComponent<Building>();
                            
                            if (currentTargetUnit != null && currentTargetUnit.health > 0)
                            {
                                float distance = Vector3.Distance(transform.position, currentTargetUnit.transform.position);
                                if (distance <= attackRange)
                                {
                                    targetUnit = currentTargetUnit;
                                    targetBuilding = null;
                                    state = UnitState.Attacking;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    Debug.Log($"{unitName} 到达路径终点，当前目标单位在攻击范围内，切换到攻击状态");
#endif
                                    path = null;
                                    return;
                                }
                            }
                            else if (currentTargetBuilding != null && currentTargetBuilding.health > 0)
                            {
                                float distance = GetDistanceToBuilding(currentTargetBuilding);
                                if (distance <= attackRange)
                                {
                                    targetBuilding = currentTargetBuilding;
                                    targetUnit = null;
                                    state = UnitState.Attacking;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                                    Debug.Log($"{unitName} 到达路径终点，当前目标建筑在攻击范围内，切换到攻击状态");
#endif
                                    path = null;
                                    return;
                                }
                            }
                        }
                        
                        // 当前目标不可用，重新搜索目标
                        Unit nearbyEnemy = FindClosestEnemyUnit();
                        Building nearbyBuilding = FindClosestEnemyBuilding();
                        
                        if (nearbyEnemy != null && Vector3.Distance(transform.position, nearbyEnemy.transform.position) <= attackRange)
                        {
                            // 有可攻击的敌方单位，切换到攻击状态
                            targetUnit = nearbyEnemy;
                            targetBuilding = null;
                            state = UnitState.Attacking;
                            currentTargetObject = targetUnit.gameObject;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log($"{unitName} 到达路径终点，发现可攻击单位，切换到攻击状态");
#endif
                        }
                        else if (nearbyBuilding != null && GetDistanceToBuilding(nearbyBuilding) <= attackRange)
                        {
                            // 有可攻击的敌方建筑，切换到攻击状态
                            targetBuilding = nearbyBuilding;
                            targetUnit = null;
                            state = UnitState.Attacking;
                            currentTargetObject = targetBuilding.gameObject;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log($"{unitName} 到达路径终点，发现可攻击建筑，切换到攻击状态");
#endif
                        }
                        else
                        {
                            // 没有可攻击的目标，切换到空闲状态
                            state = UnitState.Idle;
                            lastIdleTime = Time.time;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                            Debug.Log($"{unitName} 到达路径终点，无攻击目标，切换到空闲状态");
#endif
                        }
                        path = null;
                        return;
                    }
                }
            }
            else
            {
                // 路径不存在，保持当前位置并重新计算路径
                isMoving = false;
                CalculatePath();
                if (path == null || path.Count == 0)
                {
                    // 如果仍然没有路径，切换到空闲状态
                    state = UnitState.Idle;
                }
            }
        }
        
        /// <summary>
        /// 计算到目标的路径
        /// </summary>
        private void CalculatePath()
        {
            if (pathfinding != null)
            {
                // 检查目标位置是否可走
                GridNode targetNode = pathfindingGrid.GetNodeFromWorldPosition(moveTarget);
                if (targetNode != null && !targetNode.walkable)
                {
                    // 目标位置不可走，寻找附近的可走位置
                    Vector3 nearestWalkablePosition = FindNearestWalkablePosition(moveTarget);
                    if (nearestWalkablePosition != moveTarget)
                    {
                        moveTarget = nearestWalkablePosition;
                    }
                }
                
                // 使用A*算法计算路径
                List<GridNode> nodePath = pathfinding.FindPath(transform.position, moveTarget);
                if (nodePath != null && nodePath.Count > 0)
                {
                    // 将节点路径转换为世界坐标路径
                    path = pathfinding.ConvertPathToWorldPositions(nodePath);
                    currentPathIndex = 0;
                }
                else
                {
                    // 路径计算失败
                    path = null;
                }
            }
        }
        
        /// <summary>
        /// 寻找离目标位置最近的可走位置
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>最近的可走位置</returns>
        private Vector3 FindNearestWalkablePosition(Vector3 targetPosition)
        {
            if (pathfindingGrid == null)
                return targetPosition;
            
            // 从目标位置向外搜索
            for (int distance = 1; distance <= 10; distance++)
            {
                // 搜索当前距离的所有点
                for (int x = -distance; x <= distance; x++)
                {
                    for (int z = -distance; z <= distance; z++)
                    {
                        // 只搜索当前距离的点，不搜索内部距离的点
                        if (Mathf.Abs(x) == distance || Mathf.Abs(z) == distance)
                        {
                            Vector3 checkPosition = targetPosition + new Vector3(x * pathfindingGrid.NodeRadius * 2, 0, z * pathfindingGrid.NodeRadius * 2);
                            try
                            {
                                GridNode node = pathfindingGrid.GetNodeFromWorldPosition(checkPosition);
                                if (node != null && node.walkable)
                                {
                                    return node.worldPosition;
                                }
                            }
                            catch (System.Exception)
                            {
                                // 忽略边界检查错误
                            }
                        }
                    }
                }
            }
            
            // 找不到可走位置，返回原位置
            return targetPosition;
        }
        
        /// <summary>
        /// 检查是否需要重新计算路径
        /// </summary>
        /// <returns>是否需要重新计算路径</returns>
        private bool ShouldRecalculatePath()
        {
            // 如果目标移动了足够的距离，重新计算路径
            if (path != null && path.Count > 0)
            {
                // 计算当前目标与路径终点的距离
                float distanceToTarget = Vector3.Distance(moveTarget, path[path.Count - 1]);
                return distanceToTarget > pathRecalculationThreshold;
            }
            return true;
        }
        
        /// <summary>
        /// 创建血条
        /// </summary>
        private void CreateHealthBar()
        {
            GameObject healthBarGO = new GameObject("HealthBar");
            healthBarGO.transform.SetParent(transform, false);
            healthBar = healthBarGO.AddComponent<UnitHealthBar>();
            healthBar.Initialize(transform, new Vector3(0, 2f, 0));
        }

        /// <summary>
        /// 处理攻击动画事件，造成伤害，已弃用
        /// </summary>
        public void DealDamage()
        {
            // check if currently in attacking state
            // if (state == UnitState.Attacking)
            // {
            //     // attack target unit
            //     if (targetUnit != null && targetUnit.health > 0)
            //     {
            //         targetUnit.TakeDamage(damage);
            //         Debug.Log($"{unitName} dealt {damage} damage to {targetUnit.unitName}");
            //     }
            //     // attack target building
            //     else if (targetBuilding != null && targetBuilding.health > 0)
            //     {
            //         targetBuilding.TakeDamage(damage);
            //         Debug.Log($"{unitName} dealt {damage} damage to {targetBuilding.buildingName}");
            //     }
            // }
        }
        
        /// <summary>
        /// 处理远程兵种攻击动画事件，发射攻击物体
        /// </summary>
        public void FireProjectile()
        {
            // 防止同一攻击周期内重复触发
            if (hasFiredProjectile)
            {
                return;
            }
            
            // 检查当前是否在攻击状态
            if (state == UnitState.Attacking)
            {
                // 检查是否有攻击目标
                if ((targetUnit != null && targetUnit.health > 0) || (targetBuilding != null && targetBuilding.health > 0))
                {
                    // 检查是否有远程攻击物体预制体
                    if (projectilePrefab != null)
                    {
                        hasFiredProjectile = true;
                        
                        // 计算目标位置
                        Vector3 targetPosition = Vector3.zero;
                        if (targetUnit != null)
                        {
                            targetPosition = targetUnit.transform.position;
                        }
                        else if (targetBuilding != null)
                        {
                            targetPosition = targetBuilding.transform.position;
                        }
                        
                        // 计算发射方向
                        Vector3 direction = (targetPosition - transform.position).normalized;
                        
                        // 计算发射位置（稍微向前偏移，避免从单位模型内部发射）
                        Vector3 spawnPosition = transform.position + direction * 0.5f;
                        
                        // 实例化攻击物体
                        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
                        
                        // 设置攻击物体的属性
                        Projectile projectileScript = projectile.GetComponent<Projectile>();
                        if (projectileScript != null)
                        {
                            projectileScript.ownerId = ownerId;
                            projectileScript.damage = damage;
                            projectileScript.SetTarget(targetUnit != null ? targetUnit.gameObject : targetBuilding.gameObject);
                            projectileScript.SetDirection(direction);
                        }
                        
                    }
                    else
                    {
                    }
                }
            }
        }
        
        // 攻击目标
        private void AttackTarget()
        {
            if (isFrozen || isStunned)
            {
                isAttacking = false;
                isMoving = false;
                return;
            }

            isAttacking = true;
            isMoving = false;
            
            if (targetUnit != null && targetUnit.health > 0)
            {
                if (Vector3.Distance(transform.position, targetUnit.transform.position) > attackRange)
                {
                    isAttacking = false;
                    moveTarget = targetUnit.transform.position;
                    state = UnitState.Moving;
                    return;
                }
                
                attackTimer += Time.deltaTime;
                
                if (projectilePrefab != null && !hasFiredProjectile && attackTimer >= projectileFireDelay)
                {
                    FireProjectile();
                }
                
                if (attackTimer >= attackSpeed)
                {
                    attackTimer = 0f;
                    hasFiredProjectile = false;
                    projectileFireTime = 0f;
                    
                    if (projectilePrefab == null)
                    {
                        NetworkUnit targetNetworkUnit = targetUnit.GetComponent<NetworkUnit>();
                        if (targetNetworkUnit != null)
                        {
                            targetNetworkUnit.TakeDamage(damage);
                        }
                        else
                        {
                            targetUnit.TakeDamage(damage);
                        }
                    }
                }
            }
            else if (targetBuilding != null && targetBuilding.health > 0)
            {
                float distanceToBuilding = GetDistanceToBuilding(targetBuilding);
                if (distanceToBuilding > attackRange)
                {
                    isAttacking = false;
                    moveTarget = targetBuilding.transform.position;
                    state = UnitState.Moving;
                    return;
                }
                
                attackTimer += Time.deltaTime;
                
                if (projectilePrefab != null && !hasFiredProjectile && attackTimer >= projectileFireDelay)
                {
                    FireProjectile();
                }
                
                if (attackTimer >= attackSpeed)
                {
                    attackTimer = 0f;
                    hasFiredProjectile = false;
                    projectileFireTime = 0f;
                    
                    if (projectilePrefab == null)
                    {
                        NetworkBuilding targetNetworkBuilding = targetBuilding.GetComponent<NetworkBuilding>();
                        if (targetNetworkBuilding != null)
                        {
                            targetNetworkBuilding.TakeDamage(damage);
                        }
                        else
                        {
                            targetBuilding.TakeDamage(damage);
                        }
                    }
                }
            }
            else
            {
                isAttacking = false;
                targetUnit = null;
                targetBuilding = null;
                currentTargetObject = null;
                
                Unit newTargetUnit = FindClosestEnemyUnit();
                if (newTargetUnit != null)
                {
                    targetUnit = newTargetUnit;
                    currentTargetObject = targetUnit.gameObject;
                    if (Vector3.Distance(transform.position, targetUnit.transform.position) <= attackRange)
                    {
                        state = UnitState.Attacking;
                    }
                    else
                    {
                        moveTarget = targetUnit.transform.position;
                        state = UnitState.Moving;
                    }
                    return;
                }
                
                Building newTargetBuilding = FindClosestEnemyBuilding();
                if (newTargetBuilding != null)
                {
                    targetBuilding = newTargetBuilding;
                    currentTargetObject = targetBuilding.gameObject;
                    if (GetDistanceToBuilding(targetBuilding) <= attackRange)
                    {
                        state = UnitState.Attacking;
                    }
                    else
                    {
                        moveTarget = targetBuilding.transform.position;
                        state = UnitState.Moving;
                    }
                    return;
                }
                
                state = UnitState.Idle;
                lastIdleTime = Time.time;
            }
        }
        
        // 承受伤害
        public void TakeDamage(int damage)
        {
            TakeDamage(damage, -1);
        }

        public void TakeDamage(int damage, int sourceId)
        {
            if (health > 0)
            {
                isHit = true;
                
                health -= damage;

                // Show floating damage text
                if (Application.isPlaying) DamageTextManager.Instance.ShowDamage(transform.position + Vector3.up * 2f, damage);

                if (BattleEventSystem.Instance != null)
                {
                    BattleEventSystem.Instance.EmitUnitDamaged(GetInstanceID(), damage, sourceId);
                }
                
                if (healthBar != null)
                {
                    healthBar.UpdateHealth((float)health / maxHealth);
                }

                if (health <= 0)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"{unitName} 受到 {damage} 点伤害，死亡");
#endif
                    health = 0;
                    isDead = true;
                    isMoving = false;
                    isAttacking = false;
                    state = UnitState.Dead;
                    
                    if (BattleEventSystem.Instance != null)
                    {
                        BattleEventSystem.Instance.EmitUnitDeath(GetInstanceID(), ownerId);
                    }
                }
            }
        }

        // 治疗
        public void Heal(int amount)
        {
            if (health > 0 && health < maxHealth)
            {
                health += amount;
                if (health > maxHealth)
                {
                    health = maxHealth;
                }

                // Show healing text
                if (Application.isPlaying) DamageTextManager.Instance.ShowHeal(transform.position + Vector3.up * 2f, amount);

                if (healthBar != null)
                {
                    healthBar.UpdateHealth((float)health / maxHealth);
                }

                Debug.Log($"{unitName} healed for {amount}, current HP: {health}/{maxHealth}");
            }
        }

        // Buff methods
        public void UpdateBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].remainingTime -= Time.deltaTime;
                if (activeBuffs[i].remainingTime <= 0)
                {
                    RemoveBuff(activeBuffs[i]);
                    activeBuffs.RemoveAt(i);
                }
            }
        }

        public void AddBuff(BuffType type, float duration, float value = 0f)
        {
            // Remove existing same-type buff first
            activeBuffs.RemoveAll(b => b.type == type);

            Buff buff = new Buff(type, duration, value);
            activeBuffs.Add(buff);
            ApplyBuffImmediate(buff);
        }

        private void ApplyBuffImmediate(Buff buff)
        {
            switch (buff.type)
            {
                case BuffType.Freeze:
                    isFrozen = true;
                    break;
                case BuffType.Stun:
                    isStunned = true;
                    break;
                case BuffType.BoostDamage:
                    damageMultiplier = 1f + buff.value;
                    break;
                case BuffType.BoostSpeed:
                    speedMultiplier = 1f + buff.value;
                    break;
            }
        }

        private void RemoveBuff(Buff buff)
        {
            switch (buff.type)
            {
                case BuffType.Freeze:
                    isFrozen = false;
                    break;
                case BuffType.Stun:
                    isStunned = false;
                    break;
                case BuffType.BoostDamage:
                    damageMultiplier = 1f;
                    break;
                case BuffType.BoostSpeed:
                    speedMultiplier = 1f;
                    break;
            }
        }

        // 死亡
        private void Die()
        {
            if (healthBar != null)
            {
                healthBar.Cleanup();
                healthBar = null;
            }

            // 从BattleManager中移除
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.Units.Remove(this);
            }
            
            // 将单位返回对象池
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.UNIT_POOL, gameObject);
            }
            else
            {
                // 备用方案：销毁单位
                Destroy(gameObject);
            }
        }
        
        // 寻找最近的敌方单位
        private Unit FindClosestEnemyUnit()
        {
            if (BattleManager.Instance == null)
                return null;
            
            Unit closestUnit = null;
            float closestDistance = Mathf.Infinity;
            
            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit.ownerId != ownerId && unit.health > 0)
                {
                    float distance = Vector3.Distance(transform.position, unit.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestUnit = unit;
                    }
                }
            }
            
            return closestUnit;
        }
        
        // 寻找最近的敌方建筑
        private Building FindClosestEnemyBuilding()
        {
            if (BattleManager.Instance == null)
                return null;
            
            Building closestBuilding = null;
            float closestDistance = Mathf.Infinity;
            
            foreach (Building building in BattleManager.Instance.buildings)
            {
                if (building.ownerId != ownerId && building.health > 0)
                {
                    float distance = Vector3.Distance(transform.position, building.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBuilding = building;
                    }
                }
            }
            return closestBuilding;
        }
        
        /// <summary>
        /// 计算到建筑的攻击距离（考虑建筑的碰撞体积）
        /// </summary>
        /// <param name="building">目标建筑</param>
        /// <returns>有效攻击距离</returns>
        private float GetDistanceToBuilding(Building building)
        {
            if (building == null) return Mathf.Infinity;
            
            // 获取建筑的碰撞器
            Collider buildingCollider = building.GetComponent<Collider>();
            if (buildingCollider != null)
            {
                // 使用碰撞器的最近点计算距离
                Vector3 closestPoint = buildingCollider.ClosestPoint(transform.position);
                return Vector3.Distance(transform.position, closestPoint);
            }
            
            // 如果没有碰撞器，使用中心点距离
            return Vector3.Distance(transform.position, building.transform.position);
        }
        
        // 获取敌方基地位置
        private Vector3 GetEnemyBasePosition()
        {
            // 根据所有者ID返回敌方基地位置
            return ownerId == 1 ? new Vector3(0.34f, 0f, 9.1f) : new Vector3(0.34f, 0f, -4.89f);
        }
    }
    
    public enum UnitState
    {
        Idle,       // 空闲
        Moving,     // 移动中
        Attacking,  // 攻击中
        Dead        // 死亡
    }

    public enum BuffType
    {
        Freeze,       // Cannot move or attack
        Stun,         // Cannot act
        BoostDamage,  // Increased damage
        BoostSpeed,   // Increased movement speed
        Poison,       // Damage over time
        Slow          // Reduce movement speed
    }

    [System.Serializable]
    public class Buff
    {
        public BuffType type;
        public float remainingTime;
        public float value;  // e.g. 0.5 = +50% boost

        public Buff(BuffType type, float duration, float value = 0f)
        {
            this.type = type;
            this.remainingTime = duration;
            this.value = value;
        }
    }
}