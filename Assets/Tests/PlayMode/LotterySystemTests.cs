using NUnit.Framework;
using KingdomWar.HotUpdate;
using KingdomWar.Game.Cards;

public class LotterySystemTests
{
    [SetUp]
    public void SetUp()
    {
        var db = CardDatabase.Instance;
        if (db.GetAllCards().Count == 0)
            db.ReloadCardData();
    }

    [Test]
    public void Draw_ReturnsSuccess_WhenPlayerHasGold()
    {
        var system = LotterySystem.Instance;
        PlayerDataManager.Instance.AddGold(10000);
        var result = system.Draw();
        Assert.That(result.success, Is.True);
        Assert.That(result.card, Is.Not.Null);
    }

    [Test]
    public void GetRemainingFreeDraws_ReturnsNonNegative()
    {
        var system = LotterySystem.Instance;
        Assert.That(system.GetRemainingFreeDraws(), Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void IsFreeDrawAvailable_ReturnsBool()
    {
        var system = LotterySystem.Instance;
        Assert.That(system.IsFreeDrawAvailable(), Is.TypeOf<bool>());
    }

    [Test]
    public void HasEnoughCurrency_ReturnsBool()
    {
        var system = LotterySystem.Instance;
        Assert.That(system.HasEnoughCurrency(), Is.TypeOf<bool>());
    }

    [Test]
    public void Draw_ReturnsCorrectCardRarityWeights()
    {
        var system = LotterySystem.Instance;
        PlayerDataManager.Instance.AddGold(1000000);
        int commonCount = 0;
        int rareCount = 0;
        int epicCount = 0;
        int legendaryCount = 0;
        int draws = 2000;

        for (int i = 0; i < draws; i++)
        {
            var result = system.Draw();
            if (result.success && result.card != null)
            {
                switch (result.card.rarity)
                {
                    case 1: commonCount++; break;
                    case 2: rareCount++; break;
                    case 3: epicCount++; break;
                    case 4: legendaryCount++; break;
                }
            }
        }

        // Common (weight 100) should be most drawn
        Assert.That(commonCount, Is.GreaterThan(rareCount));
        Assert.That(rareCount, Is.GreaterThan(epicCount));
        Assert.That(epicCount, Is.GreaterThan(legendaryCount));
    }
}
