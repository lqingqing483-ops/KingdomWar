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
        rangeIndicator = new GameObject("RangeIndicator");
        rangeIndicator.transform.position = ownSideCenter;
        
        // 创建圆形网格（程序化生成的圆盘，不是 Quad）
        GameObject indicatorDisk = new GameObject("IndicatorDisk");
        indicatorDisk.transform.SetParent(rangeIndicator.transform);
        indicatorDisk.transform.localPosition = new Vector3(0, 0.05f, 0);
        indicatorDisk.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        indicatorDisk.transform.localScale = new Vector3(currentHalfWidth * 2f, currentHalfLength * 2f, 1f);
        
        // 生成圆形网格（替代默认的 Quad）
        Mesh diskMesh = CreateCircleMesh(32, 1.0f);
        indicatorDisk.AddComponent<MeshFilter>().mesh = diskMesh;
        
        MeshRenderer renderer = indicatorDisk.AddComponent<MeshRenderer>();
        ApplyRangeMaterial(renderer, true);
        
        // 添加外圈环（稍大一圈的环形线）
        GameObject borderRing = new GameObject("BorderRing");
        borderRing.transform.SetParent(rangeIndicator.transform);
        borderRing.transform.localPosition = new Vector3(0, 0.06f, 0);
        borderRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        borderRing.transform.localScale = new Vector3(currentHalfWidth * 2f * 1.02f, currentHalfLength * 2f * 1.02f, 1f);
        
        Mesh ringMesh = CreateRingMesh(48, 0.85f, 1.0f);
        borderRing.AddComponent<MeshFilter>().mesh = ringMesh;
        MeshRenderer ringRenderer = borderRing.AddComponent<MeshRenderer>();
        ApplyRangeMaterial(ringRenderer, false);
        
        // 创建方向指示器（四个小箭头标记）
        for (int i = 0; i < 4; i++)
        {
            GameObject marker = new GameObject("Marker_" + i);
            marker.transform.SetParent(rangeIndicator.transform);
            float angle = i * 90f;
            float radians = angle * Mathf.Deg2Rad;
            float radius = 0.45f;
            marker.transform.localPosition = new Vector3(
                Mathf.Sin(radians) * currentHalfWidth * radius,
                0.07f,
                Mathf.Cos(radians) * currentHalfLength * radius);
            
            MeshFilter mf = marker.AddComponent<MeshFilter>();
            mf.mesh = CreateSmallArrowMesh();
            MeshRenderer mr = marker.AddComponent<MeshRenderer>();
            Material arrowMat = new Material(Shader.Find("Unlit/Color"));
            arrowMat.color = new Color(0.5f, 0.8f, 1.0f, 0.6f);
            mr.material = arrowMat;
        }
    }
    
    /// <summary>
    /// 生成圆形网格
    /// </summary>
    private Mesh CreateCircleMesh(int segments, float radius)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        Vector2[] uv = new Vector2[segments + 1];
        
        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
            uv[i + 1] = new Vector2(Mathf.Sin(angle) * 0.5f + 0.5f, Mathf.Cos(angle) * 0.5f + 0.5f);
            
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }
    
    /// <summary>
    /// 生成环形网格（外圈边框）
    /// </summary>
    private Mesh CreateRingMesh(int segments, float innerRadius, float outerRadius)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments * 2];
        int[] triangles = new int[segments * 6];
        Vector2[] uv = new Vector2[segments * 2];
        
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            
            vertices[i * 2] = new Vector3(sin * innerRadius, 0, cos * innerRadius);
            vertices[i * 2 + 1] = new Vector3(sin * outerRadius, 0, cos * outerRadius);
            uv[i * 2] = new Vector2((float)i / segments, 0);
            uv[i * 2 + 1] = new Vector2((float)i / segments, 1);
            
            int next = (i + 1) % segments;
            triangles[i * 6] = i * 2;
            triangles[i * 6 + 1] = next * 2;
            triangles[i * 6 + 2] = i * 2 + 1;
            triangles[i * 6 + 3] = i * 2 + 1;
            triangles[i * 6 + 4] = next * 2;
            triangles[i * 6 + 5] = next * 2 + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }
    
    /// <summary>
    /// 生成小箭头网格
    /// </summary>
    private Mesh CreateSmallArrowMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0, 0, 0.15f),
            new Vector3(-0.06f, 0, -0.05f),
            new Vector3(0.06f, 0, -0.05f),
            new Vector3(0, 0, -0.15f)
        };
        mesh.vertices = vertices;
        mesh.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
        mesh.RecalculateNormals();
        return mesh;
    }
    
    /// <summary>
    /// 应用范围指示器材质
    /// </summary>
    private void ApplyRangeMaterial(MeshRenderer renderer, bool isMainDisk)
    {
        Shader shader = Shader.Find("KingdomWar/PlacementIndicator");
        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.SetColor("_MainColor", new Color(0.2f, 0.6f, 1.0f, 0.25f));
            mat.SetColor("_BorderColor", new Color(0.5f, 0.8f, 1.0f, 0.7f));
            mat.SetFloat("_BorderWidth", 0.05f);
            mat.SetFloat("_FadeDistance", isMainDisk ? 0.2f : 0.0f);
            mat.SetFloat("_IsValid", 1f);
            renderer.material = mat;
        }
        else
        {
            Material fallback = new Material(Shader.Find("Transparent/Diffuse"));
            fallback.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            renderer.material = fallback;
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
                Material mat = meshRenderer.material;
                if (mat.HasProperty("_IsValid"))
                {
                    // 使用自定义 Shader 的属性
                    mat.SetFloat("_IsValid", isInRange ? 1f : 0f);
                }
                else
                {
                    // 降级：直接改颜色
                    mat.color = isInRange ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.8f, 0.2f, 0.2f, 0.3f);
                }
            }
            
            // 根据卡片类型调整范围指示器的大小
            if (cardType == CardType.Spell)
            {
                rangeIndicator.transform.localScale = new Vector3(7f, 1f, 12f);
            }
            else
            {
                rangeIndicator.transform.localScale = new Vector3(currentHalfWidth * 2f, 1f, currentHalfLength * 2f);
                rangeIndicator.transform.position = ownSideCenter + new Vector3(0, 0.1f, 0);
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