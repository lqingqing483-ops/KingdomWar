#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using KingdomWar.Game.Pathfinding;
namespace KingdomWar.Editor
{
[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : UnityEditor.Editor
{
    private GridManager grid;
    private bool editMode = false;
    private bool paintMode = false;
    
    private void OnEnable()
    {
        grid = (GridManager)target;
    }
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("网格编辑工具", EditorStyles.boldLabel);
        
        editMode = EditorGUILayout.Toggle("编辑模式", editMode);
        
        if (editMode)
        {
            paintMode = EditorGUILayout.Toggle("Paint Unwalkable", paintMode);
            
            EditorGUILayout.HelpBox(
                "Edit Mode Instructions:\n" +
                "- Check Paint Unwalkable: Left click to set cell unwalkable\n" +
                "- Uncheck: Left click to set cell walkable\n" +
                "- After editing, click 'Save Grid Data' button",
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Initialize Grid", GUILayout.Height(30)))
            {
                grid.InitializeGrid();
                EditorUtility.SetDirty(grid);
            }
            
            if (GUILayout.Button("清空网格(全部设为可走)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Clear all grid data? Set all cells walkable?", "Yes", "No"))
                {
                    for (int x = 0; x < grid.GridSizeX; x++)
                    {
                        for (int z = 0; z < grid.GridSizeZ; z++)
                        {
                            grid.SetNodeWalkable(x, z, true);
                        }
                    }
                    EditorUtility.SetDirty(grid);
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("数据管理:", EditorStyles.boldLabel);
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save Grid Data to File", GUILayout.Height(35)))
            {
                grid.SaveGridData();
                AssetDatabase.Refresh();
            }
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("Load Grid Data from File", GUILayout.Height(30)))
            {
                grid.LoadGridData();
                EditorUtility.SetDirty(grid);
            }
            
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear Saved Grid Data", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Clear all saved grid data files?", "Yes", "No"))
                {
                    grid.ClearSavedGridData();
                    AssetDatabase.Refresh();
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("网格信息:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"网格大小: {grid.GridSizeX} x {grid.GridSizeZ}");
            EditorGUILayout.LabelField($"节点半径: {grid.nodeRadius}");
            EditorGUILayout.LabelField($"节点直径: {grid.nodeRadius * 2}");
            
            int walkableCount = 0;
            int unwalkableCount = 0;
            if (grid.GridNodes != null)
            {
                foreach (var node in grid.GridNodes)
                {
                    if (node.walkable) walkableCount++;
                    else unwalkableCount++;
                }
            }
            EditorGUILayout.LabelField($"可走格子: {walkableCount}");
            EditorGUILayout.LabelField($"不可走格�? {unwalkableCount}");
        }
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(grid);
        }
    }
    
    private void OnSceneGUI()
    {
        if (!editMode || grid.GridNodes == null)
            return;
        
        HandleMouseEvents();
        
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 250, 120));
        GUILayout.Label("Grid Editor", EditorStyles.boldLabel);
        GUILayout.Label($"Edit Mode: {(editMode ? "On" : "Off")}");
        GUILayout.Label($"Current Draw: {(paintMode ? "Unwalkable" : "Walkable")}");
        GUILayout.Label("Left Click: Draw Grid");
        GUILayout.Label("ESC: 关闭编辑模式");
        GUILayout.EndArea();
        Handles.EndGUI();
        
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            editMode = false;
            Repaint();
        }
    }
    
    private void HandleMouseEvents()
    {
        Event currentEvent = Event.current;
        Vector2 mousePosition = currentEvent.mousePosition;
        
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (gridPlane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            GridNode node = grid.GetNodeFromWorldPosition(hitPoint);
            
            if (node != null)
            {
                Handles.color = paintMode ? Color.red : Color.green;
                Handles.DrawWireCube(node.worldPosition, Vector3.one * (grid.nodeRadius * 2));
                
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                {
                    grid.SetNodeWalkable(node, !paintMode);
                    EditorUtility.SetDirty(grid);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
                {
                    grid.SetNodeWalkable(node, !paintMode);
                    EditorUtility.SetDirty(grid);
                    currentEvent.Use();
                }
            }
        }
    }
}
#endif

}
