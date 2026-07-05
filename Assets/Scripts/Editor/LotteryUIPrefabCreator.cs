using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using KingdomWar.UI;
namespace KingdomWar.Editor
{
public class LotteryUIPrefabCreator
{
    [MenuItem("Tools/Create Lottery UI Prefabs")]
    public static void CreateLotteryUIPrefabs()
    {
        CreateLotteryPanelPrefab();
        UpdateBattlePanelPrefab();
        Debug.Log("Lottery UI Prefabs created successfully!");
    }

    private static void CreateLotteryPanelPrefab()
    {
        GameObject lotteryPanel = new GameObject("lotteryPanel");
        RectTransform rectTransform = lotteryPanel.AddComponent<RectTransform>();
        
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        CanvasGroup canvasGroup = lotteryPanel.AddComponent<CanvasGroup>();
        lotteryPanel.AddComponent<lotteryPanel>();

        GameObject background = CreateImage("Background", lotteryPanel.transform);
        SetupFullStretch(background);
        background.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

        GameObject titleBar = CreateTitleBar(lotteryPanel.transform);
        
        GameObject contentArea = CreateContentArea(lotteryPanel.transform);
        
        GameObject resultPanel = CreateResultPanel(lotteryPanel.transform);
        
        GameObject loadingOverlay = CreateLoadingOverlay(lotteryPanel.transform);

        string prefabPath = "Assets/Resources/Prefabs/UIPrefab/lotteryPanel.prefab";
        SavePrefab(lotteryPanel, prefabPath);
    }

    private static GameObject CreateTitleBar(Transform parent)
    {
        GameObject titleBar = new GameObject("TitleBar");
        RectTransform rect = titleBar.AddComponent<RectTransform>();
        titleBar.transform.SetParent(parent, false);
        
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(0, 80);

        Image bg = titleBar.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.3f, 0.5f, 1f);

        GameObject titleText = CreateText("Title", titleBar.transform, "抽奖系统");
        RectTransform textRect = titleText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(200, 50);
        
        Text text = titleText.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 56;
        text.fontStyle = FontStyle.Bold;

        GameObject closeBtn = CreateButton("CloseBtn", titleBar.transform, "关闭");
        RectTransform btnRect = closeBtn.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0.5f);
        btnRect.anchorMax = new Vector2(1, 0.5f);
        btnRect.anchoredPosition = new Vector2(-60, 0);
        btnRect.sizeDelta = new Vector2(80, 40);

        return titleBar;
    }

    private static GameObject CreateContentArea(Transform parent)
    {
        GameObject contentArea = new GameObject("ContentArea");
        RectTransform rect = contentArea.AddComponent<RectTransform>();
        contentArea.transform.SetParent(parent, false);
        
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(20, 20);
        rect.offsetMax = new Vector2(-20, -100);

        GameObject infoPanel = CreateInfoPanel(contentArea.transform);
        
        GameObject buttonPanel = CreateButtonPanel(contentArea.transform);

        return contentArea;
    }

    private static GameObject CreateInfoPanel(Transform parent)
    {
        GameObject infoPanel = new GameObject("InfoPanel");
        RectTransform rect = infoPanel.AddComponent<RectTransform>();
        infoPanel.transform.SetParent(parent, false);
        
        rect.anchorMin = new Vector2(0, 0.7f);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = infoPanel.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.25f, 0.8f);

        GameObject drawCountText = CreateText("DrawCountText", infoPanel.transform, "Free Draws: 1");
        RectTransform textRect = drawCountText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.7f);
        textRect.anchorMax = new Vector2(0.5f, 0.7f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(300, 40);
        
        Text text = drawCountText.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 40;

        GameObject goldText = CreateText("GoldText", infoPanel.transform, "金币: 1000");
        RectTransform goldRect = goldText.GetComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0.5f, 0.3f);
        goldRect.anchorMax = new Vector2(0.5f, 0.3f);
        goldRect.anchoredPosition = Vector2.zero;
        goldRect.sizeDelta = new Vector2(200, 40);
        
        Text goldTextComp = goldText.GetComponent<Text>();
        goldTextComp.alignment = TextAnchor.MiddleCenter;
        goldTextComp.fontSize = 36;
        goldTextComp.color = Color.yellow;

        return infoPanel;
    }

    private static GameObject CreateButtonPanel(Transform parent)
    {
        GameObject buttonPanel = new GameObject("ButtonPanel");
        RectTransform rect = buttonPanel.AddComponent<RectTransform>();
        buttonPanel.transform.SetParent(parent, false);
        
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.3f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject freeDrawBtn = CreateButton("FreeDrawButton", buttonPanel.transform, "免费抽奖");
        RectTransform freeRect = freeDrawBtn.GetComponent<RectTransform>();
        freeRect.anchorMin = new Vector2(0.3f, 0.5f);
        freeRect.anchorMax = new Vector2(0.3f, 0.5f);
        freeRect.anchoredPosition = Vector2.zero;
        freeRect.sizeDelta = new Vector2(150, 60);
        freeDrawBtn.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);

        GameObject drawBtn = CreateButton("DrawButton", buttonPanel.transform, "抽奖 (100金币)");
        RectTransform drawRect = drawBtn.GetComponent<RectTransform>();
        drawRect.anchorMin = new Vector2(0.7f, 0.5f);
        drawRect.anchorMax = new Vector2(0.7f, 0.5f);
        drawRect.anchoredPosition = Vector2.zero;
        drawRect.sizeDelta = new Vector2(150, 60);
        drawBtn.GetComponent<Image>().color = new Color(0.8f, 0.6f, 0.2f);

        return buttonPanel;
    }

    private static GameObject CreateResultPanel(Transform parent)
    {
        GameObject resultPanel = new GameObject("ResultPanel");
        RectTransform rect = resultPanel.AddComponent<RectTransform>();
        resultPanel.transform.SetParent(parent, false);
        
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Image bg = resultPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        GameObject cardDisplay = new GameObject("CardDisplay");
        RectTransform cardRect = cardDisplay.AddComponent<RectTransform>();
        cardDisplay.transform.SetParent(resultPanel.transform, false);
        
        cardRect.anchorMin = new Vector2(0.5f, 0.6f);
        cardRect.anchorMax = new Vector2(0.5f, 0.6f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(150, 200);

        Image cardImage = cardDisplay.AddComponent<Image>();
        cardImage.color = Color.white;

        GameObject cardName = CreateText("ResultCardName", resultPanel.transform, "卡片名称");
        RectTransform nameRect = cardName.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 0.35f);
        nameRect.anchorMax = new Vector2(0.5f, 0.35f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(200, 40);
        
        Text nameText = cardName.GetComponent<Text>();
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.fontSize = 48;
        nameText.fontStyle = FontStyle.Bold;

        GameObject cardRarity = CreateText("ResultCardRarity", resultPanel.transform, "稀有度");
        RectTransform rarityRect = cardRarity.GetComponent<RectTransform>();
        rarityRect.anchorMin = new Vector2(0.5f, 0.25f);
        rarityRect.anchorMax = new Vector2(0.5f, 0.25f);
        rarityRect.anchoredPosition = Vector2.zero;
        rarityRect.sizeDelta = new Vector2(150, 30);
        
        Text rarityText = cardRarity.GetComponent<Text>();
        rarityText.alignment = TextAnchor.MiddleCenter;
        rarityText.fontSize = 36;

        GameObject confirmBtn = CreateButton("ConfirmResultButton", resultPanel.transform, "确认");
        RectTransform confirmRect = confirmBtn.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.5f, 0.1f);
        confirmRect.anchorMax = new Vector2(0.5f, 0.1f);
        confirmRect.anchoredPosition = Vector2.zero;
        confirmRect.sizeDelta = new Vector2(120, 40);

        resultPanel.SetActive(false);

        return resultPanel;
    }

    private static GameObject CreateLoadingOverlay(Transform parent)
    {
        GameObject overlay = new GameObject("LoadingOverlay");
        RectTransform rect = overlay.AddComponent<RectTransform>();
        overlay.transform.SetParent(parent, false);
        
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Image bg = overlay.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        GameObject loadingText = CreateText("LoadingText", overlay.transform, "抽奖�?..");
        RectTransform textRect = loadingText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(200, 50);
        
        Text text = loadingText.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 48;
        text.color = Color.white;

        overlay.SetActive(false);

        return overlay;
    }

    private static void UpdateBattlePanelPrefab()
    {
        string prefabPath = "Assets/Resources/Prefabs/UIPrefab/battlePanel.prefab";
        GameObject battlePanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (battlePanelPrefab == null)
        {
            Debug.LogWarning("battlePanel prefab not found. Please add hot update button manually.");
            return;
        }

        Debug.Log("Battle panel prefab found. Please add the following UI elements manually:");
        Debug.Log("- hotUpdateButton (Button)");
        Debug.Log("- lotteryButton (Button)");
        Debug.Log("- hotUpdateProgressImage (Image with Fill type)");
        Debug.Log("- hotUpdateStatusText (Text)");
        Debug.Log("- hotUpdatePanel (GameObject)");
    }

    private static GameObject CreateImage(string name, Transform parent)
    {
        GameObject imageObj = new GameObject(name);
        imageObj.transform.SetParent(parent, false);
        imageObj.AddComponent<Image>();
        return imageObj;
    }

    private static GameObject CreateText(string name, Transform parent, string content)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        
        return textObj;
    }

    private static GameObject CreateButton(string name, Transform parent, string buttonText)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.5f, 0.8f);
        
        Button button = buttonObj.AddComponent<Button>();
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        return buttonObj;
    }

    private static void SetupFullStretch(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void SavePrefab(GameObject obj, string path)
    {
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        
        Debug.Log($"Created prefab at: {path}");
    }
}

}
