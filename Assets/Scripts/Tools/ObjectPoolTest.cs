using UnityEngine;
using System.Collections;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
namespace KingdomWar.Tools
{
    public class ObjectPoolTest : MonoBehaviour
    {
        [Header("测试配置")]
        public GameObject testPrefab;         // 测试用的预制�?
        public int testCount = 1000;          // 测试次数
        public int poolInitialSize = 50;      // 对象池初始大�?
        public int poolMaxSize = 200;         // 对象池最大大�?        
        private Stopwatch stopwatch = new Stopwatch();
        private float poolTime;
        private float directTime;
        
        private void Start()
        {
            Debug.Log("开始测试对象池性能...");
            StartCoroutine(TestObjectPoolPerformance());
        }
        
        private IEnumerator TestObjectPoolPerformance()
        {
            // 测试1: 直接Instantiate和Destroy
            Debug.Log("测试1: 直接Instantiate和Destroy");
            stopwatch.Reset();
            stopwatch.Start();
            
            for (int i = 0; i < testCount; i++)
            {
                GameObject obj = Instantiate(testPrefab, Vector3.zero, Quaternion.identity);
                Destroy(obj);
                yield return null;
            }
            
            stopwatch.Stop();
            directTime = stopwatch.ElapsedMilliseconds / 1000f;
            Debug.Log($"Direct Instantiate/Destroy: {directTime:F4} sec");
            
            yield return new WaitForSeconds(1f);
            
            // Test 2: Using Object Pool
            Debug.Log("Test 2: Using Object Pool");
            
            // create temporary object
            GameObject poolManagerObj = new GameObject("ObjectPoolManager_Test");
            ObjectPoolManager poolManager = poolManagerObj.AddComponent<ObjectPoolManager>();
            
            // add test pool config
            PoolConfig testConfig = new PoolConfig
            {
                poolName = "TestPool",
                prefab = testPrefab,
                initialSize = poolInitialSize,
                maxSize = poolMaxSize
            };
            poolManager.poolConfigs.Add(testConfig);
            
            // 重新初始化对象池
            poolManager.InitializePools();
            
            // 等待一帧确保初始化完成
            yield return null;
            
            stopwatch.Reset();
            stopwatch.Start();
            
            for (int i = 0; i < testCount; i++)
            {
                GameObject obj = poolManager.GetObject("TestPool");
                if (obj != null)
                {
                    poolManager.ReturnObject("TestPool", obj);
                }
                yield return null;
            }
            
            stopwatch.Stop();
            poolTime = stopwatch.ElapsedMilliseconds / 1000f;
            Debug.Log($"Object Pool: {poolTime:F4} sec");
            
            // 计算性能提升
            float performanceImprovement = ((directTime - poolTime) / directTime) * 100f;
            Debug.Log($"性能提升: {performanceImprovement:F2}%");
            
            // Test 3: Test object pool edge cases
            Debug.Log("Test 3: Test object pool edge cases");
            
            // clear all objects
            for (int i = 0; i < poolMaxSize; i++)
            {
                GameObject obj = poolManager.GetObject("TestPool");
                yield return null;
            }
            
            // 尝试获取超出最大容量的对象
            GameObject overflowObj = poolManager.GetObject("TestPool");
            if (overflowObj == null)
            {
                Debug.Log("测试通过: 超出最大容量时返回null");
            }
            else
            {
                Debug.Log("测试失败: 超出最大容量时应该返回null");
                poolManager.ReturnObject("TestPool", overflowObj);
            }
            
            // return all objects
            for (int i = 0; i < poolMaxSize; i++)
            {
                // 这里简化处理，实际应该跟踪所有获取的对象
                yield return null;
            }
            
            // 清理测试对象
            Destroy(poolManagerObj);
            
            Debug.Log("对象池性能测试完成!");
            Debug.Log($"Summary: ");
            Debug.Log($"- Test count: {testCount}");
            Debug.Log($"- Direct Instantiate/Destroy: {directTime:F4} sec");
            Debug.Log($"- Object Pool: {poolTime:F4} sec");
            Debug.Log($"- Performance improvement: {performanceImprovement:F2}%");
        }
        
        // manually initialize object pool using reflection
        private void InitializePools(ObjectPoolManager manager)
        {
            // 使用反射调用私有方法
            System.Reflection.MethodInfo methodInfo = typeof(ObjectPoolManager).GetMethod("InitializePools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (methodInfo != null)
            {
                methodInfo.Invoke(manager, null);
            }
        }
    }

}
