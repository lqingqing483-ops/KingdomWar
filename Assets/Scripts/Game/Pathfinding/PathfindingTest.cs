using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using KingdomWar.Game.Pathfinding;
using KingdomWar.Game.Battle;

namespace KingdomWar.Game.Pathfinding
{
    /// <summary>
    /// 寻路测试类，用于测试A*寻路算法
    /// </summary>
    public class PathfindingTest : MonoBehaviour
    {
        [Header("测试配置")]
        public GridManager grid;                  // 寻路网格
        public Unit testUnit;              // 测试单位
        public Transform targetTransform;   // 目标位置
        public bool autoTest = false;       // 是否自动测试
        public float testInterval = 2.0f;   // 测试间隔
        
        private AStar pathfinding;          // A*寻路算法
        private float testTimer = 0f;       // 测试计时器
        
        private void Start()
        {
            if (grid != null)
            {
                // 初始化网格
                grid.InitializeGrid();
                
                // 创建A*实例
                pathfinding = new AStar(grid);
                
                Debug.Log("寻路测试初始化完成");
            }
            else
            {
                Debug.LogError("请指定寻路网格");
            }
        }
        
        private void Update()
        {
            // 自动测试
            if (autoTest && testUnit != null && targetTransform != null)
            {
                testTimer += Time.deltaTime;
                if (testTimer >= testInterval)
                {
                    testTimer = 0f;
                    TestPathfinding();
                }
            }
        }
        
        /// <summary>
        /// 测试寻路功能
        /// </summary>
        public void TestPathfinding()
        {
            if (pathfinding == null || testUnit == null || targetTransform == null)
            {
                Debug.LogError("测试参数不完整");
                return;
            }
            
            // 计算路径
            Debug.Log("开始测试寻路...");
            List<GridNode> path = pathfinding.FindPath(testUnit.transform.position, targetTransform.position);
            
            if (path != null && path.Count > 0)
            {
                // 显示路径
                grid.DrawPath(path);
                
                // 让单位移动到目标
                testUnit.moveTarget = targetTransform.position;
                testUnit.state = UnitState.Moving;
                
                Debug.Log($"寻路测试成功，路径点数量: {path.Count}");
            }
            else
            {
                Debug.LogError("寻路测试失败，无法找到路径");
            }
        }
        
        /// <summary>
        /// 在场景中绘制测试GUI
        /// </summary>
        private void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 200, 150), "寻路测试");
            
            if (GUI.Button(new Rect(20, 40, 180, 30), "测试寻路"))
            {
                TestPathfinding();
            }
            
            autoTest = GUI.Toggle(new Rect(20, 80, 180, 20), autoTest, "自动测试");
            
            if (autoTest)
            {
                testInterval = GUI.HorizontalSlider(new Rect(20, 110, 180, 20), testInterval, 0.5f, 5.0f);
                GUI.Label(new Rect(20, 130, 180, 20), $"测试间隔: {testInterval:F1}秒");
            }
        }
    }
}
