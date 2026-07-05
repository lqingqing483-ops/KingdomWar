using System;
using System.IO;
using UnityEngine;

namespace KingdomWar.Game.Battle
{
    /// <summary>
    /// Network data compression utilities.
    /// Provides delta compression and quantized Vector3 serialization.
    /// </summary>
    public static class NetworkCompressor
    {
        // ===== Quantized Vector3 =====
        // Compress Vector3 (12 bytes) into 3 shorts (6 bytes) with configurable precision

        private const float POSITION_SCALE = 100f;  // 0.01 cm precision
        private const float ROTATION_SCALE = 1000f; // 0.001 degree precision

        public static (short x, short y, short z) QuantizePosition(Vector3 pos)
        {
            return (
                (short)(pos.x * POSITION_SCALE),
                (short)(pos.y * POSITION_SCALE),
                (short)(pos.z * POSITION_SCALE)
            );
        }

        public static Vector3 DequantizePosition(short x, short y, short z)
        {
            return new Vector3(
                x / POSITION_SCALE,
                y / POSITION_SCALE,
                z / POSITION_SCALE
            );
        }

        public static (short x, short y, short z) QuantizeRotation(Vector3 euler)
        {
            return (
                (short)(euler.x * ROTATION_SCALE),
                (short)(euler.y * ROTATION_SCALE),
                (short)(euler.z * ROTATION_SCALE)
            );
        }

        public static Vector3 DequantizeRotation(short x, short y, short z)
        {
            return new Vector3(
                x / ROTATION_SCALE,
                y / ROTATION_SCALE,
                z / ROTATION_SCALE
            );
        }

        // ===== Delta Compression =====
        // For UnitSyncData: only write changed fields

        [Flags]
        public enum UnitChangeFlags : ushort
        {
            None        = 0,
            Position    = 1 << 0,
            Rotation    = 1 << 1,
            Velocity    = 1 << 2,
            Health      = 1 << 3,
            MaxHealth   = 1 << 4,
            State       = 1 << 5,
            TargetId    = 1 << 6,
            IsAttacking = 1 << 7,
            IsMoving    = 1 << 8,
            AttackCd    = 1 << 9,
        }

        /// <summary>
        /// Serialize UnitSyncData with delta compression.
        /// Only fields marked in changeFlags are written.
        /// Returns ~15-25 bytes for a typical update (vs ~60 bytes full).
        /// </summary>
        public static byte[] SerializeUnitDelta(ushort changeFlags, UnitSyncData data)
        {
            using (var ms = new MemoryStream(32))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(data.unitId);
                writer.Write(changeFlags);

                var flags = (UnitChangeFlags)changeFlags;

                if ((flags & UnitChangeFlags.Position) != 0)
                {
                    var q = QuantizePosition(data.position);
                    writer.Write(q.x); writer.Write(q.y); writer.Write(q.z);
                }
                if ((flags & UnitChangeFlags.Rotation) != 0)
                {
                    var q = QuantizeRotation(data.rotation.eulerAngles);
                    writer.Write(q.x); writer.Write(q.y); writer.Write(q.z);
                }
                if ((flags & UnitChangeFlags.Velocity) != 0)
                {
                    var q = QuantizePosition(data.velocity);
                    writer.Write(q.x); writer.Write(q.y); writer.Write(q.z);
                }
                if ((flags & UnitChangeFlags.Health) != 0)
                    writer.Write((short)data.health);
                if ((flags & UnitChangeFlags.MaxHealth) != 0)
                    writer.Write((short)data.maxHealth);
                if ((flags & UnitChangeFlags.State) != 0)
                    writer.Write((byte)data.state);
                if ((flags & UnitChangeFlags.TargetId) != 0)
                    writer.Write(data.targetId);
                if ((flags & UnitChangeFlags.IsAttacking) != 0)
                    writer.Write(data.isAttacking);
                if ((flags & UnitChangeFlags.IsMoving) != 0)
                    writer.Write(data.isMoving);
                if ((flags & UnitChangeFlags.AttackCd) != 0)
                    writer.Write((short)(data.attackCooldown * 100));

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Apply delta update to a UnitSyncData reference.
        /// </summary>
        public static void ApplyUnitDelta(byte[] delta, ref UnitSyncData target)
        {
            using (var ms = new MemoryStream(delta))
            using (var reader = new BinaryReader(ms))
            {
                target.unitId = reader.ReadInt32();
                ushort rawFlags = reader.ReadUInt16();
                var flags = (UnitChangeFlags)rawFlags;

                if ((flags & UnitChangeFlags.Position) != 0)
                    target.position = DequantizePosition(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
                if ((flags & UnitChangeFlags.Rotation) != 0)
                    target.rotation = Quaternion.Euler(DequantizeRotation(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16()));
                if ((flags & UnitChangeFlags.Velocity) != 0)
                    target.velocity = DequantizePosition(reader.ReadInt16(), reader.ReadInt16(), reader.ReadInt16());
                if ((flags & UnitChangeFlags.Health) != 0)
                    target.health = reader.ReadInt16();
                if ((flags & UnitChangeFlags.MaxHealth) != 0)
                    target.maxHealth = reader.ReadInt16();
                if ((flags & UnitChangeFlags.State) != 0)
                    target.state = (UnitState)reader.ReadByte();
                if ((flags & UnitChangeFlags.TargetId) != 0)
                    target.targetId = reader.ReadInt32();
                if ((flags & UnitChangeFlags.IsAttacking) != 0)
                    target.isAttacking = reader.ReadBoolean();
                if ((flags & UnitChangeFlags.IsMoving) != 0)
                    target.isMoving = reader.ReadBoolean();
                if ((flags & UnitChangeFlags.AttackCd) != 0)
                    target.attackCooldown = reader.ReadInt16() / 100f;
            }
        }

        /// <summary>
        /// Compute change flags between old and new UnitSyncData.
        /// </summary>
        public static ushort ComputeUnitChanges(UnitSyncData oldData, UnitSyncData newData)
        {
            ushort flags = 0;
            if (Vector3.Distance(oldData.position, newData.position) > 0.01f) flags |= (ushort)UnitChangeFlags.Position;
            if (Quaternion.Angle(oldData.rotation, newData.rotation) > 1f) flags |= (ushort)UnitChangeFlags.Rotation;
            if (Vector3.Distance(oldData.velocity, newData.velocity) > 0.01f) flags |= (ushort)UnitChangeFlags.Velocity;
            if (oldData.health != newData.health) flags |= (ushort)UnitChangeFlags.Health;
            if (oldData.maxHealth != newData.maxHealth) flags |= (ushort)UnitChangeFlags.MaxHealth;
            if (oldData.state != newData.state) flags |= (ushort)UnitChangeFlags.State;
            if (oldData.targetId != newData.targetId) flags |= (ushort)UnitChangeFlags.TargetId;
            if (oldData.isAttacking != newData.isAttacking) flags |= (ushort)UnitChangeFlags.IsAttacking;
            if (oldData.isMoving != newData.isMoving) flags |= (ushort)UnitChangeFlags.IsMoving;
            if (Mathf.Abs(oldData.attackCooldown - newData.attackCooldown) > 0.01f) flags |= (ushort)UnitChangeFlags.AttackCd;
            return flags;
        }

        // ===== Convenience: full UnitSyncData as delta (for initial sync) =====

        /// <summary>
        /// Serialize full UnitSyncData (equivalent to marking all flags).
        /// Uses delta format with all bits set.
        /// </summary>
        public static byte[] SerializeUnitFull(UnitSyncData data)
        {
            return SerializeUnitDelta(0xFFFF, data);
        }
    }
}
