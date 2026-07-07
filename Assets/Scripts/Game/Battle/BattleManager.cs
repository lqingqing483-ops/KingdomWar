using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using KingdomWar.Server;
using KingdomWar.Game;
using KingdomWar.Game.AI;
using KingdomWar.Game.Cards;
using KingdomWar.UI;
using KingdomWar.Game.Arena;
using KingdomWar.HotUpdate;
using KingdomWar.Game.SeasonPass;
using Photon.Pun;
using Photon.Realtime;

namespace KingdomWar.Game.Battle
{
    public class BattleManager : MonoBehaviour, IBattleManager
    {
        public static BattleManager Instance { get; private set; }
        
        [Header("战斗设置")]
        public BattleStatus battleStatus = BattleStatus.Waiting; // 战斗状态
        public float matchTime = 180f; // 比赛时间（秒）
        public float overtimeTime = 120f; // 加时时间（秒）
        public bool isOvertime = false; // 是否进入加时
        public Text battleTimeText;
        public Text enemyNamerText;
        
        [Header("玩家设置")]
        public PlayerData player1; // 玩家1数据
        public PlayerData player2; // 玩家2数据
        
        [Header("场景设置")]
        public Transform player1SpawnArea; // 玩家1出生区域
        public Transform player2SpawnArea; // 玩家2出生区域
        
        [Header("网络设置")]
        public bool isNetworkBattle = false; // 是否是网络对战
        
        [Header("阵营设置")]
        public byte localPlayerTeam = 1; // 本地玩家阵营 (1=蓝方, 2=红方)
        public Camera blueTeamCamera; // 蓝方摄像机
        public Camera redTeamCamera; // 红方摄像机
        
        private float currentTime; // 当前时间
        public float CurrentTime { get { return currentTime; } }
        private List<Unit> units = new List<Unit>(); // 所有单位
        public List<Building> buildings = new List<Building>(); // 所有建筑
        private List<Spell> spells = new List<Spell>(); // 所有法术
        public List<Unit> Units {get{return units;}}
        public List<Building> Buildings {get{return buildings;}}
        public List<Spell> Spells {get{return spells;}}

        // IBattleManager explicit interface implementations
        IReadOnlyList<Unit> IBattleManager.Units => Units;
        IReadOnlyList<Building> IBattleManager.Buildings => Buildings;
        bool IBattleManager.IsNetworkBattle => isNetworkBattle;
        byte IBattleManager.LocalPlayerTeam => localPlayerTeam;

        // 帧同步相关
        private FrameSyncManager frameSyncManager;
        private int currentFrame = 0;
        private const int TARGET_FPS = 30; // 目标帧率
        private float frameInterval = 1f / 30f; // 帧间隔
        private float lastFrameTime = 0f;
        private AIOpponentManager aiOpponent;
        private bool _trophyProcessed = false;
        private int _aiUpdateFrameCounter = 0;
        public TrophyChangeResult? LastTrophyResult { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Register with service locator
                KingdomWar.Core.ServiceLocator.Register<IBattleManager>(this);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        private void Initialize()
        {
            // 初始化玩家数据
            player1 = new PlayerData { playerId = 1, elixir = 0, maxElixir = 10, elixirRecoveryRate = 0.8f };
            player2 = new PlayerData { playerId = 2, elixir = 0, maxElixir = 10, elixirRecoveryRate = 0.8f };

            // 初始化帧同步管理器
            frameSyncManager = GetComponent<FrameSyncManager>();
            if (frameSyncManager == null)
            {
                frameSyncManager = gameObject.AddComponent<FrameSyncManager>();
            }

            // 初始化战斗状态
            currentTime = matchTime;

            // 初始化网络对战
            InitializeNetworkBattle();

            // Create AI opponent for local battles
            if (!isNetworkBattle)
            {
                InitializeAIOpponent();
            }
        }
        
        /// <summary>
        /// 初始化网络对战
        /// </summary>
        private void InitializeNetworkBattle()
        {
            // 检查是否是网络对战
            if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            {
                isNetworkBattle = true;
                Debug.Log("初始化网络对战...");
                
                // 获取本地玩家ID
                int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
                Debug.LogFormat("本地玩家ID: {0}", localPlayerId);
                
                // 初始化阵营信息
                InitializeTeamInfo();
                
                // 设置敌人名称
                if (enemyNamerText != null)
                {
                    enemyNamerText.text = "对手";
                }
                
                Debug.Log("网络对战初始化成功");
            }
            else
            {
                isNetworkBattle = false;
                Debug.Log("初始化本地对战...");
                // 本地战斗，使用默认设置
                localPlayerTeam = 1; // 默认蓝方
                SetupCameras();
            }
        }

        private void InitializeAIOpponent()
        {
            // Create AI for player 2 (opponent)
            aiOpponent = new AIOpponentManager(AIDifficulty.Medium, 2);
            
            // Subscribe to AI card play events
            aiOpponent.OnCardPlayed += OnAIPlayCard;
            
            Debug.Log("[BattleManager] AI Opponent initialized (Medium)");
        }

        private void OnAIPlayCard(string cardName, Vector3 position)
        {
            CardData card = CardDatabase.Instance.GetCardByName(cardName);
            if (card == null)
            {
                Debug.LogError($"[BattleManager] AI tried to play unknown card: {cardName}");
                return;
            }
            
            // Use NetworkEntityManager to spawn units (it handles local mode correctly)
            NetworkEntityManager entityManager = GetComponent<NetworkEntityManager>();
            if (entityManager == null)
            {
                entityManager = gameObject.AddComponent<NetworkEntityManager>();
            }
            
            // Choose spawn method based on card type
            if (card.cardType == CardType.Unit)
            {
                entityManager.SpawnUnitFromCard(card, position);
            }
            else if (card.cardType == CardType.Building)
            {
                entityManager.SpawnBuildingFromCard(card, position);
            }
            // Note: Spell cards are not yet handled by AI
        }
        
        /// <summary>
        /// 初始化阵营信息
        /// </summary>
        private void InitializeTeamInfo()
        {
            bool hasTeamInfo = false;

            if (KingdomWar.Server.NetworkManager.Instance != null)
            {
                byte networkTeam = KingdomWar.Server.NetworkManager.Instance.GetLocalPlayerTeam();
                if (networkTeam == 1 || networkTeam == 2)
                {
                    localPlayerTeam = networkTeam;
                    hasTeamInfo = true;
                    Debug.LogFormat("本地玩家阵营: {0}", localPlayerTeam == 1 ? "蓝方" : "红方");
                }
            }

            if (!hasTeamInfo)
            {
                try
                {
                    object teamCode;
                    if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("team", out teamCode))
                    {
                        localPlayerTeam = (byte)teamCode;
                        hasTeamInfo = localPlayerTeam == 1 || localPlayerTeam == 2;
                        Debug.LogFormat("从PhotonPlayer获取阵营: {0}", localPlayerTeam == 1 ? "蓝方" : "红方");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"获取阵营信息出错: {e.Message}");
                }
            }

            if (!hasTeamInfo)
            {
                Debug.LogWarning("当前未获取到有效阵营信息，保留现有阵营设置");
            }

            SetupCameras();
        }
        
        /// <summary>
        /// 设置摄像机
        /// </summary>
        private void SetupCameras()
        {
            // 确保摄像机引用存在
            if (blueTeamCamera != null && redTeamCamera != null)
            {
                if (localPlayerTeam == 1)
                {
                    blueTeamCamera.gameObject.SetActive(true);
                    redTeamCamera.gameObject.SetActive(false);
                    Debug.Log("激活蓝方摄像机");
                }
                else if (localPlayerTeam == 2)
                {
                    blueTeamCamera.gameObject.SetActive(false);
                    redTeamCamera.gameObject.SetActive(true);
                    Debug.Log("激活红方摄像机");
                }
                else
                {
                    blueTeamCamera.gameObject.SetActive(false);
                    redTeamCamera.gameObject.SetActive(false);
                    Debug.Log("当前未设置有效阵营，关闭战斗摄像机");
                }
            }
            else
            {
                Debug.LogWarning("摄像机引用未设置");
            }
        }
        
        /// <summary>
        /// 获取本地玩家阵营
        /// </summary>
        /// <returns>阵营代码 (1=蓝方, 2=红方)</returns>
        public byte GetLocalPlayerTeam()
        {
            return localPlayerTeam;
        }
        
        /// <summary>
        /// 更新阵营信息
        /// </summary>
        /// <param name="team">阵营代码 (1=蓝方, 2=红方)</param>
        public void UpdateTeamInfo(byte team)
        {
            localPlayerTeam = team;
            if (localPlayerTeam == 1 || localPlayerTeam == 2)
            {
                Debug.LogFormat("更新阵营信息: {0}", localPlayerTeam == 1 ? "蓝方" : "红方");
            }
            else
            {
                Debug.Log("清理本地阵营信息");
            }
            SetupCameras();
        }
        
        /// <summary>
        /// 检查本地玩家是否是蓝方
        /// </summary>
        /// <returns>是否是蓝方</returns>
        public bool IsLocalPlayerBlueTeam()
        {
            return localPlayerTeam == 1;
        }
        
        /// <summary>
        /// 检查本地玩家是否是红方
        /// </summary>
        /// <returns>是否是红方</returns>
        public bool IsLocalPlayerRedTeam()
        {
            return localPlayerTeam == 2;
        }
        
        /// <summary>
        /// 检查主塔状态，若一方主塔被摧毁则结束战斗
        /// </summary>
        private void CheckMainTowerStatus()
        {
            Building player1MainTower = buildings.Find(b => b != null && b.ownerId == 1 && b.buildingType == BuildingType.MainTower);
            Building player2MainTower = buildings.Find(b => b != null && b.ownerId == 2 && b.buildingType == BuildingType.MainTower);
            
            // 检查是否有一方主塔被摧毁
            if ((player1MainTower == null || player1MainTower.health <= 0) && (player2MainTower != null && player2MainTower.health > 0))
            {
                Debug.Log("玩家1主塔被摧毁，玩家2获胜！");
                EndBattle();
            }
            else if ((player2MainTower == null || player2MainTower.health <= 0) && (player1MainTower != null && player1MainTower.health > 0))
            {
                Debug.Log("玩家2主塔被摧毁，玩家1获胜！");
                EndBattle();
            }
        }
        
        /// <summary>
        /// 检查单位是否是友方
        /// </summary>
        /// <param name="unit">单位</param>
        /// <returns>是否是友方</returns>
        public bool IsFriendlyUnit(Unit unit)
        {
            // 根据单位的ownerId和本地玩家阵营判断
            if (localPlayerTeam == 1)
            {
                // 蓝方，ownerId为1的单位是友方
                return unit.ownerId == 1;
            }
            else
            {
                // 红方，ownerId为2的单位是友方
                return unit.ownerId == 2;
            }
        }
        
        /// <summary>
        /// 检查建筑是否是友方
        /// </summary>
        /// <param name="building">建筑</param>
        /// <returns>是否是友方</returns>
        public bool IsFriendlyBuilding(Building building)
        {
            // 根据建筑的ownerId和本地玩家阵营判断
            if (localPlayerTeam == 1)
            {
                // 蓝方，ownerId为1的建筑是友方
                return building.ownerId == 1;
            }
            else
            {
                // 红方，ownerId为2的建筑是友方
                return building.ownerId == 2;
            }
        }
        
        private void Start()
        {
            // 开始战斗
            StartBattle();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBattle();
                return;
            }

            if (battleStatus == BattleStatus.Fighting && (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)))
            {
                Surrender();
                return;
            }

            lastFrameTime += Time.deltaTime;
            if (lastFrameTime >= frameInterval)
            {
                ProcessFrame();
                lastFrameTime -= frameInterval;
            }

            if (battleStatus == BattleStatus.Fighting)
            {
                UpdateBattle();
            }
        }
        
        private void ProcessFrame()
        {
            // 处理当前帧的事件
            frameSyncManager.ProcessEvents(currentFrame);
            
            // 递增帧计数
            currentFrame++;
        }
        
        private void UpdateBattle()
        {
            // 检查主塔状态
            CheckMainTowerStatus();
            
            // 更新时间
            currentTime -= Time.deltaTime;
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            battleTimeText.text = string.Format("{0}:{1:00}", minutes, seconds);
            
            // 处理时间结束逻辑
            if (currentTime <= 0)
            {
                if (!isOvertime)
                {
                    // 检查是否需要进入加时
                    Building player1MainTower = buildings.Find(b => b != null && b.ownerId == 1 && b.buildingType == BuildingType.MainTower);
                    Building player2MainTower = buildings.Find(b => b != null && b.ownerId == 2 && b.buildingType == BuildingType.MainTower);
                    
                    if ((player1MainTower != null && player1MainTower.health > 0) && (player2MainTower != null && player2MainTower.health > 0))
                    {
                        // 双方主塔都还在，进入加时
                        Debug.Log("比赛时间结束，双方主塔都还在，进入加时！");
                        isOvertime = true;
                        currentTime = overtimeTime;
                    }
                    else
                    {
                        // 有一方主塔已经被摧毁，直接结束战斗
                        EndBattle();
                    }
                }
                else
                {
                    // 加时结束，结束战斗
                    Debug.Log("加时结束，结束战斗！");
                    EndBattle();
                }
            }
            
            // 更新玩家圣水
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                // 网络对战，使用NetworkBattleManager的数据
                foreach (Player player in PhotonNetwork.PlayerList)
                {
                    int playerId = player.ActorNumber;
                    PlayerData playerData = NetworkBattleManager.Instance.GetPlayerData(playerId);
                    if (playerData != null)
                    {
                        UpdatePlayerElixir(playerData);
                        NetworkBattleManager.Instance.UpdatePlayerElixir(playerId, playerData.elixir);
                    }
                }
            }
            else
            {
                // 本地对战，使用本地数据
                UpdatePlayerElixir(player1);
                UpdatePlayerElixir(player2);
            }
            
            // Update AI opponent
            if (!isNetworkBattle && aiOpponent != null)
            {
                // Sync AI elixir with player2's elixir
                aiOpponent.Elixir = player2.elixir;
                aiOpponent.UpdateAI(Time.deltaTime);
            }
            
            // 更新单位（带AI更新频率控制：每2帧执行一次，降低CPU开销）
            _aiUpdateFrameCounter++;
            bool shouldUpdateUnits = (_aiUpdateFrameCounter % 2 == 0); // 30fps → 15fps
            for (int i = units.Count - 1; i >= 0; i--)
            {
                if (units[i] != null)
                {
                    if (shouldUpdateUnits)
                        units[i].UpdateUnit();
                }
                else
                {
                    units.RemoveAt(i);
                }
            }
            
            // 更新建筑
            for (int i = buildings.Count - 1; i >= 0; i--)
            {
                if (buildings[i] != null)
                {
                    buildings[i].UpdateBuilding();
                }
                else
                {
                    buildings.RemoveAt(i);
                }
            }
            
            // 更新法术
            for (int i = spells.Count - 1; i >= 0; i--)
            {
                if (spells[i] != null)
                {
                    spells[i].UpdateSpell();
                }
                else
                {
                    spells.RemoveAt(i);
                }
            }
        }
        
        private void UpdatePlayerElixir(PlayerData player)
        {
            if (player.elixir < player.maxElixir)
            {
                player.elixir += player.elixirRecoveryRate * Time.deltaTime;
                if (player.elixir > player.maxElixir)
                {
                    player.elixir = player.maxElixir;
                }
            }
        }
        
        public void StartBattle()
        {
            ResetBattleState();
            battleStatus = BattleStatus.Fighting;
            currentTime = matchTime;
            Debug.Log("战斗开始！");
        }
        
        private void ResetBattleState()
        {
            if (aiOpponent != null)
            {
                aiOpponent.OnCardPlayed -= OnAIPlayCard;
                aiOpponent = null;
            }

            Debug.Log("[BattleManager] 重置战斗状态...");
            
            _trophyProcessed = false;
            LastTrophyResult = null;
            
            units.Clear();
            buildings.Clear();
            spells.Clear();
            
            player1 = new PlayerData { playerId = 1, elixir = 0, maxElixir = 10, elixirRecoveryRate = 0.8f };
            player2 = new PlayerData { playerId = 2, elixir = 0, maxElixir = 10, elixirRecoveryRate = 0.8f };
            
            currentTime = matchTime;
            isOvertime = false;
            currentFrame = 0;
            lastFrameTime = 0f;
            
            InitializeNetworkBattle();
            
            Debug.Log("[BattleManager] 战斗状态重置完成");
        }
        
        public void EndBattle()
        {
            battleStatus = BattleStatus.Ended;
            Debug.Log("战斗结束！");

            DetermineWinner();
        }

        public void Surrender()
        {
            Building localMainTower = buildings.Find(b => b != null && b.ownerId == localPlayerTeam && b.buildingType == BuildingType.MainTower);
            if (localMainTower == null)
            {
                Debug.LogWarning("未找到本地主塔，无法执行投降");
                return;
            }

            Debug.Log($"玩家按下投降快捷键，摧毁本方主塔: {localMainTower.buildingName}");

            NetworkBuilding networkBuilding = localMainTower.GetComponent<NetworkBuilding>();
            if (networkBuilding != null)
            {
                networkBuilding.TakeDamage(localMainTower.health);
            }
            else
            {
                localMainTower.TakeDamage(localMainTower.health);
            }
        }

        public void ExitBattle()
        {
            Debug.Log("玩家主动退出战斗！");
            battleStatus = BattleStatus.Ended;
            UIManager.Instance.ClearPanel();

            if (isNetworkBattle && NetworkManager.Instance != null)
            {
                NetworkManager.Instance.LeaveRoomAndReturnToMainScene();
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
        }

        /// <summary>
        /// 暂停战斗
        /// </summary>
        public void PauseBattle()
        {
            if (battleStatus == BattleStatus.Fighting)
            {
                battleStatus = BattleStatus.Paused;
                Debug.Log("战斗暂停！");
            }
        }

        /// <summary>
        /// 恢复战斗
        /// </summary>
        public void ResumeBattle()
        {
            if (battleStatus == BattleStatus.Paused)
            {
                battleStatus = BattleStatus.Fighting;
                Debug.Log("战斗恢复！");
            }
        }
        
        private void DetermineWinner()
        {
            int winId=-1;
            // 检查主塔状态
            Building player1MainTower = buildings.Find(b => b != null && b.ownerId == 1 && b.buildingType == BuildingType.MainTower);
            Building player2MainTower = buildings.Find(b => b != null && b.ownerId == 2 && b.buildingType == BuildingType.MainTower);
            
            if (player1MainTower == null && player2MainTower != null)
            {
                Debug.Log("玩家2获胜！");
                winId=2;
            }
            else if (player2MainTower == null && player1MainTower != null)
            {
                Debug.Log("玩家1获胜！");
                winId=1;
            }
            else if (player1MainTower == null && player2MainTower == null)
            {
                // 平局或根据剩余时间判断
                Debug.Log("平局！");
                winId=0;
            }
            else
            {
                // 双方主塔都还在
                if (isOvertime && currentTime <= 0)
                {
                    // 加时结束，根据防御塔数量判断
                    int player1DefenseTowers = buildings.Where(b => b != null && b.ownerId == 1 && b.buildingType == BuildingType.DefenseTower && b.health > 0).Count();
                    int player2DefenseTowers = buildings.Where(b => b != null && b.ownerId == 2 && b.buildingType == BuildingType.DefenseTower && b.health > 0).Count();
                    
                    if (player1DefenseTowers > player2DefenseTowers)
                    {
                        Debug.Log("玩家1防御塔数量多，获胜！");
                        winId=1;
                    }
                    else if (player2DefenseTowers > player1DefenseTowers)
                    {
                        Debug.Log("玩家2防御塔数量多，获胜！");
                        winId=2;
                    }
                    else
                    {
                        // 防御塔数量相同，根据主塔生命值判断
                        if (player1MainTower.health > player2MainTower.health)
                        {
                            Debug.Log("玩家1主塔生命值高，获胜！");
                            winId=1;
                        }
                        else if (player2MainTower.health > player1MainTower.health)
                        {
                            Debug.Log("玩家2主塔生命值高，获胜！");
                            winId=2;
                        }
                        else
                        {
                            Debug.Log("平局！");
                            winId=0;
                        }
                    }
                }
                else
                {
                    // 根据主塔生命值判断
                    if (player1MainTower.health > player2MainTower.health)
                    {
                        Debug.Log("玩家1获胜！");
                        winId=1;
                    }
                    else if (player2MainTower.health > player1MainTower.health)
                    {
                        Debug.Log("玩家2获胜！");
                        winId=2;
                    }
                    else
                    {
                        Debug.Log("平局！");
                        winId=0;
                    }
                }
            }
            
            // 网络对战，同步胜负结果
            if (isNetworkBattle && NetworkBattleManager.Instance != null && PhotonNetwork.IsMasterClient)
            {
                NetworkBattleManager.Instance.EndBattle(winId);
            }
            
            // Apply trophy changes + record battle stats (once per battle)
            if (!_trophyProcessed)
            {
                _trophyProcessed = true;

                if (!isNetworkBattle || (PhotonNetwork.IsMasterClient))
                {
                    // Determine if local player is victorious
                    bool isLocalPlayerVictory = (winId == 1 && localPlayerTeam == 1) ||
                                                (winId == 2 && localPlayerTeam == 2);
                    bool isDraw = winId == 0;

                    int playerTrophies = PlayerDataManager.Instance.GetTrophies();
                    int opponentTrophies = playerTrophies; // TODO: get actual opponent trophies from network data

                    LastTrophyResult = TrophyManager.Instance.ApplyTrophyChange(
                        isLocalPlayerVictory, isDraw, opponentTrophies);
                    
                    // Record battle stats
                    PlayerDataManager.Instance.RecordBattleResult(isLocalPlayerVictory, isDraw);

                    // Grant Season Pass exp
                    int expGained = isDraw ? SeasonPassConfigSO.Instance.battleLoseExp
                        : isLocalPlayerVictory ? SeasonPassConfigSO.Instance.battleWinExp
                        : SeasonPassConfigSO.Instance.battleLoseExp;
                    var spManager = KingdomWar.Game.SeasonPass.SeasonPassManager.Instance;
                    if (spManager != null)
                    {
                        spManager.AddExp(expGained);
                    }
                }
            }

            // 显示结算面板
            basePanel panel = UIManager.Instance.PushPanel(UIPanelType.settlementPanel);
            if (panel != null)
            {
                (panel as settlementPanel).Init(winId);
            }
        }
        
        // 添加单位
        public void AddUnit(Unit unit)
        {
            units.Add(unit);
            
            // 检查是否是网络对战
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                // 在网络对战中，确保单位的所有权正确
                int localPlayerId = PhotonNetwork.LocalPlayer.ActorNumber;
                Debug.LogFormat("Added unit with owner ID: {0}, Local player ID: {1}", unit.ownerId, localPlayerId);
            }
        }
        
        // 放置单位（网络同步）
        public void PlaceUnit(int unitType, Vector3 position, int playerId)
        {
            // 检查是否是本地玩家操作
            if (isNetworkBattle && !NetworkBattleManager.Instance.IsLocalPlayer(playerId))
            {
                return;
            }
            
            // 检查玩家是否可以操作
            if (isNetworkBattle && !NetworkBattleManager.Instance.CanPlayerOperate(playerId))
            {
                return;
            }
            
            // 检查是否可以放置单位
            if (!CanPlaceUnit(playerId, position))
            {
                return;
            }
            
            // 检查圣水是否足够
            float cost = GetUnitCost(unitType);
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                if (!NetworkBattleManager.Instance.HasEnoughElixir(playerId, cost))
                {
                    return;
                }
            }
            else
            {
                PlayerData playerData = GetPlayerData(playerId);
                if (playerData == null || playerData.elixir < cost)
                {
                    return;
                }
            }
            
            // 消耗圣水
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                if (!NetworkBattleManager.Instance.ConsumeElixir(playerId, cost))
                {
                    return;
                }
            }
            else
            {
                if (!ConsumeElixir(playerId, cost))
                {
                    return;
                }
            }
            
            // 创建单位
            // TODO: 根据unitType创建对应的单位预制体
            
            // 网络对战，广播单位放置
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                NetworkBattleManager.Instance.PlaceUnit(unitType, position, playerId);
            }
        }
        
        // 添加建筑
        public void AddBuilding(Building building)
        {
            buildings.Add(building);
        }
        
        // 放置建筑（网络同步）
        public void PlaceBuilding(int buildingType, Vector3 position, int playerId)
        {
            // 检查是否是本地玩家操作
            if (isNetworkBattle && !NetworkBattleManager.Instance.IsLocalPlayer(playerId))
            {
                return;
            }
            
            // 检查玩家是否可以操作
            if (isNetworkBattle && !NetworkBattleManager.Instance.CanPlayerOperate(playerId))
            {
                return;
            }
            
            // 检查是否可以放置建筑
            if (!CanPlaceBuilding(playerId, position))
            {
                return;
            }
            
            // 检查圣水是否足够
            float cost = GetBuildingCost(buildingType);
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                if (!NetworkBattleManager.Instance.HasEnoughElixir(playerId, cost))
                {
                    return;
                }
            }
            else
            {
                PlayerData playerData = GetPlayerData(playerId);
                if (playerData == null || playerData.elixir < cost)
                {
                    return;
                }
            }
            
            // 消耗圣水
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                if (!NetworkBattleManager.Instance.ConsumeElixir(playerId, cost))
                {
                    return;
                }
            }
            else
            {
                if (!ConsumeElixir(playerId, cost))
                {
                    return;
                }
            }
            
            // 创建建筑
            Building building = CreateBuilding(buildingType, position, playerId);
            if (building != null)
            {
                // 添加到建筑列表
                AddBuilding(building);
                
                // 网络对战，广播建筑放置
                if (isNetworkBattle && NetworkBattleManager.Instance != null)
                {
                    NetworkBattleManager.Instance.PlaceBuilding(buildingType, position, playerId);
                }
            }
        }

        // 创建建筑
        public Building CreateBuilding(int buildingType, Vector3 position, int playerId)
        {
            // TODO: 根据实际的建筑预制体配置创建建筑
            // 这里使用一个临时的建筑预制体作为示例
            // 在实际项目中，应该从资源管理器或对象池中获取建筑预制体
            
            // 临时实现：创建一个简单的建筑对象
            GameObject buildingObj = new GameObject("Building");
            buildingObj.transform.position = position;
            
            // 添加建筑组件
            Building building = buildingObj.AddComponent<Building>();
            
            // 初始化建筑属性
            building.ownerId = playerId;
            building.buildingType = (BuildingType)buildingType;
            building.buildingName = GetBuildingName((BuildingType)buildingType);
            building.health = GetBuildingHealth((BuildingType)buildingType);
            building.maxHealth = building.health;
            building.damage = GetBuildingDamage((BuildingType)buildingType);
            building.attackSpeed = GetBuildingAttackSpeed((BuildingType)buildingType);
            building.attackRange = GetBuildingAttackRange((BuildingType)buildingType);
            building.duration = GetBuildingDuration((BuildingType)buildingType);
            building.deployTime = 1.0f; // 默认部署时间
            building.state = BuildingState.Idle;
            
            return building;
        }

        // 获取建筑名称
        private string GetBuildingName(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.MainTower:
                    return "Main Tower";
                case BuildingType.DefenseTower:
                    return "Defense Tower";
                case BuildingType.ElixirCollector:
                    return "Elixir Collector";
                default:
                    return "Building";
            }
        }

        // 获取建筑生命值
        private int GetBuildingHealth(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.MainTower:
                    return 3000;
                case BuildingType.DefenseTower:
                    return 1500;
                case BuildingType.ElixirCollector:
                    return 500;
                default:
                    return 1000;
            }
        }

        // 获取建筑伤害
        private int GetBuildingDamage(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.MainTower:
                    return 100;
                case BuildingType.DefenseTower:
                    return 80;
                case BuildingType.ElixirCollector:
                    return 0;
                default:
                    return 50;
            }
        }

        // 获取建筑攻击速度
        private float GetBuildingAttackSpeed(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.MainTower:
                    return 1.5f;
                case BuildingType.DefenseTower:
                    return 1.0f;
                case BuildingType.ElixirCollector:
                    return 0f;
                default:
                    return 2.0f;
            }
        }

        // 获取建筑攻击范围
        private float GetBuildingAttackRange(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.MainTower:
                    return 8.0f;
                case BuildingType.DefenseTower:
                    return 6.0f;
                case BuildingType.ElixirCollector:
                    return 0f;
                default:
                    return 5.0f;
            }
        }

        // 获取建筑持续时间
        private float GetBuildingDuration(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.MainTower:
                    return 0f; // 永久存在
                case BuildingType.DefenseTower:
                    return 0f; // 永久存在
                case BuildingType.ElixirCollector:
                    return 60f; // 60秒
                default:
                    return 30f;
            }
        }
        
        // 添加法术
        public void AddSpell(Spell spell)
        {
            spells.Add(spell);
            Debug.Log($"[BattleManager] Spell added: {spell?.spellName}, Total spells: {spells.Count}");
        }
        
        // 释放法术（网络同步）
        public void CastSpell(int spellType, Vector3 position, int playerId)
        {
            // 检查是否是本地玩家操作
            if (isNetworkBattle && !NetworkBattleManager.Instance.IsLocalPlayer(playerId))
            {
                return;
            }
            
            // 检查玩家是否可以操作
            if (isNetworkBattle && !NetworkBattleManager.Instance.CanPlayerOperate(playerId))
            {
                return;
            }
            
            // 检查是否可以释放法术
            if (!CanCastSpell(playerId, position))
            {
                return;
            }
            
            // 检查圣水是否足够
            float cost = GetSpellCost(spellType);
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                if (!NetworkBattleManager.Instance.HasEnoughElixir(playerId, cost))
                {
                    return;
                }
            }
            else
            {
                PlayerData playerData = GetPlayerData(playerId);
                if (playerData == null || playerData.elixir < cost)
                {
                    return;
                }
            }
            
            // 消耗圣水
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                if (!NetworkBattleManager.Instance.ConsumeElixir(playerId, cost))
                {
                    return;
                }
            }
            else
            {
                if (!ConsumeElixir(playerId, cost))
                {
                    return;
                }
            }
            
            // 创建法术效果
            // TODO: 根据spellType创建对应的法术效果
            
            // 网络对战，广播法术释放
            if (isNetworkBattle && NetworkBattleManager.Instance != null)
            {
                NetworkBattleManager.Instance.CastSpell(spellType, position, playerId);
            }
        }
        
        // 获取玩家数据
        public PlayerData GetPlayerData(int playerId)
        {
            return playerId == 1 ? player1 : player2;
        }
        
        // 消耗圣水
        public bool ConsumeElixir(int playerId, float amount)
        {
            PlayerData player = GetPlayerData(playerId);
            if (player.elixir >= amount)
            {
                player.elixir -= amount;
                return true;
            }
            return false;
        }
        
        // 检查是否可以放置单位
        public bool CanPlaceUnit(int playerId, Vector3 position)
        {
            // 检查是否在玩家的放置区域内
            if (!IsInPlacementArea(playerId, position))
            {
                Debug.LogFormat("单位放置位置不在允许的区域内: {0}", position);
                return false;
            }
            
            // 检查是否与其他单位或建筑重叠
            foreach (Unit unit in units)
            {
                if (Vector3.Distance(position, unit.transform.position) < 1f)
                {
                    Debug.Log("单位放置位置与其他单位重叠");
                    return false;
                }
            }
            
            foreach (Building building in buildings)
            {
                if (Vector3.Distance(position, building.transform.position) < 1.5f)
                {
                    Debug.Log("单位放置位置与建筑重叠");
                    return false;
                }
            }
            
            return true;
        }
        
        // 检查是否在放置区域内
        private bool IsInPlacementArea(int playerId, Vector3 position)
        {
            if (playerId == 1)
            {
                return position.x <= 0f;
            }

            if (playerId == 2)
            {
                return position.x >= 0f;
            }

            return false;
        }
        
        // 检查是否可以放置建筑
        public bool CanPlaceBuilding(int playerId, Vector3 position)
        {
            // 检查是否在玩家的放置区域内
            if (!IsInPlacementArea(playerId, position))
            {
                Debug.LogFormat("建筑放置位置不在允许的区域内: {0}", position);
                return false;
            }
            
            // 检查是否与其他单位或建筑重叠
            foreach (Unit unit in units)
            {
                if (Vector3.Distance(position, unit.transform.position) < 1.5f)
                {
                    Debug.Log("建筑放置位置与其他单位重叠");
                    return false;
                }
            }
            
            foreach (Building building in buildings)
            {
                if (Vector3.Distance(position, building.transform.position) < 2f)
                {
                    Debug.Log("建筑放置位置与其他建筑重叠");
                    return false;
                }
            }
            
            return true;
        }
        
        // 检查是否可以释放法术
        public bool CanCastSpell(int playerId, Vector3 position)
        {
            // 检查是否在法术释放范围内
            // TODO: 实现法术释放范围检查
            return true;
        }
        
        // 获取单位消耗
        private float GetUnitCost(int unitType)
        {
            // TODO: 根据unitType返回对应的消耗
            return 2f; // 临时返回默认值
        }
        
        // 获取建筑消耗
        private float GetBuildingCost(int buildingType)
        {
            // TODO: 根据buildingType返回对应的消耗
            return 5f; // 临时返回默认值
        }
        
        // 获取法术消耗
        private float GetSpellCost(int spellType)
        {
            // TODO: 根据spellType返回对应的消耗
            return 3f; // 临时返回默认值
        }
    }
    
    [System.Serializable]
    public class PlayerData
    {
        public int playerId;
        public float elixir;
        public float maxElixir;
        public float elixirRecoveryRate;
    }
    
    public enum BattleStatus
    {
        Waiting,
        Countdown,
        Fighting,
        Paused,
        Ended
    }
}
