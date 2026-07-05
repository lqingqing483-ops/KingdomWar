using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public delegate void BattleEventHandler(BattleSyncEvent syncEvent);

    public class BattleEventSystem : MonoBehaviour
    {
        public static BattleEventSystem Instance { get; private set; }

        private Dictionary<BattleSyncEventType, List<BattleEventHandler>> eventHandlers;
        private Queue<BattleSyncEvent> eventQueue;
        private int currentFrame;
        private bool isProcessingEvents;

        public event System.Action<UnitSyncData> OnUnitSpawned;
        public event System.Action<int, int, int> OnUnitDamaged;
        public event System.Action<int> OnUnitDied;
        public event System.Action<BuildingSyncData> OnBuildingSpawned;
        public event System.Action<int, int, int> OnBuildingDamaged;
        public event System.Action<int> OnBuildingDestroyed;
        public event System.Action<int, int, Vector3> OnSpellCast;
        public event System.Action<int, float> OnElixirUpdated;
        public event System.Action OnBattleStarted;
        public event System.Action<int> OnBattleEnded;
        public event System.Action OnBattlePaused;
        public event System.Action OnBattleResumed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            eventHandlers = new Dictionary<BattleSyncEventType, List<BattleEventHandler>>();
            eventQueue = new Queue<BattleSyncEvent>();
            currentFrame = 0;
            isProcessingEvents = false;

            foreach (BattleSyncEventType eventType in System.Enum.GetValues(typeof(BattleSyncEventType)))
            {
                eventHandlers[eventType] = new List<BattleEventHandler>();
            }
        }

        private void Update()
        {
            currentFrame++;
            ProcessEventQueue();
        }

        public void RegisterHandler(BattleSyncEventType eventType, BattleEventHandler handler)
        {
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Add(handler);
            }
        }

        public void UnregisterHandler(BattleSyncEventType eventType, BattleEventHandler handler)
        {
            if (eventHandlers.ContainsKey(eventType))
            {
                eventHandlers[eventType].Remove(handler);
            }
        }

        public void EmitEvent(BattleSyncEvent syncEvent)
        {
            eventQueue.Enqueue(syncEvent);
        }

        private void ProcessEventQueue()
        {
            if (isProcessingEvents) return;

            isProcessingEvents = true;
            try
            {
                while (eventQueue.Count > 0)
                {
                    BattleSyncEvent syncEvent = eventQueue.Dequeue();
                    DispatchEvent(syncEvent);
                }
            }
            finally
            {
                isProcessingEvents = false;
            }
        }

        private void DispatchEvent(BattleSyncEvent syncEvent)
        {
            if (eventHandlers.TryGetValue(syncEvent.eventType, out List<BattleEventHandler> handlers))
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler?.Invoke(syncEvent);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error handling battle event {syncEvent.eventType}: {e.Message}");
                    }
                }
            }
        }

        public int GetCurrentFrame()
        {
            return currentFrame;
        }

        #region Event Emission Methods

        public void EmitUnitSpawned(Unit unit)
        {
            if (unit == null) return;

            var syncEvent = new BattleSyncEvent(BattleSyncEventType.UnitSpawn, currentFrame, unit.ownerId)
            {
                priority = SyncPriority.High
            };

            var unitData = new UnitSyncData(unit);
            syncEvent.eventData = SerializeUnitData(unitData);

            EmitEvent(syncEvent);
            OnUnitSpawned?.Invoke(unitData);

            Debug.Log($"[BattleEvent] Unit spawned: {unit.unitName}, ID: {unitData.unitId}, Owner: {unit.ownerId}");
        }

        public void EmitUnitMove(int unitId, Vector3 position, Vector3 velocity)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.UnitMove, currentFrame, 0)
            {
                priority = SyncPriority.Low
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(unitId);
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);
                writer.Write(velocity.x);
                writer.Write(velocity.y);
                writer.Write(velocity.z);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
        }

        public void EmitUnitAttack(int unitId, int targetId, int damage)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.UnitAttack, currentFrame, 0)
            {
                priority = SyncPriority.Normal
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(unitId);
                writer.Write(targetId);
                writer.Write(damage);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
        }

        public void EmitUnitDamaged(int unitId, int damage, int sourceId)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.UnitDamage, currentFrame, sourceId)
            {
                priority = SyncPriority.High
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(unitId);
                writer.Write(damage);
                writer.Write(sourceId);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnUnitDamaged?.Invoke(unitId, damage, sourceId);

            Debug.Log($"[BattleEvent] Unit damaged: ID={unitId}, Damage={damage}, Source={sourceId}");
        }

        public void EmitUnitDeath(int unitId, int playerId)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.UnitDeath, currentFrame, playerId)
            {
                priority = SyncPriority.High
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(unitId);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnUnitDied?.Invoke(unitId);

            Debug.Log($"[BattleEvent] Unit died: ID={unitId}");
        }

        public void EmitBuildingSpawned(Building building)
        {
            if (building == null) return;

            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BuildingSpawn, currentFrame, building.ownerId)
            {
                priority = SyncPriority.High
            };

            var buildingData = new BuildingSyncData(building);
            syncEvent.eventData = SerializeBuildingData(buildingData);

            EmitEvent(syncEvent);
            OnBuildingSpawned?.Invoke(buildingData);

            Debug.Log($"[BattleEvent] Building spawned: {building.buildingName}, ID: {buildingData.buildingId}, Owner: {building.ownerId}");
        }

        public void EmitBuildingDamaged(int buildingId, int damage, int sourceId)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BuildingDamage, currentFrame, sourceId)
            {
                priority = SyncPriority.High
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(buildingId);
                writer.Write(damage);
                writer.Write(sourceId);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnBuildingDamaged?.Invoke(buildingId, damage, sourceId);

            Debug.Log($"[BattleEvent] Building damaged: ID={buildingId}, Damage={damage}, Source={sourceId}");
        }

        public void EmitBuildingDestroyed(int buildingId, int playerId)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BuildingDestroy, currentFrame, playerId)
            {
                priority = SyncPriority.Critical
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(buildingId);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnBuildingDestroyed?.Invoke(buildingId);

            Debug.Log($"[BattleEvent] Building destroyed: ID={buildingId}");
        }

        public void EmitSpellCast(int playerId, int spellType, Vector3 position)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.SpellCast, currentFrame, playerId)
            {
                priority = SyncPriority.High
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(spellType);
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnSpellCast?.Invoke(playerId, spellType, position);

            Debug.Log($"[BattleEvent] Spell cast: Player={playerId}, Type={spellType}, Position={position}");
        }

        public void EmitElixirUpdated(int playerId, float newElixir)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.ElixirUpdate, currentFrame, playerId)
            {
                priority = SyncPriority.Normal
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(playerId);
                writer.Write(newElixir);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnElixirUpdated?.Invoke(playerId, newElixir);
        }

        public void EmitBattleStarted()
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BattleStart, currentFrame, 0)
            {
                priority = SyncPriority.Critical
            };

            EmitEvent(syncEvent);
            OnBattleStarted?.Invoke();

            Debug.Log("[BattleEvent] Battle started");
        }

        public void EmitBattleEnded(int winnerId)
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BattleEnd, currentFrame, winnerId)
            {
                priority = SyncPriority.Critical
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(winnerId);
                syncEvent.eventData = stream.ToArray();
            }

            EmitEvent(syncEvent);
            OnBattleEnded?.Invoke(winnerId);

            Debug.Log($"[BattleEvent] Battle ended, winner: {winnerId}");
        }

        public void EmitBattlePaused()
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BattlePause, currentFrame, 0)
            {
                priority = SyncPriority.High
            };

            EmitEvent(syncEvent);
            OnBattlePaused?.Invoke();

            Debug.Log("[BattleEvent] Battle paused");
        }

        public void EmitBattleResumed()
        {
            var syncEvent = new BattleSyncEvent(BattleSyncEventType.BattleResume, currentFrame, 0)
            {
                priority = SyncPriority.High
            };

            EmitEvent(syncEvent);
            OnBattleResumed?.Invoke();

            Debug.Log("[BattleEvent] Battle resumed");
        }

        #endregion

        #region Serialization Helpers

        private byte[] SerializeUnitData(UnitSyncData data)
        {
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(data.unitId);
                writer.Write(data.unitType);
                writer.Write(data.ownerId);
                writer.Write(data.position.x);
                writer.Write(data.position.y);
                writer.Write(data.position.z);
                writer.Write(data.rotation.x);
                writer.Write(data.rotation.y);
                writer.Write(data.rotation.z);
                writer.Write(data.rotation.w);
                writer.Write(data.health);
                writer.Write(data.maxHealth);
                writer.Write((int)data.state);
                writer.Write(data.isAttacking);
                writer.Write(data.isMoving);
                return stream.ToArray();
            }
        }

        private byte[] SerializeBuildingData(BuildingSyncData data)
        {
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(data.buildingId);
                writer.Write(data.buildingType);
                writer.Write(data.ownerId);
                writer.Write(data.position.x);
                writer.Write(data.position.y);
                writer.Write(data.position.z);
                writer.Write(data.rotation.x);
                writer.Write(data.rotation.y);
                writer.Write(data.rotation.z);
                writer.Write(data.rotation.w);
                writer.Write(data.health);
                writer.Write(data.maxHealth);
                writer.Write((int)data.state);
                return stream.ToArray();
            }
        }

        public static UnitSyncData DeserializeUnitData(byte[] data)
        {
            var result = new UnitSyncData();
            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                result.unitId = reader.ReadInt32();
                result.unitType = reader.ReadInt32();
                result.ownerId = reader.ReadInt32();
                result.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                result.rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                result.health = reader.ReadInt32();
                result.maxHealth = reader.ReadInt32();
                result.state = (UnitState)reader.ReadInt32();
                result.isAttacking = reader.ReadBoolean();
                result.isMoving = reader.ReadBoolean();
            }
            return result;
        }

        public static BuildingSyncData DeserializeBuildingData(byte[] data)
        {
            var result = new BuildingSyncData();
            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                result.buildingId = reader.ReadInt32();
                result.buildingType = reader.ReadInt32();
                result.ownerId = reader.ReadInt32();
                result.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                result.rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                result.health = reader.ReadInt32();
                result.maxHealth = reader.ReadInt32();
                result.state = (BuildingState)reader.ReadInt32();
            }
            return result;
        }

        #endregion

        public void ClearAllEvents()
        {
            eventQueue.Clear();
        }

        public int GetPendingEventCount()
        {
            return eventQueue.Count;
        }
    }
}
