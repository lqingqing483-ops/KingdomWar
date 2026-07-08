using NUnit.Framework;
using UnityEngine;
using KingdomWar.Game.Battle;

namespace KingdomWar.Tests.EditMode
{
    public class SpellEffectTests
    {
        // ===== SpellEffectType Enum Tests =====

        [Test]
        public void SpellEffectType_HasDamage()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Damage), Is.True);
        }

        [Test]
        public void SpellEffectType_HasHeal()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Heal), Is.True);
        }

        [Test]
        public void SpellEffectType_HasFreeze()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Freeze), Is.True);
        }

        [Test]
        public void SpellEffectType_HasStun()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Stun), Is.True);
        }

        [Test]
        public void SpellEffectType_HasBoost()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Boost), Is.True);
        }

        [Test]
        public void SpellEffectType_HasClone()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Clone), Is.True);
        }

        [Test]
        public void SpellEffectType_HasExactly10Values()
        {
            Assert.That(System.Enum.GetValues(typeof(SpellEffectType)).Length, Is.EqualTo(10));
        }

        [Test]
        public void SpellEffectType_HasPoison()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Poison), Is.True);
        }

        [Test]
        public void SpellEffectType_HasSlow()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Slow), Is.True);
        }

        [Test]
        public void SpellEffectType_HasKnockback()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Knockback), Is.True);
        }

        [Test]
        public void SpellEffectType_HasPull()
        {
            Assert.That(System.Enum.IsDefined(typeof(SpellEffectType), SpellEffectType.Pull), Is.True);
        }

        // ===== Buff System Tests =====

        private Unit testUnit;
        private GameObject testUnitObj;

        [SetUp]
        public void SetUp()
        {
            testUnitObj = new GameObject("TestUnit");
            testUnit = testUnitObj.AddComponent<Unit>();
            testUnit.unitName = "TestKnight";
            testUnit.maxHealth = 100;
            testUnit.health = 100;
            testUnit.ownerId = 1;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testUnitObj);
        }

        [Test]
        public void Unit_AddBuff_Freeze_SetsIsFrozen()
        {
            testUnit.AddBuff(BuffType.Freeze, 5f);
            Assert.That(testUnit.isFrozen, Is.True);
        }

        [Test]
        public void Unit_AddBuff_Stun_SetsIsStunned()
        {
            testUnit.AddBuff(BuffType.Stun, 3f);
            Assert.That(testUnit.isStunned, Is.True);
        }

        [Test]
        public void Unit_AddBuff_BoostDamage_IncreasesMultiplier()
        {
            testUnit.AddBuff(BuffType.BoostDamage, 5f, 0.5f);
            Assert.That(testUnit.damageMultiplier, Is.EqualTo(1.5f).Within(0.01f));
        }

        [Test]
        public void Unit_AddBuff_BoostSpeed_IncreasesMultiplier()
        {
            testUnit.AddBuff(BuffType.BoostSpeed, 5f, 0.3f);
            Assert.That(testUnit.speedMultiplier, Is.EqualTo(1.3f).Within(0.01f));
        }

        [Test]
        public void Unit_Buff_DefaultState_NoEffects()
        {
            Assert.That(testUnit.isFrozen, Is.False);
            Assert.That(testUnit.isStunned, Is.False);
            Assert.That(testUnit.damageMultiplier, Is.EqualTo(1f));
            Assert.That(testUnit.speedMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void Unit_AddBuff_Freeze_ThenRemove_ResetsState()
        {
            testUnit.AddBuff(BuffType.Freeze, 5f);
            Assert.That(testUnit.isFrozen, Is.True);
            // Add buff with 0 duration to trigger immediate expiration on next update
            testUnit.AddBuff(BuffType.Freeze, 0f);
            // Force buff update to process expired buffs
            testUnit.UpdateBuffs();
            Assert.That(testUnit.isFrozen, Is.False);
        }

        [Test]
        public void Unit_Heal_RestoresHealth()
        {
            testUnit.health = 50;
            testUnit.Heal(30);
            Assert.That(testUnit.health, Is.EqualTo(80));
        }

        [Test]
        public void Unit_Heal_DoesNotExceedMaxHealth()
        {
            testUnit.health = 90;
            testUnit.Heal(20);
            Assert.That(testUnit.health, Is.EqualTo(100));
        }

        [Test]
        public void Unit_Heal_NoEffectOnDeadUnit()
        {
            testUnit.health = 0;
            testUnit.Heal(50);
            Assert.That(testUnit.health, Is.EqualTo(0));
        }

        // ===== BuffType Enum Tests =====

        [Test]
        public void BuffType_HasExactly6Values()
        {
            Assert.That(System.Enum.GetValues(typeof(BuffType)).Length, Is.EqualTo(6));
        }

        [Test]
        public void BuffType_HasAllExpectedTypes()
        {
            Assert.That(System.Enum.IsDefined(typeof(BuffType), BuffType.Freeze));
            Assert.That(System.Enum.IsDefined(typeof(BuffType), BuffType.Stun));
            Assert.That(System.Enum.IsDefined(typeof(BuffType), BuffType.BoostDamage));
            Assert.That(System.Enum.IsDefined(typeof(BuffType), BuffType.BoostSpeed));
            Assert.That(System.Enum.IsDefined(typeof(BuffType), BuffType.Poison));
            Assert.That(System.Enum.IsDefined(typeof(BuffType), BuffType.Slow));
        }
    }
}
