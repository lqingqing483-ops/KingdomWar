using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.HotUpdate;
using DG.Tweening;
using KingdomWar.Game.Cards;
namespace KingdomWar.UI
{
/// <summary>
/// 抽奖面板UI控制�?/// 负责管理抽奖界面的显示、交互和动画效果
/// 继承自basePanel，使用DOTween实现动画效果
/// </summary>
public class lotteryPanel : basePanel
{
    #region UI组件引用
    
    /// <summary>
    /// 关闭按钮
    /// </summary>
    public Button closeButton;
    
    /// <summary>
    /// 付费抽奖按钮
    /// </summary>
    public Button drawButton;
    
    /// <summary>
    /// 免费抽奖按钮
    /// </summary>
    public Button freeDrawButton;
    
    /// <summary>
    /// 显示剩余抽奖次数的文�?    /// </summary>
    public Text drawCountText;
    
    /// <summary>
    /// 显示玩家金币数量的文�?    /// </summary>
    public Text goldText;
    
    /// <summary>
    /// 卡片展示区域容器
    /// </summary>
    public Transform cardDisplay;
    
    /// <summary>
    /// 结果卡片图片显示
    /// </summary>
    public Image cardDisplayImage;
    
    /// <summary>
    /// 结果卡片名称文本
    /// </summary>
    public Text resultCardName;
    
    /// <summary>
    /// 结果卡片稀有度文本
    /// </summary>
    public Text resultCardRarity;
    
    /// <summary>
    /// 抽奖结果面板（显示抽到的卡片�?    /// </summary>
    private GameObject resultPanel;
    
    /// <summary>
    /// 确认结果按钮
    /// </summary>
    public Button confirmResultButton;
    
    /// <summary>
    /// 加载遮罩�?    /// </summary>
    private GameObject loadingOverlay;
    
    #endregion

    #region 动画配置
    
    /// <summary>
    /// 卡片揭示延迟时间（秒�?    /// </summary>
    [Header("抽奖动画")]
    public float cardRevealDelay = 0.5f;
    
    /// <summary>
    /// 卡片揭示动画持续时间（秒�?    /// </summary>
    public float cardRevealDuration = 1f;
    
    /// <summary>
    /// 卡片揭示动画缓动类型
    /// </summary>
    public Ease cardRevealEase = Ease.OutBack;
    
    #endregion

    #region 稀有度颜色配置
    
    /// <summary>
    /// 普通卡片颜�?    /// </summary>
    [Header("稀有度颜色")]
    public Color commonColor = Color.white;
    
    /// <summary>
    /// 稀有卡片颜色（蓝色�?    /// </summary>
    public Color rareColor = Color.blue;
    
    /// <summary>
    /// 史诗卡片颜色（紫色）
    /// </summary>
    public Color epicColor = new Color(0.6f, 0.2f, 0.8f);
    
    /// <summary>
    /// 传说卡片颜色（金色）
    /// </summary>
    public Color legendaryColor = new Color(1f, 0.8f, 0f);
    
    #endregion

    #region 私有字段
    
    /// <summary>
    /// 默认卡片精灵图片
    /// </summary>
    private Sprite defaultCardSprite;
    
    /// <summary>
    /// 是否正在抽奖�?    /// </summary>
    private bool isDrawing = false;
    
    #endregion

    #region 生命周期方法
    
    /// <summary>
    /// Unity Awake方法
    /// 初始化UI组件
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Debug.Log("lotteryPanel: Awake() called");
        InitializeUI();
    }

    /// <summary>
    /// Unity Start方法
    /// 设置事件监听器并更新UI显示
    /// </summary>
    protected override void Start()
    {
        base.Start();
        SetupEventListeners();
        UpdateUI();
        InitializeSystems();
    }
    
    #endregion

    #region 初始化方�?    
    /// <summary>
    /// 初始化UI组件状�?    /// 设置默认值和初始显示
    /// </summary>
    private void InitializeUI()
    {
        Debug.Log("lotteryPanel: InitializeUI() called");
        // 动态查找UI组件
        FindUIComponents();
        

        
        // 默认隐藏结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        
        // 默认隐藏加载遮罩
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }

        // 加载默认卡片图片
        defaultCardSprite = Resources.Load<Sprite>("UI/cards/knight");
    }

    /// <summary>
    /// 动态查找UI组件
    /// 使用transform.Find方法根据名称查找组件
    /// 仅在组件为空时才进行查找
    /// </summary>
    private void FindUIComponents()
    {
        Debug.Log("lotteryPanel: FindUIComponents() called");
        // 查找按钮组件
        if (closeButton == null) closeButton = FindComponent<Button>("CloseBtn");
        if (drawButton == null) drawButton = FindComponent<Button>("DrawButton");
        if (freeDrawButton == null) freeDrawButton = FindComponent<Button>("FreeDrawButton");
        if (confirmResultButton == null) confirmResultButton = FindComponent<Button>("ConfirmResultButton");
        
        // 查找文本组件
        if (drawCountText == null) drawCountText = FindComponent<Text>("DrawCountText");
        if (goldText == null) goldText = FindComponent<Text>("GoldText");
        if (resultCardName == null) resultCardName = FindComponent<Text>("ResultCardName");
        if (resultCardRarity == null) resultCardRarity = FindComponent<Text>("ResultCardRarity");
        
        // find image and other components
        if (cardDisplayImage == null) cardDisplayImage = FindComponent<Image>("CardDisplay");
        if (cardDisplay == null) cardDisplay = FindTransform("CardDisplay");
        if (resultPanel == null) resultPanel = FindGameObject("ResultPanel");
        if (loadingOverlay == null) loadingOverlay = FindGameObject("LoadingOverlay");
        
        // 验证所有组件是否找�?        ValidateComponents();
    }

    /// <summary>
    /// 查找指定名称的组�?    /// </summary>
    /// <typeparam name="T">组件类型</typeparam>
    /// <param name="name">组件名称</param>
    /// <returns>找到的组件，未找到返回null</returns>
    private T FindComponent<T>(string name) where T : Component
    {
        Transform foundTransform = transform.Find(name);
        if (foundTransform != null)
        {
            return foundTransform.GetComponent<T>();
        }
        return null;
    }

    /// <summary>
    /// 查找指定名称的Transform
    /// </summary>
    /// <param name="name">Transform名称</param>
    /// <returns>找到的Transform，未找到返回null</returns>
    private Transform FindTransform(string name)
    {
        return transform.Find(name);
    }

    /// <summary>
    /// 查找指定名称的GameObject
    /// </summary>
    /// <param name="name">GameObject名称</param>
    /// <returns>找到的GameObject，未找到返回null</returns>
    private GameObject FindGameObject(string name)
    {
        Transform foundTransform = transform.Find(name);
        if (foundTransform != null)
        {
            return foundTransform.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 验证所有UI组件是否成功找到
    /// 打印未找到的组件名称
    /// </summary>
    private void ValidateComponents()
    {
        Debug.Log("lotteryPanel: ValidateComponents() called");
        List<string> missingComponents = new List<string>();
        
        Debug.Log($"lotteryPanel: closeButton found: {closeButton != null}");
        Debug.Log($"lotteryPanel: drawButton found: {drawButton != null}");
        Debug.Log($"lotteryPanel: freeDrawButton found: {freeDrawButton != null}");
        Debug.Log($"lotteryPanel: confirmResultButton found: {confirmResultButton != null}");
        Debug.Log($"lotteryPanel: drawCountText found: {drawCountText != null}");
        Debug.Log($"lotteryPanel: goldText found: {goldText != null}");
        Debug.Log($"lotteryPanel: resultCardName found: {resultCardName != null}");
        Debug.Log($"lotteryPanel: resultCardRarity found: {resultCardRarity != null}");
        Debug.Log($"lotteryPanel: cardDisplayImage found: {cardDisplayImage != null}");
        Debug.Log($"lotteryPanel: cardDisplay found: {cardDisplay != null}");
        Debug.Log($"lotteryPanel: resultPanel found: {resultPanel != null}");
        Debug.Log($"lotteryPanel: loadingOverlay found: {loadingOverlay != null}");
        
        if (closeButton == null) missingComponents.Add("CloseBtn");
        if (drawButton == null) missingComponents.Add("DrawButton");
        if (freeDrawButton == null) missingComponents.Add("FreeDrawButton");
        if (confirmResultButton == null) missingComponents.Add("ConfirmResultButton");
        if (drawCountText == null) missingComponents.Add("DrawCountText");
        if (goldText == null) missingComponents.Add("GoldText");
        if (resultCardName == null) missingComponents.Add("ResultCardName");
        if (resultCardRarity == null) missingComponents.Add("ResultCardRarity");
        if (cardDisplayImage == null) missingComponents.Add("CardDisplay");
        if (cardDisplay == null) missingComponents.Add("CardDisplay");
        if (resultPanel == null) missingComponents.Add("ResultPanel");
        if (loadingOverlay == null) missingComponents.Add("LoadingOverlay");
        
        Debug.Log($"lotteryPanel: Missing components count: {missingComponents.Count}");
        
        if (missingComponents.Count > 0)
        {
            string missingList = string.Join(", ", missingComponents);
            Debug.LogWarning($"lotteryPanel: Missing components: {missingList}");
        }
        else
        {
            Debug.Log("lotteryPanel: All UI components found successfully");
        }
    }

    /// <summary>
    /// 初始化系统引�?    /// 订阅抽奖系统和玩家数据系统的事件
    /// </summary>
    private void InitializeSystems()
    {
        // 获取系统实例并订阅事�?
        var lotterySystem = LotterySystem.Instance;
        var playerData = PlayerDataManager.Instance;
        
        // 订阅抽奖次数变化事件
        lotterySystem.OnDrawCountChanged += OnDrawCountChanged;
        // 订阅卡片获得事件
        lotterySystem.OnCardObtained += OnCardObtained;
        // 订阅金币变化事件
        playerData.OnGoldChanged += OnGoldChanged;
    }

    /// <summary>
    /// 设置UI事件监听�?    /// 为各个按钮绑定点击事�?    /// </summary>
    private void SetupEventListeners()
    {
        // 关闭按钮点击事件
         if (closeButton != null)
         {
             closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // 付费抽奖按钮点击事件
        if (drawButton != null)
        {
            drawButton.onClick.AddListener(OnDrawButtonClicked);
        }

        // 免费抽奖按钮点击事件
        if (freeDrawButton != null)
        {
            freeDrawButton.onClick.AddListener(OnFreeDrawButtonClicked);
        }

        // 确认结果按钮点击事件
        if (confirmResultButton != null)
        {
            confirmResultButton.onClick.AddListener(OnConfirmResultButtonClicked);
        }
    }
    
    #endregion

    #region UI更新方法
    
    /// <summary>
    /// 更新所有UI显示
    /// </summary>
    private void UpdateUI()
    {
        UpdateDrawCountDisplay();
        UpdateGoldDisplay();
        UpdateButtonStates();
    }

    /// <summary>
    /// 更新抽奖次数显示
    /// </summary>
    private void UpdateDrawCountDisplay()
    {
        if (drawCountText != null)
        {
            int freeDraws = LotterySystem.Instance.GetRemainingFreeDraws();
            drawCountText.text = $"Free draws: {freeDraws}";
        }
    }

    /// <summary>
    /// 更新金币显示
    /// </summary>
    private void UpdateGoldDisplay()
    {
        if (goldText != null)
        {
            goldText.text = $"金币: {PlayerDataManager.Instance.GetGold()}";
        }
    }

    /// <summary>
    /// 更新按钮状�?    /// 根据抽奖条件启用或禁用按�?    /// </summary>
    private void UpdateButtonStates()
    {
        // 检查是否可以免费抽奖和付费抽奖
        bool canFreeDraw = LotterySystem.Instance.IsFreeDrawAvailable();
        bool canPaidDraw = LotterySystem.Instance.HasEnoughCurrency();
        
        // update free draw button state
        if (freeDrawButton != null)
        {
            freeDrawButton.interactable = canFreeDraw && !isDrawing;
            freeDrawButton.gameObject.SetActive(canFreeDraw);
        }

        // 更新付费抽奖按钮状�?        // if (drawButton != null)
        // {
        //     drawButton.interactable = canPaidDraw && !isDrawing;
        //     drawButton.gameObject.SetActive(!canFreeDraw);
        // }
    }
    
    #endregion

    #region 按钮事件处理
    
    /// <summary>
    /// 关闭按钮点击处理
    /// 关闭抽奖面板
    /// </summary>
     private void OnCloseButtonClicked()
     {
         Debug.Log("关闭抽奖页面");
         UIManager.Instance.PopPanel();
     }

    /// <summary>
    /// 付费抽奖按钮点击处理
    /// 执行付费抽奖流程
    /// </summary>
    private void OnDrawButtonClicked()
    {
        if (!isDrawing)
        {
            StartCoroutine(PerformDraw(false));
        }
    }

    /// <summary>
    /// 免费抽奖按钮点击处理
    /// 执行免费抽奖流程
    /// </summary>
    private void OnFreeDrawButtonClicked()
    {
        if (!isDrawing)
        {
            StartCoroutine(PerformDraw(true));
        }
    }

    /// <summary>
    /// 确认结果按钮点击处理
    /// 关闭结果面板并更新UI
    /// </summary>
    private void OnConfirmResultButtonClicked()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }
        UpdateUI();
    }
    
    #endregion

    #region 抽奖流程
    
    /// <summary>
    /// 执行抽奖流程
    /// 协程方法，包含加载动画和结果展示
    /// </summary>
    /// <param name="isFreeDraw">是否为免费抽�?/param>
    /// <returns>协程迭代�?/returns>
    private IEnumerator PerformDraw(bool isFreeDraw)
    {
        // 设置抽奖中状�?        isDrawing = true;
        UpdateButtonStates();

        // 显示加载遮罩
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(true);
        }

        // 等待一小段时间显示加载效果
        yield return new WaitForSeconds(0.5f);

        // 执行抽奖
        LotteryResult result = LotterySystem.Instance.Draw();

        // 隐藏加载遮罩
        if (loadingOverlay != null)
        {
            loadingOverlay.SetActive(false);
        }

        if (result.success)
        {
            // draw success, show card reveal animation
            yield return ShowCardRevealAnimation(result.card);
        }
        else
        {
            // 抽奖失败，显示提示消�?            UIManager.Instance.CreatepromptMessage(result.message);
            isDrawing = false;
            UpdateButtonStates();
        }
    }

    /// <summary>
    /// 显示卡片揭示动画
    /// 使用DOTween实现卡片缩放动画效果
    /// </summary>
    /// <param name="card">抽到的卡片数�?/param>
    /// <returns>协程迭代�?/returns>
    private IEnumerator ShowCardRevealAnimation(CardData card)
    {
        // 显示结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

        // set card image and play scale animation
        if (cardDisplayImage != null)
        {
            cardDisplayImage.sprite = card.cardIcon != null ? card.cardIcon : defaultCardSprite;
            cardDisplayImage.transform.localScale = Vector3.zero;
            cardDisplayImage.transform.DOScale(1f, cardRevealDuration)
                .SetEase(cardRevealEase);
        }

        // set card name and color
        if (resultCardName != null)
        {
            resultCardName.text = card.cardName;
            resultCardName.color = GetRarityColor(card.rarity);
        }

        // set rarity text and color
        if (resultCardRarity != null)
        {
            string rarityText = GetRarityText(card.rarity);
            resultCardRarity.text = rarityText;
            resultCardRarity.color = GetRarityColor(card.rarity);
        }

        // 等待动画完成
        yield return new WaitForSeconds(cardRevealDuration);
        
        // 重置抽奖状�?        isDrawing = false;
        UpdateUI();
    }
    
    #endregion

    #region 辅助方法
    
    /// <summary>
    /// 根据稀有度获取对应颜色
    /// </summary>
    /// <param name="rarity">稀有度等级 (1-4)</param>
    /// <returns>对应的颜�?/returns>
    private Color GetRarityColor(int rarity)
    {
        switch (rarity)
        {
            case 1: return commonColor;      // 普�?- 白色
            case 2: return rareColor;        // 稀�?- 蓝色
            case 3: return epicColor;        // 史诗 - 紫色
            case 4: return legendaryColor;   // 传说 - 金色
            default: return commonColor;
        }
    }

    /// <summary>
    /// 根据稀有度获取对应文本
    /// </summary>
    /// <param name="rarity">稀有度等级 (1-4)</param>
    /// <returns>稀有度文本</returns>
    private string GetRarityText(int rarity)
    {
        switch (rarity)
        {
            case 1: return "Common";
            case 2: return "Rare";
            case 3: return "史诗";
            case 4: return "传说";
            default: return "Common";
        }
    }
    
    #endregion

    #region 事件回调
    
    /// <summary>
    /// 抽奖次数变化回调
    /// 更新UI显示
    /// </summary>
    /// <param name="count">新的抽奖次数</param>
    private void OnDrawCountChanged(int count)
    {
        UpdateDrawCountDisplay();
    }

    /// <summary>
    /// 金币变化回调
    /// 更新金币显示和按钮状�?    /// </summary>
    /// <param name="gold">新的金币数量</param>
    private void OnGoldChanged(int gold)
    {
        UpdateGoldDisplay();
        UpdateButtonStates();
    }

    /// <summary>
    /// 卡片获得回调
    /// 记录获得的卡�?    /// </summary>
    /// <param name="card">获得的卡片数�?/param>
    private void OnCardObtained(CardData card)
    {
        Debug.Log($"Card obtained: {card.cardName}");
    }
    
    #endregion

    #region 清理方法
    
    /// <summary>
    /// Unity OnDestroy方法
    /// 取消事件订阅，防止内存泄�?    /// </summary>
    private void OnDestroy()
    {
        // 取消抽奖系统事件订阅
        if (LotterySystem.Instance != null)
        {
            LotterySystem.Instance.OnDrawCountChanged -= OnDrawCountChanged;
            LotterySystem.Instance.OnCardObtained -= OnCardObtained;
        }
        
        // 取消玩家数据系统事件订阅
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged -= OnGoldChanged;
        }
    }
    
    #endregion
}

}
