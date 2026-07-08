using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using KingdomWar.Game.Cards;
using KingdomWar.Game.Decks;
namespace KingdomWar.UI
{
/// <summary>
/// 卡组选择面板�?
/// 实现皇室战争风格的卡组选择UI，支持卡牌的添加、移除、拖动排序和卡组管理
/// </summary>
public class deckPanel : basePanel, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region 序列化字�?
    
    [Header("卡组配置")]
    public int maxCardsInDeck = 8;              // 卡组最大卡牌数量（皇室战争标准�?张）
    public Transform deckSlotsContainer;         // 卡组槽位容器
    public GameObject cardSlotPrefab;            // 卡组槽位预制�?
    
    [Header("Card Library Config")]
    public Transform cardLibraryContainer;       // 卡牌库容�?
    public GameObject cardLibraryItemPrefab;     // 卡牌库项目预制体
    public ScrollRect cardLibraryScrollView;     // 卡牌库滚动视�?
    
    [Header("统计信息")]
    public Text avgElixirText;                   // 平均圣水消耗文�?
    public Text cardCountText;                   // 卡牌数量文本
    public Text unitCountText;                   // 单位卡牌数量文本
    public Text spellCountText;                  // 法术卡牌数量文本
    public Text buildingCountText;               // 建筑卡牌数量文本
    public Text rarityDistributionText;          // 稀有度分布文本
    
    [Header("卡组管理")]
    public Button saveDeckButton;                // 保存卡组按钮
    public Button loadDeckButton;                // 加载卡组按钮
    public InputField deckNameInput;             // 卡组名称输入�?
    
    [Header("拖动配置")]
    public GameObject dragCardPrefab;            // 拖动卡牌预览预制�?
    
    #endregion
    
    #region 私有字段
    
    private List<GameObject> deckSlots = new List<GameObject>();     // 卡组槽位游戏对象列表
    private List<CardData> currentDeck = new List<CardData>();       // 当前卡组的卡牌数据列�?
    private List<CardData> cardLibrary = new List<CardData>();       // 卡牌库数据列�?
    
    private GameObject draggingCard;             // 当前正在拖动的卡牌预�?
    private CardData draggedCardData;            // 正在拖动的卡牌数�?
    private int draggedDeckIndex = -1;           // 正在拖动的卡组槽位索�?
    private int draggedLibraryIndex = -1;        // 正在拖动的卡牌库索引
    private Vector3 dragOffset;                  // 拖动偏移�?
    private RectTransform canvasRect;            // 画布矩形变换
    
    #endregion
    
    #region 生命周期方法
    
    /// <summary>
    /// 唤醒方法，初始化画布矩形
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        
        
    }
    
    /// <summary>
    /// 开始方法，初始化面板组�?
    /// </summary>
    protected override void Start()
    {
        base.Start();

        // 获取画布矩形变换，用于计算拖动位�?
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        
        InitializeDeckSlots();     // 初始化卡组槽�?
        InitializeCardLibrary();   // 初始化卡牌库
        InitializeButtons();       // 初始化按钮事�?
        UpdateDeckStats();         // 更新卡组统计信息
    }
    
    #endregion
    
    #region 初始化方�?
    
    /// <summary>
    /// 初始化卡组槽�?
    /// </summary>
    private void InitializeDeckSlots()
    {
        // 检查必要的引用是否存在
        if (deckSlotsContainer == null || cardSlotPrefab == null)
        {
            Debug.LogError("Deck slots container or card slot prefab not assigned!");
            return;
        }
        
        // 创建指定数量的卡组槽�?
        for (int i = 0; i < maxCardsInDeck; i++)
        {
            // 实例化槽位预制体
            GameObject slot = Instantiate(cardSlotPrefab, deckSlotsContainer);
            slot.name = "CardSlot_" + i;
            
            // 添加点击事件监听
            Button slotButton = slot.GetComponent<Button>();
            if (slotButton != null)
            {
                int slotIndex = i; // 闭包捕获变量
                slotButton.onClick.AddListener(() => OnDeckSlotClicked(slotIndex));
            }
            
            // 添加到槽位列�?
            deckSlots.Add(slot);
        }
        UpdateDeckSlotsUI(); // 更新UI
    }
    
    /// <summary>
    /// 初始化卡牌库
    /// </summary>
    private void InitializeCardLibrary()
    {
        // 检查必要的引用是否存在
        if (cardLibraryContainer == null || cardLibraryItemPrefab == null)
        {
            Debug.LogError("Card library container or card library item prefab not assigned!");
            return;
        }
        
        // 优化卡牌库滚动性能
        if (cardLibraryScrollView != null)
        {
            cardLibraryScrollView.movementType = ScrollRect.MovementType.Elastic;
            cardLibraryScrollView.inertia = true;
            cardLibraryScrollView.decelerationRate = 0.135f;
            // 确保Viewport有Mask组件来裁剪不可见区域
            if (cardLibraryScrollView.viewport != null)
            {
                var mask = cardLibraryScrollView.viewport.GetComponent<RectMask2D>();
                if (mask == null)
                    cardLibraryScrollView.viewport.gameObject.AddComponent<RectMask2D>();
            }
        }
        
        // 从CardDatabase获取所有卡牌数�?
        cardLibrary = CardDatabase.Instance.GetAllCards();
        
        // 为每张卡牌创建库项目
        foreach (CardData cardData in cardLibrary)
        {
            // 实例化库项目预制�?
            GameObject libraryItem = Instantiate(cardLibraryItemPrefab, cardLibraryContainer);
            libraryItem.name = "Card_" + cardData.cardName;
            
            // 设置卡牌信息
            Image cardIcon = libraryItem.transform.Find("CardIcon").GetComponent<Image>();
            Text cardNameText = libraryItem.transform.Find("CardName").GetComponent<Text>();
            Text elixirCostText = libraryItem.transform.Find("ElixirCost").GetComponent<Text>();
            
            // 更新UI元素
            if (cardIcon != null && cardData.cardIcon != null)
                cardIcon.sprite = cardData.cardIcon;
            if (cardNameText != null)
                cardNameText.text = cardData.cardName;
            if (elixirCostText != null)
                elixirCostText.text = cardData.elixirCost.ToString();
            
            // 添加点击事件监听
            Button libraryButton = libraryItem.GetComponent<Button>();
            if (libraryButton != null)
            {
                CardData currentCard = cardData; // 闭包捕获变量
                libraryButton.onClick.AddListener(() => {
                    libraryButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 0.5f);
                    OnLibraryCardClicked(currentCard);
                });
            }
        }
    }
    
    /// <summary>
    /// 初始化按钮事�?
    /// </summary>
    private void InitializeButtons()
    {
        // 保存卡组按钮
        if (saveDeckButton != null)
        {
            saveDeckButton.onClick.AddListener(OnSaveDeckClicked);
        }
        
        // 加载卡组按钮
        if (loadDeckButton != null)
        {
            loadDeckButton.onClick.AddListener(OnLoadDeckClicked);
        }
    }
    
    #endregion
    
    #region 事件处理方法
    
    /// <summary>
    /// 卡组槽位点击事件
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    private void OnDeckSlotClicked(int slotIndex)
    {
        // 检查索引是否有�?
        if (slotIndex >= 0 && slotIndex < currentDeck.Count)
        {
            // 移除该槽位的卡牌
            currentDeck.RemoveAt(slotIndex);
            UpdateDeckSlotsUI(); // 更新UI
            UpdateDeckStats();   // 更新统计信息
        }
    }
    
    /// <summary>
    /// 卡牌库项目点击事�?
    /// </summary>
    /// <param name="cardData">卡牌数据</param>
    private void OnLibraryCardClicked(CardData cardData)
    {
        // 检查卡组是否已�?
        if (currentDeck.Count >= maxCardsInDeck)
        {
            Debug.Log("Deck is full! Maximum 8 cards allowed.");
            return;
        }
        
        // 检查卡牌是否已在卡组中
        if (currentDeck.Contains(cardData))
        {
            Debug.Log("Card already in deck!");
            return;
        }
        
        // 添加卡牌到卡�?
        currentDeck.Add(cardData);
        UpdateDeckSlotsUI(); // 更新UI
        UpdateDeckStats();   // 更新统计信息
    }
    
    /// <summary>
    /// 保存卡组按钮点击事件
    /// </summary>
    private void OnSaveDeckClicked()
    {
        // 获取卡组名称
        string deckName = deckNameInput != null && !string.IsNullOrEmpty(deckNameInput.text) ? deckNameInput.text : "New Deck";
        
        // 检查卡组是否为�?
        if (currentDeck.Count == 0)
        {
            Debug.LogError("Cannot save empty deck");
            return;
        }
        
        // 保存卡组逻辑
        DeckManager.Instance.SaveDeck(deckName, currentDeck);
        
        Debug.Log("Deck saved as: " + deckName);
        // 显示保存成功提示
    }
    
    /// <summary>
    /// 加载卡组按钮点击事件
    /// </summary>
    private void OnLoadDeckClicked()
    {
        // 获取卡组名称
        string deckName = deckNameInput != null && !string.IsNullOrEmpty(deckNameInput.text) ? deckNameInput.text : "New Deck";
        
        // 加载卡组逻辑
        List<CardData> loadedDeck = DeckManager.Instance.LoadDeck(deckName);
        if (loadedDeck != null)
        {
            currentDeck = loadedDeck;
            UpdateDeckSlotsUI(); // 更新UI
            UpdateDeckStats();   // 更新统计信息
        }
        
        Debug.Log("Attempting to load deck: " + deckName);
        // 显示加载结果提示
    }
    
    #endregion
    
    #region UI更新方法
    
    /// <summary>
    /// 更新卡组槽位UI
    /// </summary>
    private void UpdateDeckSlotsUI()
    {
        // 遍历所有槽�?
        for (int i = 0; i < deckSlots.Count; i++)
        {
            GameObject slot = deckSlots[i];
            Image cardIcon = slot.transform.Find("CardIcon").GetComponent<Image>();
            Text elixirCostText = slot.transform.Find("ElixirCost").GetComponent<Text>();
            Text cardName = slot.transform.Find("CardName").GetComponent<Text>();
            
            // 检查是否有卡牌
            if (i < currentDeck.Count)
            {
                CardData cardData = currentDeck[i];
                // 更新卡牌信息
                if (cardIcon != null && cardData.cardIcon != null)
                    cardIcon.sprite = cardData.cardIcon;
                if (elixirCostText != null)
                    elixirCostText.text = cardData.elixirCost.ToString();
                if(cardName!=null)
                    cardName.text = cardData.cardName;
                
                // 显示UI元素
                cardIcon.gameObject.SetActive(true);
                elixirCostText.gameObject.SetActive(true);
                if (cardName != null)
                    cardName.gameObject.SetActive(true);
            }
            else
            {
                // 隐藏空槽位的UI元素
                if (cardIcon != null)
                    cardIcon.gameObject.SetActive(false);
                if (elixirCostText != null)
                    elixirCostText.gameObject.SetActive(false);
                if (cardName != null)
                {
                    cardName.text = "";
                    cardName.gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// 更新卡组统计信息
    /// </summary>
    private void UpdateDeckStats()
    {
        // 更新平均圣水消�?
        if (avgElixirText != null)
        {
            float totalElixir = 0;
            foreach (CardData card in currentDeck)
            {
                totalElixir += card.elixirCost;
            }
            
            float avgElixir = currentDeck.Count > 0 ? totalElixir / currentDeck.Count : 0;
            avgElixirText.text = " " + avgElixir.ToString("F1");
        }
        
        // 更新卡牌数量
        if (cardCountText != null)
        {
            cardCountText.text = "卡牌数量: " + currentDeck.Count + "/" + maxCardsInDeck;
        }
        
        // 计算卡牌类型分布
        int unitCount = 0;
        int spellCount = 0;
        int buildingCount = 0;
        
        foreach (CardData card in currentDeck)
        {
            switch (card.cardType)
            {
                case CardType.Unit:
                    unitCount++;
                    break;
                case CardType.Spell:
                    spellCount++;
                    break;
                case CardType.Building:
                    buildingCount++;
                    break;
            }
        }
        
        // 更新类型分布
        if (unitCountText != null)
        {
            unitCountText.text = "单位: " + unitCount;
        }
        
        if (spellCountText != null)
        {
            spellCountText.text = "法术: " + spellCount;
        }
        
        if (buildingCountText != null)
        {
            buildingCountText.text = "建筑: " + buildingCount;
        }
        
        // 计算稀有度分布
        if (rarityDistributionText != null)
        {
            Dictionary<int, int> rarityCount = new Dictionary<int, int>();
            
            foreach (CardData card in currentDeck)
            {
                if (!rarityCount.ContainsKey(card.rarity))
                {
                    rarityCount[card.rarity] = 0;
                }
                rarityCount[card.rarity]++;
            }
            
            string rarityText = "稀有度: ";
            foreach (var pair in rarityCount)
            {
                rarityText += GetRarityName(pair.Key) + "(" + pair.Value + ") ";
            }
            
            rarityDistributionText.text = rarityText;
        }
    }
    
    /// <summary>
    /// 获取稀有度名称
    /// </summary>
    /// <param name="rarity">稀有度�?/param>
    /// <returns>稀有度名称</returns>
    private string GetRarityName(int rarity)
    {
        switch (rarity)
        {
            case 1:
                return "Common";
            case 2:
                return "Rare";
            case 3:
                return "史诗";
            case 4:
                return "传奇";
            default:
                return "未知";
        }
    }
    
    #endregion
    
    #region 卡组操作方法
    
    /// <summary>
    /// 添加卡牌到卡�?
    /// </summary>
    /// <param name="cardData">卡牌数据</param>
    public void AddCardToDeck(CardData cardData)
    {
        // 检查卡组是否已�?
        if (currentDeck.Count >= maxCardsInDeck)
        {
            Debug.Log("Deck is full! Maximum 8 cards allowed.");
            return;
        }
        
        // 检查卡牌是否已在卡组中
        if (currentDeck.Contains(cardData))
        {
            Debug.Log("Card already in deck!");
            return;
        }
        
        // 添加卡牌
        currentDeck.Add(cardData);
        UpdateDeckSlotsUI(); // 更新UI
        UpdateDeckStats();   // 更新统计信息
    }
    
    /// <summary>
    /// 从卡组移除卡�?
    /// </summary>
    /// <param name="cardData">卡牌数据</param>
    public void RemoveCardFromDeck(CardData cardData)
    {
        // 检查卡牌是否在卡组�?
        if (currentDeck.Contains(cardData))
        {
            // 移除卡牌
            currentDeck.Remove(cardData);
            UpdateDeckSlotsUI(); // 更新UI
            UpdateDeckStats();   // 更新统计信息
        }
    }
    
    /// <summary>
    /// 获取当前卡组
    /// </summary>
    /// <returns>当前卡组卡牌列表</returns>
    public List<CardData> GetCurrentDeck()
    {
        return currentDeck;
    }
    
    #endregion
    
    #region 拖动事件处理
    
    /// <summary>
    /// 开始拖动事�?
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        
        // 检查是否是卡组槽位中的卡牌
        for (int i = 0; i < deckSlots.Count; i++)
        {
            if (deckSlots[i] == draggedObject && i < currentDeck.Count)
            {
                draggedDeckIndex = i;
                draggedCardData = currentDeck[i];
                StartDraggingCard(eventData);
                return;
            }
        }
        
        // 检查是否是卡牌库中的卡�?
        for (int i = 0; i < cardLibrary.Count; i++)
        {
            if (draggedObject.name == "Card_" + cardLibrary[i].cardName)
            {
                draggedLibraryIndex = i;
                draggedCardData = cardLibrary[i];
                StartDraggingCard(eventData);
                return;
            }
        }
    }
    
    /// <summary>
    /// 拖动中事�?
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (draggingCard != null)
        {
            Vector3 pos;
            // 计算世界位置
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out pos))
            {
                // 更新拖动卡牌位置
                draggingCard.transform.position = pos + dragOffset;
            }
        }
    }
    
    /// <summary>
    /// 结束拖动事件
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingCard != null)
        {
            // 销毁拖动预�?
            Destroy(draggingCard);
            draggingCard = null;
            
            // 检查是否拖动到了卡组槽�?
            GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
            if (dropTarget != null)
            {
                // 检查是否是卡组槽位
                for (int i = 0; i < deckSlots.Count; i++)
                {
                    if (deckSlots[i] == dropTarget || deckSlots[i].transform.IsChildOf(dropTarget.transform))
                    {
                        HandleDropOnDeckSlot(i);
                        ResetDragState();
                        return;
                    }
                }
            }
            
            // 检查是否拖动出了面板（移除卡牌�?
            if (draggedDeckIndex != -1)
            {
                // 检查是否拖动到了面板外
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                    GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
                {
                    // 从卡组中移除卡牌
                    currentDeck.RemoveAt(draggedDeckIndex);
                    UpdateDeckSlotsUI(); // 更新UI
                    UpdateDeckStats();   // 更新统计信息
                }
            }
            
            ResetDragState();
        }
    }
    
    /// <summary>
    /// 重置拖动状�?
    /// </summary>
    private void ResetDragState()
    {
        draggedDeckIndex = -1;
        draggedLibraryIndex = -1;
        draggedCardData = null;
    }
    
    /// <summary>
    /// 开始拖动卡�?
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    private void StartDraggingCard(PointerEventData eventData)
    {
        if (dragCardPrefab == null || draggedCardData == null)
            return;
        
        // 创建拖动的卡牌预�?
        draggingCard = Instantiate(dragCardPrefab, transform.parent);
        draggingCard.transform.SetAsLastSibling();
        
        // 设置卡牌信息
        Image cardIcon = draggingCard.transform.Find("CardIcon").GetComponent<Image>();
        Text elixirCostText = draggingCard.transform.Find("ElixirCost").GetComponent<Text>();
        
        if (cardIcon != null && draggedCardData.cardIcon != null)
            cardIcon.sprite = draggedCardData.cardIcon;
        if (elixirCostText != null)
            elixirCostText.text = draggedCardData.elixirCost.ToString();
        
        // 计算拖动偏移
        RectTransform rectTransform = draggingCard.GetComponent<RectTransform>();
        Vector2 localMousePos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out localMousePos))
        {
            dragOffset = (Vector3)(rectTransform.anchoredPosition - localMousePos);
        }
        
        // 设置初始位置
        Vector3 pos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out pos))
        {
            draggingCard.transform.position = pos + dragOffset;
        }
    }
    
    /// <summary>
    /// 处理拖动到卡组槽�?
    /// </summary>
    /// <param name="slotIndex">槽位索引</param>
    private void HandleDropOnDeckSlot(int slotIndex)
    {
        if (draggedDeckIndex != -1)
        {
            // 从卡组槽位拖动到另一个槽位（调整顺序�?
            if (draggedDeckIndex != slotIndex)
            {
                CardData card = currentDeck[draggedDeckIndex];
                currentDeck.RemoveAt(draggedDeckIndex);
                
                if (slotIndex < currentDeck.Count)
                {
                    currentDeck.Insert(slotIndex, card);
                }
                else
                {
                    currentDeck.Add(card);
                }
                
                UpdateDeckSlotsUI(); // 更新UI
                UpdateDeckStats();   // 更新统计信息
            }
        }
        else if (draggedLibraryIndex != -1)
        {
            // 从卡牌库拖动到卡组槽�?
            if (currentDeck.Count < maxCardsInDeck && !currentDeck.Contains(draggedCardData))
            {
                if (slotIndex < currentDeck.Count)
                {
                    currentDeck.Insert(slotIndex, draggedCardData);
                }
                else
                {
                    currentDeck.Add(draggedCardData);
                }
                
                UpdateDeckSlotsUI(); // 更新UI
                UpdateDeckStats();   // 更新统计信息
            }
        }
    }
    
    #endregion
}

}
