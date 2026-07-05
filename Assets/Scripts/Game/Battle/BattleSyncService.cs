using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace KingdomWar.Game.Battle
{
    public class BattleSyncService : MonoBehaviourPunCallbacks
    {
        public static BattleSyncService Instance { get; private set; }

        [Header("同步配置")]
        public float syncInterval = BattleSyncConfig.DEFAULT_SYNC_INTERVAL;
        public float minSyncInterval = BattleSyncConfig.MIN_SYNC_INTERVAL;
        public float maxSyncInterval = BattleSyncConfig.MAX_SYNC_INTERVAL;
        public bool enableInterpolation = true;
        public bool enableExtrapolation = true;

        [Header("状态快照")]
        public int snapshotHistorySize = BattleSyncConfig.MAX_SNAPSHOT_HISTORY;

        private PhotonView photonViewComponent;
        private Queue<BattleStateSnapshot> snapshotHistory;
        private BattleStateSnapshot latestSnapshot;
        private BattleStateSnapshot predictedSnapshot;

        private Dictionary<int, UnitSyncData> unitStates;
        private Dictionary<int, BuildingSyncData> buildingStates;
        private Dictionary<int, PlayerSyncData> playerStates;
        private Dictionary<int, Queue<UnitSyncData>> unitStateBuffer;

        private Queue<BattleSyncEvent> pendingEvents;
        private Dictionary<int, BattleSyncEvent> awaitingAck;
        private int localSequenceId;
        private int remoteSequenceId;

        private BattleSyncStatistics statistics;
        private float lastSyncTime;
        private float lastFullSyncTime;
        private int networkQuality;
        private bool isInitialized;

        private float interpolationBufferTime = BattleSyncConfig.INTERPOLATION_DELAY;

        public event System.Action<BattleStateSnapshot> OnSnapshotReceived;
        public event System.Action<BattleSyncEvent> OnEventReceived;
        public event System.Action<int> OnConflictDetected;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                EnsurePhotonView();
                Initialize();
            }
            else
            {
                Destroy(gameObject);
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
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            
            if (photonViewComponent != null && photonViewComponent.ViewID == 0)
            {
                photonViewComponent.ViewID = PhotonNetwork.AllocateViewID(0);
                Debug.Log($"[BattleSyncService] Allocated ViewID on join: {photonViewComponent.ViewID}");
            }
        }

        private void Initialize()
        {
            snapshotHistory = new Queue<BattleStateSnapshot>();
            unitStates = new Dictionary<int, UnitSyncData>();
            buildingStates = new Dictionary<int, BuildingSyncData>();
            playerStates = new Dictionary<int, PlayerSyncData>();
            unitStateBuffer = new Dictionary<int, Queue<UnitSyncData>>();
            pendingEvents = new Queue<BattleSyncEvent>();
            awaitingAck = new Dictionary<int, BattleSyncEvent>();
            statistics = new BattleSyncStatistics();
            localSequenceId = 0;
            remoteSequenceId = 0;
            networkQuality = 5;
            isInitialized = true;

            RegisterEventHandlers();
            
            Debug.Log("[BattleSyncService] Initialized with PhotonView");
        }

        private void RegisterEventHandlers()
        {
            if (BattleEventSystem.Instance != null)
            {
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.UnitSpawn, HandleUnitSpawnEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.UnitDamage, HandleUnitDamageEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.UnitDeath, HandleUnitDeathEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.BuildingSpawn, HandleBuildingSpawnEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.BuildingDamage, HandleBuildingDamageEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.BuildingDestroy, HandleBuildingDestroyEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.SpellCast, HandleSpellCastEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.ElixirUpdate, HandleElixirUpdateEvent);
                Debug.Log("[BattleSyncService] Event handlers registered");
            }
            else
            {
                Debug.LogWarning("[BattleSyncService] BattleEventSystem not found, will retry registration");
                StartCoroutine(DelayedRegisterEventHandlers());
            }
        }

        private System.Collections.IEnumerator DelayedRegisterEventHandlers()
        {
            int retryCount = 0;
            while (BattleEventSystem.Instance == null && retryCount < 10)
            {
                yield return new UnityEngine.WaitForSeconds(0.1f);
                retryCount++;
            }

            if (BattleEventSystem.Instance != null)
            {
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.UnitSpawn, HandleUnitSpawnEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.UnitDamage, HandleUnitDamageEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.UnitDeath, HandleUnitDeathEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.BuildingSpawn, HandleBuildingSpawnEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.BuildingDamage, HandleBuildingDamageEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.BuildingDestroy, HandleBuildingDestroyEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.SpellCast, HandleSpellCastEvent);
                BattleEventSystem.Instance.RegisterHandler(BattleSyncEventType.ElixirUpdate, HandleElixirUpdateEvent);
                Debug.Log("[BattleSyncService] Event handlers registered (delayed)");
            }
            else
            {
                Debug.LogError("[BattleSyncService] Failed to register event handlers: BattleEventSystem not available");
            }
        }

        private void Update()
        {
            if (!isInitialized || !PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
                return;

            UpdateNetworkQuality();
            ProcessPendingEvents();
            ProcessAwaitingAcks();
            InterpolateStates();
        }

        private void UpdateNetworkQuality()
        {
            int ping = PhotonNetwork.GetPing();
            
            if (ping < 50) networkQuality = 5;
            else if (ping < 100) networkQuality = 4;
            else if (ping < 150) networkQuality = 3;
            else if (ping < 200) networkQuality = 2;
            else networkQuality = 1;

            syncInterval = Mathf.Lerp(minSyncInterval, maxSyncInterval, (5 - networkQuality) / 4f);
        }

        #region State Synchronization

        public BattleStateSnapshot CreateSnapshot()
        {
            var snapshot = new BattleStateSnapshot
            {
                frameNumber = BattleEventSystem.Instance?.GetCurrentFrame() ?? 0,
                timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                battleStatus = BattleManager.Instance?.battleStatus ?? BattleStatus.Waiting,
                battleTime = BattleManager.Instance?.CurrentTime ?? 0f
            };

            if (BattleManager.Instance != null)
            {
                foreach (var unit in BattleManager.Instance.Units)
                {
                    if (unit != null)
                        snapshot.units.Add(new UnitSyncData(unit));
                }

                foreach (var building in BattleManager.Instance.Buildings)
                {
                    if (building != null)
                        snapshot.buildings.Add(new BuildingSyncData(building));
                }
            }

            snapshot.checksum = snapshot.CalculateChecksum();
            return snapshot;
        }

        public void ApplySnapshot(BattleStateSnapshot snapshot)
        {
            if (snapshot == null) return;

            latestSnapshot = snapshot;
            snapshotHistory.Enqueue(snapshot);

            while (snapshotHistory.Count > snapshotHistorySize)
            {
                snapshotHistory.Dequeue();
            }

            foreach (var unitData in snapshot.units)
            {
                if (!unitStates.ContainsKey(unitData.unitId))
                {
                    unitStates[unitData.unitId] = unitData;
                    unitStateBuffer[unitData.unitId] = new Queue<UnitSyncData>();
                }
                else
                {
                    unitStates[unitData.unitId] = unitData;
                }

                if (unitStateBuffer.ContainsKey(unitData.unitId))
                {
                    unitStateBuffer[unitData.unitId].Enqueue(unitData.Clone());
                    
                    while (unitStateBuffer[unitData.unitId].Count > 10)
                    {
                        unitStateBuffer[unitData.unitId].Dequeue();
                    }
                }
            }

            foreach (var buildingData in snapshot.buildings)
            {
                buildingStates[buildingData.buildingId] = buildingData;
            }

            foreach (var playerData in snapshot.players)
            {
                playerStates[playerData.playerId] = playerData;
            }

            OnSnapshotReceived?.Invoke(snapshot);
        }

        public void SyncFullState()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var snapshot = CreateSnapshot();
            var message = BattleSyncMessage.CreateFullStateMessage(snapshot);

            photonViewComponent.RPC("RPC_ReceiveFullState", RpcTarget.Others, message.payload);
            statistics.totalEventsSent++;
            statistics.totalBytesSent += message.payload.Length;
        }

        [PunRPC]
        private void RPC_ReceiveFullState(byte[] payload)
        {
            try
            {
                var snapshot = DeserializeSnapshot(payload);
                ApplySnapshot(snapshot);
                statistics.totalEventsReceived++;
                statistics.totalBytesReceived += payload.Length;

                float latency = (System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - snapshot.timestamp) / 1000f;
                statistics.RecordLatency(latency);

                Debug.Log($"[BattleSyncService] Received full state snapshot, frame: {snapshot.frameNumber}, latency: {latency * 1000}ms");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattleSyncService] Error deserializing snapshot: {e.Message}");
            }
        }

        #endregion

        #region Event Broadcasting

        public void BroadcastEvent(BattleSyncEvent syncEvent)
        {
            if (syncEvent == null) return;

            localSequenceId++;
            syncEvent.sequenceId = localSequenceId;

            byte[] eventData = SerializeEvent(syncEvent);
            photonViewComponent.RPC("RPC_ReceiveSyncEvent", RpcTarget.Others, 
                (byte)syncEvent.eventType, 
                syncEvent.frameNumber, 
                syncEvent.playerId, 
                syncEvent.sequenceId, 
                syncEvent.timestamp,
                eventData);

            statistics.totalEventsSent++;
            statistics.totalBytesSent += eventData.Length;

            if (syncEvent.requiresAck)
            {
                awaitingAck[syncEvent.sequenceId] = syncEvent;
            }
        }

        [PunRPC]
        private void RPC_ReceiveSyncEvent(byte eventType, int frameNumber, int playerId, int sequenceId, long timestamp, byte[] eventData)
        {
            var syncEvent = new BattleSyncEvent((BattleSyncEventType)eventType, frameNumber, playerId)
            {
                sequenceId = sequenceId,
                timestamp = timestamp,
                eventData = eventData
            };

            if (sequenceId <= remoteSequenceId)
            {
                statistics.outOfOrderPackets++;
                Debug.LogWarning($"[BattleSyncService] Out of order packet received: {sequenceId} <= {remoteSequenceId}");
                return;
            }

            remoteSequenceId = sequenceId;
            statistics.totalEventsReceived++;
            statistics.totalBytesReceived += eventData.Length;

            float latency = (System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timestamp) / 1000f;
            statistics.RecordLatency(latency);

            if (syncEvent.requiresAck)
            {
                photonViewComponent.RPC("RPC_ReceiveAck", RpcTarget.Others, sequenceId);
            }

            ProcessReceivedEvent(syncEvent);
            OnEventReceived?.Invoke(syncEvent);
        }

        [PunRPC]
        private void RPC_ReceiveAck(int sequenceId)
        {
            if (awaitingAck.ContainsKey(sequenceId))
            {
                awaitingAck.Remove(sequenceId);
            }
        }

        private void ProcessReceivedEvent(BattleSyncEvent syncEvent)
        {
            switch (syncEvent.eventType)
            {
                case BattleSyncEventType.UnitSpawn:
                    ProcessUnitSpawn(syncEvent);
                    break;
                case BattleSyncEventType.UnitDamage:
                    ProcessUnitDamage(syncEvent);
                    break;
                case BattleSyncEventType.UnitDeath:
                    ProcessUnitDeath(syncEvent);
                    break;
                case BattleSyncEventType.BuildingSpawn:
                    ProcessBuildingSpawn(syncEvent);
                    break;
                case BattleSyncEventType.BuildingDamage:
                    ProcessBuildingDamage(syncEvent);
                    break;
                case BattleSyncEventType.BuildingDestroy:
                    ProcessBuildingDestroy(syncEvent);
                    break;
                case BattleSyncEventType.SpellCast:
                    ProcessSpellCast(syncEvent);
                    break;
                case BattleSyncEventType.ElixirUpdate:
                    ProcessElixirUpdate(syncEvent);
                    break;
            }
        }

        #endregion

        #region Event Processing

        private void HandleUnitSpawnEvent(BattleSyncEvent syncEvent)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                BroadcastEvent(syncEvent);
            }
        }

        private void HandleUnitDamageEvent(BattleSyncEvent syncEvent)
        {
            BroadcastEvent(syncEvent);
        }

        private void HandleUnitDeathEvent(BattleSyncEvent syncEvent)
        {
            BroadcastEvent(syncEvent);
        }

        private void HandleBuildingSpawnEvent(BattleSyncEvent syncEvent)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                BroadcastEvent(syncEvent);
            }
        }

        private void HandleBuildingDamageEvent(BattleSyncEvent syncEvent)
        {
            BroadcastEvent(syncEvent);
        }

        private void HandleBuildingDestroyEvent(BattleSyncEvent syncEvent)
        {
            BroadcastEvent(syncEvent);
        }

        private void HandleSpellCastEvent(BattleSyncEvent syncEvent)
        {
            BroadcastEvent(syncEvent);
        }

        private void HandleElixirUpdateEvent(BattleSyncEvent syncEvent)
        {
            BroadcastEvent(syncEvent);
        }

        private void ProcessUnitSpawn(BattleSyncEvent syncEvent)
        {
            var unitData = BattleEventSystem.DeserializeUnitData(syncEvent.eventData);
            if (unitData != null)
            {
                unitStates[unitData.unitId] = unitData;
                Debug.Log($"[BattleSyncService] Processing unit spawn: {unitData.unitId}");
            }
        }

        private void ProcessUnitDamage(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int unitId = reader.ReadInt32();
                int damage = reader.ReadInt32();
                int sourceId = reader.ReadInt32();

                if (unitStates.ContainsKey(unitId))
                {
                    var unitData = unitStates[unitId];
                    unitData.health = Mathf.Max(0, unitData.health - damage);
                    unitStates[unitId] = unitData;

                    ApplyDamageToUnit(unitId, damage, sourceId);
                }
            }
        }

        private void ProcessUnitDeath(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int unitId = reader.ReadInt32();

                if (unitStates.ContainsKey(unitId))
                {
                    unitStates.Remove(unitId);
                    unitStateBuffer.Remove(unitId);
                }

                RemoveUnitFromBattle(unitId);
            }
        }

        private void ProcessBuildingSpawn(BattleSyncEvent syncEvent)
        {
            var buildingData = BattleEventSystem.DeserializeBuildingData(syncEvent.eventData);
            if (buildingData != null)
            {
                buildingStates[buildingData.buildingId] = buildingData;
                Debug.Log($"[BattleSyncService] Processing building spawn: {buildingData.buildingId}");
            }
        }

        private void ProcessBuildingDamage(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int buildingId = reader.ReadInt32();
                int damage = reader.ReadInt32();
                int sourceId = reader.ReadInt32();

                if (buildingStates.ContainsKey(buildingId))
                {
                    var buildingData = buildingStates[buildingId];
                    buildingData.health = Mathf.Max(0, buildingData.health - damage);
                    buildingStates[buildingId] = buildingData;

                    ApplyDamageToBuilding(buildingId, damage, sourceId);
                }
            }
        }

        private void ProcessBuildingDestroy(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int buildingId = reader.ReadInt32();

                if (buildingStates.ContainsKey(buildingId))
                {
                    buildingStates.Remove(buildingId);
                }

                RemoveBuildingFromBattle(buildingId);
            }
        }

        private void ProcessSpellCast(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int spellType = reader.ReadInt32();
                Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                Debug.Log($"[BattleSyncService] Processing spell cast: type={spellType}, position={position}");
            }
        }

        private void ProcessElixirUpdate(BattleSyncEvent syncEvent)
        {
            using (var stream = new System.IO.MemoryStream(syncEvent.eventData))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                int playerId = reader.ReadInt32();
                float elixir = reader.ReadSingle();

                if (playerStates.ContainsKey(playerId))
                {
                    playerStates[playerId].elixir = elixir;
                }
            }
        }

        #endregion

        #region State Application

        private void ApplyDamageToUnit(int unitId, int damage, int sourceId)
        {
            if (BattleManager.Instance == null) return;

            foreach (var unit in BattleManager.Instance.Units)
            {
                if (unit != null && unit.GetInstanceID() == unitId)
                {
                    // 直接修改血量，而不是调用 TakeDamage
                    // 因为 TakeDamage 会触发 EmitUnitDamaged 事件，导致循环调用
                    unit.health = Mathf.Max(0, unit.health - damage);
                    Debug.Log($"[BattleSyncService] 同步单位 {unit.unitName} 血量: {unit.health}, 伤害: {damage}");
                    
                    // 检查是否死亡
                    if (unit.health <= 0)
                    {
                        unit.state = UnitState.Dead;
                        if (BattleManager.Instance != null)
                        {
                            BattleManager.Instance.Units.Remove(unit);
                        }
                    }
                    break;
                }
            }
        }

        private void RemoveUnitFromBattle(int unitId)
        {
            if (BattleManager.Instance == null) return;

            var units = BattleManager.Instance.Units;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                if (units[i] != null && units[i].GetInstanceID() == unitId)
                {
                    units[i].state = UnitState.Dead;
                    break;
                }
            }
        }

        private void ApplyDamageToBuilding(int buildingId, int damage, int sourceId)
        {
            if (BattleManager.Instance == null) return;

            foreach (var building in BattleManager.Instance.Buildings)
            {
                if (building != null && building.GetInstanceID() == buildingId)
                {
                    // 直接修改血量，而不是调用 TakeDamage
                    // 因为 TakeDamage 会触发 EmitBuildingDamaged 事件，导致循环调用
                    building.health = Mathf.Max(0, building.health - damage);
                    Debug.Log($"[BattleSyncService] 同步建筑 {building.buildingName} 血量: {building.health}, 伤害: {damage}");
                    
                    // 检查是否被摧毁
                    if (building.health <= 0)
                    {
                        building.state = BuildingState.Dead;
                        if (BattleManager.Instance != null)
                        {
                            BattleManager.Instance.buildings.Remove(building);
                        }
                    }
                    break;
                }
            }
        }

        private void RemoveBuildingFromBattle(int buildingId)
        {
            if (BattleManager.Instance == null) return;

            var buildings = BattleManager.Instance.Buildings;
            for (int i = buildings.Count - 1; i >= 0; i--)
            {
                if (buildings[i] != null && buildings[i].GetInstanceID() == buildingId)
                {
                    buildings[i].state = BuildingState.Dead;
                    break;
                }
            }
        }

        #endregion

        #region Interpolation

        private void InterpolateStates()
        {
            if (!enableInterpolation) return;

            float renderTime = (float)PhotonNetwork.Time - interpolationBufferTime;

            foreach (var kvp in unitStateBuffer)
            {
                var buffer = kvp.Value;
                if (buffer.Count < 2) continue;

                UnitSyncData fromState = null;
                UnitSyncData toState = null;

                var states = buffer.ToArray();
                for (int i = 0; i < states.Length - 1; i++)
                {
                    if (states[i].lastUpdateTime <= renderTime * 1000 && states[i + 1].lastUpdateTime >= renderTime * 1000)
                    {
                        fromState = states[i];
                        toState = states[i + 1];
                        break;
                    }
                }

                if (fromState != null && toState != null)
                {
                    float t = (renderTime * 1000 - fromState.lastUpdateTime) / (toState.lastUpdateTime - fromState.lastUpdateTime);
                    t = Mathf.Clamp01(t);

                    ApplyInterpolatedState(kvp.Key, fromState, toState, t);
                }
            }
        }

        private void ApplyInterpolatedState(int unitId, UnitSyncData from, UnitSyncData to, float t)
        {
            if (BattleManager.Instance == null) return;

            foreach (var unit in BattleManager.Instance.Units)
            {
                if (unit != null && unit.GetInstanceID() == unitId)
                {
                    unit.transform.position = Vector3.Lerp(from.position, to.position, t);
                    unit.transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, t);
                    break;
                }
            }
        }

        #endregion

        #region Conflict Resolution

        public void ResolveConflict(BattleSyncEvent localEvent, BattleSyncEvent remoteEvent)
        {
            statistics.conflictResolutions++;
            OnConflictDetected?.Invoke(localEvent.sequenceId);

            if (PhotonNetwork.IsMasterClient)
            {
                if (remoteEvent.timestamp > localEvent.timestamp)
                {
                    ProcessReceivedEvent(remoteEvent);
                    Debug.Log($"[BattleSyncService] Conflict resolved: using remote event (later timestamp)");
                }
                else
                {
                    BroadcastEvent(localEvent);
                    Debug.Log($"[BattleSyncService] Conflict resolved: using local event (master client)");
                }
            }
            else
            {
                ProcessReceivedEvent(remoteEvent);
                Debug.Log($"[BattleSyncService] Conflict resolved: using remote event (non-master client)");
            }
        }

        #endregion

        #region Pending Events

        private void ProcessPendingEvents()
        {
            while (pendingEvents.Count > 0)
            {
                var syncEvent = pendingEvents.Dequeue();
                BroadcastEvent(syncEvent);
            }
        }

        private void ProcessAwaitingAcks()
        {
            var currentTime = Time.time;
            var toRemove = new List<int>();

            foreach (var kvp in awaitingAck)
            {
                var syncEvent = kvp.Value;
                var elapsed = currentTime - (syncEvent.timestamp / 1000f);

                if (elapsed > BattleSyncConfig.ACK_TIMEOUT)
                {
                    toRemove.Add(kvp.Key);
                    statistics.droppedPackets++;

                    Debug.LogWarning($"[BattleSyncService] Event {kvp.Key} ack timeout, resending...");
                    BroadcastEvent(syncEvent);
                }
            }

            foreach (var key in toRemove)
            {
                awaitingAck.Remove(key);
            }
        }

        #endregion

        #region Serialization

        private byte[] SerializeEvent(BattleSyncEvent syncEvent)
        {
            return syncEvent.eventData ?? new byte[0];
        }

        private BattleStateSnapshot DeserializeSnapshot(byte[] data)
        {
            var snapshot = new BattleStateSnapshot();

            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                snapshot.frameNumber = reader.ReadInt32();
                snapshot.timestamp = reader.ReadInt64();
                snapshot.battleStatus = (BattleStatus)reader.ReadInt32();
                snapshot.battleTime = reader.ReadSingle();

                int playerCount = reader.ReadInt32();
                for (int i = 0; i < playerCount; i++)
                {
                    var playerData = new PlayerSyncData
                    {
                        playerId = reader.ReadInt32(),
                        elixir = reader.ReadSingle(),
                        maxElixir = reader.ReadSingle(),
                        teamId = reader.ReadInt32()
                    };
                    snapshot.players.Add(playerData);
                }

                int unitCount = reader.ReadInt32();
                for (int i = 0; i < unitCount; i++)
                {
                    var unitData = new UnitSyncData
                    {
                        unitId = reader.ReadInt32(),
                        ownerId = reader.ReadInt32(),
                        position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                        health = reader.ReadInt32(),
                        state = (UnitState)reader.ReadInt32()
                    };
                    snapshot.units.Add(unitData);
                }

                int buildingCount = reader.ReadInt32();
                for (int i = 0; i < buildingCount; i++)
                {
                    var buildingData = new BuildingSyncData
                    {
                        buildingId = reader.ReadInt32(),
                        ownerId = reader.ReadInt32(),
                        position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                        health = reader.ReadInt32(),
                        state = (BuildingState)reader.ReadInt32()
                    };
                    snapshot.buildings.Add(buildingData);
                }
            }

            return snapshot;
        }

        #endregion

        #region Public API

        public UnitSyncData GetUnitState(int unitId)
        {
            return unitStates.TryGetValue(unitId, out var state) ? state : null;
        }

        public BuildingSyncData GetBuildingState(int buildingId)
        {
            return buildingStates.TryGetValue(buildingId, out var state) ? state : null;
        }

        public PlayerSyncData GetPlayerState(int playerId)
        {
            return playerStates.TryGetValue(playerId, out var state) ? state : null;
        }

        public BattleSyncStatistics GetStatistics()
        {
            return statistics;
        }

        public int GetNetworkQuality()
        {
            return networkQuality;
        }

        public float GetAverageLatency()
        {
            return statistics.averageLatency;
        }

        public void QueueEvent(BattleSyncEvent syncEvent)
        {
            pendingEvents.Enqueue(syncEvent);
        }

        #endregion

        #region Photon Callbacks

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);

            int playerId = otherPlayer.ActorNumber;
            if (playerStates.ContainsKey(playerId))
            {
                playerStates[playerId].isConnected = false;
            }

            Debug.Log($"[BattleSyncService] Player {playerId} left room");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);

            unitStates.Clear();
            buildingStates.Clear();
            playerStates.Clear();
            unitStateBuffer.Clear();
            pendingEvents.Clear();
            awaitingAck.Clear();
            snapshotHistory.Clear();

            Debug.Log($"[BattleSyncService] Disconnected: {cause}");
        }

        #endregion
    }
}
