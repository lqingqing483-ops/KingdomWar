using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomWar.Game.SeasonPass;
using KingdomWar.HotUpdate;

namespace KingdomWar.Tests.PlayMode
{
    public class SeasonPassIntegrationTests
    {
        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Ensure PlayerDataManager singleton exists
            var pdm = PlayerDataManager.Instance;
            Assert.IsNotNull(pdm);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_AddExp_IncreasesExp()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            int initialExp = spManager.GetSaveData().totalExp;

            // Act
            spManager.AddExp(50);

            // Assert
            Assert.AreEqual(initialExp + 50, spManager.GetSaveData().totalExp);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_GetCurrentLevel_ReturnsCorrectLevel()
        {
            // Arrange
            var config = SeasonPassConfigSO.Instance;
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            // Act: add enough for 2 levels
            int expFor2Levels = config.expPerLevel * 2;
            spManager.AddExp(expFor2Levels);

            // Assert
            Assert.AreEqual(2, spManager.GetCurrentLevel());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ClaimReward_FreeTier_MarksClaimed()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            int level5Exp = SeasonPassConfigSO.Instance.expPerLevel * 5;
            spManager.AddExp(level5Exp);

            // Check if there's a free reward at level 5
            var config = SeasonPassConfigSO.Instance;
            var rewardsAt5 = config.GetRewardsAtLevel(5, SeasonPassTier.Free);

            if (rewardsAt5.Count > 0)
            {
                // Act
                bool result = spManager.ClaimReward(5, SeasonPassTier.Free);

                // Assert
                Assert.IsTrue(result);
                Assert.IsTrue(spManager.IsRewardClaimed(5, SeasonPassTier.Free));
            }
            else
            {
                // No free reward at level 5 in default config — skip
                Assert.Ignore("No free reward at level 5 configured");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_PurchasePremium_DeductsGems()
        {
            // Arrange
            var pdm = PlayerDataManager.Instance;
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            int cost = SeasonPassConfigSO.Instance.premiumPassCostGems;
            pdm.AddGems(cost + 100); // ensure enough gems

            int gemsBefore = pdm.GetGems();

            // Act
            bool result = spManager.PurchasePremiumPass();

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(gemsBefore - cost, pdm.GetGems());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ClaimReward_Premium_RequiresPass()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            spManager.AddExp(SeasonPassConfigSO.Instance.expPerLevel * 5);

            // Act — try claiming premium without having premium pass
            bool result = spManager.ClaimReward(5, SeasonPassTier.Premium);

            // Assert
            Assert.IsFalse(result, "Should not be able to claim premium without purchasing pass");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_IsExpired_ReturnsFalseForNewSeason()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            // Assert — new season should not be expired
            Assert.IsFalse(spManager.IsExpired());
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_GetLevelProgress_IsBetweenZeroAndOne()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            spManager.AddExp(50); // add some but not full level

            // Act
            float progress = spManager.GetLevelProgress();

            // Assert
            Assert.GreaterOrEqual(progress, 0f);
            Assert.LessOrEqual(progress, 1f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ResetSeason_ClearsData()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            spManager.AddExp(500);
            Assert.Greater(spManager.GetSaveData().totalExp, 0);

            // Act
            spManager.ResetSeason();

            // Assert
            Assert.AreEqual(0, spManager.GetSaveData().totalExp);
            Assert.IsFalse(spManager.GetSaveData().hasPremiumPass);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ClaimEndOfSeason_NotExpired_ReturnsFalse()
        {
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            bool result = spManager.ClaimEndOfSeasonReward();
            Assert.IsFalse(result, "ClaimEndOfSeasonReward should return false when season not expired");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ClaimEndOfSeason_WithoutPremium_ReturnsFalse()
        {
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            Assert.IsFalse(spManager.GetSaveData().hasPremiumPass);

            bool result = spManager.ClaimEndOfSeasonReward();
            Assert.IsFalse(result, "Should return false without premium pass");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_PurchasePremium_SetsHasPremium()
        {
            var pdm = PlayerDataManager.Instance;
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            int cost = SeasonPassConfigSO.Instance.premiumPassCostGems;
            pdm.AddGems(cost + 100);

            spManager.PurchasePremiumPass();

            Assert.IsTrue(spManager.HasPremiumPass(), "HasPremiumPass should be true after purchase");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_AddExp_PastMaxLevel_CapsAtMax()
        {
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            var config = SeasonPassConfigSO.Instance;
            int hugeExp = config.maxLevel * config.expPerLevel * 2;
            spManager.AddExp(hugeExp);

            Assert.AreEqual(config.maxLevel, spManager.GetCurrentLevel(), "Level should be capped at maxLevel");
            yield return null;
        }
    }
}
