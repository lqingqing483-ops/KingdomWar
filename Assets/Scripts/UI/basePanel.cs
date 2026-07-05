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
    private Vector2 offset; // 鼠标位置与物体中心的偏移
    protected virtual void Awake()
    {
        //自定义函数在Awake和Stack之间的时机调�?
        canvasGroup = AddAndGetComponent<CanvasGroup>(this.gameObject);
        //canvasGroup= this.GetComponent<CanvasGroup>();

        panel = this.GetComponent<RectTransform>();

        //默认隐藏
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
    }
    protected virtual void Start()
    {
        if (canvasGroup == null)
            Debug.LogError(this.gameObject.name + "缺少CanvasGroup"); // 再次确认
        if (panel == null)
            Debug.LogError(this.gameObject.name + "缺少RectTransform");



        showHideTween = panel.DOAnchorPos(Vector3.zero, 1f) // 移动到中�?
            .SetAutoKill(false); // 禁止自动销�?
        showHideTween.Pause(); // 暂停等待播放

        clockTrans = transform.Find("CloseBtn");
        //防止有些界面没有封闭按钮
        if (clockTrans != null)
        {
            clockBtn = clockTrans.GetComponent<Button>();
            clockBtn.onClick.AddListener(OnClickBtn);
        }
    }

    protected virtual void OnClickBtn()
    {

        UIManager.Instance.PopPanel();
    }

    public virtual void OnEnter()
    {
        if (panel != null)
        {
            panel.DOPlayForward();
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
        }

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
        if (panel != null)
        {
            panel.DOPlayBackwards();
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }
    }
    /// <summary>
    /// 添加并获取脚�?
    /// </summary>
    protected T AddAndGetComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null)
        {
            comp = obj.AddComponent<T>();
        }
        return comp;
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        offset = (Vector2)transform.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //transform.position = eventData.position + offset;
    }
}

}
