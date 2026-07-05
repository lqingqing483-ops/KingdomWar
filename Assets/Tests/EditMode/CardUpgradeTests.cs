using NUnit.Framework;
using KingdomWar.Game.Cards;

public class CardUpgradeTableTests
{
    [Test]
    public void Table_Has13Levels()
    {
        Assert.That(CardUpgradeTable.GetMaxLevel(), Is.EqualTo(13));
    }

    [Test]
    public void Level1_HasZeroCost()
    {
        var level = CardUpgradeTable.GetLevel(1);
        Assert.That(level.fragmentsRequired, Is.Zero);
        Assert.That(level.goldRequired, Is.Zero);
        Assert.That(level.statMultiplier, Is.EqualTo(1.0f));
    }

    [Test]
    public void Level2_HasCorrectMultiplier()
    {
        var level = CardUpgradeTable.GetLevel(2);
        Assert.That(level.statMultiplier, Is.EqualTo(1.1f).Within(0.01f));
    }

    [Test]
    public void Level13_HasHighestMultiplier()
    {
        var level = CardUpgradeTable.GetLevel(13);
        Assert.That(level.statMultiplier, Is.GreaterThan(3.0f));
        Assert.That(level.fragmentsRequired, Is.EqualTo(4000));
        Assert.That(level.goldRequired, Is.EqualTo(204800));
    }

    [Test]
    public void GetLevel_ReturnsNull_ForInvalidLevel()
    {
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid card level: 0. Must be between 1 and 13.");
        Assert.That(CardUpgradeTable.GetLevel(0), Is.Null);
        UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, "Invalid card level: 14. Must be between 1 and 13.");
        Assert.That(CardUpgradeTable.GetLevel(14), Is.Null);
    }

    [Test]
    public void GetUpgradeCost_IncreasesWithLevel()
    {
        int prev = 0;
        for (int i = 2; i <= 13; i++)
        {
            int cost = CardUpgradeTable.GetUpgradeCost(i);
            Assert.That(cost, Is.GreaterThan(prev));
            prev = cost;
        }
    }

    [Test]
    public void GetStatMultiplier_IncreasesWithLevel()
    {
        float prev = 1.0f;
        for (int i = 2; i <= 13; i++)
        {
            float mult = CardUpgradeTable.GetStatMultiplier(i);
            Assert.That(mult, Is.GreaterThan(prev));
            prev = mult;
        }
    }

    [Test]
    public void GetFragmentsRequired_ReturnsCorrectValues()
    {
        Assert.That(CardUpgradeTable.GetFragmentsRequired(2), Is.EqualTo(2));
        Assert.That(CardUpgradeTable.GetFragmentsRequired(7), Is.EqualTo(100));
        Assert.That(CardUpgradeTable.GetFragmentsRequired(13), Is.EqualTo(4000));
    }
}

public class CardUpgradeExtensionsTests
{
    [Test]
    public void UnitHealthAtLevel1_EqualsBaseHealth()
    {
        var unit = new UnitData { health = 1000, damage = 200 };
        Assert.That(unit.GetHealthAtLevel(1), Is.EqualTo(1000));
        Assert.That(unit.GetDamageAtLevel(1), Is.EqualTo(200));
    }

    [Test]
    public void UnitHealthAtLevel13_About3xBase()
    {
        var unit = new UnitData { health = 1000, damage = 200 };
        Assert.That(unit.GetHealthAtLevel(13), Is.EqualTo(3138));
        // Mathf.RoundToInt(200 * 3.138) = Mathf.RoundToInt(627.6) = 628
        Assert.That(unit.GetDamageAtLevel(13), Is.EqualTo(628));
    }

    [Test]
    public void BuildingHealthAtLevel1_EqualsBaseHealth()
    {
        var building = new BuildingData { health = 2000, damage = 100 };
        Assert.That(building.GetHealthAtLevel(1), Is.EqualTo(2000));
        Assert.That(building.GetDamageAtLevel(1), Is.EqualTo(100));
    }

    [Test]
    public void SpellDamageAtLevel1_EqualsBaseDamage()
    {
        var spell = new SpellData { damage = 500 };
        Assert.That(spell.GetDamageAtLevel(1), Is.EqualTo(500));
    }

    [Test]
    public void SpellDamageAtLevel9_MoreThanDouble()
    {
        var spell = new SpellData { damage = 500 };
        Assert.That(spell.GetDamageAtLevel(9), Is.GreaterThan(1000));
    }
}
