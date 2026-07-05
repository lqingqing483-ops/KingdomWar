using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using KingdomWar.Tools;

namespace KingdomWar.Game.Battle
{
    public class BuildingHealthBarManager : MonoBehaviour
    {
        [Header("血条配置")]
        public GameObject healthBarPrefab; // 血条预制体
        public Canvas healthBarCanvas;      // 血条所在的Canvas
        
        private Dictionary<Building, BuildingHealthBar> healthBars = new Dictionary<Building, BuildingHealthBar>(); // 建筑血条映射
        
        private void Start()
        {
            // 确保血条Canvas存在
            if (healthBarCanvas == null)
            {
                CreateHealthBarCanvas();
            }
        }
        
        private void Update()
        {
            // 检查并移除已销毁的建筑血条
            List<Building> buildingsToRemove = new List<Building>();
            foreach (KeyValuePair<Building, BuildingHealthBar> pair in healthBars)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    buildingsToRemove.Add(pair.Key);
                }
            }
            
            foreach (Building building in buildingsToRemove)
            {
                healthBars.Remove(building);
            }
        }
        
        /// <summary>
        /// 为建筑添加血条
        /// </summary>
        /// <param name="building">建筑</param>
        public void AddHealthBar(Building building)
        {
            if (building == null || healthBars.ContainsKey(building))
                return;
            
            // 确保血条Canvas存在
            if (healthBarCanvas == null)
            {
                CreateHealthBarCanvas();
            }
            
            // 从对象池获取血条实例
            GameObject healthBarObj = null;
            if (ObjectPoolManager.Instance != null && healthBarPrefab != null)
            {
                healthBarObj = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.HEALTH_BAR_POOL);
                if (healthBarObj != null)
                {
                    healthBarObj.transform.SetParent(healthBarCanvas.transform);
                }
            }
            
            if (healthBarObj == null)
            {
                // 备用方案：直接实例化
                healthBarObj = Instantiate(healthBarPrefab, healthBarCanvas.transform);
            }
            
            BuildingHealthBar healthBar = healthBarObj.GetComponent<BuildingHealthBar>();
            //Debug.Log("创建血条");
            if (healthBar != null)
            {
                // 初始化血条
                healthBar.Initialize(building, healthBarCanvas);
                
                // 添加到映射中
                healthBars[building] = healthBar;
            }
            else
            {
                Debug.LogError("HealthBar component not found on prefab!");
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.HEALTH_BAR_POOL, healthBarObj);
                }
                else
                {
                    Destroy(healthBarObj);
                }
            }
        }
        
        /// <summary>
        /// 移除建筑的血条
        /// </summary>
        /// <param name="building">建筑</param>
        public void RemoveHealthBar(Building building)
        {
            if (building == null || !healthBars.ContainsKey(building))
                return;
            
            BuildingHealthBar healthBar = healthBars[building];
            if (healthBar != null)
            {
                // 将血条返回对象池
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.HEALTH_BAR_POOL, healthBar.gameObject);
                }
                else
                {
                    // 备用方案：销毁血条
                    Destroy(healthBar.gameObject);
                }
            }
            
            healthBars.Remove(building);
        }
        
        /// <summary>
        /// 显示建筑的血条
        /// </summary>
        /// <param name="building">建筑</param>
        public void ShowHealthBar(Building building)
        {
            if (building == null || !healthBars.ContainsKey(building))
                return;
            
            BuildingHealthBar healthBar = healthBars[building];
            if (healthBar != null)
            {
                healthBar.ShowHealthBar();
            }
        }
        
        /// <summary>
        /// 隐藏建筑的血条
        /// </summary>
        /// <param name="building">建筑</param>
        public void HideHealthBar(Building building)
        {
            if (building == null || !healthBars.ContainsKey(building))
                return;
            
            BuildingHealthBar healthBar = healthBars[building];
            if (healthBar != null)
            {
                healthBar.HideHealthBar();
            }
        }
        
        /// <summary>
        /// 创建血条Canvas
        /// </summary>
        private void CreateHealthBarCanvas()
        {
            // 创建Canvas
            GameObject canvasObj = new GameObject("HealthBarCanvas");
            healthBarCanvas = canvasObj.AddComponent<Canvas>();
            healthBarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // 添加CanvasScaler
            CanvasScaler canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            // 添加GraphicRaycaster
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 设置层级
            canvasObj.transform.SetAsLastSibling();
            
            Debug.Log("Created HealthBarCanvas");
        }
    }
}