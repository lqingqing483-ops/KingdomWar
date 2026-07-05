using NUnit.Framework;
using KingdomWar.Game.Config;

namespace KingdomWar.Tests.EditMode
{
    public class EconomyBalanceTests
    {
        private EconomyBalanceSO economy;

        [SetUp]
        public void SetUp()
        {
            economy = EconomyBalanceSO.Instance;
        }

        // ==================== Instance Tests ====================

        [Test]
        public void Instance_IsNotNull()
        {
            Assert.That(economy, Is.Not.Null);
        }

        // ==================== Player Defaults Tests ====================

        [Test]
        public void DefaultGold_IsPositive()
        {
            Assert.That(economy.defaultGold, Is.GreaterThan(0));
        }

        [Test]
        public void DefaultGems_IsPositive()
        {
            Assert.That(economy.defaultGems, Is.GreaterThan(0));
        }

        // ==================== Shop Settings Tests ====================

        [Test]
        public void ShopDailyCardCount_IsBetween1And8()
        {
            Assert.That(economy.shopDailyCardCount, Is.InRange(1, 8));
        }

        [Test]
        public void ShopCardBasePrice_IsPositive()
        {
            Assert.That(economy.shopCardBasePrice, Is.GreaterThan(0));
        }

        [Test]
        public void ShopCardPriceMultiplier_IsPositive()
        {
            Assert.That(economy.shopCardPriceMultiplier, Is.GreaterThan(0));
        }

        // ==================== Free Chest Tests ====================

        [Test]
        public void FreeChestGold_IsPositive()
        {
            Assert.That(economy.freeChestGold, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void FreeChestCooldown_IsReasonable()
        {
            // Between 1 hour (3600s) and 24 hours (86400s)
            Assert.That(economy.freeChestCooldownSeconds, Is.InRange(3600, 86400));
        }

        [Test]
        public void FreeChestRarityWeights_Has4Elements()
        {
            Assert.That(economy.freeChestRarityWeights, Has.Length.EqualTo(4));
        }

        [Test]
        public void FreeChestRarityWeights_SumTo100()
        {
            int sum = 0;
            foreach (int w in economy.freeChestRarityWeights)
            {
                sum += w;
            }
            Assert.That(sum, Is.EqualTo(100));
        }

        [Test]
        public void FreeChestCardCount_IsPositive()
        {
            Assert.That(economy.freeChestCardCount, Is.GreaterThan(0));
        }

        // ==================== Victory Chest Tests ====================

        [Test]
        public void VictoryChestGold_IsGreaterThanFreeChest()
        {
            Assert.That(economy.victoryChestGold, Is.GreaterThan(economy.freeChestGold));
        }

        [Test]
        public void VictoryChestRarityWeights_Has4Elements()
        {
            Assert.That(economy.victoryChestRarityWeights, Has.Length.EqualTo(4));
        }

        [Test]
        public void VictoryChestRarityWeights_SumTo100()
        {
            int sum = 0;
            foreach (int w in economy.victoryChestRarityWeights)
            {
                sum += w;
            }
            Assert.That(sum, Is.EqualTo(100));
        }

        [Test]
        public void VictoryChestUnlockMinutes_IsPositive()
        {
            Assert.That(economy.victoryChestUnlockMinutes, Is.GreaterThan(0));
        }

        [Test]
        public void VictoryChestCardCount_IsGreaterThanFreeChest()
        {
            Assert.That(economy.victoryChestCardCount, Is.GreaterThan(economy.freeChestCardCount));
        }

        // ==================== Chest SpeedUp Tests ====================

        [Test]
        public void ChestSpeedUpGemCost_IsPositive()
        {
            Assert.That(economy.chestSpeedUpGemCostPerMinute, Is.GreaterThan(0));
        }

        // ==================== Battle Rewards Tests ====================

        [Test]
        public void BattleWinGold_IsPositive()
        {
            Assert.That(economy.battleWinGold, Is.GreaterThan(0));
        }

        [Test]
        public void BattleLoseGold_IsLessThanWinGold()
        {
            Assert.That(economy.battleLoseGold, Is.LessThan(economy.battleWinGold));
        }

        [Test]
        public void BattleDrawGold_IsBetweenLoseAndWin()
        {
            Assert.That(economy.battleDrawGold, Is.InRange(economy.battleLoseGold, economy.battleWinGold));
        }
    }
}
