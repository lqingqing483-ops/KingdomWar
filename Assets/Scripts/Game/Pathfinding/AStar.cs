using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.Pathfinding
{
    /// <summary>
    /// A*寻路算法类
    /// </summary>
    public class AStar
    {
        private GridManager grid;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="grid">寻路网格</param>
        public AStar(GridManager grid)
        {
            this.grid = grid;
        }
        
        /// <summary>
        /// 计算从起点到终点的路径
        /// </summary>
        /// <param name="startPos">起点世界坐标</param>
        /// <param name="targetPos">终点世界坐标</param>
        /// <returns>路径节点列表</returns>
        public List<GridNode> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            // 获取起点和终点节点
            GridNode startNode = grid.GetNodeFromWorldPosition(startPos);
            GridNode targetNode = grid.GetNodeFromWorldPosition(targetPos);
            
            // 检查起点和终点是否有效
            if (startNode == null || targetNode == null || !startNode.walkable || !targetNode.walkable)
            {
                Debug.LogWarning("Invalid start or target node!");
                return null;
            }
            
            // 重置所有节点的寻路值
            ResetAllNodes();
            
            // 打开列表和关闭列表
            List<GridNode> openList = new List<GridNode>();
            HashSet<GridNode> closedList = new HashSet<GridNode>();
            
            // 将起点添加到打开列表
            openList.Add(startNode);
            
            // 开始寻路
            while (openList.Count > 0)
            {
                // 找到打开列表中fCost最小的节点
                GridNode currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].fCost < currentNode.fCost || 
                        (openList[i].fCost == currentNode.fCost && openList[i].hCost < currentNode.hCost))
                    {
                        currentNode = openList[i];
                    }
                }
                
                // 将当前节点从打开列表移到关闭列表
                openList.Remove(currentNode);
                closedList.Add(currentNode);
                
                // 如果找到终点，回溯构建路径
                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }
                
                // 检查当前节点的所有邻居
                foreach (GridNode neighbour in grid.GetNeighbours(currentNode))
                {
                    // 如果邻居不可走或已在关闭列表中，跳过
                    if (!neighbour.walkable || closedList.Contains(neighbour))
                    {
                        continue;
                    }
                    
                    // 计算从起点到邻居的新代价
                    int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                    
                    // 如果新代价更低或邻居不在打开列表中
                    if (newCostToNeighbour < neighbour.gCost || !openList.Contains(neighbour))
                    {
                        // 更新邻居的代价和父节点
                        neighbour.gCost = newCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;
                        
                        // 如果邻居不在打开列表中，添加它
                        if (!openList.Contains(neighbour))
                        {
                            openList.Add(neighbour);
                        }
                    }
                }
            }
            
            // 如果没有找到路径，返回null
            Debug.LogWarning("No path found!");
            return null;
        }
        
        /// <summary>
        /// 回溯构建路径
        /// </summary>
        /// <param name="startNode">起点节点</param>
        /// <param name="endNode">终点节点</param>
        /// <returns>路径节点列表</returns>
        private List<GridNode> RetracePath(GridNode startNode, GridNode endNode)
        {
            List<GridNode> path = new List<GridNode>();
            GridNode currentNode = endNode;
            
            // 从终点回溯到起点
            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
                
                // 防止无限循环
                if (currentNode == null)
                {
                    Debug.LogWarning("Path retrace failed - null parent node!");
                    return null;
                }
            }
            
            // 将起点添加到路径
            path.Add(startNode);
            
            // 反转路径，使其从起点到终点
            path.Reverse();
            
            return path;
        }
        
        /// <summary>
        /// 计算两个节点之间的距离
        /// </summary>
        /// <param name="nodeA">节点A</param>
        /// <param name="nodeB">节点B</param>
        /// <returns>距离代价</returns>
        private int GetDistance(GridNode nodeA, GridNode nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridPosition.x - nodeB.gridPosition.x);
            int dstZ = Mathf.Abs(nodeA.gridPosition.y - nodeB.gridPosition.y);
            
            // 对角线移动的代价为14，直线移动的代价为10
            if (dstX > dstZ)
                return 14 * dstZ + 10 * (dstX - dstZ);
            return 14 * dstX + 10 * (dstZ - dstX);
        }
        
        /// <summary>
        /// 重置所有节点的寻路值
        /// </summary>
        private void ResetAllNodes()
        {
            if (grid == null || grid.GridNodes == null)
                return;
            
            for (int x = 0; x < grid.GridSizeX; x++)
            {
                for (int z = 0; z < grid.GridSizeZ; z++)
                {
                    grid.GridNodes[x, z].ResetPathfindingValues();
                }
            }
        }
        
        /// <summary>
        /// 将路径节点列表转换为世界坐标列表
        /// </summary>
        /// <param name="path">路径节点列表</param>
        /// <returns>世界坐标列表</returns>
        public List<Vector3> ConvertPathToWorldPositions(List<GridNode> path)
        {
            List<Vector3> worldPositions = new List<Vector3>();
            
            if (path == null)
                return worldPositions;
            
            foreach (GridNode node in path)
            {
                worldPositions.Add(node.worldPosition);
            }
            
            return worldPositions;
        }
    }
}
