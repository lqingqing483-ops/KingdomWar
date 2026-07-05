using UnityEngine;
using KingdomWar.Game.Battle;

namespace KingdomWar.Game.Battle
{
    public class Projectile : MonoBehaviour
    {
        public int ownerId;      // 所有者ID
        public int damage;       // 伤害值
        
        private GameObject target;   // 目标对象
        private Vector3 direction;   // 飞行方向
        private float speed = 10f;   // 飞行速度
        private float lifetime = 5f; // 生命周期（防止无限飞行）
        private float lifeTimer = 0f;// 生命周期计时器
        private bool hasHit = false; // 是否已经命中目标（防止重复触发）
        
        /// <summary>
        /// 设置目标
        /// </summary>
        /// <param name="targetObj">目标游戏对象</param>
        public void SetTarget(GameObject targetObj)
        {
            target = targetObj;
        }
        
        /// <summary>
        /// 设置飞行方向
        /// </summary>
        /// <param name="dir">方向向量</param>
        public void SetDirection(Vector3 dir)
        {
            direction = dir;
        }
        
        private void Update()
        {
            // 更新生命周期
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            
            // 更新目标方向（如果目标存在）
            if (target != null)
            {
                direction = (target.transform.position - transform.position).normalized;
            }
            
            // 向前飞行
            transform.position += direction * speed * Time.deltaTime;
            
            // 朝向飞行方向
            transform.forward = direction;
            
            // 检测碰撞
            CheckCollision();
        }
        
        /// <summary>
        /// 检测碰撞
        /// </summary>
        private void CheckCollision()
        {
            if (hasHit) return;
            
            // 检测与目标的距离
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= 0.5f)
                {
                    // 命中目标
                    HitTarget();
                }
            }
        }
        
        /// <summary>
        /// 命中目标
        /// </summary>
        private void HitTarget()
        {
            if (hasHit) return;
            hasHit = true;
            
            if (target != null)
            {
                // 尝试获取Unit组件
                Unit unit = target.GetComponent<Unit>();
                if (unit != null && unit.ownerId != ownerId)
                {
                    // 对敌方单位造成伤害（通过网络同步）
                    NetworkUnit networkUnit = unit.GetComponent<NetworkUnit>();
                    if (networkUnit != null)
                    {
                        networkUnit.TakeDamage(damage);
                    }
                    else
                    {
                        unit.TakeDamage(damage);
                    }
                    Debug.Log($"攻击物体命中 {unit.unitName}，造成 {damage} 点伤害");
                }
                // use else if to ensure only one target takes damage
                else if (unit == null)
                {
                    // 尝试获取Building组件
                    Building building = target.GetComponent<Building>();
                    if (building != null && building.ownerId != ownerId)
                    {
                        // 对敌方建筑造成伤害（通过网络同步）
                        NetworkBuilding networkBuilding = building.GetComponent<NetworkBuilding>();
                        if (networkBuilding != null)
                        {
                            networkBuilding.TakeDamage(damage);
                        }
                        else
                        {
                            building.TakeDamage(damage);
                        }
                        Debug.Log($"攻击物体命中 {building.buildingName}，造成 {damage} 点伤害");
                    }
                }
            }
            
            // 销毁攻击物体
            Destroy(gameObject);
        }
        
        /// <summary>
        /// 处理碰撞检测
        /// </summary>
        /// <param name="other">碰撞对象</param>
        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;
            
            // 检测与其他物体的碰撞
            GameObject otherObj = collision.gameObject;
            
            // 检查是否是敌方单位或建筑
            Unit otherUnit = otherObj.GetComponent<Unit>();
            Building otherBuilding = otherObj.GetComponent<Building>();
            
            if ((otherUnit != null && otherUnit.ownerId != ownerId) || 
                (otherBuilding != null && otherBuilding.ownerId != ownerId))
            {
                // 命中敌方单位或建筑
                target = otherObj;
                HitTarget();
            }
            else if (otherObj.layer != LayerMask.NameToLayer("Ground"))
            {
                // 命中其他物体，销毁攻击物体
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}
