using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace KingdomWar.Game.Pathfinding
{
    public class GridManager : MonoBehaviour
    {
        [Header("网格配置")]
        public Vector2 gridSize = new Vector2(34, 54);
        public float nodeRadius = 0.5f;
        public LayerMask unwalkableMask;
        public bool showGrid = true;
        public bool showPath = true;
        public string gridDataFileName = "GridData.json";
        
        public float NodeRadius { get { return nodeRadius; } }
        
        private GridNode[,] nodes;
        private float nodeDiameter;
        public int gridSizeX=20;
        public int gridSizeZ=20;
        public Vector3 gridOrigin = new Vector3(-8.1f, 0, -10.5f);
        
        public GridNode[,] GridNodes { get { return nodes; } }
        public int GridSizeX { get { return gridSizeX; } }
        public int GridSizeZ { get { return gridSizeZ; } }
        
        private void Start()
        {
            InitializeGrid();
        }
        
        public void InitializeGrid()
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridSize.x / nodeDiameter);
            gridSizeZ = Mathf.RoundToInt(gridSize.y / nodeDiameter);
            
            float actualGridSizeX = gridSizeX * nodeDiameter;
            float actualGridSizeZ = gridSizeZ * nodeDiameter;
            
            CreateGrid();
            LoadGridData();
        }
        
        private void CreateGrid()
        {
            nodes = new GridNode[gridSizeX, gridSizeZ];
            
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 worldPos = gridOrigin + Vector3.right * (x * nodeDiameter + nodeRadius) + 
                                      Vector3.forward * (z * nodeDiameter + nodeRadius);
                    
                    bool walkable = !(Physics.CheckSphere(worldPos, nodeRadius, unwalkableMask));
                    
                    nodes[x, z] = new GridNode(x, z, worldPos, walkable);
                }
            }
        }
        
        /// <summary>
        /// 根据世界坐标获取网格节点
        /// </summary>
        /// <param name="worldPos">世界坐标</param>
        /// <returns>对应的网格节点</returns>
        public GridNode GetNodeFromWorldPosition(Vector3 worldPos)
        {
            // 计算实际网格大小
            float actualGridSizeX = gridSizeX * nodeDiameter;
            float actualGridSizeZ = gridSizeZ * nodeDiameter;
            
            // 计算归一化坐标
            float percentX = (worldPos.x - gridOrigin.x) / actualGridSizeX;
            float percentZ = (worldPos.z - gridOrigin.z) / actualGridSizeZ;
            
            // 确保坐标在有效范围内
            percentX = Mathf.Clamp01(percentX);
            percentZ = Mathf.Clamp01(percentZ);
            
            // 计算网格坐标
            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);
            
            return nodes[x, z];
        }
        
        /// <summary>
        /// 获取节点的相邻节点
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <returns>相邻节点列表</returns>
        public List<GridNode> GetNeighbours(GridNode node)
        {
            List<GridNode> neighbours = new List<GridNode>();
            
            // 检查周围8个方向的节点
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    // 跳过自身
                    if (x == 0 && z == 0)
                        continue;
                    
                    // 计算相邻节点坐标
                    int checkX = node.gridPosition.x + x;
                    int checkZ = node.gridPosition.y + z;
                    
                    // 检查坐标是否在网格范围内
                    if (checkX >= 0 && checkX < gridSizeX && checkZ >= 0 && checkZ < gridSizeZ)
                    {
                        neighbours.Add(nodes[checkX, checkZ]);
                    }
                }
            }
            
            return neighbours;
        }
        
        /// <summary>
        /// 设置节点的可走性
        /// </summary>
        /// <param name="node">要设置的节点</param>
        /// <param name="walkable">是否可走</param>
        public void SetNodeWalkable(GridNode node, bool walkable)
        {
            if (node != null)
            {
                node.walkable = walkable;
            }
        }
        
        /// <summary>
        /// 根据网格坐标设置节点的可走性
        /// </summary>
        /// <param name="x">网格X坐标</param>
        /// <param name="z">网格Z坐标</param>
        /// <param name="walkable">是否可走</param>
        public void SetNodeWalkable(int x, int z, bool walkable)
        {
            if (x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ)
            {
                nodes[x, z].walkable = walkable;
            }
        }
        
        /// <summary>
        /// 绘制网格 gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            // 绘制网格边界
            Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, 0, gridSize.y));
            
            // 如果显示网格且网格已初始化
            if (showGrid && nodes != null)
            {
                foreach (GridNode node in nodes)
                {
                    // 设置gizmo颜色
                    Gizmos.color = node.walkable ? Color.white : Color.red;
                    
                    // 绘制节点
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
                }
            }
        }
        
        /// <summary>
        /// 绘制路径 gizmos
        /// </summary>
        /// <param name="path">路径节点列表</param>
        public void DrawPath(List<GridNode> path)
        {
            if (showPath && path != null && path.Count > 0)
            {
                foreach (GridNode node in path)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.1f));
                }
            }
        }
        
        public void SaveGridData()
        {
            if (nodes == null || gridSizeX == 0 || gridSizeZ == 0)
            {
                Debug.LogWarning("网格未初始化，无法保存数据");
                return;
            }
            
            bool[] walkableData = new bool[gridSizeX * gridSizeZ];
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    walkableData[x * gridSizeZ + z] = nodes[x, z].walkable;
                }
            }
            
            GridData gridData = new GridData { walkableData = walkableData, gridSizeX = gridSizeX, gridSizeZ = gridSizeZ };
            string jsonData = JsonUtility.ToJson(gridData, true);
            
#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "Resources", gridDataFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(editorPath));
            File.WriteAllText(editorPath, jsonData);
            Debug.Log($"格子数据保存到编辑器路径: {editorPath}");
#endif
            
            string streamingPath = Path.Combine(Application.streamingAssetsPath, gridDataFileName);
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.WriteAllText(streamingPath, jsonData);
            Debug.Log($"格子数据保存到 StreamingAssets: {streamingPath}");
        }
        
        public void LoadGridData()
        {
            if (nodes == null || gridSizeX == 0 || gridSizeZ == 0)
            {
                Debug.LogWarning("网格未初始化，无法加载数据");
                return;
            }
            
            GridData gridData = null;
            
            TextAsset textAsset = Resources.Load<TextAsset>(Path.GetFileNameWithoutExtension(gridDataFileName));
            if (textAsset != null)
            {
                try
                {
                    gridData = JsonUtility.FromJson<GridData>(textAsset.text);
                    Debug.Log("从 Resources 加载格子数据成功");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("解析 Resources 中的格子数据时出错: " + e.Message);
                }
            }
            
            if (gridData == null)
            {
                string streamingPath = Path.Combine(Application.streamingAssetsPath, gridDataFileName);
                if (File.Exists(streamingPath))
                {
                    try
                    {
                        string jsonData = File.ReadAllText(streamingPath);
                        gridData = JsonUtility.FromJson<GridData>(jsonData);
                        Debug.Log("从 StreamingAssets 加载格子数据成功");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("加载 StreamingAssets 中的格子数据时出错: " + e.Message);
                    }
                }
            }
            
            if (gridData != null && gridData.gridSizeX == gridSizeX && gridData.gridSizeZ == gridSizeZ && gridData.walkableData != null)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    for (int z = 0; z < gridSizeZ; z++)
                    {
                        int index = x * gridSizeZ + z;
                        if (index < gridData.walkableData.Length)
                        {
                            nodes[x, z].walkable = gridData.walkableData[index];
                        }
                    }
                }
                Debug.Log("格子数据应用成功");
            }
            else if (gridData != null)
            {
                Debug.LogWarning($"保存的格子数据大小({gridData.gridSizeX}x{gridData.gridSizeZ})与当前网格({gridSizeX}x{gridSizeZ})不匹配，无法加载");
            }
            else
            {
                Debug.Log("没有找到保存的格子数据，使用默认网格设置");
            }
        }
        
        public void ClearSavedGridData()
        {
#if UNITY_EDITOR
            string editorPath = Path.Combine(Application.dataPath, "Resources", gridDataFileName);
            if (File.Exists(editorPath))
            {
                File.Delete(editorPath);
                Debug.Log($"已删除编辑器路径的格子数据: {editorPath}");
            }
#endif
            
            string streamingPath = Path.Combine(Application.streamingAssetsPath, gridDataFileName);
            if (File.Exists(streamingPath))
            {
                File.Delete(streamingPath);
                Debug.Log($"已删除 StreamingAssets 的格子数据: {streamingPath}");
            }
            
            Debug.Log("保存的格子数据已清除");
        }
        
        /// <summary>
        /// 格子数据类，用于序列化
        /// </summary>
        [System.Serializable]
        private class GridData
        {
            public bool[] walkableData;
            public int gridSizeX;
            public int gridSizeZ;
        }
    }
}
