using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using ExitGames.Client.Photon;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;
using KingdomWar.Game.Battle;
using KingdomWar.Game;
namespace KingdomWar.Server
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager Instance { get; private set; }
        
        public MatchmakingService Matchmaking { get; private set; }

        [Header("Network Settings")]
        public string gameVersion = "1.0";
        public string roomName = "KingdomWarRoom";
        public int maxPlayersPerRoom = 2;
        
        [Header("UI References")]
        public GameObject searchPanel;
        public GameObject loadPanel;
        
        [Header("Reconnection Settings")]
        public int maxReconnectionAttempts = 3;
        public float reconnectionDelay = 2f;
        
        private bool isConnecting = false;
        private int reconnectionAttempts = 0;
        private Coroutine reconnectionCoroutine;
        private const string TeamKey = "team";
        private byte currentRoomTeam = 0;
        private bool loadMainSceneAfterLeaveRoom = false;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                PhotonNetwork.AutomaticallySyncScene = true;
                Matchmaking = new MatchmakingService();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // 初始化PUN2
            InitializePhoton();
        }

        private void Update()
        {
            // Update matchmaking service each frame (for timeout/range expansion)
            if (Matchmaking != null)
            {
                Matchmaking.UpdateMatchmaking(Time.deltaTime);
            }
        }
        
        /// <summary>
        /// 初始化Photon网络
        /// </summary>
        public void InitializePhoton()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("正在连接到Photon Network...");
                Debug.LogFormat("使用的GameVersion: {0}", gameVersion);
                
                // 只设置必要的属性，避免使用任何可能导致"Unsupported Plugin"错误的功�?                PhotonNetwork.GameVersion = gameVersion;
                
                // 连接到Photon服务器，使用最基本的设�?                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                Debug.LogFormat("已经连接到Photon Network，状�? {0}", PhotonNetwork.NetworkClientState);
            }
        }
        
        /// <summary>
        /// 开始匹�?        /// </summary>
        public void StartMatching()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InLobby)
            {
                Debug.Log("正在开始匹�?..");
                isConnecting = true;
                
                // 尝试加入现有房间
                PhotonNetwork.JoinRandomRoom();
            }
            else if (PhotonNetwork.IsConnected)
            {
                Debug.Log("已连接但未进入大厅，正在加入大厅...");
                isConnecting = true;
                PhotonNetwork.JoinLobby();
            }
            else
            {
                Debug.LogError("没有连接到Photon Network. Initializing...");
                InitializePhoton();
                isConnecting = true;
            }
        }
        
        /// <summary>
        /// 创建房间
        /// </summary>
                private void CreateRoom()
        {
            Debug.Log("Creating room...");
            
            RoomOptions roomOptions = new RoomOptions();
            roomOptions.MaxPlayers = (byte)maxPlayersPerRoom;
            
            // Set room custom properties with host trophy info
            int hostTrophies = (Matchmaking != null) ? Matchmaking.PlayerTrophies : 0;
            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps["TrophyHost"] = hostTrophies;
            roomProps["TrophyRange"] = Matchmaking?.CurrentTrophyRange ?? 300;
            roomOptions.CustomRoomProperties = roomProps;
            roomOptions.CustomRoomPropertiesForLobby = new string[] { "TrophyHost" };
            
            Debug.Log("Created room, host trophies: " + hostTrophies);
            PhotonNetwork.CreateRoom(null, roomOptions, null);
        }
        
        public override void OnCreatedRoom()
        {
            Debug.Log("Room created successfully!");
            Debug.LogFormat("Room {0}, Max Players: {1}", PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CurrentRoom.MaxPlayers);
            
            // Notify matchmaking service
            if (Matchmaking != null)
            {
                Matchmaking.OnRoomCreated();
            }
        }
        
        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogErrorFormat("创建房间失败: {0}, {1}", returnCode, message);
            
            // 尝试使用不同的房间名或区�?            //TryDifferentRegion();
        }
        
        /// <summary>
        /// 离开房间
        /// </summary>
        public void LeaveRoom()
        {
            LeaveRoomAndReturnToMainScene(false);
        }

        public void LeaveRoomAndReturnToMainScene()
        {
            LeaveRoomAndReturnToMainScene(true);
        }

        private void LeaveRoomAndReturnToMainScene(bool loadMainSceneAfterLeave)
        {
            loadMainSceneAfterLeaveRoom = loadMainSceneAfterLeave;
            ClearLocalTeamState();

            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
                return;
            }

            if (loadMainSceneAfterLeaveRoom)
            {
                LoadMainScene();
            }
        }
        
        #region PUN2 Callbacks
        
        public override void OnConnectedToMaster()
        {
            Debug.Log("已连接到Photon主服务器");
            
            // 重置重连尝试次数
            reconnectionAttempts = 0;
            
            if (isConnecting)
            {
                // 加入大厅
                PhotonNetwork.JoinLobby();
            }
        }
        
        public override void OnJoinedLobby()
        {
            Debug.Log("Joined lobby successfully");
            
            if (isConnecting)
            {
                // 现在可以开始匹�?                PhotonNetwork.JoinRandomRoom();
            }
        }
        
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogFormat("已从Photon断开连接: {0}", cause);
            isConnecting = false;
            ClearLocalTeamState();

            // 处理断开连接
            HandleDisconnection(cause);
        }
        
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.LogFormat("无法加入随机房间: {0}, {1}", returnCode, message);
            
            // Notify matchmaking service
            if (Matchmaking != null)
            {
                Matchmaking.OnJoinRandomRoomFailed();
            }
            
            // 没有房间，创建新房间
            CreateRoom();
        }
        
        public override void OnJoinedRoom()
        {
            Debug.Log("Joined room successfully");
            Debug.LogFormat("Room: {0}, Players: {1}/{2}", PhotonNetwork.CurrentRoom.Name, PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);

            isConnecting = false;
            currentRoomTeam = 0;

            // 分配玩家阵营
            AssignPlayerTeam();

            // check if room is full
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
            {
                Debug.Log("Room full, loading battle scene...");
                // 加载战斗场景
                LoadBattleScene();
            }
            else
            {
                Debug.Log("Waiting for another player to join...");
                // 显示等待UI
                if (searchPanel != null)
                {
                    searchPanel.SetActive(true);
                }
            }
        }
        
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.LogFormat("玩家已加入房�? {0}", newPlayer.NickName);

            // Notify matchmaking service
            if (Matchmaking != null)
            {
                Matchmaking.OnPlayerJoined();
            }

            AssignPlayerTeam();
            StartCoroutine(RefreshLocalTeamAfterRemoteJoin());

            // check if room is full
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
            {
                Debug.Log("Room full, loading battle scene...");
                // 加载战斗场景
                LoadBattleScene();
            }
        }
        
        public override void OnLeftRoom()
        {
            ClearLocalTeamState();
            Debug.Log("已成功离开房间");

            if (loadMainSceneAfterLeaveRoom)
            {
                LoadMainScene();
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer == PhotonNetwork.LocalPlayer && changedProps.ContainsKey(TeamKey))
            {
                byte updatedTeam = (byte)changedProps[TeamKey];
                currentRoomTeam = updatedTeam;
                Debug.LogFormat("本地玩家阵营属性已更新: {0}", updatedTeam == 1 ? "蓝方" : "红方");

                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.UpdateTeamInfo(updatedTeam);
                }
            }
        }
        
        public override void OnErrorInfo(ErrorInfo errorInfo)
        {
            Debug.LogErrorFormat("Photon错误信息: {0}, {1}", errorInfo.Info, errorInfo.Info);
            
            // 处理网络错误
            HandleNetworkError(0, errorInfo.Info);
        }
        
        #endregion
        
        /// <summary>
        /// 加载战斗场景
        /// </summary>
        private void LoadBattleScene()
        {
            // 显示加载面板
            if (loadPanel != null)
            {
                loadPanel.SetActive(true);
            }
            
            // 加载Main场景
            PhotonNetwork.LoadLevel(SceneNames.Battle);
        }
        
        /// <summary>
        /// 获取玩家ID
        /// </summary>
        /// <returns>玩家ID (1�?)</returns>
        public int GetPlayerId()
        {
            if (PhotonNetwork.InRoom)
            {
                // 根据玩家在房间中的索引分配ID
                return PhotonNetwork.LocalPlayer.ActorNumber;
            }
            return 1; // 默认返回1
        }
        
        /// <summary>
        /// 检查是否是本地玩家的回�?        /// </summary>
        /// <returns>是否是本地玩家的回合</returns>
        public bool IsLocalPlayerTurn()
        {
            // temporarily return true because we are using real-time battle
            return true;
        }
        
        /// <summary>
        /// 加载主界面场�?        /// </summary>
        private void LoadMainScene()
        {
            loadMainSceneAfterLeaveRoom = false;
            SceneManager.LoadScene(SceneNames.MainMenu);
        }

        /// <summary>
        /// 清理本地阵营状�?        /// </summary>
        private void ClearLocalTeamState()
        {
            currentRoomTeam = 0;

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.UpdateTeamInfo(0);
            }
        }

        private IEnumerator RefreshLocalTeamAfterRemoteJoin()
        {
            yield return null;

            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
            {
                yield break;
            }

            object teamCode;
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(TeamKey, out teamCode))
            {
                byte updatedTeam = (byte)teamCode;
                currentRoomTeam = updatedTeam;
                Debug.LogFormat("远端玩家加入后重新确认本地阵�? {0}", updatedTeam == 1 ? "蓝方" : "红方");

                if (BattleManager.Instance != null)
                {
                    BattleManager.Instance.UpdateTeamInfo(updatedTeam);
                }
            }
        }

        #region 错误处理和重�?        
        /// <summary>
        /// 处理断开连接
        /// </summary>
        /// <param name="cause">断开原因</param>
        private void HandleDisconnection(DisconnectCause cause)
        {
            Debug.LogFormat("处理断开连接: {0}", cause);
            
            // handle differently based on disconnect cause
            switch (cause)
            {
                case DisconnectCause.None:
                    Debug.Log("Normal disconnection");
                    break;
                case DisconnectCause.ServerTimeout:
                    Debug.Log("Server timeout");
                    AttemptReconnection();
                    break;
                case DisconnectCause.ClientTimeout:
                    Debug.Log("Client timeout");
                    AttemptReconnection();
                    break;
                default:
                    Debug.LogFormat("其他断开原因: {0}", cause);
                    AttemptReconnection();
                    break;
            }
        }
        
        /// <summary>
        /// 尝试重连
        /// </summary>
        private void AttemptReconnection()
        {
            if (reconnectionAttempts < maxReconnectionAttempts)
            {
                reconnectionAttempts++;
                Debug.LogFormat("Reconnecting ({0}/{1})...", reconnectionAttempts, maxReconnectionAttempts);
                
                // stop existing reconnection coroutine
                if (reconnectionCoroutine != null)
                {
                    StopCoroutine(reconnectionCoroutine);
                }
                
                // 延迟重连
                reconnectionCoroutine = StartCoroutine(ReconnectAfterDelay());
            }
            else
            {
                Debug.LogError("Reconnection failed, max attempts reached");
                // show error UI to player
            }
        }
        
        /// <summary>
        /// 延迟重连协程
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReconnectAfterDelay()
        {
            yield return new WaitForSeconds(reconnectionDelay);
            
            Debug.Log("正在重连到Photon服务�?..");
            PhotonNetwork.ConnectUsingSettings();
        }
        
        /// <summary>
        /// 处理网络错误
        /// </summary>
        /// <param name="errorCode">错误代码</param>
        /// <param name="errorMessage">错误信息</param>
        private void HandleNetworkError(short errorCode, string errorMessage)
        {
            Debug.LogErrorFormat("网络错误: {0}, {1}", errorCode, errorMessage);
            
            // handle differently based on error code
            switch (errorCode)
            {
                case 32752: // Unsupported Plugin
                    Debug.LogError("Error: Unsupported plugin, please check PhotonServerSettings");
                    break;
                case 32767: // Invalid Operation Code
                    Debug.LogError("Error: Invalid operation code");
                    break;
                default:
                    Debug.LogErrorFormat("未知网络错误: {0}, {1}", errorCode, errorMessage);
                    break;
            }
        }
        
        #endregion
        
        #region 阵营管理
        
        /// <summary>
        /// 分配玩家阵营
        /// </summary>
        private void AssignPlayerTeam()
        {
            Debug.Log("开始分配阵�?..");
            try
            {
                int localActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
                Debug.LogFormat("本地玩家ActorNumber: {0}", localActorNumber);

                Player[] sortedPlayers = PhotonNetwork.PlayerList.OrderBy(player => player.ActorNumber).ToArray();
                int playerIndex = System.Array.FindIndex(sortedPlayers, player => player == PhotonNetwork.LocalPlayer);
                Debug.LogFormat("本地玩家在按ActorNumber排序后的房间索引: {0}", playerIndex);

                byte assignedTeamCode;
                if (playerIndex == 0)
                {
                    assignedTeamCode = 1;
                    Debug.Log("分配为蓝方（排序后第一个玩家）");
                }
                else if (playerIndex == 1)
                {
                    assignedTeamCode = 2;
                    Debug.Log("Assigned as Red team (second player after sorting)");
                }
                else
                {
                    assignedTeamCode = (byte)((playerIndex % 2) + 1);
                    Debug.LogFormat("分配为阵�? {0}", assignedTeamCode == 1 ? "蓝方" : "红方");
                }

                currentRoomTeam = assignedTeamCode;
                Debug.LogFormat("准备为本地玩�?{0} 分配阵营: {1}", localActorNumber, assignedTeamCode == 1 ? "蓝方" : "红方");

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props[TeamKey] = assignedTeamCode;
                PhotonNetwork.LocalPlayer.SetCustomProperties(props);

                Debug.LogFormat("本地玩家 {0} 已分配到阵营: {1}", localActorNumber, assignedTeamCode == 1 ? "蓝方" : "红方");

                if (KingdomWar.Game.Battle.BattleManager.Instance != null)
                {
                    KingdomWar.Game.Battle.BattleManager.Instance.UpdateTeamInfo(assignedTeamCode);
                    Debug.Log("BattleManager team info updated");
                }
                else
                {
                    Debug.Log("BattleManager instance not found, team info will be updated on scene load");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogErrorFormat("分配阵营时出�? {0}", e.Message);
                Debug.LogErrorFormat("异常堆栈: {0}", e.StackTrace);
            }
        }
        
        /// <summary>
        /// 获取本地玩家的阵�?        /// </summary>
        /// <returns>阵营代码 (1=蓝方, 2=红方)</returns>
        public byte GetLocalPlayerTeam()
        {
            Debug.Log("NetworkManager.GetLocalPlayerTeam() called");
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                Debug.Log("已连接到Photon并且在房间中");
                object teamCode;
                if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(TeamKey, out teamCode))
                {
                    byte code = (byte)teamCode;
                    currentRoomTeam = code;
                    Debug.LogFormat("从CustomProperties获取到阵营代�? {0}", code);
                    return code;
                }

                if (currentRoomTeam != 0)
                {
                    Debug.LogWarning($"CustomProperties中没�?{TeamKey}'键，回退到当前房间缓存阵�? {currentRoomTeam}");
                    return currentRoomTeam;
                }

                Debug.LogError("当前房间中无法获取本地玩家阵营，返回0等待后续同步");
                return 0;
            }

            Debug.LogWarning("未连接到Photon或不在房间中，返�?");
            return 0;
        }
        
        /// <summary>
        /// 检查本地玩家是否是蓝方
        /// </summary>
        /// <returns>是否是蓝�?/returns>
        public bool IsLocalPlayerBlueTeam()
        {
            return GetLocalPlayerTeam() == 1;
        }
        
        /// <summary>
        /// 检查本地玩家是否是红方
        /// </summary>
        /// <returns>是否是红�?/returns>
        public bool IsLocalPlayerRedTeam()
        {
            return GetLocalPlayerTeam() == 2;
        }
        
        #endregion
        
        #region 网络状态检�?        
        /// <summary>
        /// 检查是否已连接到Photon
        /// </summary>
        /// <returns>是否已连�?/returns>
        public bool IsConnected()
        {
            return PhotonNetwork.IsConnected;
        }
        
        /// <summary>
        /// 检查是否在房间�?        /// </summary>
        /// <returns>是否在房间中</returns>
        public bool IsInRoom()
        {
            return PhotonNetwork.IsConnected && PhotonNetwork.InRoom;
        }
        
        /// <summary>
        /// 检查是否正在连�?        /// </summary>
        /// <returns>是否正在连接</returns>
        public bool IsConnecting()
        {
            return isConnecting;
        }
        
        #endregion
    }

}

