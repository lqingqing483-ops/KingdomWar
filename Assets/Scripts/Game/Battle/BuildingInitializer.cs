using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public class BuildingInitializer : MonoBehaviour
    {
        [Header("建筑配置")]
        public GameObject redMainTowerObj;      // 红方主塔对象
        public GameObject redDefenseTower1Obj; // 红方防御塔1对象
        public GameObject redDefenseTower2Obj; // 红方防御塔2对象
        public GameObject blueMainTowerObj;      // 蓝方主塔对象
        public GameObject blueDefenseTower1Obj; // 蓝方防御塔1对象
        public GameObject blueDefenseTower2Obj; // 蓝方防御塔2对象
        
        [Header("主塔属性")]
        public int mainTowerHealth = 5000;      // 主塔生命值
        public int mainTowerDamage = 100;        // 主塔攻击力
        public float mainTowerAttackSpeed = 1.5f; // 主塔攻击速度
        public float mainTowerAttackRange = 7f;   // 主塔攻击范围
        
        [Header("防御塔属性")]
        public int defenseTowerHealth = 3000;     // 防御塔生命值
        public int defenseTowerDamage = 80;       // 防御塔攻击力
        public float defenseTowerAttackSpeed = 1.2f; // 防御塔攻击速度
        public float defenseTowerAttackRange = 6f;   // 防御塔攻击范围
        
        private void Start()
        {
            // 初始化所有建筑
            InitializeBuildings();
        }
        
        /// <summary>
        /// 初始化场景中的所有建筑
        /// </summary>
        private void InitializeBuildings()
        {
            Debug.Log("Initializing buildings...");
            
            // 初始化红方建筑
            InitializeBuilding(redMainTowerObj, 2, "红方主塔", mainTowerHealth, mainTowerDamage, 
                mainTowerAttackSpeed, mainTowerAttackRange, 0, BuildingType.MainTower);
            
            InitializeBuilding(redDefenseTower1Obj, 2, "红方防御塔1", defenseTowerHealth, defenseTowerDamage, 
                defenseTowerAttackSpeed, defenseTowerAttackRange, 0, BuildingType.DefenseTower);
            
            InitializeBuilding(redDefenseTower2Obj, 2, "红方防御塔2", defenseTowerHealth, defenseTowerDamage, 
                defenseTowerAttackSpeed, defenseTowerAttackRange, 0, BuildingType.DefenseTower);
            
            // 初始化蓝方建筑
            InitializeBuilding(blueMainTowerObj, 1, "蓝方主塔", mainTowerHealth, mainTowerDamage, 
                mainTowerAttackSpeed, mainTowerAttackRange, 0, BuildingType.MainTower);
            
            InitializeBuilding(blueDefenseTower1Obj, 1, "蓝方防御塔1", defenseTowerHealth, defenseTowerDamage,  
                defenseTowerAttackSpeed, defenseTowerAttackRange, 0, BuildingType.DefenseTower);
            
            InitializeBuilding(blueDefenseTower2Obj, 1, "蓝方防御塔2", defenseTowerHealth, defenseTowerDamage,  
                defenseTowerAttackSpeed, defenseTowerAttackRange, 0, BuildingType.DefenseTower);
            
            Debug.Log("Building initialization completed!");
        }
        
        /// <summary>
        /// 初始化单个建筑
        /// </summary>
        /// <param name="buildingObj">建筑对象</param>gName">建筑名称</param>
        /// <param name="ownerId">所有者ID</param>
        /// <param name="displayName">显示名称</param>
        /// <param name="health">生命值</param>
        /// <param name="damage">攻击力</param>
        /// <param name="attackSpeed">攻击速度</param>
        /// <param name="attackRange">攻击范围</param>
        /// <param name="duration">持续时间（0表示永久）</param>
        /// <param name="buildingType">建筑类型</param>
        private void InitializeBuilding(GameObject buildingObj, int ownerId, string displayName, int health, int damage, 
            float attackSpeed, float attackRange, float duration, BuildingType buildingType)
        {
            if (buildingObj == null)
            {
                Debug.LogWarning($"Building not found: {buildingObj?.name ?? "null"}");
                return;
            }
            
            Building buildingComponent = buildingObj.GetComponent<Building>();
            if (buildingComponent == null)
            {
                buildingComponent = buildingObj.AddComponent<Building>();
                Debug.Log($"Added Building component to: {buildingObj.name}");
            }

            NetworkBuilding networkBuilding = buildingObj.GetComponent<NetworkBuilding>();
            if (networkBuilding == null)
            {
                networkBuilding = buildingObj.AddComponent<NetworkBuilding>();
                Debug.Log($"Added NetworkBuilding component to: {buildingObj.name}");
            }
            
            buildingComponent.ownerId = ownerId;
            buildingComponent.buildingName = displayName;
            buildingComponent.health = health;
            buildingComponent.maxHealth = health;
            buildingComponent.damage = damage;
            buildingComponent.attackSpeed = attackSpeed;
            buildingComponent.attackRange = attackRange;
            buildingComponent.duration = duration;
            buildingComponent.buildingType = buildingType;
            buildingComponent.state = BuildingState.Idle;

            networkBuilding.ownerId = ownerId;
            networkBuilding.buildingId = buildingObj.GetInstanceID();
            
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.AddBuilding(buildingComponent);
                Debug.Log($"Added {displayName} to BattleManager");
            }
            else
            {
                Debug.LogWarning("BattleManager instance not found!");
            }
            
            AddHealthBar(buildingComponent);
        }
        
        /// <summary>
        /// 为建筑添加血条
        /// </summary>
        /// <param name="building">建筑</param>
        private void AddHealthBar(Building building)
        {
            // 查找BuildingHealthBarManager
            BuildingHealthBarManager healthBarManager = FindObjectOfType<BuildingHealthBarManager>();
            if (healthBarManager != null)
            {
                healthBarManager.AddHealthBar(building);
                Debug.Log($"Added health bar to {building.buildingName}");
            }
            else
            {
                Debug.LogWarning("BuildingHealthBarManager instance not found!");
            }
        }
    }
    
}