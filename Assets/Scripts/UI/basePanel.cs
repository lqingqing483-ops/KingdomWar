using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace KingdomWar.UI
{
public class basePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    protected CanvasGroup canvasGroup;
    private Button clockBtn;
    private Transform clockTrans;
    protected RectTransform panel;
    protected Tween showHideTween;
    protected Sequence animSequence;
    private Vector2 offset; // mouse offset for drag
    private Vector2 hiddenPos;
    
    [Header("Animation Settings")]
    public float enterDuration = 0.4f;
    public float exitDuration = 0.25f;
    public float slideDistance = 200f;
    public bool enableBounce = true;
    
    protected virtual void Awake()
    {
        canvasGroup = AddAndGetComponent<CanvasGroup>(this.gameObject);
        panel = this.GetComponent<RectTransform>();
        
        // Store hidden position (slide down)
        hiddenPos = panel.anchoredPosition - new Vector2(0, slideDistance);
        
        // Start hidden
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        panel.anchoredPosition = hiddenPos;
        panel.localScale = Vector3.one * 0.95f;
    }
    
    protected virtual void Start()
    {
        if (canvasGroup == null)
            Debug.LogError(this.gameObject.name + " missing CanvasGroup");
        if (panel == null)
            Debug.LogError(this.gameObject.name + " missing RectTransform");

        clockTrans = transform.Find("CloseBtn");
        if (clockTrans != null)
        {
            clockBtn = clockTrans.GetComponent<Button>();
            clockBtn.onClick.AddListener(OnClickBtn);
            
            // Button hover scale effect
            clockBtn.onClick.AddListener(() => {
                clockTrans.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
            });
        }
        
        // Add hover effects to all buttons in this panel
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            if (btn == clockBtn) continue;
            AddButtonHoverEffect(btn);
        }
    }

    private void AddButtonHoverEffect(Button btn)
    {
        EventTrigger trigger = btn.GetComponent<EventTrigger>();
        if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) => {
            btn.transform.DOScale(1.08f, 0.15f).SetEase(Ease.OutQuad);
        });
        trigger.triggers.Add(enter);
        
        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) => {
            btn.transform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);
        });
        trigger.triggers.Add(exit);
    }

    public virtual void OnEnter()
    {
        if (panel == null) return;
        
        // Kill any running animations
        animSequence?.Kill();
        animSequence = DOTween.Sequence();
        
        // Reset to start state
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = true;
        panel.anchoredPosition = hiddenPos;
        panel.localScale = Vector3.one * 0.95f;
        
        // Animate in
        animSequence.Join(panel.DOAnchorPos(hiddenPos + new Vector2(0, slideDistance), enterDuration)
            .SetEase(Ease.OutBack, 1.2f));
        animSequence.Join(canvasGroup.DOFade(1, enterDuration * 0.8f));
        animSequence.Join(panel.DOScale(1f, enterDuration).SetEase(Ease.OutBack, 1.2f));
        animSequence.Play();
    }
    
    public virtual void OnPause()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    public virtual void OnResume()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }
    
    public virtual void OnExit()
    {
        if (panel == null) return;
        
        animSequence?.Kill();
        animSequence = DOTween.Sequence();
        
        canvasGroup.blocksRaycasts = false;
        
        animSequence.Join(panel.DOAnchorPos(hiddenPos, exitDuration).SetEase(Ease.InBack));
        animSequence.Join(canvasGroup.DOFade(0, exitDuration * 0.7f));
        animSequence.Join(panel.DOScale(0.95f, exitDuration).SetEase(Ease.InBack));
        animSequence.Play();
    }
    
    /// <summary>
    /// Add stagger animation to list items (for shop, deck, etc.)
    /// </summary>
    protected void AnimateItems(Transform container, float staggerDelay = 0.05f)
    {
        int count = container.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform item = container.GetChild(i);
            item.localScale = Vector3.zero;
            item.DOScale(1f, 0.3f).SetDelay(i * staggerDelay).SetEase(Ease.OutBack, 1.5f);
        }
    }

    protected T AddAndGetComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = obj.AddComponent<T>();
        return comp;
    }

    protected virtual void OnClickBtn()
    {
        UIManager.Instance.PopPanel();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = (Vector2)transform.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Optional drag behavior
    }
}
}
