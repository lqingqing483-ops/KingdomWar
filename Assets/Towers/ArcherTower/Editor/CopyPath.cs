using UnityEditor;
using UnityEngine;

public class CopyPath : EditorWindow
{
    [MenuItem("GameObject/Copy Path", priority = 0)]
    static void CopyGameObjectPathCommand(MenuCommand menuCommand)
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            string gameObjectPath = GetGameObjectPath(selectedObject);
            EditorGUIUtility.systemCopyBuffer = gameObjectPath;
            EditorGUIUtility.systemCopyBuffer = gameObjectPath;
            Debug.Log("选择的节点路径为: " + gameObjectPath);
        }
        else
        {
            Debug.LogWarning("No GameObject selected.");
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}