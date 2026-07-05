// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;

// namespace KingdomWar.Game.Pathfinding
// {
//     [CustomEditor(typeof(GridManager))]
//     public class GridEditor : Editor
//     {
//         private GridManager grid;
//         private bool editMode = false;
//         private bool paintMode = true;
//         private Vector2 lastMousePosition;

//         public override void OnInspectorGUI()
//         {
//             DrawDefaultInspector();

//             editMode = EditorGUILayout.Toggle("Edit Mode", editMode);

//             if (editMode)
//             {
//                 paintMode = EditorGUILayout.Toggle("Paint Walkable", paintMode);

//                 if (GUILayout.Button("Initialize Grid"))
//                 {
//                     grid.InitializeGrid();
//                 }

//                 if (GUILayout.Button("Reset Grid"))
//                 {
//                     if (EditorUtility.DisplayDialog("Confirm", "Reset all grid data?", "Yes", "No"))
//                     {
//                         for (int x = 0; x < grid.GridSizeX; x++)
//                         {
//                             for (int z = 0; z < grid.GridSizeZ; z++)
//                             {
//                                 grid.SetNodeWalkable(x, z, true);
//                             }
//                         }
//                     }
//                 }

//                 EditorGUILayout.Space();
//                 EditorGUILayout.LabelField("Grid Info:");
//                 EditorGUILayout.LabelField($"Grid Size: {grid.GridSizeX} x {grid.GridSizeZ}");
//                 EditorGUILayout.LabelField($"Node Radius: {grid.nodeRadius}");

//                 EditorGUILayout.Space();
//                 EditorGUILayout.LabelField("Data Management:");

//                 if (GUILayout.Button("Save Grid Data"))
//                 {
//                     grid.SaveGridData();
//                 }

//                 if (GUILayout.Button("Load Grid Data"))
//                 {
//                     grid.LoadGridData();
//                 }

//                 if (GUILayout.Button("Clear Saved Data"))
//                 {
//                     if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear saved grid data?", "Yes", "No"))
//                     {
//                         grid.ClearSavedGridData();
//                     }
//                 }
//             }

//             if (GUI.changed)
//             {
//                 EditorUtility.SetDirty(grid);
//             }
//         }

//         private void OnSceneGUI()
//         {
//             if (!editMode || grid.GridNodes == null)
//                 return;

//             HandleMouseEvents();
//             DrawUI();
//         }

//         private void DrawUI()
//         {
//             Handles.BeginGUI();
//             GUILayout.BeginArea(new Rect(10, 10, 200, 100));
//             GUILayout.Label("Grid Editor:");
//             GUILayout.Label($"Mode: {(editMode ? "Edit" : "View")}");
//             GUILayout.Label($"Paint: {(paintMode ? "Walkable" : "Unwalkable")}");
//             GUILayout.Label("Left: Paint");
//             GUILayout.Label("Right: Cancel");
//             GUILayout.EndArea();
//             Handles.EndGUI();
//         }

//         private void HandleMouseEvents()
//         {
//             Event currentEvent = Event.current;
//             Vector2 mousePosition = currentEvent.mousePosition;

//             Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
//             float distance;

//             Plane gridPlane = new Plane(Vector3.up, Vector3.zero);

//             if (gridPlane.Raycast(ray, out distance))
//             {
//                 Vector3 hitPoint = ray.GetPoint(distance);

//                 GridNode node = grid.GetNodeFromWorldPosition(hitPoint);
//                 if (node != null)
//                 {
//                     Handles.color = paintMode ? Color.green : Color.red;
//                     Handles.DrawWireCube(node.worldPosition, Vector3.one * (grid.nodeRadius * 2));

//                     if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
//                     {
//                         grid.SetNodeWalkable(node, paintMode);
//                         lastMousePosition = mousePosition;
//                         currentEvent.Use();
//                     }
//                     else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0)
//                     {
//                         if ((mousePosition - lastMousePosition).magnitude > 5)
//                         {
//                             grid.SetNodeWalkable(node, paintMode);
//                             lastMousePosition = mousePosition;
//                             currentEvent.Use();
//                         }
//                     }
//                     else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1)
//                     {
//                         paintMode = !paintMode;
//                         currentEvent.Use();
//                     }
//                 }
//             }
//         }

//         private void OnDrawGizmos()
//         {
//             if (grid == null || grid.GridNodes == null)
//                 return;

//             Gizmos.DrawWireCube(grid.transform.position, new Vector3(grid.gridSize.x, 0, grid.gridSize.y));

//             foreach (GridNode node in grid.GridNodes)
//             {
//                 if (node != null)
//                 {
//                     Gizmos.color = node.walkable ? Color.white : Color.red;
//                     Gizmos.DrawCube(node.worldPosition, Vector3.one * (grid.nodeRadius * 2 - 0.1f));
//                 }
//             }
//         }
//     }
// }
