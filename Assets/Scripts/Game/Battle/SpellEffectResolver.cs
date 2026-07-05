using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game.Cards;

namespace KingdomWar.Game.Battle
{
    /// <summary>
    /// Resolves spell effects: Damage, Heal, Freeze, Stun, Boost, Clone.
    /// Called by Spell.UpdateSpell() when applying effects.
    /// </summary>
    public enum SpellEffectType
    {
        Damage,     // AoE damage
        Heal,       // Heal friendly units
        Freeze,     // Freeze enemies (no movement, no attack)
        Stun,       // Stun enemies (interrupt current action)
        Boost,      // Boost friendly units (damage + speed)
        Clone       // Clone targeted unit
    }

    public static class SpellEffectResolver
    {
        /// <summary>
        /// Apply a spell effect at the target position.
        /// </summary>
        public static void ApplyEffect(SpellEffectType effectType, int ownerId, Vector3 targetPosition,
                                        int damage, float radius, float duration, float effectValue)
        {
            switch (effectType)
            {
                case SpellEffectType.Damage:
                    ApplyDamage(ownerId, targetPosition, damage, radius);
                    break;
                case SpellEffectType.Heal:
                    ApplyHeal(ownerId, targetPosition, damage, radius);  // damage = heal amount
                    break;
                case SpellEffectType.Freeze:
                    ApplyFreeze(ownerId, targetPosition, radius, duration);
                    break;
                case SpellEffectType.Stun:
                    ApplyStun(ownerId, targetPosition, radius, duration);
                    break;
                case SpellEffectType.Boost:
                    ApplyBoost(ownerId, targetPosition, radius, duration, effectValue);
                    break;
                case SpellEffectType.Clone:
                    ApplyClone(ownerId, targetPosition, radius);
                    break;
            }
        }

        private static void ApplyDamage(int ownerId, Vector3 position, int damage, float radius)
        {
            if (BattleManager.Instance == null) return;

            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit == null || unit.ownerId == ownerId || unit.health <= 0) continue;
                float dist = Vector3.Distance(unit.transform.position, position);
                if (dist <= radius)
                {
                    unit.TakeDamage(damage);
                    Debug.Log($"[SpellEffect] Damage hits {unit.unitName} for {damage}");
                }
            }

            foreach (Building building in BattleManager.Instance.Buildings)
            {
                if (building == null || building.ownerId == ownerId || building.health <= 0) continue;
                float dist = Vector3.Distance(building.transform.position, position);
                if (dist <= radius)
                {
                    building.TakeDamage(damage);
                    Debug.Log($"[SpellEffect] Damage hits {building.buildingName} for {damage}");
                }
            }
        }

        private static void ApplyHeal(int ownerId, Vector3 position, int amount, float radius)
        {
            if (BattleManager.Instance == null) return;

            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit == null || unit.ownerId != ownerId || unit.health <= 0 || unit.health >= unit.maxHealth) continue;
                float dist = Vector3.Distance(unit.transform.position, position);
                if (dist <= radius)
                {
                    unit.Heal(amount);
                    Debug.Log($"[SpellEffect] Heal restores {amount} HP to {unit.unitName}");
                }
            }
        }

        private static void ApplyFreeze(int ownerId, Vector3 position, float radius, float duration)
        {
            if (BattleManager.Instance == null) return;

            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit == null || unit.ownerId == ownerId || unit.health <= 0) continue;
                float dist = Vector3.Distance(unit.transform.position, position);
                if (dist <= radius)
                {
                    unit.AddBuff(BuffType.Freeze, duration);
                    Debug.Log($"[SpellEffect] Freeze applied to {unit.unitName} for {duration}s");
                }
            }
        }

        private static void ApplyStun(int ownerId, Vector3 position, float radius, float duration)
        {
            if (BattleManager.Instance == null) return;

            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit == null || unit.ownerId == ownerId || unit.health <= 0) continue;
                float dist = Vector3.Distance(unit.transform.position, position);
                if (dist <= radius)
                {
                    unit.AddBuff(BuffType.Stun, duration);
                    Debug.Log($"[SpellEffect] Stun applied to {unit.unitName} for {duration}s");
                }
            }
        }

        private static void ApplyBoost(int ownerId, Vector3 position, float radius, float duration, float multiplier)
        {
            if (BattleManager.Instance == null) return;

            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit == null || unit.ownerId != ownerId || unit.health <= 0) continue;
                float dist = Vector3.Distance(unit.transform.position, position);
                if (dist <= radius)
                {
                    unit.AddBuff(BuffType.BoostDamage, duration, multiplier);
                    unit.AddBuff(BuffType.BoostSpeed, duration, multiplier);
                    Debug.Log($"[SpellEffect] Boost applied to {unit.unitName} for {duration}s x{multiplier}");
                }
            }
        }

        private static void ApplyClone(int ownerId, Vector3 position, float radius)
        {
            if (BattleManager.Instance == null) return;

            List<Unit> cloneCandidates = new List<Unit>();
            foreach (Unit unit in BattleManager.Instance.Units)
            {
                if (unit == null || unit.ownerId != ownerId || unit.health <= 0) continue;
                float dist = Vector3.Distance(unit.transform.position, position);
                if (dist <= radius)
                {
                    cloneCandidates.Add(unit);
                }
            }

            // Clone the first found friendly unit (or the closest)
            if (cloneCandidates.Count > 0)
            {
                Unit target = cloneCandidates[0];
                // Create a clone next to the original
                Vector3 clonePos = target.transform.position + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

                // Use NetworkEntityManager to spawn if available, otherwise direct Instantiate
                NetworkEntityManager entityManager = Object.FindObjectOfType<NetworkEntityManager>();
                if (entityManager != null)
                {
                    // Clone has half HP and full damage (common game pattern)
                    CardData cardData = new CardData();
                    cardData.cardName = target.unitName + "_Clone";
                    cardData.cardType = CardType.Unit;
                    // We need a UnitData to spawn
                    UnitData unitData = new UnitData();
                    unitData.health = target.maxHealth / 2;  // Half HP for clones
                    unitData.damage = target.damage;
                    unitData.attackSpeed = target.attackSpeed;
                    unitData.moveSpeed = target.moveSpeed;
                    unitData.attackRange = target.attackRange;
                    cardData.unitData = unitData;
                    // Spawn using the existing method available.
                    // Since we don't have a simple "spawn unit with stats" method,
                    // Instantiate directly and copy properties:
                    GameObject cloneObj = Object.Instantiate(target.gameObject, clonePos, Quaternion.identity);
                    Unit cloneUnit = cloneObj.GetComponent<Unit>();
                    if (cloneUnit != null)
                    {
                        cloneUnit.ownerId = ownerId;
                        cloneUnit.maxHealth = target.maxHealth / 2;
                        cloneUnit.health = target.maxHealth / 2;
                        cloneUnit.unitName = target.unitName + "_Clone";
                        BattleManager.Instance.AddUnit(cloneUnit);
                    }
                    Debug.Log($"[SpellEffect] Clone created: {target.unitName}_Clone at {clonePos}");
                }
            }
        }
    }
}
