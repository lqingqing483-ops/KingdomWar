using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 确保能够访问卡片相关的类
using KingdomWar.Game.Cards;
namespace KingdomWar.UI
{
public class CardsPanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("卡片配置")]
    public Transform ActiveCards;       //当前卡组
    public Image NextCard;              //下一张卡
    public int maxActiveCards = 4;      //当前卡组最大数�?
    public GameObject cardPrefab;       //卡片预制�?
    
    [Header("卡牌概率配置")]
    public float knightProbability = 0.4f; // 骑士卡牌获取概率
    
    [Header("圣水配置")]
    public Slider Slider;               //当前圣水进度条（最�?0�?
    public Text ElixirNumber;           //当前圣水数量
    public float maxElixir = 10f;       //最大圣水数�?
    public float elixirRecoveryRate = 0.8f; //每秒恢复圣水的速度
    
    [Header("拖动配置")]
    public GameObject dragCardPrefab;   //拖动卡片预览预制�?
    
    private List<CardData> currentDeck = new List<CardData>();       //当前卡组
    private List<GameObject> activeCardObjects = new List<GameObject>(); //当前显示的卡片对�?
    private Queue<CardData> cardQueue = new Queue<CardData>();       //卡片队列
    private float currentElixir = 10f;   //当前圣水数量
    private float elixirTimer = 0f;      //圣水恢复计时�?
    
    private GameObject draggingCard;     //正在拖动的卡�?
    private GameObject draggingModel;     //正在拖动的模型预�?
    private CardData draggedCardData;    //正在拖动的卡片数�?
    private int draggedCardIndex = -1;   //正在拖动的卡片索�?
    private Vector3 dragOffset;          //拖动偏移�?
    private RectTransform canvasRect;    //画布矩形
    
    private void Awake()
    {
        // 获取画布矩形
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        
        // 初始化滑块最大�?
        if (Slider != null)
        {
            Slider.maxValue = maxElixir;
        }
        
        // 确保有EventSystem
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        
        // 确保有UseRangeManager
        if (UseRangeManager.Instance == null)
        {
            GameObject useRangeManagerObj = new GameObject("UseRangeManager");
            useRangeManagerObj.AddComponent<UseRangeManager>();
        }
        
        // 确保有CardEffectManager
        if (CardEffectManager.Instance == null)
        {
            GameObject cardEffectManagerObj = new GameObject("CardEffectManager");
            cardEffectManagerObj.AddComponent<CardEffectManager>();
        }
    }
    
    private void Start()
    {
        // 初始化卡�?
        InitializeDeck();
        
        // 初始化卡片显�?
        UpdateActiveCards();
        
        // 初始化下一张卡片预�?
        UpdateNextCardPreview();
        
        // 初始化圣水显�?
        UpdateElixirDisplay();
    }
    
    private void Update()
    {
        // 恢复圣水
        RecoverElixir();
    }
    
    /// <summary>
    /// 更新当前显示的卡�?
    /// </summary>
    private void UpdateActiveCards()
    {
        // 清除现有的卡�?
        foreach (GameObject cardObj in activeCardObjects)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }
        activeCardObjects.Clear();
        
        // 创建新的卡片
        if (ActiveCards != null && cardPrefab != null)
        {
            for (int i = 0; i < currentDeck.Count; i++)
            {
                CardData cardData = currentDeck[i];
                GameObject cardObj = Instantiate(cardPrefab, ActiveCards);
                cardObj.name = "Card_" + i;
                //cardObj.tag = "Card"; // 添加Card标签以支持拖�?
                
                // 设置卡片信息
                Image cardIcon = cardObj.transform.Find("CardIcon").GetComponent<Image>();
                Text elixirCostText = cardObj.transform.Find("ElixirCost").GetComponent<Text>();
                
                if (cardIcon != null && cardData.cardIcon != null)
                {
                    cardIcon.sprite = cardData.cardIcon;
                }
                if (elixirCostText != null)
                {
                    elixirCostText.text = cardData.elixirCost.ToString();
                }
                
                // 添加点击事件
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    int cardIndex = i;
                    //cardButton.onClick.AddListener(() => OnCardClicked(cardIndex));
                }
                
                // 添加EventTrigger组件以支持拖�?
                EventTrigger eventTrigger = cardObj.GetComponent<EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = cardObj.AddComponent<EventTrigger>();
                }
                
                // 添加拖动事件
                EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
                beginDragEntry.eventID = EventTriggerType.BeginDrag;
                beginDragEntry.callback.AddListener((data) => OnBeginDrag((PointerEventData)data));
                eventTrigger.triggers.Add(beginDragEntry);
                
                EventTrigger.Entry dragEntry = new EventTrigger.Entry();
                dragEntry.eventID = EventTriggerType.Drag;
                dragEntry.callback.AddListener((data) => OnDrag((PointerEventData)data));
                eventTrigger.triggers.Add(dragEntry);
                
                EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
                endDragEntry.eventID = EventTriggerType.EndDrag;
                endDragEntry.callback.AddListener((data) => OnEndDrag((PointerEventData)data));
                eventTrigger.triggers.Add(endDragEntry);
                
                activeCardObjects.Add(cardObj);
            }
        }
    }
    
    /// <summary>
    /// 更新下一张卡片预�?
    /// </summary>
    private void UpdateNextCardPreview()
    {
        if (NextCard != null && cardQueue.Count > 0)
        {
            CardData nextCard = cardQueue.Peek();
            if (nextCard.cardIcon != null)
            {
                NextCard.sprite = nextCard.cardIcon;
                NextCard.gameObject.SetActive(true);
            }
        }
        else if (NextCard != null)
        {
            NextCard.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 恢复圣水
    /// </summary>
    private void RecoverElixir()
    {
        if (currentElixir < maxElixir)
        {
            elixirTimer += Time.deltaTime * elixirRecoveryRate;
            if (elixirTimer >= 1f)
            {
                currentElixir += Mathf.Floor(elixirTimer);
                elixirTimer -= Mathf.Floor(elixirTimer);
                
                // 确保圣水不超过最大�?
                if (currentElixir > maxElixir)
                {
                    currentElixir = maxElixir;
                }
                
                // 更新圣水显示
                UpdateElixirDisplay();
            }
            
            // 更新滑块显示
            if (Slider != null)
            {
                Slider.value = currentElixir + elixirTimer;
            }
        }
        else if (Slider != null)
        {
            Slider.value = maxElixir;
        }
    }
    
    /// <summary>
    /// 更新圣水显示
    /// </summary>
    private void UpdateElixirDisplay()
    {
        if (ElixirNumber != null)
        {
            ElixirNumber.text = Mathf.Floor(currentElixir).ToString();
        }
        
        if (Slider != null)
        {
            Slider.value = currentElixir;
        }
    }
    
    /// <summary>
    /// 卡片点击事件
    /// </summary>
    /// <param name="cardIndex">卡片索引</param>
    private void OnCardClicked(int cardIndex)
    {
        if (cardIndex >= 0 && cardIndex < currentDeck.Count)
        {
            CardData cardData = currentDeck[cardIndex];
            
            // 检查圣水是否足�?
            if (currentElixir >= cardData.elixirCost)
            {
                // 消耗圣�?
                currentElixir -= cardData.elixirCost;
                UpdateElixirDisplay();
                
                // 使用卡片
                UseCard(cardIndex);
            }
            else
            {
                Debug.Log("Not enough elixir!");
            }
        }
    }
    
    /// <summary>
    /// 使用卡片
    /// </summary>
    /// <param name="cardIndex">卡片索引</param>
    private void UseCard(int cardIndex)
    {
        if (cardIndex >= 0 && cardIndex < currentDeck.Count)
        {
            // 从当前卡组移除卡�?
            currentDeck.RemoveAt(cardIndex);
            
            // 从卡片队列中取出下一张卡片加入当前卡�?
            if (cardQueue.Count > 0)
            {
                CardData nextCard = cardQueue.Dequeue();
                currentDeck.Add(nextCard);
                
                // 将使用的卡片重新加入队列末尾
                // 这里简化处理，实际应该是使用后将卡片加入牌库洗�?
                // 这里只是为了演示，直接将下一张卡片加入队�?
                if (CardDatabase.Instance.GetAllCards().Count > 0)
                {
                    List<CardData> allCards = CardDatabase.Instance.GetAllCards();
                    int randomIndex = Random.Range(0, allCards.Count);
                    cardQueue.Enqueue(allCards[randomIndex]);
                }
            }
            
            // 更新卡片显示
            UpdateActiveCards();
            
            // 更新下一张卡片预�?
            UpdateNextCardPreview();
        }
    }
    
    /// <summary>
    /// 开始拖�?
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Start dragging");
        GameObject draggedObject = eventData.pointerDrag;
        // 检查是否是当前卡组中的卡片
        for (int i = 0; i < activeCardObjects.Count; i++)
        {
            if (activeCardObjects[i] == draggedObject)
            {
                draggedCardIndex = i;
                if (i < currentDeck.Count)
                {
                    draggedCardData = currentDeck[i];
                    if(currentElixir>=draggedCardData.elixirCost)
                    {
                        StartDraggingCard(eventData);
                        // 使用UseRangeManager创建范围指示�?
                        if (UseRangeManager.Instance != null)
                        {
                            UseRangeManager.Instance.CreateRangeIndicator();
                        }
                    }
                }
                return;
            }
        }
    }
    
    /// <summary>
    /// 拖动�?
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnDrag(PointerEventData eventData)
    {
        // if (draggingCard != null)
        // {
        //     Vector3 pos;
        //     if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
        //         canvasRect, eventData.position, eventData.pressEventCamera, out pos))
        //     {
        //         //draggingCard.transform.position = pos + dragOffset;
        //     }
        // }
        
        // 更新模型预览位置
        if (draggingModel != null)
        {
            Vector3 worldPosition = GetWorldPositionFromScreen(eventData.position);
            draggingModel.transform.position = worldPosition;
        }
        
        // 更新范围指示�?
        if (UseRangeManager.Instance != null && draggedCardData != null)
        {
            Vector3 worldPosition = GetWorldPositionFromScreen(eventData.position);
            UseRangeManager.Instance.UpdateRangeIndicator(worldPosition, draggedCardData.cardType);
        }
    }
    
    /// <summary>
    /// 结束拖动
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingModel != null)
        {
            // Destroy(draggingCard);
            // draggingCard = null;
            
            // 销毁模型预�?
            if (draggingModel != null)
            {
                Destroy(draggingModel);
                draggingModel = null;
            }
            
            // 销毁范围指示器
            if (UseRangeManager.Instance != null)
            {
                UseRangeManager.Instance.DestroyRangeIndicator();
            }
            
            // 检查是否可以使用卡�?
            if (draggedCardIndex != -1 && draggedCardData != null)
            {
                // 检查圣水是否足�?
                if (currentElixir >= draggedCardData.elixirCost)
                {
                    // 获取拖动结束位置的世界坐�?
                    Vector3 worldPosition = GetWorldPositionFromScreen(eventData.position);
                    
                    // 检查位置是否在可使用范围内
                    if (UseRangeManager.Instance != null && UseRangeManager.Instance.IsInUseRange(worldPosition, draggedCardData.cardType))
                    {
                        // 消耗圣�?
                        currentElixir -= draggedCardData.elixirCost;
                        UpdateElixirDisplay();
                        
                        // 执行卡片使用效果
                        if (CardEffectManager.Instance != null)
                        {
                            CardEffectManager.Instance.UseCardEffect(draggedCardData, worldPosition);
                        }
                        
                        // 使用卡片（从卡组中移除）
                        UseCard(draggedCardIndex);
                    }
                    else
                    {
                        Debug.Log("Position not in usable range");
                    }
                }
                else
                {
                    Debug.Log("Not enough elixir!");
                }
            }
            
            // 重置拖动状�?
            draggedCardIndex = -1;
            draggedCardData = null;
        }
    }
    
    /// <summary>
    /// 从屏幕坐标获取世界坐�?
    /// </summary>
    /// <param name="screenPosition">屏幕坐标</param>
    /// <returns>世界坐标</returns>
    private Vector3 GetWorldPositionFromScreen(Vector2 screenPosition)
    {
        // 使用主相机将屏幕坐标转换为世界坐�?
        if (Camera.main != null)
        {
            // �?D环境中，我们需要考虑相机的位置和视角
            // 使用射线检测获取地面上的点
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);
            RaycastHit hit;
            
            // 专门检测Ground层的地面
            int groundLayer = LayerMask.GetMask("Ground");
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
            {
                return hit.point;
            }
            else
            {
                // 如果没有击中Ground层，尝试检测所有层
                if (Physics.Raycast(ray, out hit))
                {
                    return hit.point;
                }
                else
                {
                    // 如果没有击中任何物体，使用相机前方一定距离的�?
                    return Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
                }
            }
        }
        
        // 如果没有主相机，返回默认位置
        return Vector3.zero;
    }
    
    /// <summary>
    /// 开始拖动卡�?
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    private void StartDraggingCard(PointerEventData eventData)
    {
        if (dragCardPrefab != null && draggedCardData != null)
        {
            // // 创建拖动预览
            // draggingCard = Instantiate(dragCardPrefab, transform.parent);
            // draggingCard.transform.SetAsLastSibling();
            // draggingCard.transform.localScale =
            new Vector3(0.6f, 0.6f, 1.0f);
            
            // // 设置卡片信息
            // Image cardIcon = draggingCard.transform.Find("CardIcon").GetComponent<Image>();
            // Text elixirCostText = draggingCard.transform.Find("ElixirCost").GetComponent<Text>();
            
            // if (cardIcon != null && draggedCardData.cardIcon != null)
            // {
            //     cardIcon.sprite = draggedCardData.cardIcon;
            // }
            // if (elixirCostText != null)
            // {
            //     elixirCostText.text = draggedCardData.elixirCost.ToString();
            // }
            
            // 创建模型预览
            if (draggedCardData != null)
            {
                Vector3 worldPosition = GetWorldPositionFromScreen(eventData.position);
                if(draggedCardData.cardType == CardType.Spell)
                {
                    if(draggedCardData.spellData.draggedPrefab != null)
                    {
                        draggingModel = Instantiate(draggedCardData.spellData.draggedPrefab, worldPosition, Quaternion.identity);
                    }
                }
                if(draggingModel == null && draggedCardData.cardPrefab != null)
                    draggingModel = Instantiate(draggedCardData.cardPrefab, worldPosition, Quaternion.identity);
                
                
                // 禁用模型的碰撞器和脚�?
                if (draggingModel == null) return;

                Collider[] colliders = draggingModel.GetComponentsInChildren<Collider>();
                foreach (Collider collider in colliders)
                {
                    collider.enabled = false;
                }
                
                MonoBehaviour[] scripts = draggingModel.GetComponentsInChildren<MonoBehaviour>();
                foreach (MonoBehaviour script in scripts)
                {
                    script.enabled = false;
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化卡�?
    /// </summary>
    private void InitializeDeck()
    {
        List<CardData> allCards = CardDatabase.Instance.GetAllCards();
        
        if (allCards.Count > 0)
        {
            for (int i = 0; i < maxActiveCards && i < allCards.Count; i++)
            {
                CardData selectedCard = GetRandomCardWithKnightProbability(allCards);
                currentDeck.Add(selectedCard);
            }
            
            for (int i = 0; i < 10; i++)
            {
                if (allCards.Count > 0)
                {
                    CardData selectedCard = GetRandomCardWithKnightProbability(allCards);
                    cardQueue.Enqueue(selectedCard);
                }
            }
        }
        else
        {
            Debug.LogError("No cards found in CardDatabase! Using test cards.");
            
            UseTestCards();
        }
    }
    
    private CardData GetRandomCardWithKnightProbability(List<CardData> allCards)
    {
        if (Random.value < knightProbability)
        {
            CardData knightCard = allCards.Find(card => card.cardName == "骑士");
            if (knightCard != null)
            {
                return knightCard;
            }
        }
        
        int randomIndex = Random.Range(0, allCards.Count);
        return allCards[randomIndex];
    }
    
    /// <summary>
    /// 使用测试卡片
    /// </summary>
    private void UseTestCards()
    {
        // 创建测试卡片数据
        CardData knightCard = ScriptableObject.CreateInstance<CardData>();
        knightCard.cardName = "骑士";
        knightCard.cardType = CardType.Unit;
        knightCard.elixirCost = 3;
        knightCard.rarity = 1;
        
        CardData archerCard = ScriptableObject.CreateInstance<CardData>();
        archerCard.cardName = "Archer";
        archerCard.cardType = CardType.Unit;
        archerCard.elixirCost = 3;
        archerCard.rarity = 1;
        
        CardData fireballCard = ScriptableObject.CreateInstance<CardData>();
        fireballCard.cardName = "火球";
        fireballCard.cardType = CardType.Spell;
        fireballCard.elixirCost = 4;
        fireballCard.rarity = 1;
        
        CardData giantCard = ScriptableObject.CreateInstance<CardData>();
        giantCard.cardName = "巨人";
        giantCard.cardType = CardType.Unit;
        giantCard.elixirCost = 5;
        giantCard.rarity = 2;
        
        // 填充当前卡组
        currentDeck.Add(knightCard);
        currentDeck.Add(archerCard);
        currentDeck.Add(fireballCard);
        currentDeck.Add(giantCard);
        
        // 填充卡片队列
        cardQueue.Enqueue(knightCard);
        cardQueue.Enqueue(archerCard);
        cardQueue.Enqueue(fireballCard);
        cardQueue.Enqueue(giantCard);
        cardQueue.Enqueue(knightCard);
        cardQueue.Enqueue(archerCard);
        cardQueue.Enqueue(fireballCard);
        cardQueue.Enqueue(giantCard);
    }
}

}
