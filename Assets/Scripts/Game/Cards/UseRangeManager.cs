using UnityEngine;
using KingdomWar.Tools;
using KingdomWar.Game.Battle;

namespace KingdomWar.Game.Cards
{
    public class UseRangeManager : MonoBehaviour
{
    public static UseRangeManager Instance { get; private set; }
    
    [Header("使用范围设置")]
    public float initialHalfWidth = 8f;  // 初始半场宽度
    public float initialHalfLength = 6.5f; // 初始半场长度
    public Vector3 ownSideCenter = new Vector3(0.4f, 0f, 10f); // 己方半场中心-4.2
    
    [Header("范围扩展设置")]
    public float towerRangeExtension = 3f; // 摧毁一个防御塔后扩展的范围
    
    [Header("范围指示器设置")]
    public GameObject rangeIndicatorPrefab; // 范围指示器预制体
    
    private float currentHalfWidth;  // 当前半场宽度
    private float currentHalfLength; // 当前半场长度
    private int destroyedTowers = 0;  // 已摧毁的防御塔数量
    private GameObject rangeIndicator; // 范围指示器实例
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeRange();
            Debug.LogFormat("UseRangeManager初始化完成，己方半场中心位置: {0}", ownSideCenter);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 初始化使用范围
    /// </summary>
    private void InitializeRange()
    {
        currentHalfWidth = initialHalfWidth;
        currentHalfLength = initialHalfLength;
        destroyedTowers = 0;
        
        // 根据玩家阵营设置己方半场中心位置
        if (BattleManager.Instance != null)
        {
            byte team = BattleManager.Instance.GetLocalPlayerTeam();
            if (team == 1) // 蓝方
            {
                ownSideCenter = new Vector3(0.4f, 0f, 10f);
                Debug.Log("设置为蓝方半场中心位置");
            }
            else if (team == 2) // 红方
            {
                ownSideCenter = new Vector3(0.4f, 0f, -4.2f);
                Debug.Log("设置为红方半场中心位置");
            }
        }
        else
        {
            Debug.Log("BattleManager实例不存在，使用默认半场中心位置");
        }
    }
    
    /// <summary>
    /// 确保范围参数在编辑器中也能正确初始化
    /// </summary>
    private void OnEnable()
    {
        // 确保范围参数已初始化
        if (currentHalfWidth <= 0)
        {
            currentHalfWidth = initialHalfWidth;
        }
        if (currentHalfLength <= 0)
        {
            currentHalfLength = initialHalfLength;
        }
    }
    
    /// <summary>
    /// 检查指定位置是否在可使用范围内
    /// </summary>
    /// <param name="position">要检查的位置</param>
    /// <param name="cardType">卡片类型</param>
    /// <returns>是否在可使用范围内</returns>
    public bool IsInUseRange(Vector3 position, CardType cardType)
    {
        // 法术卡片可以在任意位置使用
        if (cardType == CardType.Spell)
        {
            return true;
        }
        
        // 人物和建筑卡片只能在己方范围内使用
        float minX = ownSideCenter.x - currentHalfWidth;
        float maxX = ownSideCenter.x + currentHalfWidth;
        float minZ = ownSideCenter.z - currentHalfLength;
        float maxZ = ownSideCenter.z + currentHalfLength;
        
        return position.x >= minX && position.x <= maxX &&
               position.z >= minZ && position.z <= maxZ;
    }
    
    /// <summary>
    /// 扩展使用范围（摧毁防御塔后调用）
    /// </summary>
    public void ExtendRange()
    {
        destroyedTowers++;
        currentHalfWidth += towerRangeExtension;
        
        // 更新己方半场中心位置
        ownSideCenter.x += towerRangeExtension * 0.5f;
        
        Debug.Log($"使用范围已扩展！当前半场宽度: {currentHalfWidth}, 已摧毁防御塔: {destroyedTowers}");
    }
    
    /// <summary>
    /// 获取当前使用范围的边界
    /// </summary>
    /// <returns>使用范围的边界</returns>
    public Bounds GetCurrentUseBounds()
    {
        float minX = ownSideCenter.x - currentHalfWidth;
        float maxX = ownSideCenter.x + currentHalfWidth;
        float minZ = ownSideCenter.z - currentHalfLength;
        float maxZ = ownSideCenter.z + currentHalfLength;
        
        Vector3 center = new Vector3((minX + maxX) * 0.5f, ownSideCenter.y, (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(maxX - minX, 1f, maxZ - minZ);
        
        return new Bounds(center, size);
    }
    
    /// <summary>
    /// 重置使用范围
    /// </summary>
    public void ResetRange()
    {
        InitializeRange();
        Debug.Log("使用范围已重置");
    }
    
    /// <summary>
    /// 获取已摧毁的防御塔数量
    /// </summary>
    /// <returns>已摧毁的防御塔数量</returns>
    public int GetDestroyedTowersCount()
    {
        return destroyedTowers;
    }
    
    /// <summary>
    /// 在编辑器中可视化使用范围
    /// </summary>
    private void OnDrawGizmos()
    {
        // 绘制己方使用范围
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
        
        float minX = ownSideCenter.x - currentHalfWidth;
        float maxX = ownSideCenter.x + currentHalfWidth;
        float minZ = ownSideCenter.z - currentHalfLength;
        float maxZ = ownSideCenter.z + currentHalfLength;
        
        // 绘制范围的四个角
        Vector3[] corners = new Vector3[]
        {
            new Vector3(minX, ownSideCenter.y, minZ),
            new Vector3(maxX, ownSideCenter.y, minZ),
            new Vector3(maxX, ownSideCenter.y, maxZ),
            new Vector3(minX, ownSideCenter.y, maxZ)
        };
        
        // 绘制边框
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }
        
        // 绘制对角线，增强可视化效果
        Gizmos.DrawLine(corners[0], corners[2]);
        Gizmos.DrawLine(corners[1], corners[3]);
        
        // 绘制范围中心
        Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawSphere(ownSideCenter, 0.2f);
    }
    
    /// <summary>
    /// 在运行时可视化使用范围
    /// </summary>
    public void VisualizeRange()
    {
        // 从对象池获取可视化对象
        GameObject visualizer = null;
        if (ObjectPoolManager.Instance != null)
        {
            visualizer = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.RANGE_VISUALIZER_POOL, ownSideCenter, Quaternion.identity);
        }
        
        if (visualizer == null)
        {
            // 备用方案：创建一个新的可视化对象
            visualizer = new GameObject("RangeVisualizer");
            visualizer.transform.position = ownSideCenter;
            
            // 添加四个立方体作为范围的角
            for (int i = 0; i < 4; i++)
            {
                GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
                corner.transform.SetParent(visualizer.transform);
                corner.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                
                // 设置颜色
                Renderer renderer = corner.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
                }
            }
            
            // 添加线段作为边框
            GameObject lines = new GameObject("RangeLines");
            lines.transform.SetParent(visualizer.transform);
            
            // 使用LineRenderer绘制边框
            LineRenderer lineRenderer = lines.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 5;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        }
        else
        {
            // 重置可视化对象的位置
            visualizer.transform.position = ownSideCenter;
        }
        
        // 更新立方体位置
        Transform[] corners = visualizer.GetComponentsInChildren<Transform>();
        int cornerIndex = 0;
        foreach (Transform child in corners)
        {
            if (child.name == "Cube" && cornerIndex < 4)
            {
                // 根据索引设置位置
                float x = (cornerIndex % 2 == 0) ? -currentHalfWidth : currentHalfWidth;
                float z = (cornerIndex < 2) ? -currentHalfLength : currentHalfLength;
                child.localPosition = new Vector3(x, 0f, z);
                cornerIndex++;
            }
        }
        
        // 更新线段位置
        LineRenderer lineRendererComponent = visualizer.GetComponentInChildren<LineRenderer>();
        if (lineRendererComponent != null)
        {
            // 设置线段位置
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-currentHalfWidth, 0f, -currentHalfLength),
                new Vector3(currentHalfWidth, 0f, -currentHalfLength),
                new Vector3(currentHalfWidth, 0f, currentHalfLength),
                new Vector3(-currentHalfWidth, 0f, currentHalfLength),
                new Vector3(-currentHalfWidth, 0f, -currentHalfLength)
            };
            lineRendererComponent.SetPositions(positions);
        }
        
        // 激活可视化对象
        visualizer.SetActive(true);
        
        // 3秒后将可视化对象返回对象池
        StartCoroutine(ReturnRangeVisualizerToPool(visualizer, 3f));
        
        Debug.Log("使用范围已可视化");
    }
    
    /// <summary>
    /// 延迟后将范围可视化对象返回对象池
    /// </summary>
    /// <param name="visualizer">可视化对象</param>
    /// <param name="delay">延迟时间</param>
    private System.Collections.IEnumerator ReturnRangeVisualizerToPool(GameObject visualizer, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (visualizer != null && ObjectPoolManager.Instance != null)
        {
            ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.RANGE_VISUALIZER_POOL, visualizer);
        }
        else if (visualizer != null)
        {
            Destroy(visualizer);
        }
    }
    
    /// <summary>
    /// 创建范围指示器
    /// </summary>
    public void CreateRangeIndicator()
    {
        // 先销毁现有的范围指示器
        DestroyRangeIndicator();
        
        if (rangeIndicatorPrefab != null && ObjectPoolManager.Instance != null)
        {
            // 从对象池获取范围指示器
            rangeIndicator = ObjectPoolManager.Instance.GetObject(ObjectPoolManager.RANGE_INDICATOR_POOL);
            if (rangeIndicator != null)
            {
                rangeIndicator.name = "RangeIndicator";
                
                // 设置范围指示器的初始状态
                UpdateRangeIndicator(Vector3.zero, CardType.Unit);
            }
            else
            {
                // 备用方案：创建范围指示器
                rangeIndicator = Instantiate(rangeIndicatorPrefab);
                rangeIndicator.name = "RangeIndicator";
                
                // 设置范围指示器的初始状态
                UpdateRangeIndicator(Vector3.zero, CardType.Unit);
            }
        }
        else if (rangeIndicatorPrefab != null)
        {
            // 备用方案：创建范围指示器
            rangeIndicator = Instantiate(rangeIndicatorPrefab);
            rangeIndicator.name = "RangeIndicator";
            
            // 设置范围指示器的初始状态
            UpdateRangeIndicator(Vector3.zero, CardType.Unit);
        }
        else
        {
            // 如果没有提供范围指示器预制体，创建一个默认的3D范围指示器
            CreateDefault3DRangeIndicator();
        }
    }
    
    /// <summary>
    /// 更新范围指示器
    /// </summary>
    /// <param name="position">指示器位置</param>
    public void UpdateRangeIndicator(Vector3 position)
    {
        UpdateRangeIndicator(position, CardType.Unit);
    }
    
    /// <summary>
    /// 创建默认的3D范围指示器
    /// </summary>
    private void CreateDefault3DRangeIndicator()
    {
        // 创建一个新的游戏对象作为范围指示器
        rangeIndicator = new GameObject("RangeIndicator");
        
        // 添加一个平面作为指示器的视觉效果
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.SetParent(rangeIndicator.transform);
        plane.transform.localScale = new Vector3(currentHalfWidth * 2f / 10f, 0.1f, currentHalfLength * 2f / 10f);
        //plane.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        
        // 添加一个MeshRenderer组件以设置颜色
        MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            // 创建一个半透明的材质
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            material.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            meshRenderer.material = material;
        }
        
        // 禁用碰撞器，避免影响游戏
        Collider collider = plane.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }
    
    /// <summary>
    /// 更新范围指示器
    /// </summary>
    /// <param name="position">指示器位置</param>
    /// <param name="cardType">卡片类型</param>
    public void UpdateRangeIndicator(Vector3 position, CardType cardType)
    {
        if (rangeIndicator != null)
        {
            // 设置范围指示器的位置
            rangeIndicator.transform.position = ownSideCenter + new Vector3(0,0.1f,0);
            
            // 检查位置是否在可使用范围内
            bool isInRange = IsInUseRange(position, cardType);
            
            // 根据是否在范围内设置指示器的颜色
            MeshRenderer meshRenderer = rangeIndicator.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                if (isInRange)
                {
                    // 在范围内显示绿色
                    meshRenderer.material.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
                }
                else
                {
                    // 不在范围内显示红色
                    meshRenderer.material.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
                }
            }
            
            // 根据卡片类型调整范围指示器的大小
            if (cardType == CardType.Spell)
            {
                // 法术卡片使用范围为整个战场
                rangeIndicator.transform.localScale = new Vector3(1.75f, 1f, 4.5f);
            }
            else
            {
                // 人物和建筑卡片使用范围为己方区域
                rangeIndicator.transform.localScale = new Vector3(currentHalfWidth * 2f / 10f, 1f, currentHalfLength * 2f / 10f);
                rangeIndicator.transform.position = ownSideCenter + new Vector3(0,0.1f,0);
            }
        }
    }
    
    /// <summary>
    /// 销毁范围指示器
    /// </summary>
    public void DestroyRangeIndicator()
    {
        if (rangeIndicator != null)
        {
            // 将范围指示器返回对象池
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.RANGE_INDICATOR_POOL, rangeIndicator);
            }
            else
            {
                // 备用方案：销毁范围指示器
                Destroy(rangeIndicator);
            }
            rangeIndicator = null;
        }
    }
}
}