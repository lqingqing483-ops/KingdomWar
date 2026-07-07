using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.UI;

public static class SeasonPassPanelPrefabBuilder
{
    // ── Layout constants (mobile-friendly, 1080×1920 base) ──
    private const float PanelWidth = 900f;
    private const float PanelHeight = 1600f;
    private const float HeaderHeight = 80f;
    private const float ProgressHeight = 120f;
    private const float ScrollBottomMargin = 100f; // room for bottom buttons
    private const float BottomButtonHeight = 80f;
    private const float CloseBtnSize = 80f;
    private const float BtnMargin = 10f;
    private const float SectionPadding = 10f;

    // ── Colors ──
    private static readonly Color BgColor = new Color(0.102f, 0.102f, 0.180f, 0.95f); // #1A1A2E
    private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.25f, 1f);
    private static readonly Color ProgressBgColor = new Color(0.12f, 0.12f, 0.18f, 1f);
    private static readonly Color TitleColor = Color.white;
    private static readonly Color CountdownColor = new Color(1f, 0.843f, 0f, 1f); // #FFD700
    private static readonly Color LevelColor = Color.white;
    private static readonly Color ExpColor = new Color(0.667f, 0.667f, 0.667f, 1f); // #AAAAAA
    private static readonly Color BarBgColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    private static readonly Color BarFillColor = new Color(0.2f, 0.8f, 1f, 1f);
    private static readonly Color ButtonTextColor = Color.white;
    private static readonly Color BuyBtnColor = new Color(0.290f, 0.565f, 0.851f, 1f); // #4A90D9
    private static readonly Color ClaimBtnColor = new Color(0.180f, 0.804f, 0.443f, 1f); // #2ECC71
    private static readonly Color CloseBtnColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    private static readonly Color ScrollBgColor = new Color(0f, 0f, 0f, 0.2f);
    private static readonly Color ScrollbarColor = new Color(0.4f, 0.4f, 0.45f, 1f);

    [MenuItem("Tools/Season Pass/Build Panel Prefab")]
    private static void BuildPrefab()
    {
        // ── Ensure target directory exists ──
        const string dir = "Assets/Resources/Prefabs/UIPrefab";
        System.IO.Directory.CreateDirectory(dir);

        // ── Root: seasonPassPanel ──
        GameObject root = new GameObject("seasonPassPanel", typeof(RectTransform));
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        rootRt.anchoredPosition = Vector2.zero;

        // Root background Image
        Image rootImg = root.AddComponent<Image>();
        rootImg.color = BgColor;
        rootImg.raycastTarget = true;

        // Gradient for root panel (dark blue-to-purple)
        var rootGrad = root.AddComponent<UIGradient>();
        rootGrad.horizontal = true;
        rootGrad.color2 = new Color(0.15f, 0.10f, 0.25f);

        // CanvasGroup (required by basePanel.Awake())
        root.AddComponent<CanvasGroup>();

        // Season Pass Panel script (UIManager will skip adding it if already present)
        root.AddComponent<seasonPassPanel>();

        // ── Bg: extra background layer (stretches to fill root) ──
        CreateBg(root.transform);

        // ── CloseBtn (direct child of root so basePanel.Start() finds it) ──
        CreateCloseBtn(root.transform);

        // ── HeaderArea ──
        CreateHeaderArea(root.transform);

        // ── ProgressArea ──
        CreateProgressArea(root.transform);

        // ── ScrollView (fills space between header+progress and bottom buttons) ──
        CreateScrollView(root.transform);

        // ── Bottom buttons ──
        CreateBuyPremiumBtn(root.transform);
        CreateClaimAllBtn(root.transform);

        // ── Save prefab ──
        string path = $"{dir}/seasonPassPanel.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        GameObject.DestroyImmediate(root);

        Debug.Log($"[SeasonPassPanelPrefabBuilder] Prefab created at: {path}");
    }

    // ── Bg ──────────────────────────────────────────────────────────────

    private static void CreateBg(Transform parent)
    {
        GameObject go = new GameObject("Bg", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = BgColor;
        img.raycastTarget = false;
    }

    // ── CloseBtn ────────────────────────────────────────────────────────

    private static void CreateCloseBtn(Transform parent)
    {
        GameObject go = new GameObject("CloseBtn", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(CloseBtnSize, CloseBtnSize);
        rt.anchoredPosition = new Vector2(-BtnMargin, -BtnMargin);

        Image img = go.AddComponent<Image>();
        img.color = CloseBtnColor;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1f);
        colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
        btn.colors = colors;

        // Text child
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        Text txt = textGo.AddComponent<Text>();
        txt.text = "X";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
    }

    // ── HeaderArea ──────────────────────────────────────────────────────

    private static void CreateHeaderArea(Transform parent)
    {
        GameObject go = new GameObject("HeaderArea", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, -HeaderHeight);
        rt.anchoredPosition = Vector2.zero;

        // Header background
        Image bg = go.AddComponent<Image>();
        bg.color = HeaderBgColor;
        bg.raycastTarget = false;

        // UILinearGradient for header area
        var headerGrad = go.AddComponent<UIGradient>();
        headerGrad.horizontal = true;
        headerGrad.color2 = new Color(0.2f, 0.18f, 0.3f);

        // Title text (left half)
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform));
        titleGo.transform.SetParent(go.transform, false);

        RectTransform trt = titleGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(SectionPadding, 0f);
        trt.offsetMax = new Vector2(0f, 0f);

        Text title = titleGo.AddComponent<Text>();
        title.text = "Season Pass";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 32;
        title.alignment = TextAnchor.MiddleLeft;
        title.color = TitleColor;

        // Countdown text (right half)
        GameObject countGo = new GameObject("CountdownText", typeof(RectTransform));
        countGo.transform.SetParent(go.transform, false);

        RectTransform crt = countGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.offsetMin = new Vector2(0f, 0f);
        crt.offsetMax = new Vector2(-SectionPadding, 0f);

        Text countdown = countGo.AddComponent<Text>();
        countdown.text = "12d remaining";
        countdown.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdown.fontSize = 22;
        countdown.alignment = TextAnchor.MiddleRight;
        countdown.color = CountdownColor;
    }

    // ── ProgressArea ────────────────────────────────────────────────────

    private static void CreateProgressArea(Transform parent)
    {
        GameObject go = new GameObject("ProgressArea", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, -ProgressHeight);
        rt.anchoredPosition = new Vector2(0f, -HeaderHeight);

        // Background
        Image bg = go.AddComponent<Image>();
        bg.color = ProgressBgColor;
        bg.raycastTarget = false;

        // Level text (top-left area)
        GameObject lvlGo = new GameObject("LevelText", typeof(RectTransform));
        lvlGo.transform.SetParent(go.transform, false);

        RectTransform lrt = lvlGo.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 0.45f);
        lrt.anchorMax = new Vector2(0.5f, 1f);
        lrt.offsetMin = new Vector2(SectionPadding, 0f);
        lrt.offsetMax = new Vector2(0f, -2f);

        Text levelText = lvlGo.AddComponent<Text>();
        levelText.text = "Level 18";
        levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        levelText.fontSize = 28;
        levelText.alignment = TextAnchor.MiddleLeft;
        levelText.color = LevelColor;

        // Exp text (top-right area)
        GameObject expGo = new GameObject("ExpText", typeof(RectTransform));
        expGo.transform.SetParent(go.transform, false);

        RectTransform ert = expGo.GetComponent<RectTransform>();
        ert.anchorMin = new Vector2(0.5f, 0.45f);
        ert.anchorMax = new Vector2(1f, 1f);
        ert.offsetMin = new Vector2(0f, 0f);
        ert.offsetMax = new Vector2(-SectionPadding, -2f);

        Text expText = expGo.AddComponent<Text>();
        expText.text = "230 / 500 EXP";
        expText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        expText.fontSize = 20;
        expText.alignment = TextAnchor.MiddleRight;
        expText.color = ExpColor;

        // Progress bar background (bottom half)
        GameObject barBgGo = new GameObject("ProgressBarBg", typeof(RectTransform));
        barBgGo.transform.SetParent(go.transform, false);

        RectTransform brt = barBgGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0.4f);
        brt.offsetMin = new Vector2(SectionPadding, 2f);
        brt.offsetMax = new Vector2(-SectionPadding, -2f);

        Image barBg = barBgGo.AddComponent<Image>();
        barBg.color = BarBgColor;
        barBg.raycastTarget = false;

        // Progress bar fill (child of ProgressBarBg)
        GameObject fillGo = new GameObject("ProgressBarFill", typeof(RectTransform));
        fillGo.transform.SetParent(barBgGo.transform, false);

        RectTransform frt = fillGo.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        Image fill = fillGo.AddComponent<Image>();
        fill.color = BarFillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0.36f; // ~36% progress for preview
        fill.raycastTarget = false;
    }

    // ── ScrollView ──────────────────────────────────────────────────────

    private static void CreateScrollView(Transform parent)
    {
        GameObject svGo = new GameObject("ScrollView", typeof(RectTransform));
        svGo.transform.SetParent(parent, false);

        RectTransform svRt = svGo.GetComponent<RectTransform>();
        svRt.anchorMin = new Vector2(0f, 0f);
        svRt.anchorMax = new Vector2(1f, 1f);
        svRt.offsetMin = new Vector2(0f, ScrollBottomMargin);
        svRt.offsetMax = new Vector2(0f, -(HeaderHeight + ProgressHeight));

        // ScrollView background
        Image svBg = svGo.AddComponent<Image>();
        svBg.color = ScrollBgColor;
        svBg.raycastTarget = false;

        ScrollRect scrollRect = svGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.decelerationRate = 0.135f; // default

        // ── Viewport ──
        GameObject vpGo = new GameObject("Viewport", typeof(RectTransform));
        vpGo.transform.SetParent(svGo.transform, false);

        RectTransform vpRt = vpGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(0f, 0f);
        vpRt.offsetMax = new Vector2(-20f, 0f); // room for vertical scrollbar

        Image vpImg = vpGo.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0f);
        vpImg.raycastTarget = false;

        // Standard Mask for viewport clipping
        vpGo.AddComponent<Mask>().showMaskGraphic = false;

        // ── Content ──
        GameObject contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vpGo.transform, false);

        RectTransform crt = contentGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = contentGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(10, 10, 5, 5);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = crt;

        // ── Scrollbar Vertical ──
        GameObject scrollbarGo = new GameObject("ScrollbarVertical", typeof(RectTransform));
        scrollbarGo.transform.SetParent(svGo.transform, false);

        RectTransform sbrt = scrollbarGo.GetComponent<RectTransform>();
        sbrt.anchorMin = new Vector2(1f, 0f);
        sbrt.anchorMax = new Vector2(1f, 1f);
        sbrt.pivot = new Vector2(1f, 0.5f);
        sbrt.sizeDelta = new Vector2(20f, 0f);
        sbrt.anchoredPosition = Vector2.zero;

        Scrollbar scrollbar = scrollbarGo.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.value = 1f;
        scrollbar.size = 0.1f;
        scrollbar.numberOfSteps = 0;

        // SlidingArea
        GameObject slidingGo = new GameObject("SlidingArea", typeof(RectTransform));
        slidingGo.transform.SetParent(scrollbarGo.transform, false);

        RectTransform slidingRt = slidingGo.GetComponent<RectTransform>();
        slidingRt.anchorMin = Vector2.zero;
        slidingRt.anchorMax = Vector2.one;
        slidingRt.offsetMin = Vector2.zero;
        slidingRt.offsetMax = Vector2.zero;

        // Handle
        GameObject handleGo = new GameObject("Handle", typeof(RectTransform));
        handleGo.transform.SetParent(slidingGo.transform, false);

        RectTransform handleRt = handleGo.GetComponent<RectTransform>();
        handleRt.anchorMin = new Vector2(0f, 0f);
        handleRt.anchorMax = new Vector2(1f, 1f);
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;

        Image handleImg = handleGo.AddComponent<Image>();
        handleImg.color = ScrollbarColor;
        handleImg.raycastTarget = true;

        scrollbar.targetGraphic = handleImg;
        scrollbar.handleRect = handleRt;

        // Wire up ScrollRect <-> Scrollbar
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3f;

        // Fix ScrollRect viewport reference
        scrollRect.viewport = vpRt;
    }

    // ── BuyPremiumBtn ───────────────────────────────────────────────────

    private static void CreateBuyPremiumBtn(Transform parent)
    {
        GameObject go = new GameObject("BuyPremiumBtn", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0f);
        rt.anchorMax = new Vector2(0.48f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, BottomButtonHeight);
        rt.anchoredPosition = new Vector2(0f, BtnMargin);

        Image img = go.AddComponent<Image>();
        img.color = BuyBtnColor;

        // Gradient for buy premium button
        var btnGrad = go.AddComponent<UIGradient>();
        btnGrad.horizontal = true;
        btnGrad.color2 = new Color(0.2f, 0.4f, 0.8f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.35f, 0.65f, 0.95f, 1f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
        btn.colors = colors;

        // Text child
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);

        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        Text txt = textGo.AddComponent<Text>();
        txt.text = "Buy Premium Pass - 800 Gems";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = ButtonTextColor;
    }

    // ── ClaimAllBtn ─────────────────────────────────────────────────────

    private static void CreateClaimAllBtn(Transform parent)
    {
        GameObject go = new GameObject("ClaimAllBtn", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.52f, 0f);
        rt.anchorMax = new Vector2(0.95f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, BottomButtonHeight);
        rt.anchoredPosition = new Vector2(0f, BtnMargin);

        Image img = go.AddComponent<Image>();
        img.color = ClaimBtnColor;

        // Gradient for claim all button
        var claimGrad = go.AddComponent<UIGradient>();
        claimGrad.horizontal = true;
        claimGrad.color2 = new Color(0.1f, 0.7f, 0.35f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.9f, 0.5f, 1f);
        colors.pressedColor = new Color(0.12f, 0.6f, 0.3f, 1f);
        btn.colors = colors;

        // Text child
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);

        RectTransform trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        Text txt = textGo.AddComponent<Text>();
        txt.text = "Claim All Rewards";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = ButtonTextColor;
    }
}
