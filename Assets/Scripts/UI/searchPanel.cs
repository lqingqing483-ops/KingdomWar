using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using KingdomWar.Server;
namespace KingdomWar.UI
{
public class searchPanel : basePanel
{
    public Text statusText;
    public Text detailText;       // NEW: shows trophy range, elapsed time
    public Button cancelButton;
    public Button botButton;      // NEW: appears when matchmaking times out
    
    private bool isSearching = false;
    private float loadingDotsTimer = 0f;
    private int loadingDotsCount = 0;
    private float loadingDotsInterval = 0.5f;
    
    protected virtual void Awake()
    {
        base.Awake();
        try
        {
            if(statusText==null)
            {
                Transform statusTextTrans = transform.Find("Top/scheduleText");
                if (statusTextTrans != null)
                {
                    statusText = statusTextTrans.GetComponent<Text>();
                }
            }
            if(cancelButton == null)
            {
                Transform cancelButtonTrans = transform.Find("cancelButton");
                if (cancelButtonTrans != null)
                {
                    cancelButton = cancelButtonTrans.GetComponent<Button>();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in searchPanel Awake: " + e.Message);
        }
    }
    
    protected virtual void Start()
    {
        base.Start();
        
        if(cancelButton != null)
            cancelButton.onClick.AddListener(CancelSearch);
        
        if(botButton != null)
        {
            botButton.gameObject.SetActive(false);
            botButton.onClick.AddListener(StartBotMatch);
        }
    }
    
    public override void OnEnter()
    {
        base.OnEnter();
        
        gameObject.SetActive(true);
        StartSearch();
    }
    
    public override void OnExit()
    {
        base.OnExit();
        
        UnsubscribeMatchmakingEvents();
        gameObject.SetActive(false);
        isSearching = false;
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
    /// 开始搜索匹�?
    /// </summary>
    private void StartSearch()
    {
        Debug.Log("正在开始搜索匹�?..");
        isSearching = true;
        loadingDotsTimer = 0f;
        loadingDotsCount = 0;
        
        if (statusText != null)
        {
            statusText.text = "正在匹配对手";
        }
        if (detailText != null)
        {
            detailText.text = "";
        }
        if (botButton != null)
        {
            botButton.gameObject.SetActive(false);
        }
        
        // Use MatchmakingService for trophy-based matchmaking
        if (NetworkManager.Instance != null && NetworkManager.Instance.Matchmaking != null)
        {
            // Subscribe to events
            NetworkManager.Instance.Matchmaking.OnRangeUpdated += OnMatchmakingRangeUpdated;
            NetworkManager.Instance.Matchmaking.OnMatchFound += OnMatchmakingMatchFound;
            NetworkManager.Instance.Matchmaking.OnTimedOut += OnMatchmakingTimedOut;
            NetworkManager.Instance.Matchmaking.OnCancelled += OnMatchmakingCancelled;
            NetworkManager.Instance.Matchmaking.OnError += OnMatchmakingError;
            
            NetworkManager.Instance.Matchmaking.StartSearch();
        }
        else
        {
            Debug.LogError("NetworkManager或MatchmakingService未找到！");
            if (statusText != null)
            {
                statusText.text = "网络错误，请重试";
            }
        }
    }
    
    // Matchmaking event handlers
    private void OnMatchmakingRangeUpdated(int range, int elapsed)
    {
        if (detailText != null)
        {
            detailText.text = $"Searching... {elapsed}s\nTrophy range: +/-{range}";
        }
    }
    
    private void OnMatchmakingMatchFound()
    {
        if (statusText != null)
        {
            statusText.text = "Opponent found!";
        }
    }
    
    private void OnMatchmakingTimedOut()
    {
        isSearching = false;
        if (statusText != null)
        {
            statusText.text = "No opponent found.";
        }
        if (detailText != null)
        {
            detailText.text = "Try playing vs AI?";
        }
        if (botButton != null)
        {
            botButton.gameObject.SetActive(true);
        }
    }
    
    private void OnMatchmakingCancelled()
    {
        isSearching = false;
    }
    
    private void OnMatchmakingError(string error)
    {
        isSearching = false;
        if (statusText != null)
        {
            statusText.text = "Error: " + error;
        }
    }
    
    private void UnsubscribeMatchmakingEvents()
    {
        if (NetworkManager.Instance != null && NetworkManager.Instance.Matchmaking != null)
        {
            NetworkManager.Instance.Matchmaking.OnRangeUpdated -= OnMatchmakingRangeUpdated;
            NetworkManager.Instance.Matchmaking.OnMatchFound -= OnMatchmakingMatchFound;
            NetworkManager.Instance.Matchmaking.OnTimedOut -= OnMatchmakingTimedOut;
            NetworkManager.Instance.Matchmaking.OnCancelled -= OnMatchmakingCancelled;
            NetworkManager.Instance.Matchmaking.OnError -= OnMatchmakingError;
        }
    }
    
    /// <summary>
    /// Start a match vs AI bot instead of waiting for human opponent.
    /// </summary>
    private void StartBotMatch()
    {
        Debug.Log("Starting bot match...");
        isSearching = false;
        UnsubscribeMatchmakingEvents();
        
        // Cancel matchmaking
        if (NetworkManager.Instance != null && NetworkManager.Instance.Matchmaking != null)
        {
            NetworkManager.Instance.Matchmaking.CancelSearch();
        }
        
        // Close search panel
        UIManager.Instance.ClearPanel();
        
        // Load battle scene - BattleManager will create AI opponent for local battles
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }

    /// <summary>
    /// 取消搜索
    /// </summary>
    private void CancelSearch()
    {
        Debug.Log("正在取消搜索...");
        isSearching = false;
        
        UnsubscribeMatchmakingEvents();
        
        // Cancel matchmaking via service
        if (NetworkManager.Instance != null && NetworkManager.Instance.Matchmaking != null)
        {
            NetworkManager.Instance.Matchmaking.CancelSearch();
        }
        else if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.LeaveRoom();
        }
        
        // 关闭搜索面板
        UIManager.Instance.PopPanel();
    }
    
    private void Update()
    {
        if (isSearching)
        {
            // 更新搜索状�?
            UpdateSearchStatus();
            
            // 更新加载动画
            UpdateLoadingAnimation();
        }
    }
    
    /// <summary>
    /// 更新加载动画
    /// </summary>
    private void UpdateLoadingAnimation()
    {
        if (statusText != null)
        {
            // 检查当前状态文本是否包�?正在匹配对手"
            if (statusText.text.StartsWith("正在匹配对手"))
            {
                loadingDotsTimer += Time.deltaTime;
                if (loadingDotsTimer >= loadingDotsInterval)
                {
                    loadingDotsTimer = 0f;
                    loadingDotsCount = (loadingDotsCount + 1) % 4;
                    
                    // 更新状态文本，添加动态变化的�?
                    string dots = new string('.', loadingDotsCount);
                    statusText.text = "正在匹配对手" + dots;
                }
            }
            else if (statusText.text.StartsWith("正在寻找房间"))
            {
                loadingDotsTimer += Time.deltaTime;
                if (loadingDotsTimer >= loadingDotsInterval)
                {
                    loadingDotsTimer = 0f;
                    loadingDotsCount = (loadingDotsCount + 1) % 4;
                    
                    // 更新状态文本，添加动态变化的�?
                    string dots = new string('.', loadingDotsCount);
                    statusText.text = "正在寻找房间" + dots;
                }
            }
            else if (statusText.text.StartsWith("正在连接网络"))
            {
                loadingDotsTimer += Time.deltaTime;
                if (loadingDotsTimer >= loadingDotsInterval)
                {
                    loadingDotsTimer = 0f;
                    loadingDotsCount = (loadingDotsCount + 1) % 4;
                    
                    // 更新状态文本，添加动态变化的�?
                    string dots = new string('.', loadingDotsCount);
                    statusText.text = "正在连接网络" + dots;
                }
            }
        }
    }
    
    /// <summary>
    /// 更新搜索状�?
    /// </summary>
    private void UpdateSearchStatus()
    {
        if (NetworkManager.Instance != null)
        {
            if (PhotonNetwork.IsConnected)
            {
                if (PhotonNetwork.InRoom)
                {
                    int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
                    int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
                    
                    if (statusText != null)
                    {
                        statusText.text = string.Format("已找到房间，等待其他玩家 ({0}/{1})\n房间ID: {2}", 
                            playerCount, maxPlayers, PhotonNetwork.CurrentRoom.Name);
                    }
                }
                else if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.JoinedLobby)
                {
                    if (statusText != null)
                    {
                        statusText.text = "正在寻找房间";
                    }
                }
                else if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.ConnectedToMasterServer)
                {
                    if (statusText != null)
                    {
                        statusText.text = "正在加入大厅";
                    }
                }
                else
                {
                    if (statusText != null)
                    {
                        statusText.text = string.Format("连接�?({0})", PhotonNetwork.NetworkClientState);
                    }
                }
            }
            else
            {
                if (PhotonNetwork.NetworkClientState == Photon.Realtime.ClientState.Disconnected)
                {
                    if (statusText != null)
                    {
                        statusText.text = "正在连接网络";
                    }
                }
                else
                {
                    if (statusText != null)
                    {
                        statusText.text = string.Format("连接�?({0})", PhotonNetwork.NetworkClientState);
                    }
                }
            }
        }
    }
}

}
