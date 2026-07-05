using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace KingdomWar.Game.Battle
{
    public class NetworkBattleManager : MonoBehaviourPunCallbacks
    {
        public static NetworkBattleManager Instance { get; private set; }

        [Header("网络战斗设置")]
        public float syncInterval = BattleSyncConfig.DEFAULT_SYNC_INTERVAL;
        public float minSyncInterval = BattleSyncConfig.MIN_SYNC_INTERVAL;
        public float maxSyncInterval = BattleSyncConfig.MAX_SYNC_INTERVAL;
        public int maxSyncRetries = BattleSyncConfig.MAX_RETRIES;

        [Header("战斗状态")]
        public BattleStatus battleStatus = BattleStatus.Waiting;
        public float countdownTime = 3f;

        [Header("同步服务")]
        public bool useBattleSyncService = true;

        private Dictionary<int, PlayerData> playerData = new Dictionary<int, PlayerData>();
        private Dictionary<int, BuildingSyncData> buildingSyncData = new Dictionary<int, BuildingSyncData>();
        private Dictionary<int, UnitSyncData> unitSyncData = new Dictionary<int, UnitSyncData>();
        private int nextBuildingId = 1;
        private int nextUnitId = 1;

        private float lastSyncTime = 0f;
        private bool isSyncing = false;
        private int networkQuality = 5;
        private float lastNetworkCheckTime = 0f;
        private float networkCheckInterval = 5f;
        private float lastFullSyncTime = 0f;
        private float fullSyncInterval = 1f;

        private BattleSyncService syncService;
        private BattleEventSystem eventSystem;
        private PhotonView photonViewComponent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                EnsurePhotonView();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (syncService != null)
            {
                syncService.OnEventReceived -= HandleSyncEventReceived;
                syncService.OnSnapshotReceived -= HandleSnapshotReceived;
                syncService.OnConflictDetected -= HandleConflictDetected;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void EnsurePhotonView()
        {
            photonViewComponent = GetComponent<PhotonView>();
            if (photonViewComponent == null)
            {
                photonViewComponent = gameObject.AddComponent<PhotonView>();
            }
            
            if (photonViewComponent.ViewID == 0 && PhotonNetwork.IsConnected)
            {
                photonViewComponent.ViewID = PhotonNetwork.AllocateViewID(0);
            }
            Debug.Log($"[NetworkBattleManager] PhotonView initialized, ViewID: {photonViewComponent.ViewID}");
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            
            if (photonViewComponent != null && photonViewComponent.ViewID == 0)
            {
                photonViewComponent.ViewID = PhotonNetwork.AllocateViewID(0);
                Debug.Log($"[NetworkBattleManager] Allocated ViewID on join: {photonViewComponent.ViewID}");
            }
        }

        private void Start()
        {
            InitializePlayerData();
            InitializeSyncServices();
            StartCoroutine(SyncCoroutine());
        }

        private void OnEnable()
        {
            InitializePlayerData();
        }

        private void InitializeSyncServices()
        {
            syncService = BattleSyncService.Instance;
            eventSystem = BattleEventSystem.Instance;

            Debug.Log($"[NetworkBattleManager] Sync services: Service={syncService != null}, EventSystem={eventSystem != null}");

            if (syncService != null)
            {
                syncService.OnEventReceived += HandleSyncEventReceived;
                syncService.OnSnapshotReceived += HandleSnapshotReceived;
                syncService.OnConflictDetected += HandleConflictDetected;
            }

            Debug.Log("[NetworkBattleManager] Sync services initialized");
        }

        private void HandleSyncEventReceived(BattleSyncEvent syncEvent)
        {
            if (PhotonNetwork.IsMasterClient) return;

            switch (syncEvent.eventType)
            {
                case BattleSyncEventType.ElixirUpdate:
                    ProcessElixirUpdate(syncEvent);
                    break;
                case BattleSyncEventType.BattleStart:
                    RPC_StartBattle();
                    break;
                case BattleSyncEventType.BattleEnd:
                    using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
                    using (var reader = new System.IO.BinaryReader(stream))
                    {
                        int winnerId = reader.ReadInt32();
                        RPC_EndBattle(winnerId);
                    }
                    break;
            }
        }

        private void HandleSnapshotReceived(BattleStateSnapshot snapshot)
        {
            if (PhotonNetwork.IsMasterClient) return;

            battleStatus = snapshot.battleStatus;

            foreach (var player in snapshot.players)
            {
                if (playerData.ContainsKey(player.playerId))
                {
                    playerData[player.playerId].elixir = player.elixir;
                }
            }
        }

        private void HandleConflictDetected(int sequenceId)
        {
            Debug.LogWarning($"[NetworkBattleManager] Conflict detected for sequence {sequenceId}");
        }

        private void ProcessElixirUpdate(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int playerId = reader.ReadInt32();
                float elixir = reader.ReadSingle();

                if (playerData.ContainsKey(playerId))
                {
                    playerData[playerId].elixir = elixir;
                }
            }
        }

        private void Update()
        {
            CheckNetworkQuality();
        }

        /// <summary>
        /// 检查网络质量
        /// </summary>
        private void CheckNetworkQuality()
        {
            if (Time.time - lastNetworkCheckTime >= networkCheckInterval)
            {
                lastNetworkCheckTime = Time.time;
                
                // 检查网络连接状态
                if (PhotonNetwork.IsConnected)
                {
                    // 基于Ping值评估网络质量
                    int ping = PhotonNetwork.GetPing();
                    
                    if (ping < 50)
                    {
                        networkQuality = 5; // 优秀
                    }
                    else if (ping < 100)
                    {
                        networkQuality = 4; // 良好
                    }
                    else if (ping < 150)
                    {
                        networkQuality = 3; // 一般
                    }
                    else if (ping < 200)
                    {
                        networkQuality = 2; // 较差
                    }
                    else
                    {
                        networkQuality = 1; // 很差
                    }
                    
                    // 根据网络质量调整同步间隔
                    AdjustSyncInterval();
                    
                    Debug.LogFormat("网络质量检查: Ping={0}ms, 质量={1}, 同步间隔={2}s", ping, networkQuality, syncInterval);
                }
            }
        }

        /// <summary>
        /// 根据网络质量调整同步间隔
        /// </summary>
        private void AdjustSyncInterval()
        {
            switch (networkQuality)
            {
                case 5: // 优秀
                    syncInterval = minSyncInterval;
                    break;
                case 4: // 良好
                    syncInterval = minSyncInterval * 1.2f;
                    break;
                case 3: // 一般
                    syncInterval = (minSyncInterval + maxSyncInterval) / 2;
                    break;
                case 2: // 较差
                    syncInterval = maxSyncInterval * 0.8f;
                    break;
                case 1: // 很差
                    syncInterval = maxSyncInterval;
                    break;
            }
            
            // 确保同步间隔在合理范围内
            syncInterval = Mathf.Clamp(syncInterval, minSyncInterval, maxSyncInterval);
        }

        /// <summary>
        /// 初始化玩家数据
        /// </summary>
        private void InitializePlayerData()
        {
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                // 为每个玩家创建数据
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    int playerId = player.ActorNumber;
                    playerData[playerId] = new PlayerData
                    {
                        playerId = playerId,
                        elixir = 0,
                        maxElixir = 10,
                        elixirRecoveryRate = 0.8f
                    };
                }
            }
        }

        private IEnumerator SyncCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(syncInterval);
                SyncGameState();
            }
        }

        private void SyncGameState()
        {
            if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom || isSyncing)
                return;

            isSyncing = true;

            try
            {
                SyncPlayerStates();
                SyncBattleState();
                SyncBuildingStates();
                SyncUnitStates();

                if (useBattleSyncService && syncService != null && PhotonNetwork.IsMasterClient)
                {
                    if (Time.time - lastFullSyncTime >= fullSyncInterval)
                    {
                        lastFullSyncTime = Time.time;
                        syncService.SyncFullState();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"同步游戏状态失败: {e.Message}");
            }
            finally
            {
                isSyncing = false;
            }
        }

        /// <summary>
        /// 同步玩家状态
        /// </summary>
        private void SyncPlayerStates()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // Master Client同步所有玩家状态
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    int playerId = player.ActorNumber;
                    if (playerData.ContainsKey(playerId))
                    {
                        // 更新玩家属性
                        ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                playerProps["Elixir"] = playerData[playerId].elixir;
                playerProps["PlayerId"] = playerId;
                player.SetCustomProperties(playerProps);
                    }
                }
            }
            else
            {
                // 普通客户端只同步自己的状态
                int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
                if (playerData.ContainsKey(localPlayerId))
                {
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["Elixir"] = playerData[localPlayerId].elixir;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
                }
            }
        }

        /// <summary>
        /// 同步战斗状态
        /// </summary>
        private void SyncBattleState()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // 同步房间状态
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["BattleStatus"] = (int)battleStatus;
                roomProps["CountdownTime"] = countdownTime;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }

        private void SyncBuildingStates()
        {
            if (PhotonNetwork.IsMasterClient && BattleManager.Instance != null)
            {
                List<object> buildingsData = new List<object>();
                foreach (Building building in BattleManager.Instance.buildings)
                {
                    if (building != null)
                    {
                        object[] buildingInfo = new object[]
                        {
                            building.GetInstanceID(),
                            (int)building.buildingType,
                            building.transform.position,
                            building.ownerId,
                            building.health,
                            building.maxHealth,
                            (int)building.state
                        };
                        buildingsData.Add(buildingInfo);
                    }
                }

                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["BuildingsData"] = buildingsData;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }

        private void SyncUnitStates()
        {
            if (PhotonNetwork.IsMasterClient && BattleManager.Instance != null)
            {
                List<object> unitsData = new List<object>();
                foreach (Unit unit in BattleManager.Instance.Units)
                {
                    if (unit != null)
                    {
                        object[] unitInfo = new object[]
                        {
                            unit.GetInstanceID(),
                            unit.ownerId,
                            unit.transform.position,
                            unit.transform.rotation,
                            unit.health,
                            unit.maxHealth,
                            (int)unit.state,
                            unit.state == UnitState.Attacking ? 1 : 0,
                            unit.state == UnitState.Moving ? 1 : 0
                        };
                        unitsData.Add(unitInfo);
                    }
                }

                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["UnitsData"] = unitsData;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }

        /// <summary>
        /// 更新建筑状态
        /// </summary>
        /// <param name="buildingsData">建筑数据列表</param>
        private void UpdateBuildingStates(List<object> buildingsData)
        {
            if (!PhotonNetwork.IsMasterClient && BattleManager.Instance != null)
            {
                // 这里可以添加建筑状态的更新逻辑
                // 例如，根据服务器发送的建筑数据，更新本地建筑的状态
                // 注意：这里需要处理建筑的创建、更新和删除逻辑
                Debug.LogFormat("收到建筑同步数据，数量: {0}", buildingsData.Count);
            }
        }

        /// <summary>
        /// 开始战斗倒计时
        /// </summary>
        public void StartBattleCountdown()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                battleStatus = BattleStatus.Countdown;
                StartCoroutine(CountdownCoroutine());
            }
        }

        /// <summary>
        /// 倒计时协程
        /// </summary>
        private IEnumerator CountdownCoroutine()
        {
            float currentCountdown = countdownTime;

            while (currentCountdown > 0)
            {
                yield return new WaitForSeconds(1f);
                currentCountdown -= 1f;

                // 同步倒计时
                if (PhotonNetwork.IsMasterClient)
                {
                    ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                    roomProps["CountdownTime"] = currentCountdown;
                    PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
                }
            }

            // 倒计时结束，开始战斗
            StartBattle();
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartBattle()
        {
            ResetBattleState();
            
            if (PhotonNetwork.IsMasterClient)
            {
                battleStatus = BattleStatus.Fighting;

                photonViewComponent.RPC("RPC_StartBattle", RpcTarget.All);

                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["BattleStatus"] = (int)battleStatus;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    SetPlayerOperationPermission(player.ActorNumber, true);
                }
            }
        }
        
        private void ResetBattleState()
        {
            Debug.Log("[NetworkBattleManager] 重置战斗状态...");
            
            playerData.Clear();
            buildingSyncData.Clear();
            unitSyncData.Clear();
            
            nextBuildingId = 1;
            nextUnitId = 1;
            lastSyncTime = 0f;
            isSyncing = false;
            lastFullSyncTime = 0f;
            
            InitializePlayerData();
            
            battleStatus = BattleStatus.Waiting;
            
            Debug.Log("[NetworkBattleManager] 战斗状态重置完成");
        }

        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndBattle(int winnerId)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                battleStatus = BattleStatus.Ended;

                // 广播战斗结束
                photonViewComponent.RPC("RPC_EndBattle", RpcTarget.All, winnerId);

                // 同步战斗结果
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["BattleStatus"] = (int)battleStatus;
                roomProps["Winner"] = winnerId;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // 禁用所有玩家的操作权限
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    SetPlayerOperationPermission(player.ActorNumber, false);
                }
            }
        }

        /// <summary>
        /// 暂停战斗
        /// </summary>
        public void PauseBattle()
        {
            if (PhotonNetwork.IsMasterClient && battleStatus == BattleStatus.Fighting)
            {
                battleStatus = BattleStatus.Paused;

                // 广播战斗暂停
                photonViewComponent.RPC("RPC_PauseBattle", RpcTarget.All);

                // 同步战斗状态
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["BattleStatus"] = (int)battleStatus;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // 禁用所有玩家的操作权限
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    SetPlayerOperationPermission(player.ActorNumber, false);
                }
            }
        }

        /// <summary>
        /// 恢复战斗
        /// </summary>
        public void ResumeBattle()
        {
            if (PhotonNetwork.IsMasterClient && battleStatus == BattleStatus.Paused)
            {
                battleStatus = BattleStatus.Fighting;

                // 广播战斗恢复
                photonViewComponent.RPC("RPC_ResumeBattle", RpcTarget.All);

                // 同步战斗状态
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["BattleStatus"] = (int)battleStatus;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // 恢复所有玩家的操作权限
                foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
                {
                    SetPlayerOperationPermission(player.ActorNumber, true);
                }
            }
        }

        /// <summary>
        /// 获取当前战斗状态
        /// </summary>
        /// <returns>战斗状态</returns>
        public BattleStatus GetBattleStatus()
        {
            return battleStatus;
        }

        /// <summary>
        /// 检查战斗是否正在进行
        /// </summary>
        /// <returns>是否正在进行</returns>
        public bool IsBattleActive()
        {
            return battleStatus == BattleStatus.Fighting;
        }

        /// <summary>
        /// 检查战斗是否已结束
        /// </summary>
        /// <returns>是否已结束</returns>
        public bool IsBattleEnded()
        {
            return battleStatus == BattleStatus.Ended;
        }

        /// <summary>
        /// 放置单位
        /// </summary>
        /// <param name="unitType">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        public void PlaceUnit(int unitType, Vector3 position, int playerId)
        {
            // 广播单位放置
            photonViewComponent.RPC("RPC_PlaceUnit", RpcTarget.All, unitType, position, playerId);
        }

        /// <summary>
        /// 放置建筑
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        public void PlaceBuilding(int buildingType, Vector3 position, int playerId)
        {
            // 广播建筑放置
            photonViewComponent.RPC("RPC_PlaceBuilding", RpcTarget.All, buildingType, position, playerId);
        }

        /// <summary>
        /// 释放法术
        /// </summary>
        /// <param name="spellType">法术类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        public void CastSpell(int spellType, Vector3 position, int playerId)
        {
            // 广播法术释放
            photonViewComponent.RPC("RPC_CastSpell", RpcTarget.All, spellType, position, playerId);
        }

        /// <summary>
        /// 更新玩家圣水
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="amount">圣水数量</param>
        public void UpdatePlayerElixir(int playerId, float amount)
        {
            if (playerData.ContainsKey(playerId))
            {
                playerData[playerId].elixir = amount;

                // 同步圣水变化
                if (PhotonNetwork.LocalPlayer.ActorNumber == playerId)
                {
                    ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
                    playerProps["Elixir"] = amount;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
                }
            }
        }

        /// <summary>
        /// 消耗玩家圣水（网络同步）
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="amount">消耗数量</param>
        /// <returns>是否消耗成功</returns>
        public bool ConsumeElixir(int playerId, float amount)
        {
            if (playerData.ContainsKey(playerId))
            {
                PlayerData player = playerData[playerId];
                if (player.elixir >= amount)
                {
                    player.elixir -= amount;
                    UpdatePlayerElixir(playerId, player.elixir);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取玩家圣水数量
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>圣水数量</returns>
        public float GetPlayerElixir(int playerId)
        {
            if (playerData.ContainsKey(playerId))
            {
                return playerData[playerId].elixir;
            }
            return 0f;
        }

        /// <summary>
        /// 检查玩家是否有足够的圣水
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="amount">需要的圣水数量</param>
        /// <returns>是否有足够的圣水</returns>
        public bool HasEnoughElixir(int playerId, float amount)
        {
            return GetPlayerElixir(playerId) >= amount;
        }

        /// <summary>
        /// 设置玩家操作权限
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="canOperate">是否可以操作</param>
        public void SetPlayerOperationPermission(int playerId, bool canOperate)
        {
            if (playerData.ContainsKey(playerId))
            {
                // 这里可以添加操作权限的管理逻辑
                // 例如，在特定状态下禁止玩家操作
                Debug.Log($"设置玩家 {playerId} 的操作权限: {canOperate}");
            }
        }

        /// <summary>
        /// 检查玩家是否可以操作
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否可以操作</returns>
        public bool CanPlayerOperate(int playerId)
        {
            // 检查是否是本地玩家
            if (!IsLocalPlayer(playerId))
            {
                return false;
            }

            // 检查战斗状态
            if (battleStatus != BattleStatus.Fighting)
            {
                return false;
            }

            // 检查玩家数据是否存在
            if (!playerData.ContainsKey(playerId))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取玩家数据
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家数据</returns>
        public PlayerData GetPlayerData(int playerId)
        {
            if (playerData.ContainsKey(playerId))
            {
                return playerData[playerId];
            }
            return null;
        }

        /// <summary>
        /// 检查是否是本地玩家
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否是本地玩家</returns>
        public bool IsLocalPlayer(int playerId)
        {
            return PhotonNetwork.IsConnected && PhotonNetwork.LocalPlayer.ActorNumber == playerId;
        }

        #region RPC Methods

        [PunRPC]
        private void RPC_StartBattle()
        {
            battleStatus = BattleStatus.Fighting;
            Debug.Log("战斗开始！");

            // 通知BattleManager战斗开始
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartBattle();
            }
        }

        [PunRPC]
        private void RPC_EndBattle(int winnerId)
        {
            battleStatus = BattleStatus.Ended;
            Debug.Log($"战斗结束！获胜者: {winnerId}");

            // 通知BattleManager战斗结束
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.EndBattle();
            }
        }

        [PunRPC]
        private void RPC_PauseBattle()
        {
            battleStatus = BattleStatus.Paused;
            Debug.Log("战斗暂停！");

            // 通知BattleManager战斗暂停
            if (BattleManager.Instance != null)
            {
                // TODO: 实现BattleManager的暂停方法
                // BattleManager.Instance.PauseBattle();
            }
        }

        [PunRPC]
        private void RPC_ResumeBattle()
        {
            battleStatus = BattleStatus.Fighting;
            Debug.Log("战斗恢复！");

            // 通知BattleManager战斗恢复
            if (BattleManager.Instance != null)
            {
                // TODO: 实现BattleManager的恢复方法
                // BattleManager.Instance.ResumeBattle();
            }
        }

        [PunRPC]
        private void RPC_PlaceUnit(int unitType, Vector3 position, int playerId)
        {
            Debug.Log($"放置单位: 类型={unitType}, 位置={position}, 玩家={playerId}");

            // 通知BattleManager放置单位
            if (BattleManager.Instance != null)
            {
                // 这里需要根据实际的单位创建逻辑来实现
                // BattleManager.Instance.PlaceUnit(unitType, position, playerId);
            }
        }

        [PunRPC]
        private void RPC_PlaceBuilding(int buildingType, Vector3 position, int playerId)
        {
            Debug.Log($"放置建筑: 类型={buildingType}, 位置={position}, 玩家={playerId}");

            // 通知BattleManager放置建筑
            if (BattleManager.Instance != null)
            {
                // 在非本地玩家操作时，直接创建建筑而不检查圣水和权限
                // 因为这些检查已经在发起操作的客户端完成
                if (!IsLocalPlayer(playerId))
                {
                    Building building = BattleManager.Instance.CreateBuilding(buildingType, position, playerId);
                    if (building != null)
                    {
                        BattleManager.Instance.AddBuilding(building);
                    }
                }
                else
                {
                    // 本地玩家操作，使用正常的放置逻辑
                    BattleManager.Instance.PlaceBuilding(buildingType, position, playerId);
                }
            }
        }

        [PunRPC]
        private void RPC_CastSpell(int spellType, Vector3 position, int playerId)
        {
            Debug.Log($"释放法术: 类型={spellType}, 位置={position}, 玩家={playerId}");

            // 通知BattleManager释放法术
            if (BattleManager.Instance != null)
            {
                // 这里需要根据实际的法术释放逻辑来实现
                // BattleManager.Instance.CastSpell(spellType, position, playerId);
            }
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            // 为新玩家创建数据
            int playerId = newPlayer.ActorNumber;
            playerData[playerId] = new PlayerData
            {
                playerId = playerId,
                elixir = 0,
                maxElixir = 10,
                elixirRecoveryRate = 0.8f
            };

            // 获取玩家阵营信息
            byte playerTeam = 1; // 默认蓝方
            object teamCode;
            if (newPlayer.CustomProperties.TryGetValue("team", out teamCode))
            {
                playerTeam = (byte)teamCode;
                Debug.LogFormat("玩家 {0} 加入，阵营: {1}", playerId, playerTeam == 1 ? "蓝方" : "红方");
            }
            else
            {
                Debug.LogFormat("玩家 {0} 加入，未设置阵营，默认蓝方", playerId);
            }

            Debug.Log($"玩家加入: ID={playerId}");

            // 如果房间已满，开始战斗倒计时
            if (PhotonNetwork.PlayerList.Length >= 2 && PhotonNetwork.IsMasterClient)
            {
                StartBattleCountdown();
            }
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);

            // 移除离开玩家的数据
            int playerId = otherPlayer.ActorNumber;
            if (playerData.ContainsKey(playerId))
            {
                playerData.Remove(playerId);
            }

            Debug.Log($"玩家离开: ID={playerId}");

            // 如果玩家离开，结束战斗
            if (PhotonNetwork.PlayerList.Length < 2)
            {
                EndBattle(PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }

        public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // 更新玩家属性
            int playerId = targetPlayer.ActorNumber;
            if (playerData.ContainsKey(playerId))
            {
                if (changedProps.ContainsKey("Elixir"))
                {
                    playerData[playerId].elixir = (float)changedProps["Elixir"];
                }
            }
        }

        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("BattleStatus"))
            {
                battleStatus = (BattleStatus)(int)propertiesThatChanged["BattleStatus"];
            }

            if (propertiesThatChanged.ContainsKey("CountdownTime"))
            {
                countdownTime = (float)propertiesThatChanged["CountdownTime"];
            }

            if (propertiesThatChanged.ContainsKey("Winner"))
            {
                int winnerId = (int)propertiesThatChanged["Winner"];
                Debug.Log($"战斗结果: 获胜者={winnerId}");
            }

            if (propertiesThatChanged.ContainsKey("BuildingsData"))
            {
                List<object> buildingsData = (List<object>)propertiesThatChanged["BuildingsData"];
                UpdateBuildingStates(buildingsData);
            }

            if (propertiesThatChanged.ContainsKey("UnitsData"))
            {
                List<object> unitsData = (List<object>)propertiesThatChanged["UnitsData"];
                UpdateUnitStates(unitsData);
            }
        }

        private void UpdateUnitStates(List<object> unitsData)
        {
            if (!PhotonNetwork.IsMasterClient && BattleManager.Instance != null)
            {
                foreach (object[] unitInfo in unitsData)
                {
                    try
                    {
                        int unitId = (int)unitInfo[0];
                        int ownerId = (int)unitInfo[1];
                        Vector3 position = (Vector3)unitInfo[2];
                        Quaternion rotation = (Quaternion)unitInfo[3];
                        int health = (int)unitInfo[4];
                        int maxHealth = (int)unitInfo[5];
                        UnitState state = (UnitState)(int)unitInfo[6];
                        bool isAttacking = (int)unitInfo[7] == 1;
                        bool isMoving = (int)unitInfo[8] == 1;

                        var syncData = new UnitSyncData
                        {
                            unitId = unitId,
                            ownerId = ownerId,
                            position = position,
                            rotation = rotation,
                            health = health,
                            maxHealth = maxHealth,
                            state = state,
                            isAttacking = isAttacking,
                            isMoving = isMoving
                        };

                        unitSyncData[unitId] = syncData;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"解析单位同步数据失败: {e.Message}");
                    }
                }

                ApplyUnitSyncData();
            }
        }

        private void ApplyUnitSyncData()
        {
            if (BattleManager.Instance == null) return;

            foreach (var unit in BattleManager.Instance.Units)
            {
                if (unit == null) continue;

                int unitId = unit.GetInstanceID();
                if (unitSyncData.TryGetValue(unitId, out var syncData))
                {
                    if (!PhotonNetwork.LocalPlayer.ActorNumber.Equals(unit.ownerId))
                    {
                        unit.transform.position = Vector3.Lerp(unit.transform.position, syncData.position, 0.5f);
                        unit.transform.rotation = Quaternion.Lerp(unit.transform.rotation, syncData.rotation, 0.5f);
                        
                        // 直接同步血量，而不是调用 TakeDamage
                        // 因为 RPC_TakeDamage 已经处理了血量修改，这里只需要确保同步
                        if (unit.health != syncData.health)
                        {
                            // 只在血量减少时同步（避免回血被覆盖）
                            if (syncData.health < unit.health)
                            {
                                unit.health = syncData.health;
                                Debug.Log($"[NetworkBattleManager] 同步单位 {unit.unitName} 血量: {unit.health}");
                            }
                        }
                    }
                }
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);

            Debug.Log($"网络断开: {cause}");

            // 清理玩家数据
            playerData.Clear();

            // 重置战斗状态
            battleStatus = BattleStatus.Waiting;
        }

        #endregion

        #region 建筑同步数据类

        /// <summary>
        /// 建筑同步数据
        /// </summary>
        public class BuildingSyncData
        {
            public int buildingId;
            public int buildingType;
            public Vector3 position;
            public int playerId;
            public int health;
            public int maxHealth;
            public BuildingState state;

            public BuildingSyncData(int buildingId, int buildingType, Vector3 position, int playerId, int health, int maxHealth, BuildingState state)
            {
                this.buildingId = buildingId;
                this.buildingType = buildingType;
                this.position = position;
                this.playerId = playerId;
                this.health = health;
                this.maxHealth = maxHealth;
                this.state = state;
            }
        }

        #endregion
    }
}