using UnityEngine;
using System.Collections;
using KingdomWar.Game.Cards;
using KingdomWar.Tools;
namespace KingdomWar.Game.Battle
{
    public class Spell : MonoBehaviour
    {
        [Header("法术属性")]
        public int ownerId;              // 所有者ID
        public string spellName;         // 法术名称
        public int damage;               // 伤害值
        public float radius;             // 范围半径
        public float duration;           // 持续时间
        public float deployTime;         // 部署时间
        
        [Header("法术状态")]
        public SpellState state = SpellState.Idle; // 法术状态
        
        private float durationTimer = 0f; // 持续时间计时器
        private float damageTimer = 0f;   // 伤害计时器（用于持续伤害）
        private float damageInterval = 0.5f; // 伤害间隔
        private bool hasAppliedInitialDamage = false; // 是否已应用初始伤害
        private bool hasAppliedInstantDamage = false; // 是否已应用瞬时伤害（防止重复）
        private Vector3 targetPosition;  // 目标位置
        public SpellEffectType spellEffectType = SpellEffectType.Damage;  // 法术效果类型

        // 初始化法术
        public void Initialize(int ownerId, string spellName, int damage, float radius, float duration, Vector3 targetPosition)
        {
            this.ownerId = ownerId;
            this.spellName = spellName;
            this.damage = damage;
            this.radius = radius;
            this.duration = duration;
            this.targetPosition = targetPosition;
            this.state = SpellState.Casting;
            this.durationTimer = 0f;
            this.damageTimer = 0f;
            this.hasAppliedInitialDamage = false;
            this.hasAppliedInstantDamage = false;
            Debug.Log($"[Spell] Initialize: {spellName}, damage: {damage}, radius: {radius}, duration: {duration}, targetPosition: {targetPosition}, ownerId: {ownerId}");
        }
        
        // 初始化法术
        public void Initialize(int ownerId, CardData cardData, Vector3 targetPosition)
        {
            Initialize(ownerId, cardData.cardName, cardData.spellData.damage, cardData.spellData.radius, cardData.spellData.duration, targetPosition);
        }

        // 初始化法术（含效果类型）
        public void Initialize(int ownerId, string spellName, int damage, float radius, float duration,
                               Vector3 targetPosition, SpellEffectType effectType)
        {
            Initialize(ownerId, spellName, damage, radius, duration, targetPosition);
            this.spellEffectType = effectType;
        }

        // 更新法术
        public void UpdateSpell()
        {
            switch (state)
            {
                case SpellState.Casting:
                    CastSpell();
                    break;
                case SpellState.Active:
                    UpdateActiveSpell();
                    break;
                case SpellState.Ended:
                    EndSpell();
                    break;
            }
        }
        
        // 释放法术
        private void CastSpell()
        {
            if (hasAppliedInstantDamage)
            {
                state = SpellState.Ended;
                return;
            }
            
            if (duration <= 0)
            {
                ApplySpellEffect();
                hasAppliedInstantDamage = true;
                hasAppliedInitialDamage = true;
                state = SpellState.Ended;
            }
            else
            {
                if (!hasAppliedInitialDamage)
                {
                    ApplySpellEffect();
                    hasAppliedInitialDamage = true;
                }
                damageTimer = 0f;
                state = SpellState.Active;
            }
        }
        
        // 更新活跃的法术
        private void UpdateActiveSpell()
        {
            // 更新持续时间
            durationTimer += Time.deltaTime;
            if (durationTimer >= duration)
            {
                state = SpellState.Ended;
                return;
            }
            
            // 持续伤害类型的法术，使用独立的伤害计时器
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                ApplySpellEffect();
                damageTimer = 0f; // 重置伤害计时器
            }
        }
        
        private void ApplySpellEffect()
        {
            Debug.Log($"[Spell] ApplySpellEffect called: {spellName}, damage: {damage}, radius: {radius}, targetPosition: {targetPosition}, ownerId: {ownerId}");

            float effectValue = 0f;
            if (spellEffectType == SpellEffectType.Boost) effectValue = 0.4f;  // +40% boost

            // For Heal, use positive damage value
            int effectiveDamage = (spellEffectType == SpellEffectType.Heal) ? Mathf.Abs(damage) : damage;

            SpellEffectResolver.ApplyEffect(spellEffectType, ownerId, targetPosition,
                                             effectiveDamage, radius, duration, effectValue);
        }
        
        // 结束法术
        private void EndSpell()
        {
            // 从BattleManager中移除
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.Spells.Remove(this);
            }
            
            // 将法术返回对象池
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnObject(ObjectPoolManager.SPELL_POOL, gameObject);
            }
            else
            {
                // 备用方案：销毁法术对象
                Destroy(gameObject);
            }
        }
    }
    
    public enum SpellState
    {
        Idle,       // 空闲
        Casting,    // 释放中
        Active,     // 活跃中
        Ended       // 已结束
    }
}