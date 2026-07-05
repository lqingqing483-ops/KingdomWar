using UnityEngine;

namespace KingdomWar.Game.Pathfinding
{
    /// <summary>
    /// 网格节点类，表示寻路网格中的单个节点
    /// </summary>
    public class GridNode
    {
        /// <summary>
        /// 节点的网格坐标
        /// </summary>
        public Vector2Int gridPosition;
        
        /// <summary>
        /// 节点的世界坐标
        /// </summary>
        public Vector3 worldPosition;
        
        /// <summary>
        /// 节点是否可走
        /// </summary>
        public bool walkable;
        
        /// <summary>
        /// 寻路相关值
        /// </summary>
        public int gCost;    // 从起点到当前节点的代价
        public int hCost;    // 从当前节点到终点的估计代价
        public GridNode parent; // 父节点
        
        /// <summary>
        /// 总代价
        /// </summary>
        public int fCost
        {
            get { return gCost + hCost; }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">网格X坐标</param>
        /// <param name="z">网格Z坐标</param>
        /// <param name="worldPos">世界坐标</param>
        /// <param name="isWalkable">是否可走</param>
        public GridNode(int x, int z, Vector3 worldPos, bool isWalkable)
        {
            gridPosition = new Vector2Int(x, z);
            worldPosition = worldPos;
            walkable = isWalkable;
        }
        
        /// <summary>
        /// 重置寻路相关值
        /// </summary>
        public void ResetPathfindingValues()
        {
            gCost = 0;
            hCost = 0;
            parent = null;
        }
        
        /// <summary>
        /// 比较两个节点的总代价
        /// </summary>
        /// <param name="other">另一个节点</param>
        /// <returns>如果当前节点总代价小于另一个节点，返回true</returns>
        public bool CompareTo(GridNode other)
        {
            return fCost < other.fCost || (fCost == other.fCost && hCost < other.hCost);
        }
    }
}
