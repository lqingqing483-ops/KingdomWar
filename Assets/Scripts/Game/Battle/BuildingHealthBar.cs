using UnityEngine;
using UnityEngine.UI;

namespace KingdomWar.Game.Battle
{
    public class BuildingHealthBar : MonoBehaviour
    {
        [Header("血条配置")]
        public Slider healthSlider;       // 血条滑块
        public Text healthText;           // 血条文本
        public float followOffset = 2f;    // 血条跟随偏移量
        public float showDuration = 3f;    // 血条显示持续时间
        public float hideDelay = 2f;       // 血条隐藏延迟
        
        private Building building;         // 关联的建筑
        private Transform targetTransform; // 目标变换
        private Canvas canvas;             // 血条所在的Canvas
        private float showTimer = 0f;      // 显示计时器
        private bool isVisible = true;     // 是否可见
        
        /// <summary>
        /// 初始化血条
        /// </summary>
        /// <param name="building">关联的建筑</param>
        /// <param name="canvas">血条所在的Canvas</param>
        public void Initialize(Building building, Canvas canvas)
        {
            this.building = building;
            this.canvas = canvas;
            this.targetTransform = building.transform;
            
            // 初始化血条值
            UpdateHealthBar();
            
            // 开始显示血条
            ShowHealthBar();
        }
        
        private void Update()
        {
            if (building == null || targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }
            
            // 更新血条位置
            UpdateHealthBarPosition();
            
            // 更新血条值
            UpdateHealthBar();
            
            // 更新显示状态
            UpdateVisibility();
        }
        
        /// <summary>
        /// 更新血条位置
        /// </summary>
        private void UpdateHealthBarPosition()
        {
            if (canvas == null || targetTransform == null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            // 将建筑位置转换为屏幕坐标
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetTransform.position + new Vector3(0, followOffset, 0));

            // 确保血条在屏幕内
            screenPosition.x = Mathf.Clamp(screenPosition.x, 0, Screen.width);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 0, Screen.height);

            // 更新血条位置
            transform.position = screenPosition;
        }
        
        /// <summary>
        /// 更新血条值
        /// </summary>
        private void UpdateHealthBar()
        {
            if (building == null || healthSlider == null)
                return;

            if (building.maxHealth <= 0)
                return;

            // 更新血条滑块值
            float healthPercentage = (float)building.health / building.maxHealth;
            healthSlider.value = healthPercentage;

            // 更新血条文本
            if (healthText != null)
            {
                healthText.text = $"{building.health}";
            }
        }
        
        /// <summary>
        /// 更新血条可见性
        /// </summary>
        private void UpdateVisibility()
        {
            if (building == null)
                return;
            
            // 血条常显
            if (!isVisible)
            {
                gameObject.SetActive(true);
                isVisible = true;
            }
        }
        
        /// <summary>
        /// 显示血条
        /// </summary>
        public void ShowHealthBar()
        {
            gameObject.SetActive(true);
            isVisible = true;
        }
        
        /// <summary>
        /// 强制隐藏血条
        /// </summary>
        public void HideHealthBar()
        {
            gameObject.SetActive(false);
            isVisible = false;
        }
    }
}