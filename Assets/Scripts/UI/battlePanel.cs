using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using KingdomWar.HotUpdate;
using Photon.Pun;
using KingdomWar.Server;

/// <summary>
/// 战斗面板UI控制�?
/// 负责管理主界面的战斗、热更新和抽奖功能入�?
/// 继承自basePanel，整合多个子系统的初始化和交�?
/// </summary>
namespace KingdomWar.UI
{
public class battlePanel : basePanel
{
    #region UI组件引用
    
    /// <summary>
    /// 战斗按钮 - 点击进入匹配
    /// </summary>
    public Button battleButon;
    
    /// <summary>
    /// AI对战按钮 - 点击进入单人AI对战
    /// </summary>
    public Button aiBattleButton;
    
    /// <summary>
    /// 热更新按�?- 点击触发热更新流�?
    /// </summary>
    public Button hotUpdateButton;
    
    /// <summary>
    /// 抽奖按钮 - 点击打开抽奖面板
    /// </summary>
    public Button lotteryButton;

    /// <summary>
    /// 设置按钮 - 点击打开设置面板
    /// </summary>
    public Button settingButon;
    
    /// <summary>
    /// 热更新进度条图片（Fill类型�?
    /// </summary>
    public Image hotUpdateProgressImage;
    
    /// <summary>
    /// 热更新状态文�?
    /// </summary>
    public Text hotUpdateStatusText;
    
    /// <summary>
    /// 热更新进度面�?
    /// </summary>
    public GameObject hotUpdatePanel;
    
    #endregion

    #region 私有字段
    
    /// <summary>
    /// 是否正在热更新中
    /// </summary>
    private bool isHotUpdating = false;
    
    #endregion

    #region 生命周期方法
    
    /// <summary>
    /// Unity Awake方法
    /// 初始化热更新UI组件
    /// </summary>
    protected virtual void Awake()
    {
        base.Awake();
        InitializeHotUpdateUI();
    }

    /// <summary>
    /// Unity Start方法
    /// 初始化网络管理器、热更新系统和抽奖系�?
    /// </summary>
    protected virtual void Start()
    {
        base.Start();

        // 绑定战斗按钮点击事件
        battleButon.onClick.AddListener(GoToBattle);
        if (aiBattleButton != null)
            aiBattleButton.onClick.AddListener(GoToAIBattle);
        
        // 初始化各个子系统
        InitializeNetworkManager();
        InitializeHotUpdateSystem();
        InitializeLotterySystem();

    }
    
    #endregion

    #region 初始化方�?
    
    /// <summary>
    /// 初始化热更新UI组件
    /// 设置按钮事件和初始状�?
    /// </summary>
    private void InitializeHotUpdateUI()
    {
        // 绑定热更新按钮点击事�?
        if (hotUpdateButton != null)
        {
            hotUpdateButton.onClick.AddListener(OnHotUpdateButtonClicked);
        }

        // 设置抽奖按钮初始状�?
        if (lotteryButton != null)
        {
            lotteryButton.onClick.AddListener(OnLotteryButtonClicked);
        }

        // 绑定设置按钮点击事件
        if (settingButon != null)
        {
            settingButon.onClick.AddListener(OnSettingButtonClicked);
        }

        // 隐藏热更新进度面�?
        if (hotUpdatePanel != null)
        {
            hotUpdatePanel.SetActive(false);
        }

        // 重置进度�?
        if (hotUpdateProgressImage != null)
        {
            hotUpdateProgressImage.fillAmount = 0f;
        }
    }

    /// <summary>
    /// 初始化热更新系统
    /// 订阅热更新事件并检查更新脚�?
    /// </summary>
    private void InitializeHotUpdateSystem()
    {
        var hotUpdateManager = HotUpdateManager.Instance;
        hotUpdateManager.Initialize();
        
        hotUpdateManager.OnHotUpdateComplete += OnHotUpdateComplete;
        hotUpdateManager.OnHotUpdateError += OnHotUpdateError;
        hotUpdateManager.OnDownloadProgress += OnDownloadProgress;

        CheckForUpdateScript();
    }

    /// <summary>
    /// 下载进度回调
    /// 更新进度条显�?
    /// </summary>
    private void OnDownloadProgress(float progress)
    {
        if (hotUpdateProgressImage != null)
        {
            hotUpdateProgressImage.fillAmount = progress;
        }
    }

    /// <summary>
    /// 初始化抽奖系�?
    /// 预加载抽奖系统和相关依赖
    /// </summary>
    private void InitializeLotterySystem()
    {
        // 获取各系统实例，触发初始�?
        var lotterySystem = LotterySystem.Instance;
        var playerData = PlayerDataManager.Instance;
        
        Debug.Log("Lottery system initialized");
    }

    /// <summary>
    /// 检查是否有热更新资�?
    /// 如果有，直接显示抽奖按钮
    /// </summary>
    private void CheckForUpdateScript()
    {
        if (HotUpdateManager.Instance.HasUpdateResource())
        {
            //ShowLotteryButton();
        }
    }
    
    #endregion

    #region 热更新流�?
    
    /// <summary>
    /// 热更新按钮点击处�?
    /// 触发热更新流�?
    /// </summary>
    private void OnHotUpdateButtonClicked()
    {
        if (isHotUpdating)
        {
            Debug.Log("Hot update already in progress");
            return;
        }

        StartCoroutine(PerformHotUpdate());
    }

    /// <summary>
    /// 执行热更新流�?
    /// 协程方法，显示进度并等待完成
    /// </summary>
    /// <returns>协程迭代�?/returns>
    private IEnumerator PerformHotUpdate()
    {
        // 设置热更新中状�?
        isHotUpdating = true;
        
        // 显示热更新进度面�?
        if (hotUpdatePanel != null)
        {
            hotUpdatePanel.SetActive(true);
        }

        // 设置初始状态文�?
        if (hotUpdateStatusText != null)
        {
            hotUpdateStatusText.text = "正在检查更�?..";
        }

        // 执行热更新检�?
        yield return StartCoroutine(HotUpdateManager.Instance.CheckAndUpdate());
    }

    /// <summary>
    /// 热更新完成回�?
    /// 显示抽奖按钮并更新UI
    /// </summary>
    private void OnHotUpdateComplete()
    {
        isHotUpdating = false;
        
        if (hotUpdateStatusText != null)
        {
            hotUpdateStatusText.text = "热更新完�?";
        }

        if (hotUpdateProgressImage != null)
        {
            hotUpdateProgressImage.fillAmount = 1f;
        }

        StartCoroutine(HideHotUpdatePanelAfterDelay(1.5f));
        
        UIManager.Instance.CreatepromptMessage("Hot update successful! UI resources updated");
    }

    /// <summary>
    /// 热更新错误回�?
    /// 显示错误信息并隐藏进度面�?
    /// </summary>
    /// <param name="error">错误消息</param>
    private void OnHotUpdateError(string error)
    {
        // isHotUpdating = false;
        
        // // 更新错误状态文�?
        // if (hotUpdateStatusText != null)
        // {
        //     hotUpdateStatusText.text = $"更新失败: {error}";
        // }

        // // 延迟隐藏进度面板
        // StartCoroutine(HideHotUpdatePanelAfterDelay(2f));
        
        // // 显示错误提示
        // UIManager.Instance.CreatepromptMessage($"热更新失�? {error}");
        OnHotUpdateComplete();
    }



    /// <summary>
    /// 延迟隐藏热更新进度面�?
    /// </summary>
    /// <param name="delay">延迟时间（秒�?/param>
    /// <returns>协程迭代�?/returns>
    private IEnumerator HideHotUpdatePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (hotUpdatePanel != null)
        {
            hotUpdatePanel.SetActive(false);
        }
    }
    
    #endregion

    #region 抽奖功能
    
    /// <summary>
    /// 抽奖按钮点击处理
    /// 打开抽奖面板
    /// </summary>
    private void OnLotteryButtonClicked()
    {
        UIManager.Instance.PushPanel(UIPanelType.lotteryPanel);
        Debug.Log("打开抽奖页面");
    }
    
    #endregion

    #region 设置功能
    
    /// <summary>
    /// 设置按钮点击处理
    /// 打开设置面板
    /// </summary>
    private void OnSettingButtonClicked()
    {
        UIManager.Instance.PushPanel(UIPanelType.settingPanel);
        Debug.Log("打开设置页面");
    }
    
    #endregion

    #region 网络和战斗功�?
    
    /// <summary>
    /// 初始化网络管理器
    /// 确保Photon网络连接正常
    /// </summary>
    private void InitializeNetworkManager()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.Log("Creating NetworkManager instance...");
            GameObject networkManagerObj = new GameObject("NetworkManager");
            networkManagerObj.AddComponent<NetworkManager>();
        }
        else
        {
            Debug.Log("NetworkManager instance already exists");
            NetworkManager.Instance.InitializePhoton();
        }
    }

    /// <summary>
    /// 进入战斗
    /// 初始化网络连接并跳转到搜索面�?
    /// </summary>
    private void GoToBattle()
    {
        // 确保网络管理器已初始�?
        if (NetworkManager.Instance == null)
        {
            InitializeNetworkManager();
        }
        
        // 确保已连接到Photon服务�?
        if (!PhotonNetwork.IsConnected)
        {
            NetworkManager.Instance.InitializePhoton();
        }
        
        // 跳转到搜索匹配面�?
        UIManager.Instance.PushPanel(UIPanelType.searchPanel);
        Debug.Log("Start matching opponent");
    }

    private void GoToAIBattle()
    {
        // 单人AI对战 — 加载战斗场景，AI作为对手
        Debug.Log("Starting AI battle...");
        SceneManager.LoadScene("BattleScene");
    }
    
    #endregion

    #region 清理方法
    
    /// <summary>
    /// Unity OnDestroy方法
    /// 取消事件订阅，防止内存泄�?
    /// </summary>
    private void OnDestroy()
    {
        var hotUpdateManager = HotUpdateManager.Instance;
        if (hotUpdateManager != null)
        {
            hotUpdateManager.OnHotUpdateComplete -= OnHotUpdateComplete;
            hotUpdateManager.OnHotUpdateError -= OnHotUpdateError;
            hotUpdateManager.OnDownloadProgress -= OnDownloadProgress;
        }
    }
    
    #endregion
}

}
