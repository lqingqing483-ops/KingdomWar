using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public enum BattleSyncEventType : byte
    {
        UnitSpawn = 1,
        UnitMove = 2,
        UnitAttack = 3,
        UnitDamage = 4,
        UnitDeath = 5,
        BuildingSpawn = 6,
        BuildingDamage = 7,
        BuildingDestroy = 8,
        SpellCast = 9,
        ElixirUpdate = 10,
        BattleStart = 11,
        BattleEnd = 12,
        BattlePause = 13,
        BattleResume = 14,
        FullStateSync = 15,
        PingSync = 16,
        TimeSync = 17
    }

    public enum SyncPriority : byte
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public class BattleSyncEvent
    {
        public BattleSyncEventType eventType;
        public int frameNumber;
        public int playerId;
        public long timestamp;
        public SyncPriority priority;
        public byte[] eventData;
        public int sequenceId;
        public bool requiresAck;

        private static int globalSequenceId = 0;

        public BattleSyncEvent()
        {
            sequenceId = System.Threading.Interlocked.Increment(ref globalSequenceId);
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            priority = SyncPriority.Normal;
            requiresAck = false;
        }

        public BattleSyncEvent(BattleSyncEventType type, int frame, int player) : this()
        {
            eventType = type;
            frameNumber = frame;
            playerId = player;
        }
    }

    [System.Serializable]
    public class UnitSyncData
    {
        public int unitId;
        public int unitType;
        public int ownerId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public int health;
        public int maxHealth;
        public UnitState state;
        public int targetId;
        public bool isAttacking;
        public bool isMoving;
        public float attackCooldown;
        public long lastUpdateTime;

        public UnitSyncData() { }

        public UnitSyncData(Unit unit)
        {
            if (unit == null) return;
            
            unitId = unit.GetInstanceID();
            ownerId = unit.ownerId;
            position = unit.transform.position;
            rotation = unit.transform.rotation;
            health = unit.health;
            maxHealth = unit.maxHealth;
            state = unit.state;
            isAttacking = unit.state == UnitState.Attacking;
            isMoving = unit.state == UnitState.Moving;
            lastUpdateTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public UnitSyncData Clone()
        {
            return new UnitSyncData
            {
                unitId = this.unitId,
                unitType = this.unitType,
                ownerId = this.ownerId,
                position = this.position,
                rotation = this.rotation,
                velocity = this.velocity,
                health = this.health,
                maxHealth = this.maxHealth,
                state = this.state,
                targetId = this.targetId,
                isAttacking = this.isAttacking,
                isMoving = this.isMoving,
                attackCooldown = this.attackCooldown,
                lastUpdateTime = this.lastUpdateTime
            };
        }
    }

    [System.Serializable]
    public class BuildingSyncData
    {
        public int buildingId;
        public int buildingType;
        public int ownerId;
        public Vector3 position;
        public Quaternion rotation;
        public int health;
        public int maxHealth;
        public BuildingState state;
        public float remainingDuration;
        public long lastUpdateTime;

        public BuildingSyncData() { }

        public BuildingSyncData(Building building)
        {
            if (building == null) return;
            
            buildingId = building.GetInstanceID();
            buildingType = (int)building.buildingType;
            ownerId = building.ownerId;
            position = building.transform.position;
            rotation = building.transform.rotation;
            health = building.health;
            maxHealth = building.maxHealth;
            state = building.state;
            lastUpdateTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public BuildingSyncData Clone()
        {
            return new BuildingSyncData
            {
                buildingId = this.buildingId,
                buildingType = this.buildingType,
                ownerId = this.ownerId,
                position = this.position,
                rotation = this.rotation,
                health = this.health,
                maxHealth = this.maxHealth,
                state = this.state,
                remainingDuration = this.remainingDuration,
                lastUpdateTime = this.lastUpdateTime
            };
        }
    }

    [System.Serializable]
    public class SpellSyncData
    {
        public int spellId;
        public int spellType;
        public int ownerId;
        public Vector3 position;
        public Vector3 direction;
        public float duration;
        public float remainingDuration;
        public bool isActive;
        public long castTime;

        public SpellSyncData() { }

        public SpellSyncData Clone()
        {
            return new SpellSyncData
            {
                spellId = this.spellId,
                spellType = this.spellType,
                ownerId = this.ownerId,
                position = this.position,
                direction = this.direction,
                duration = this.duration,
                remainingDuration = this.remainingDuration,
                isActive = this.isActive,
                castTime = this.castTime
            };
        }
    }

    [System.Serializable]
    public class PlayerSyncData
    {
        public int playerId;
        public float elixir;
        public float maxElixir;
        public int teamId;
        public bool isConnected;
        public int unitCount;
        public int buildingCount;
        public long lastUpdateTime;

        public PlayerSyncData() { }

        public PlayerSyncData(int id, float currentElixir, float maxElixirValue, int team)
        {
            playerId = id;
            elixir = currentElixir;
            maxElixir = maxElixirValue;
            teamId = team;
            isConnected = true;
            lastUpdateTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public PlayerSyncData Clone()
        {
            return new PlayerSyncData
            {
                playerId = this.playerId,
                elixir = this.elixir,
                maxElixir = this.maxElixir,
                teamId = this.teamId,
                isConnected = this.isConnected,
                unitCount = this.unitCount,
                buildingCount = this.buildingCount,
                lastUpdateTime = this.lastUpdateTime
            };
        }
    }

    [System.Serializable]
    public class BattleStateSnapshot
    {
        public int frameNumber;
        public long timestamp;
        public BattleStatus battleStatus;
        public float battleTime;
        public List<PlayerSyncData> players;
        public List<UnitSyncData> units;
        public List<BuildingSyncData> buildings;
        public List<SpellSyncData> spells;
        public int checksum;

        public BattleStateSnapshot()
        {
            players = new List<PlayerSyncData>();
            units = new List<UnitSyncData>();
            buildings = new List<BuildingSyncData>();
            spells = new List<SpellSyncData>();
        }

        public int CalculateChecksum()
        {
            int hash = 17;
            hash = hash * 31 + frameNumber;
            hash = hash * 31 + (int)battleStatus;
            hash = hash * 31 + Mathf.RoundToInt(battleTime * 100);
            
            foreach (var player in players)
            {
                hash = hash * 31 + player.playerId;
                hash = hash * 31 + Mathf.RoundToInt(player.elixir * 100);
            }
            
            foreach (var unit in units)
            {
                hash = hash * 31 + unit.unitId;
                hash = hash * 31 + unit.health;
            }
            
            foreach (var building in buildings)
            {
                hash = hash * 31 + building.buildingId;
                hash = hash * 31 + building.health;
            }
            
            return hash;
        }
    }

    [System.Serializable]
    public class BattleSyncMessage
    {
        public int messageId;
        public BattleSyncEventType eventType;
        public int senderId;
        public long timestamp;
        public byte[] payload;
        public bool requiresAck;
        public int ackId;

        private static int globalMessageId = 0;

        public BattleSyncMessage()
        {
            messageId = System.Threading.Interlocked.Increment(ref globalMessageId);
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            requiresAck = false;
        }

        public static BattleSyncMessage CreateUnitSpawnMessage(int playerId, int unitType, Vector3 position, int unitId)
        {
            var msg = new BattleSyncMessage
            {
                eventType = BattleSyncEventType.UnitSpawn,
                senderId = playerId,
                requiresAck = true
            };
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(unitType);
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);
                writer.Write(unitId);
                var rawData = stream.ToArray();
                msg.payload = CompressionHelper.Compress(rawData);
            }
            
            return msg;
        }

        public static BattleSyncMessage CreateUnitDamageMessage(int unitId, int damage, int sourceId)
        {
            var msg = new BattleSyncMessage
            {
                eventType = BattleSyncEventType.UnitDamage,
                senderId = sourceId
            };
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(unitId);
                writer.Write(damage);
                writer.Write(sourceId);
                var rawData = stream.ToArray();
                msg.payload = CompressionHelper.Compress(rawData);
            }
            
            return msg;
        }

        public static BattleSyncMessage CreateBuildingDamageMessage(int buildingId, int damage, int sourceId)
        {
            var msg = new BattleSyncMessage
            {
                eventType = BattleSyncEventType.BuildingDamage,
                senderId = sourceId
            };
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(buildingId);
                writer.Write(damage);
                writer.Write(sourceId);
                var rawData = stream.ToArray();
                msg.payload = CompressionHelper.Compress(rawData);
            }
            
            return msg;
        }

        public static BattleSyncMessage CreateSpellCastMessage(int playerId, int spellType, Vector3 position)
        {
            var msg = new BattleSyncMessage
            {
                eventType = BattleSyncEventType.SpellCast,
                senderId = playerId,
                requiresAck = true
            };
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(spellType);
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);
                var rawData = stream.ToArray();
                msg.payload = CompressionHelper.Compress(rawData);
            }
            
            return msg;
        }

        public static BattleSyncMessage CreateFullStateMessage(BattleStateSnapshot snapshot)
        {
            var msg = new BattleSyncMessage
            {
                eventType = BattleSyncEventType.FullStateSync,
                requiresAck = true
            };
            
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(snapshot.frameNumber);
                writer.Write(snapshot.timestamp);
                writer.Write((int)snapshot.battleStatus);
                writer.Write(snapshot.battleTime);
                
                writer.Write(snapshot.players.Count);
                foreach (var player in snapshot.players)
                {
                    writer.Write(player.playerId);
                    writer.Write(player.elixir);
                    writer.Write(player.maxElixir);
                    writer.Write(player.teamId);
                }
                
                writer.Write(snapshot.units.Count);
                foreach (var unit in snapshot.units)
                {
                    writer.Write(unit.unitId);
                    writer.Write(unit.ownerId);
                    writer.Write(unit.position.x);
                    writer.Write(unit.position.y);
                    writer.Write(unit.position.z);
                    writer.Write(unit.health);
                    writer.Write((int)unit.state);
                }
                
                writer.Write(snapshot.buildings.Count);
                foreach (var building in snapshot.buildings)
                {
                    writer.Write(building.buildingId);
                    writer.Write(building.ownerId);
                    writer.Write(building.position.x);
                    writer.Write(building.position.y);
                    writer.Write(building.position.z);
                    writer.Write(building.health);
                    writer.Write((int)building.state);
                }
                
                var rawData = stream.ToArray();
                msg.payload = CompressionHelper.Compress(rawData);
            }
            
            return msg;
        }
    }

    public class BattleSyncConfig
    {
        public const int TARGET_FRAME_RATE = 30;
        public const float FRAME_INTERVAL = 1f / TARGET_FRAME_RATE;
        public const int MAX_PENDING_EVENTS = 100;
        public const int MAX_SNAPSHOT_HISTORY = 60;
        public const float POSITION_SYNC_THRESHOLD = 0.1f;
        public const float ROTATION_SYNC_THRESHOLD = 5f;
        public const float HEALTH_SYNC_THRESHOLD = 1;
        public const float DEFAULT_SYNC_INTERVAL = 0.1f;
        public const float MIN_SYNC_INTERVAL = 0.05f;
        public const float MAX_SYNC_INTERVAL = 0.3f;
        public const int MAX_RETRIES = 3;
        public const float ACK_TIMEOUT = 1f;
        public const float INTERPOLATION_DELAY = 0.1f;
        public const float EXTRAPOLATION_LIMIT = 0.5f;
    }

    public class BattleSyncStatistics
    {
        public int totalEventsSent;
        public int totalEventsReceived;
        public int totalBytesSent;
        public int totalBytesReceived;
        public float averageLatency;
        public float maxLatency;
        public int droppedPackets;
        public int outOfOrderPackets;
        public int conflictResolutions;
        public float lastSyncTime;

        public void Reset()
        {
            totalEventsSent = 0;
            totalEventsReceived = 0;
            totalBytesSent = 0;
            totalBytesReceived = 0;
            averageLatency = 0;
            maxLatency = 0;
            droppedPackets = 0;
            outOfOrderPackets = 0;
            conflictResolutions = 0;
            lastSyncTime = 0;
        }

        public void RecordLatency(float latency)
        {
            averageLatency = (averageLatency * totalEventsReceived + latency) / (totalEventsReceived + 1);
            if (latency > maxLatency)
                maxLatency = latency;
        }
    }
}
