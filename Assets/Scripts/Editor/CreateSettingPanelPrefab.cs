using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using KingdomWar.UI;
namespace KingdomWar.Editor
{
/// <summary>
/// 辅助脚本：创�?settingPanel 预制�?/// 使用方法：在 Unity 编辑器中，点击菜�?"Tools/Create SettingPanel Prefab"
/// </summary>
public class CreateSettingPanelPrefab : EditorWindow
{
    [MenuItem("Tools/Create SettingPanel Prefab")]
    public static void CreatePrefab()
    {
        // define prefab path
        string prefabPath = "Assets/Resources_moved/Prefabs/UIPrefab/settingPanel.prefab";
        
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
            if (EditorUtility.DisplayDialog("提示", "settingPanel 预制体已存在，是否覆盖？", "覆盖", "取消"))
            {
                DeleteExistingPrefab(prefabPath);
            }
            else
            {
                return;
            }
        }
        
        // 创建�?GameObject
        GameObject settingPanelObj = new GameObject("settingPanel");
        
        // 添加 RectTransform
        RectTransform rectTransform = settingPanelObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(600, 700);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        
        // 添加 Image 背景
        Image bgImage = settingPanelObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        
        // 添加 CanvasGroup
        settingPanelObj.AddComponent<CanvasGroup>();
        
        // 添加 settingPanel 脚本
        settingPanelObj.AddComponent<settingPanel>();
        
        // 创建标题
        CreateTitle(settingPanelObj.transform);
        
        // 创建关闭按钮
        CreateCloseButton(settingPanelObj.transform);
        
        // 创建音量控制区域
        CreateVolumeControl(settingPanelObj.transform);
        
        // 创建帧率设置区域
        CreateFPSSetting(settingPanelObj.transform);
        
        // 创建退出游戏按�?        CreateExitButton(settingPanelObj.transform);
        
        // 保存为预制体
        SaveAsPrefab(settingPanelObj, prefabPath);
        
        Debug.Log($"�?settingPanel 预制体已创建：{prefabPath}");
    }
    
    private static void CreateTitle(Transform parent)
    {
        GameObject title = new GameObject("Title");
        title.transform.SetParent(parent, false);
        
        RectTransform rectTransform = title.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 60);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0, -40);
        
        Text text = title.AddComponent<Text>();
        text.text = "设置";
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        text.fontSize = 36;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
    }
    
    private static void CreateCloseButton(Transform parent)
    {
        GameObject closeBtn = new GameObject("CloseBtn");
        closeBtn.transform.SetParent(parent, false);
        
        RectTransform rectTransform = closeBtn.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(50, 50);
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-30, -30);
        
        Image image = closeBtn.AddComponent<Image>();
        image.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        
        Button button = closeBtn.AddComponent<Button>();
        button.targetGraphic = image;
        
        // 创建�?GameObject 用于文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeBtn.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = "×";
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
        text.fontSize = 32;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        // 设置文本 RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);
    }
    
    private static void CreateVolumeControl(Transform parent)
    {
        GameObject volumeControl = new GameObject("VolumeControl");
        volumeControl.transform.SetParent(parent, false);
        
        RectTransform rectTransform = volumeControl.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(450, 120);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0, -150);
        
        // 标题
        GameObject volumeTitle = new GameObject("VolumeTitle");
        volumeTitle.transform.SetParent(volumeControl.transform, false);
        
        RectTransform titleRect = volumeTitle.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(200, 40);
        titleRect.anchorMin = new Vector2(0, 1f);
        titleRect.anchorMax = new Vector2(0, 1f);
        titleRect.pivot = new Vector2(0, 0.5f);
        titleRect.anchoredPosition = new Vector2(20, -30);
        
        Text titleText = volumeTitle.AddComponent<Text>();
        titleText.text = "Volume";
        titleText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleLeft;
        
        // Slider
        GameObject sliderObj = new GameObject("VolumeSlider");
        sliderObj.transform.SetParent(volumeControl.transform, false);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(250, 30);
        sliderRect.anchorMin = new Vector2(1f, 1f);
        sliderRect.anchorMax = new Vector2(1f, 1f);
        sliderRect.pivot = new Vector2(1f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(-30, -30);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = 1;
        slider.interactable = true;
        
        // Slider 背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(0, -10);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        
        RectTransform fillRect = fillArea.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0.25f);
        fillRect.anchorMax = new Vector2(1, 0.75f);
        fillRect.sizeDelta = new Vector2(-10, 0);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        
        Image fillImage = fillArea.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        fillImage.raycastTarget = false;
        
        // Handle Slide Area
        GameObject handleSlideArea = new GameObject("Handle Slide Area");
        handleSlideArea.transform.SetParent(sliderObj.transform, false);
        
        RectTransform handleSlideRect = handleSlideArea.AddComponent<RectTransform>();
        handleSlideRect.anchorMin = Vector2.zero;
        handleSlideRect.anchorMax = Vector2.one;
        handleSlideRect.sizeDelta = new Vector2(-20, 0);
        handleSlideRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleSlideArea.transform, false);
        
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        handleRect.anchorMin = new Vector2(0, 0.5f);
        handleRect.anchorMax = new Vector2(0, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 1f);
        
        slider.handleRect = handleRect;
        
        // volume percentage text
        GameObject volumeTextObj = new GameObject("VolumeText");
        volumeTextObj.transform.SetParent(volumeControl.transform, false);
        
        RectTransform textRect = volumeTextObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(80, 40);
        textRect.anchorMin = new Vector2(1f, 0);
        textRect.anchorMax = new Vector2(1f, 0);
        textRect.pivot = new Vector2(1f, 0.5f);
        textRect.anchoredPosition = new Vector2(-30, 30);
        
        Text volumeText = volumeTextObj.AddComponent<Text>();
        volumeText.text = "100%";
        volumeText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        volumeText.fontSize = 20;
        volumeText.color = Color.white;
        volumeText.alignment = TextAnchor.MiddleRight;
    }
    
    private static void CreateFPSSetting(Transform parent)
    {
        GameObject fpsSetting = new GameObject("FPSSetting");
        fpsSetting.transform.SetParent(parent, false);
        
        RectTransform rectTransform = fpsSetting.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(450, 120);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0, -290);
        
        // 标题
        GameObject fpsTitle = new GameObject("FPSTitle");
        fpsTitle.transform.SetParent(fpsSetting.transform, false);
        
        RectTransform titleRect = fpsTitle.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(200, 40);
        titleRect.anchorMin = new Vector2(0, 1f);
        titleRect.anchorMax = new Vector2(0, 1f);
        titleRect.pivot = new Vector2(0, 0.5f);
        titleRect.anchoredPosition = new Vector2(20, -30);
        
        Text titleText = fpsTitle.AddComponent<Text>();
        titleText.text = "FPS";
        titleText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        titleText.fontSize = 24;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleLeft;
        
        // Dropdown
        GameObject dropdownObj = new GameObject("FPSDropdown");
        dropdownObj.transform.SetParent(fpsSetting.transform, false);
        
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.sizeDelta = new Vector2(200, 50);
        dropdownRect.anchorMin = new Vector2(1f, 1f);
        dropdownRect.anchorMax = new Vector2(1f, 1f);
        dropdownRect.pivot = new Vector2(1f, 0.5f);
        dropdownRect.anchoredPosition = new Vector2(-30, -30);
        
        Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
        
        // Dropdown 背景
        Image bgImage = dropdownObj.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        dropdown.image = bgImage;
        
        // 标签文本
        GameObject label = new GameObject("Label");
        label.transform.SetParent(dropdownObj.transform, false);
        
        RectTransform labelRect = label.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.sizeDelta = new Vector2(-20, -10);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        
        Text labelText = label.AddComponent<Text>();
        labelText.text = "60 FPS";
        labelText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        labelText.fontSize = 20;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        
        dropdown.captionText = labelText;
        
        // 箭头
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(dropdownObj.transform, false);
        
        RectTransform arrowRect = arrow.AddComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        
        Text arrowText = arrow.AddComponent<Text>();
        arrowText.text = ">";
        arrowText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        arrowText.fontSize = 14;
        arrowText.color = Color.white;
        arrowText.alignment = TextAnchor.MiddleCenter;
        
        // 创建模板
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        templateObj.SetActive(false);
        
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.sizeDelta = new Vector2(200, 150);
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = new Vector2(0, -2);
        
        // 添加 Canvas 确保独立渲染层级
        Canvas templateCanvas = templateObj.AddComponent<Canvas>();
        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = 1000;
        
        templateObj.AddComponent<GraphicRaycaster>();
        
        Image templateImage = templateObj.AddComponent<Image>();
        templateImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // ScrollRect
        GameObject scrollRectObj = new GameObject("Scroll View");
        scrollRectObj.transform.SetParent(templateObj.transform, false);
        
        RectTransform scrollRectRect = scrollRectObj.AddComponent<RectTransform>();
        scrollRectRect.anchorMin = Vector2.zero;
        scrollRectRect.anchorMax = Vector2.one;
        scrollRectRect.sizeDelta = new Vector2(-10, -10);
        scrollRectRect.pivot = new Vector2(0.5f, 0.5f);
        
        ScrollRect scrollRect = scrollRectObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        
        // Content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollRectObj.transform, false);
        
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(0, 100);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = new Vector2(0, 0);
        
        // 设置 ScrollRect
        scrollRect.content = contentRect;
        
        // 创建 Item (Toggle)
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(200, 30);
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(1, 1);
        itemRect.pivot = new Vector2(0.5f, 1);
        itemRect.anchoredPosition = new Vector2(0, 0);
        
        Toggle toggle = itemObj.AddComponent<Toggle>();
        toggle.isOn = true;
        
        Image itemImage = itemObj.AddComponent<Image>();
        itemImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        toggle.targetGraphic = itemImage;
        
        // Item Label
        GameObject itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        
        RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.sizeDelta = new Vector2(-10, 0);
        itemLabelRect.pivot = new Vector2(0.5f, 0.5f);
        
        Text itemLabelText = itemLabelObj.AddComponent<Text>();
        itemLabelText.text = "Item";
        itemLabelText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        itemLabelText.fontSize = 18;
        itemLabelText.color = Color.white;
        itemLabelText.alignment = TextAnchor.MiddleLeft;
        
        dropdown.template = templateRect;
        dropdown.itemText = itemLabelText;
    }
    
    private static void CreateExitButton(Transform parent)
    {
        GameObject exitBtn = new GameObject("ExitBtn");
        exitBtn.transform.SetParent(parent, false);
        
        RectTransform rectTransform = exitBtn.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 70);
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0, 50);
        
        Image image = exitBtn.AddComponent<Image>();
        image.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        
        Button button = exitBtn.AddComponent<Button>();
        button.targetGraphic = image;
        
        // 创建�?GameObject 用于文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(exitBtn.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = "Exit Game";
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        
        // 设置文本 RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);
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
