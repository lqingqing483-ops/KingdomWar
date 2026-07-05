using UnityEngine;
using System.Collections;
using KingdomWar.Game.Cards;
using KingdomWar.Tools;

namespace KingdomWar.Game.Battle
{
    public class Building : MonoBehaviour
    {
        [Header("建筑属性")]
        public int ownerId;              // 所有者ID
        public string buildingName;      // 建筑名称
        public int health;               // 生命值
        public int maxHealth;            // 最大生命值
        public int damage;               // 伤害值
        public float attackSpeed;        // 攻击速度
        public float attackRange;        // 攻击范围
        public float duration;           // 持续时间
        public float deployTime;         // 部署时间
        
        [Header("建筑类型")]
        public BuildingType buildingType = BuildingType.Normal; // 建筑类型
        
        [Header("建筑状态")]
        public BuildingState state = BuildingState.Idle; // 建筑状态
        
        private float attackTimer = 0f;  // 攻击计时器
        private float durationTimer = 0f; // 持续时间计时器
        private Unit targetUnit;         // 目标单位
        private Building targetBuilding; // 目标建筑
        
        // 初始化建筑
        public void Initialize(int ownerId, string buildingName, int health, int damage, float attackSpeed, float attackRange, float duration)
        {
            this.ownerId = ownerId;
            this.buildingName = buildingName;
            this.health = health;
            this.maxHealth = health;
            this.damage = damage;
            this.attackSpeed = attackSpeed;
            this.attackRange = attackRange;
            this.duration = duration;
            this.state = BuildingState.Idle;
            
            // 添加血条
            AddHealthBar();
            Debug.Log("初始化完成");
        }
        
        // 初始化建筑
        public void Initialize(int ownerId, CardData cardData)
        {
            Initialize(ownerId, cardData.cardName, cardData.buildingData.health, cardData.buildingData.damage, cardData.buildingData.attackSpeed, cardData.buildingData.attackRange, cardData.buildingData.duration);
        }
        
        /// <summary>
        /// 为建筑添加血条
        /// </summary>
        private void AddHealthBar()
        {
            // 查找BuildingHealthBarManager
            BuildingHealthBarManager healthBarManager = FindObjectOfType<BuildingHealthBarManager>();
            Debug.Log("查找血条管理器");
            if (healthBarManager != null)
            {
                healthBarManager.AddHealthBar(this);
                Debug.Log("添加血条");
            }
        }
        
        /// <summary>
        /// 显示建筑血条
        /// </summary>
        private void ShowHealthBar()
        {
            // 查找BuildingHealthBarManager
            BuildingHealthBarManager healthBarManager = FindObjectOfType<BuildingHealthBarManager>();
            if (healthBarManager != null)
            {
                healthBarManager.ShowHealthBar(this);
            }
        }
        
        // 更新建筑
        public void UpdateBuilding()
        {
            switch (state)
            {
                case BuildingState.Idle:
                    FindTarget();
                    break;
                case BuildingState.Attacking:
                    AttackTarget();
                    break;
                case BuildingState.Dead:
                    Die();
                    break;
            }
            
            // 更新持续时间
            if (duration > 0)
            {
                durationTimer += Time.deltaTime;
                if (durationTimer >= duration)
                {
                    Die();
                }
            }
        }
        
        // 寻找目标
        private void FindTarget()
        {
            // 优先攻击敌方单位
            targetUnit = FindClosestEnemyUnit();
            if (targetUnit != null && Vector3.Distance(transform.position, targetUnit.transform.position) <= attackRange)
            {
                state = BuildingState.Attacking;
                return;
            }
            
            // 其次攻击敌方建筑
            targetBuilding = FindClosestEnemyBuilding();
            if (targetBuilding != null && Vector3.Distance(transform.position, targetBuilding.transform.position) <= attackRange)
            {
                state = BuildingState.Attacking;
                return;
            }
        }
        
        // 攻击目标
        private void AttackTarget()
        {
            // 检查目标是否存在
            if (targetUnit != null && targetUnit.health > 0)
            {
                // 检查是否在攻击范围内
                if (Vector3.Distance(transform.position, targetUnit.transform.position) > attackRange)
                {
                    state = BuildingState.Idle;
                    return;
                }
                
                // 攻击
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackSpeed)
                {
                    attackTimer = 0f;
                    // 通过网络同步伤害
                    NetworkUnit targetNetworkUnit = targetUnit.GetComponent<NetworkUnit>();
                    if (targetNetworkUnit != null)
                    {
                        targetNetworkUnit.TakeDamage(damage);
                    }
                    else
                    {
                        targetUnit.TakeDamage(damage);
                    }
                    Debug.Log($"{buildingName} attacks {targetUnit.unitName} for {damage} damage!");
                }
            }
            else if (targetBuilding != null && targetBuilding.health > 0)
            {
                // 检查是否在攻击范围内
                if (Vector3.Distance(transform.position, targetBuilding.transform.position) > attackRange)
                {
                    state = BuildingState.Idle;
                    return;
                }
                
                // 攻击
                attackTimer += Time.deltaTime;
                if (attackTimer >= attackSpeed)
                {
                    attackTimer = 0f;
                    // 通过网络同步伤害
                    NetworkBuilding targetNetworkBuilding = targetBuilding.GetComponent<NetworkBuilding>();
                    if (targetNetworkBuilding != null)
                    {
                        targetNetworkBuilding.TakeDamage(damage);
                    }
                    else
                    {
                        targetBuilding.TakeDamage(damage);
                    }
                    Debug.Log($"{buildingName} attacks {targetBuilding.buildingName} for {damage} damage!");
                }
            }
            else
            {
                // 目标不存在，重新寻找
                state = BuildingState.Idle;
            }
        }
        
        public void TakeDamage(int damage)
        {
            TakeDamage(damage, -1);
        }

        public void TakeDamage(int damage, int sourceId)
        {
            health -= damage;
            
            if (BattleEventSystem.Instance != null)
            {
                BattleEventSystem.Instance.EmitBuildingDamaged(GetInstanceID(), damage, sourceId);
            }
            
            if (health <= 0)
            {
                health = 0;
                state = BuildingState.Dead;
                
                if (BattleEventSystem.Instance != null)
                {
                    BattleEventSystem.Instance.EmitBuildingDestroyed(GetInstanceID(), ownerId);
                }
            }
            
            ShowHealthBar();
        }
        
        // 死亡
        private void Die()
        {
            // 从BattleManager中移除
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.buildings.Remove(this);
                
                // 如果是主塔被摧毁，检查游戏是否结束
                if (buildingType == BuildingType.MainTower)
                {
                    BattleManager.Instance.EndBattle();
                }
                // 如果是防御塔被摧毁，扩展使用范围
                else if (buildingType == BuildingType.DefenseTower)
                {
                    if (UseRangeManager.Instance != null)
                    {
                        UseRangeManager.Instance.ExtendRange();
                    }
                }
            }
            
            // 将建筑返回对象池
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.BUILDING_POOL, gameObject);
            }
            else
            {
                // 备用方案：销毁建筑
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
    }
    
    public enum BuildingState
    {
        Idle,       // 空闲
        Attacking,  // 攻击中
        Dead        // 死亡
    }
    
    public enum BuildingType
    {
        Normal,         // 普通建筑
        MainTower,      // 主塔
        DefenseTower,   // 防御塔
        ElixirCollector // 圣水收集器
    }
}