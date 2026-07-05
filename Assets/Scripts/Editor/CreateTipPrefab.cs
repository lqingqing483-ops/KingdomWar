using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using KingdomWar.UI;
namespace KingdomWar.Editor
{
/// <summary>
/// 辅助脚本：创�?Tip 预制�?/// 使用方法：在 Unity 编辑器中，点击菜�?"Tools/Create Tip Prefab"
/// </summary>
public class CreateTipPrefab : EditorWindow
{
    [MenuItem("Tools/Create Tip Prefab")]
    public static void CreatePrefab()
    {
        // define prefab path
        string prefabPath = "Assets/Resources_moved/Prefabs/UIPrefab/Tip.prefab";
        
        // 确保目录存在
        string directory = Path.GetDirectoryName(prefabPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }
        
        // 检查是否已存在
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            if (EditorUtility.DisplayDialog("提示", "Tip 预制体已存在，是否覆盖？", "覆盖", "取消"))
            {
                DeleteExistingPrefab(prefabPath);
            }
            else
            {
                return;
            }
        }
        
        // 创建 GameObject
        GameObject tipObj = new GameObject("Tip");
        
        // 添加 RectTransform 组件
        RectTransform rectTransform = tipObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 80);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0, -100); // 距离顶部 100 像素
        
        // 添加 Image 组件作为背景
        Image image = tipObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // 深灰色半透明背景
        
        // 添加 Tip 脚本组件
        tipObj.AddComponent<Tip>();
        
        // 创建文本子对�?        CreateTextChild(tipObj.transform);
        
        // 保存为预制体
        SaveAsPrefab(tipObj, prefabPath);
        
        Debug.Log($"�?Tip 预制体已创建：{prefabPath}");
    }
    
    private static void CreateTextChild(Transform parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);
        
        // 添加 Text 组件
        Text text = textObj.AddComponent<Text>();
        text.text = "提示信息";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        
        // 添加 RectTransform
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.sizeDelta = new Vector2(-20, -10); // 留边�?        rectTransform.pivot =
        new Vector2(0.5f, 0.5f);
        
        // 添加 ContentSizeFitter 让文本自适应
        ContentSizeFitter fitter = textObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
    
    private static void SaveAsPrefab(GameObject obj, string path)
    {
        // 创建预制�?        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        
        // 清理临时对象
        GameObject.DestroyImmediate(obj);
        
        // 刷新资源数据�?        AssetDatabase.Refresh();
    }
    
    private static void DeleteExistingPrefab(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            AssetDatabase.Refresh();
        }
    }
}

}
