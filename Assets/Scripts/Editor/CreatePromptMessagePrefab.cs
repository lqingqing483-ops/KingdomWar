using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using KingdomWar.UI;
namespace KingdomWar.Editor
{
/// <summary>
/// 辅助脚本：创�?promptMessage 预制�?/// 使用方法：在 Unity 编辑器中，点击菜�?"Tools/Create PromptMessage Prefab"
/// </summary>
public class CreatePromptMessagePrefab : EditorWindow
{
    [MenuItem("Tools/Create PromptMessage Prefab")]
    public static void CreatePrefab()
    {
        // define prefab path
        string prefabPath = "Assets/Resources_moved/Prefabs/UIPrefab/promptMessage.prefab";
        
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
            if (EditorUtility.DisplayDialog("提示", "promptMessage 预制体已存在，是否覆盖？", "覆盖", "取消"))
            {
                DeleteExistingPrefab(prefabPath);
            }
            else
            {
                return;
            }
        }
        
        // 创建 GameObject
        GameObject promptMessageObj = new GameObject("promptMessage");
        
        // 添加 RectTransform 组件
        RectTransform rectTransform = promptMessageObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 100);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 添加 CanvasGroup 组件
        CanvasGroup canvasGroup = promptMessageObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // 添加 promptMessage 脚本组件
        promptMessage script = promptMessageObj.AddComponent<promptMessage>();
        
        // 创建背景面板（可选，让提示更美观�?        CreateBackgroundPanel(promptMessageObj.transform);
        
        // 创建文本子对�?        CreateTextChild(promptMessageObj.transform);
        
        // 保存为预制体
        SaveAsPrefab(promptMessageObj, prefabPath);
        
        Debug.Log($"�?promptMessage 预制体已创建：{prefabPath}");
    }
    
    private static void CreateBackgroundPanel(Transform parent)
    {
        GameObject background = new GameObject("Background");
        background.transform.SetParent(parent, false);
        
        // 添加 Image 组件
        Image image = background.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.8f); // 半透明黑色背景
        
        // 添加 RectTransform
        RectTransform rectTransform = background.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        // 添加 Rounded Image（可选，让边角圆润）
        // 如果需要圆角，可以添加 Mask 和调�?Image 设置
    }
    
    private static void CreateTextChild(Transform parent)
    {
        GameObject textObj = new GameObject("prompt");
        textObj.transform.SetParent(parent, false);
        
        // 添加 Text 组件
        Text text = textObj.AddComponent<Text>();
        text.text = "提示信息";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        
        // 添加 RectTransform
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.sizeDelta = new Vector2(-40, 0); // 左右留边�?        rectTransform.pivot =
        new Vector2(0.5f, 0.5f);
        
        // 添加 ContentSizeFitter 让文本自适应
        ContentSizeFitter fitter = textObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        
        // 添加 LayoutElement 控制布局
        LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 50;
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
