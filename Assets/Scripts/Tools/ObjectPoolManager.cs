using UnityEngine;
using System.Collections.Generic;
namespace KingdomWar.Tools
{
    /// <summary>
    /// 对象池管理器
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        // 单例实例
        public static ObjectPoolManager Instance { get; private set; }
        
        [Header("Object Pool Config")]
        public List<PoolConfig> poolConfigs = new List<PoolConfig>();
        
        // 预定义的池名称常�?
        public const string UNIT_POOL = "UnitPool";
        public const string BUILDING_POOL = "BuildingPool";
        public const string SPELL_POOL = "SpellPool";
        public const string AWAIT_EFFECT_POOL = "AwaitEffectPool";
        public const string UNIT_SPAWN_EFFECT_POOL = "UnitSpawnEffectPool";
        public const string SPELL_EFFECT_POOL = "SpellEffectPool";
        public const string BUILDING_SPAWN_EFFECT_POOL = "BuildingSpawnEffectPool";
        public const string RANGE_VISUALIZER_POOL = "RangeVisualizerPool";
        public const string RANGE_INDICATOR_POOL = "RangeIndicatorPool";
        public const string HEALTH_BAR_POOL = "HealthBarPool";
        
        private Dictionary<string, ObjectPool> pools = new Dictionary<string, ObjectPool>();
        private Transform poolParent;
        
        private void Awake()
        {
            // 单例模式
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // 创建对象池父节点
            poolParent = new GameObject("ObjectPools").transform;
            poolParent.parent = transform;
            
            // 初始化对象池
            InitializePools();
        }
        
        /// <summary>
        /// 初始化对象池
        /// </summary>
        public void InitializePools()
        {
            foreach (PoolConfig config in poolConfigs)
            {
                if (config.prefab != null)
                {
                    // 创建对象池父节点
                    Transform poolTransform = new GameObject(config.poolName).transform;
                    poolTransform.parent = poolParent;
                    
                    // 创建对象�?
                    ObjectPool pool = new ObjectPool();
                    pool.Initialize(config.prefab, config.initialSize, config.maxSize, poolTransform, config.poolName);
                    
                    // 添加到字�?
                    pools[config.poolName] = pool;
                    
                    Debug.Log($"Initialized object pool: {config.poolName} with {config.initialSize} objects");
                }
            }
        }
        
        /// <summary>
        /// 获取对象�?
        /// </summary>
        /// <param name="poolName">池名�?/param>
        /// <returns>对象�?/returns>
        public ObjectPool GetPool(string poolName)
        {
            if (pools.ContainsKey(poolName))
            {
                return pools[poolName];
            }
            else
            {
                Debug.LogWarning($"Object pool {poolName} not found!");
                return null;
            }
        }
        
        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        /// <param name="poolName">池名�?/param>
        /// <returns>对象实例</returns>
        public GameObject GetObject(string poolName)
        {
            ObjectPool pool = GetPool(poolName);
            return pool != null ? pool.GetObject() : null;
        }
        
        /// <summary>
        /// 从对象池获取对象并设置位置和旋转
        /// </summary>
        /// <param name="poolName">池名�?/param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <returns>对象实例</returns>
        public GameObject GetObject(string poolName, Vector3 position, Quaternion rotation)
        {
            ObjectPool pool = GetPool(poolName);
            return pool != null ? pool.GetObject(position, rotation) : null;
        }
        
        /// <summary>
        /// 将对象返回对象池
        /// </summary>
        /// <param name="poolName">池名�?/param>
        /// <param name="obj">要返回的对象</param>
        public void ReturnObject(string poolName, GameObject obj)
        {
            ObjectPool pool = GetPool(poolName);
            if (pool != null)
            {
                pool.ReturnObject(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
        
        /// <summary>
        /// 动态创建对象池
        /// </summary>
        /// <param name="poolName">池名�?/param>
        /// <param name="prefab">预制�?/param>
        /// <param name="initialSize">初始池大�?/param>
        /// <param name="maxSize">最大池大小</param>
        public void CreatePool(string poolName, GameObject prefab, int initialSize, int maxSize)
        {
            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"Object pool {poolName} already exists!");
                return;
            }
            
            if (prefab == null)
            {
                Debug.LogError($"Prefab is null for pool {poolName}!");
                return;
            }
            
            // 创建对象池父节点
            Transform poolTransform = new GameObject(poolName).transform;
            poolTransform.parent = poolParent;
            
            // 创建对象�?
            ObjectPool pool = new ObjectPool();
            pool.Initialize(prefab, initialSize, maxSize, poolTransform, poolName);
            
            // 添加到字�?
            pools[poolName] = pool;
            
            Debug.Log($"Created object pool: {poolName} with {initialSize} objects");
        }
        
        /// <summary>
        /// 初始化预定义的对象池
        /// </summary>
        public void InitializeDefaultPools()
        {
            // 这里可以添加默认的对象池配置
            // 例如�?
            // if (somePrefab != null) {
            //     CreatePool(UNIT_POOL, somePrefab, ObjectPoolSizeRecommendations.UNIT_POOL_INITIAL, ObjectPoolSizeRecommendations.UNIT_POOL_MAX);
            // }
        }
        
        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (ObjectPool pool in pools.Values)
            {
                pool.ClearPool();
            }
            pools.Clear();
        }
        
        /// <summary>
        /// 清空指定对象�?
        /// </summary>
        /// <param name="poolName">池名�?/param>
        public void ClearPool(string poolName)
        {
            ObjectPool pool = GetPool(poolName);
            if (pool != null)
            {
                pool.ClearPool();
                pools.Remove(poolName);
            }
        }
    }
    
    /// <summary>
    /// 对象池配�?
    /// </summary>
    [System.Serializable]
    public class PoolConfig
    {
        public string poolName;         // 池名�?
        public GameObject prefab;        // 预制�?
        public int initialSize = 5;      // 初始池大�?
        public int maxSize = 20;         // 最大池大小
    }
    
    /// <summary>
    /// 对象池大小建议配�?
    /// </summary>
    public static class ObjectPoolSizeRecommendations
    {
        // 单位池配�?
        public const int UNIT_POOL_INITIAL = 10;
        public const int UNIT_POOL_MAX = 50;
        
        // 建筑池配�?
        public const int BUILDING_POOL_INITIAL = 5;
        public const int BUILDING_POOL_MAX = 20;
        
        // 法术池配�?
        public const int SPELL_POOL_INITIAL = 8;
        public const int SPELL_POOL_MAX = 30;
        
        // 特效池配�?
        public const int EFFECT_POOL_INITIAL = 15;
        public const int EFFECT_POOL_MAX = 60;
        
        // 范围指示器池配置
        public const int RANGE_INDICATOR_POOL_INITIAL = 1;
        public const int RANGE_INDICATOR_POOL_MAX = 1;
        
        // 范围可视化池配置
        public const int RANGE_VISUALIZER_POOL_INITIAL = 1;
        public const int RANGE_VISUALIZER_POOL_MAX = 2;
        
        // 血条池配置
        public const int HEALTH_BAR_POOL_INITIAL = 5;
        public const int HEALTH_BAR_POOL_MAX = 15;
    }

}
