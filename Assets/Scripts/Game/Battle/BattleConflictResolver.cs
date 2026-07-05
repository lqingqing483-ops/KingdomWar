using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public class BattleConflictResolver
    {
        private Dictionary<int, BattleSyncEvent> localEvents;
        private Dictionary<int, BattleSyncEvent> remoteEvents;
        private Dictionary<int, ConflictResolution> resolvedConflicts;
        private int maxHistorySize = 100;

        public event System.Action<int, ConflictResolutionType> OnConflictResolved;

        public enum ConflictResolutionType
        {
            UseLocal,
            UseRemote,
            Merge,
            Retry,
            Discard
        }

        public struct ConflictResolution
        {
            public int conflictId;
            public BattleSyncEvent localEvent;
            public BattleSyncEvent remoteEvent;
            public ConflictResolutionType resolutionType;
            public BattleSyncEvent resolvedEvent;
            public long resolvedTimestamp;
        }

        public BattleConflictResolver()
        {
            localEvents = new Dictionary<int, BattleSyncEvent>();
            remoteEvents = new Dictionary<int, BattleSyncEvent>();
            resolvedConflicts = new Dictionary<int, ConflictResolution>();
        }

        public bool DetectConflict(BattleSyncEvent localEvent, BattleSyncEvent remoteEvent)
        {
            if (localEvent == null || remoteEvent == null)
                return false;

            if (localEvent.eventType != remoteEvent.eventType)
                return false;

            if (localEvent.frameNumber != remoteEvent.frameNumber)
                return false;

            switch (localEvent.eventType)
            {
                case BattleSyncEventType.UnitDamage:
                    return DetectDamageConflict(localEvent, remoteEvent);
                case BattleSyncEventType.UnitMove:
                    return DetectPositionConflict(localEvent, remoteEvent);
                case BattleSyncEventType.ElixirUpdate:
                    return DetectElixirConflict(localEvent, remoteEvent);
                default:
                    return false;
            }
        }

        private bool DetectDamageConflict(BattleSyncEvent local, BattleSyncEvent remote)
        {
            var localData = ParseDamageEvent(local.eventData);
            var remoteData = ParseDamageEvent(remote.eventData);

            return localData.unitId == remoteData.unitId &&
                   localData.damage != remoteData.damage;
        }

        private bool DetectPositionConflict(BattleSyncEvent local, BattleSyncEvent remote)
        {
            var localPos = ParsePositionEvent(local.eventData);
            var remotePos = ParsePositionEvent(remote.eventData);

            return Vector3.Distance(localPos, remotePos) > BattleSyncConfig.POSITION_SYNC_THRESHOLD;
        }

        private bool DetectElixirConflict(BattleSyncEvent local, BattleSyncEvent remote)
        {
            var localElixir = ParseElixirEvent(local.eventData);
            var remoteElixir = ParseElixirEvent(remote.eventData);

            return Mathf.Abs(localElixir - remoteElixir) > 0.1f;
        }

        public BattleSyncEvent ResolveConflict(BattleSyncEvent localEvent, BattleSyncEvent remoteEvent, bool isMasterClient)
        {
            ConflictResolutionType resolutionType;
            BattleSyncEvent resolvedEvent;

            if (isMasterClient)
            {
                if (remoteEvent.timestamp > localEvent.timestamp)
                {
                    resolutionType = ConflictResolutionType.UseRemote;
                    resolvedEvent = remoteEvent;
                }
                else
                {
                    resolutionType = ConflictResolutionType.UseLocal;
                    resolvedEvent = localEvent;
                }
            }
            else
            {
                resolutionType = ConflictResolutionType.UseRemote;
                resolvedEvent = remoteEvent;
            }

            var resolution = new ConflictResolution
            {
                conflictId = localEvent.sequenceId,
                localEvent = localEvent,
                remoteEvent = remoteEvent,
                resolutionType = resolutionType,
                resolvedEvent = resolvedEvent,
                resolvedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            resolvedConflicts[resolution.conflictId] = resolution;

            if (resolvedConflicts.Count > maxHistorySize)
            {
                var oldestKey = GetOldestConflictId();
                resolvedConflicts.Remove(oldestKey);
            }

            OnConflictResolved?.Invoke(resolution.conflictId, resolutionType);

            Debug.Log($"[ConflictResolver] Resolved conflict {resolution.conflictId}: {resolutionType}");
            return resolvedEvent;
        }

        public BattleSyncEvent ResolveDamageConflict(BattleSyncEvent local, BattleSyncEvent remote, bool isMasterClient)
        {
            var localData = ParseDamageEvent(local.eventData);
            var remoteData = ParseDamageEvent(remote.eventData);

            int resolvedDamage;
            if (isMasterClient)
            {
                resolvedDamage = localData.damage;
            }
            else
            {
                resolvedDamage = remoteData.damage;
            }

            var resolvedEvent = new BattleSyncEvent(BattleSyncEventType.UnitDamage, local.frameNumber, local.playerId)
            {
                priority = SyncPriority.High
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(localData.unitId);
                writer.Write(resolvedDamage);
                writer.Write(localData.sourceId);
                resolvedEvent.eventData = stream.ToArray();
            }

            return resolvedEvent;
        }

        public BattleSyncEvent ResolvePositionConflict(BattleSyncEvent local, BattleSyncEvent remote)
        {
            var localPos = ParsePositionEvent(local.eventData);
            var remotePos = ParsePositionEvent(remote.eventData);

            var resolvedPos = Vector3.Lerp(localPos, remotePos, 0.5f);

            var resolvedEvent = new BattleSyncEvent(BattleSyncEventType.UnitMove, local.frameNumber, local.playerId)
            {
                priority = SyncPriority.Low
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(local.eventData[0]);
                writer.Write(resolvedPos.x);
                writer.Write(resolvedPos.y);
                writer.Write(resolvedPos.z);
                resolvedEvent.eventData = stream.ToArray();
            }

            return resolvedEvent;
        }

        public BattleSyncEvent ResolveElixirConflict(BattleSyncEvent local, BattleSyncEvent remote, bool isMasterClient)
        {
            var localElixir = ParseElixirEvent(local.eventData);
            var remoteElixir = ParseElixirEvent(remote.eventData);

            float resolvedElixir = isMasterClient ? localElixir : remoteElixir;

            var resolvedEvent = new BattleSyncEvent(BattleSyncEventType.ElixirUpdate, local.frameNumber, local.playerId)
            {
                priority = SyncPriority.Normal
            };

            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(local.playerId);
                writer.Write(resolvedElixir);
                resolvedEvent.eventData = stream.ToArray();
            }

            return resolvedEvent;
        }

        public void RecordLocalEvent(BattleSyncEvent syncEvent)
        {
            if (syncEvent == null) return;
            localEvents[syncEvent.sequenceId] = syncEvent;

            if (localEvents.Count > maxHistorySize)
            {
                var oldestKey = GetOldestEventId(localEvents);
                localEvents.Remove(oldestKey);
            }
        }

        public void RecordRemoteEvent(BattleSyncEvent syncEvent)
        {
            if (syncEvent == null) return;
            remoteEvents[syncEvent.sequenceId] = syncEvent;

            if (remoteEvents.Count > maxHistorySize)
            {
                var oldestKey = GetOldestEventId(remoteEvents);
                remoteEvents.Remove(oldestKey);
            }
        }

        public List<ConflictResolution> GetConflictHistory()
        {
            return new List<ConflictResolution>(resolvedConflicts.Values);
        }

        public int GetConflictCount()
        {
            return resolvedConflicts.Count;
        }

        public void ClearHistory()
        {
            localEvents.Clear();
            remoteEvents.Clear();
            resolvedConflicts.Clear();
        }

        private (int unitId, int damage, int sourceId) ParseDamageEvent(byte[] data)
        {
            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                return (reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            }
        }

        private Vector3 ParsePositionEvent(byte[] data)
        {
            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                reader.ReadInt32();
                return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
        }

        private float ParseElixirEvent(byte[] data)
        {
            using (var stream = new System.IO.MemoryStream(data))
            using (var reader = new System.IO.BinaryReader(stream))
            {
                reader.ReadInt32();
                return reader.ReadSingle();
            }
        }

        private int GetOldestConflictId()
        {
            int oldestId = int.MaxValue;
            long oldestTime = long.MaxValue;

            foreach (var kvp in resolvedConflicts)
            {
                if (kvp.Value.resolvedTimestamp < oldestTime)
                {
                    oldestTime = kvp.Value.resolvedTimestamp;
                    oldestId = kvp.Key;
                }
            }

            return oldestId;
        }

        private int GetOldestEventId(Dictionary<int, BattleSyncEvent> events)
        {
            int oldestId = int.MaxValue;
            long oldestTime = long.MaxValue;

            foreach (var kvp in events)
            {
                if (kvp.Value.timestamp < oldestTime)
                {
                    oldestTime = kvp.Value.timestamp;
                    oldestId = kvp.Key;
                }
            }

            return oldestId;
        }
    }
}
