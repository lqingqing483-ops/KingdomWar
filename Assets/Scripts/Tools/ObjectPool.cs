using UnityEngine;
using System.Collections.Generic;
namespace KingdomWar.Tools
{
    /// <summary>
    /// 单个对象池类
    /// </summary>
    public class ObjectPool
    {
        private GameObject prefab;             // 预制�?
        private Queue<GameObject> pool;         // 对象队列
        private Transform poolParent;           // 对象池父节点
        private int maxPoolSize;               // 最大池大小
        private int initialPoolSize;           // 初始池大�?
        private string poolName;               // 池名�?        
        /// <summary>
        /// 初始化对象池
        /// </summary>
        /// <param name="prefab">预制�?/param>
        /// <param name="initialSize">初始池大�?/param>
        /// <param name="maxSize">最大池大小</param>
        /// <param name="parent">对象池父节点</param>
        /// <param name="name">池名�?/param>
        public void Initialize(GameObject prefab, int initialSize, int maxSize, Transform parent, string name)
        {
            this.prefab = prefab;
            this.pool = new Queue<GameObject>();
            this.poolParent = parent;
            this.maxPoolSize = maxSize;
            this.initialPoolSize = initialSize;
            this.poolName = name;
            
            // pre-create objects
            for (int i = 0; i < initialSize; i++)
            {
                CreateObject();
            }
        }
        
        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public GameObject GetObject()
        {
            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            else if (pool.Count < maxPoolSize)
            {
                return CreateObject();
            }
            else
            {
                Debug.LogWarning($"Object pool {poolName} is full! Max size: {maxPoolSize}");
                return null;
            }
        }
        
        /// <summary>
        /// 从对象池获取对象并设置位置和旋转
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <returns>对象实例</returns>
        public GameObject GetObject(Vector3 position, Quaternion rotation)
        {
            GameObject obj = GetObject();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }
        
        /// <summary>
        /// 将对象返回对象池
        /// </summary>
        /// <param name="obj">要返回的对象</param>
        public void ReturnObject(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                obj.transform.SetParent(poolParent);
                pool.Enqueue(obj);
            }
        }
        
        /// <summary>
        /// 创建新对�?        /// </summary>
        /// <returns>新创建的对象</returns>
        private GameObject CreateObject()
        {
            GameObject obj = GameObject.Instantiate(prefab, poolParent);
            obj.SetActive(false);
            obj.name = $"{prefab.name}_Pooled_{pool.Count}";
            pool.Enqueue(obj);
            return obj;
        }
        
        /// <summary>
        /// 清空对象�?        /// </summary>
        public void ClearPool()
        {
            foreach (GameObject obj in pool)
            {
                GameObject.Destroy(obj);
            }
            pool.Clear();
        }
        
        /// <summary>
        /// 获取当前池大�?        /// </summary>
        public int CurrentPoolSize { get { return pool.Count; } }
        
        /// <summary>
        /// 获取最大池大小
        /// </summary>
        public int MaxPoolSize { get { return maxPoolSize; } }
        
        /// <summary>
        /// 获取池名�?        /// </summary>
        public string PoolName { get { return poolName; } }
    }

}
