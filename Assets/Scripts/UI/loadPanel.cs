using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using KingdomWar.Game;
using KingdomWar.Server;
namespace KingdomWar.UI
{
public class loadPanel : basePanel
{
    public Text loadText;
    public Slider loadSlider;
    
    private float loadProgress = 0f;
    private bool isLoading = false;
    
    protected virtual void Awake()
    {
        base.Awake();
        
        //loadText = transform.Find("loadText").GetComponent<Text>();
        //loadSlider = transform.Find("loadSlider").GetComponent<Slider>();
    }
    
    protected virtual void Start()
    {
        base.Start();
        
        // 初始化加载进度条
        if (loadSlider != null)
        {
            loadSlider.value = 0f;
        }
        
        // 开始加载战斗场�?
        StartCoroutine(LoadBattleScene());
    }
    
    public override void OnEnter()
    {
        base.OnEnter();
        
        gameObject.SetActive(true);
        loadProgress = 0f;
        isLoading = true;
        
        if (loadText != null)
        {
            loadText.text = "准备战斗...";
        }
    }
    
    public override void OnExit()
    {
        base.OnExit();
        
        gameObject.SetActive(false);
        isLoading = false;
    }
    
    public override void OnPause()
    {
        base.OnPause();
        
        gameObject.SetActive(false);
    }
    
    public override void OnResume()
    {
        base.OnResume();
        
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 加载战斗场景
    /// </summary>
    private System.Collections.IEnumerator LoadBattleScene()
    {
        Debug.Log("Loading battle scene...");
        
        // 模拟加载过程
        float startTime = Time.time;
        float loadDuration = 2f; // 模拟加载时间
        
        while (loadProgress < 1f)
        {
            loadProgress = Mathf.Clamp01((Time.time - startTime) / loadDuration);
            
            // 更新加载进度�?
            if (loadSlider != null)
            {
                loadSlider.value = loadProgress;
            }
            
            // 更新加载文本
            if (loadText != null)
            {
                int progressPercentage = Mathf.RoundToInt(loadProgress * 100);
                loadText.text = string.Format("加载�?.. {0}%", progressPercentage);
            }
            
            yield return null;
        }
        
        // 加载完成
        if (loadText != null)
        {
            loadText.text = "加载完成，准备开始战斗！";
        }
        
        // 延迟一下再进入场景
        yield return new WaitForSeconds(0.5f);
        
        // 调用NetworkManager加载场景
        if (NetworkManager.Instance != null)
        {
            // PhotonNetwork.LoadLevel会自动同步场景给所有玩�?
            Debug.Log("Loading Main scene via PhotonNetwork...");
            // 场景加载由NetworkManager在房间满员时处理
        }
        else
        {
            // 本地加载场景
            Debug.Log("Loading Main scene locally...");
            SceneManager.LoadScene(SceneNames.Battle);
        }
        
        isLoading = false;
    }
    
    private void Update()
    {
        if (isLoading)
        {
            // 检查加载状�?
            CheckLoadStatus();
        }
    }
    
    /// <summary>
    /// 检查加载状�?
    /// </summary>
    private void CheckLoadStatus()
    {
        // 检查是否已经进入战斗场�?
        if (SceneManager.GetActiveScene().name == SceneNames.Battle)
        {
            // 战斗场景已加载，关闭加载面板
            Debug.Log("Battle scene loaded, closing load panel...");
            UIManager.Instance.PopPanel();
        }
    }
}

}
