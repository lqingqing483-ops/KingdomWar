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
                loadText.text = string.Format("Loading... {0}%", progressPercentage);
            }
            
            yield return null;
        }
        
        // 加载完成
        if (loadText != null)
        {
            loadText.text = "Loading complete!";
        }
        
        // 延迟一下再进入场景
        yield return new WaitForSeconds(0.5f);
        
        // 检查是否是AI对战
        string battleMode = PlayerPrefs.GetString("BattleMode", "");
        bool isAIBattle = battleMode == "AI";

        // 加载场景
        if (!isAIBattle && NetworkManager.Instance != null)
        {
            // Photon网络对战 — 由NetworkManager处理场景同步
            Debug.Log("AI vs Player battle, waiting for Photon scene sync...");
        }
        else
        {
            // AI对战或本地测试 — 直接加载场景
            Debug.Log("Loading battle scene locally...");
            SceneManager.LoadScene(SceneNames.Battle);
        }
        // 清除AI对战标识
        if (isAIBattle) PlayerPrefs.DeleteKey("BattleMode");
        
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
