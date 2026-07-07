using System;
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

        [UnityTest]
        public IEnumerator SeasonPassManager_GetUnclaimedFreeLevels_ReturnsCorrectLevels()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            var config = SeasonPassConfigSO.Instance;

            // Reach level 12 (1200 exp) — should unlock free rewards at 5 (gold) and 10 (gems)
            spManager.AddExp(config.expPerLevel * 12);

            // Act
            var unclaimed = spManager.GetUnclaimedFreeLevels();

            // Assert — at level 12, free rewards exist at levels 5 and 10
            Assert.Contains(5, unclaimed, "Level 5 (gold) should be unclaimed");
            Assert.Contains(10, unclaimed, "Level 10 (gems) should be unclaimed");
            // Level 15 is past current level, should NOT be included
            Assert.IsFalse(unclaimed.Contains(15), "Level 15 should not be unclaimed (past current level)");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_GetUnclaimedFreeLevels_AfterClaim_ExcludesClaimed()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            var config = SeasonPassConfigSO.Instance;
            spManager.AddExp(config.expPerLevel * 12);

            // Claim level 5 reward
            spManager.ClaimReward(5, SeasonPassTier.Free);

            // Act
            var unclaimed = spManager.GetUnclaimedFreeLevels();

            // Assert
            Assert.IsFalse(unclaimed.Contains(5), "Level 5 should not be unclaimed after claiming");
            Assert.Contains(10, unclaimed, "Level 10 should still be unclaimed");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_GetUnclaimedPremiumLevels_ReturnsEmptyWithoutPremium()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            spManager.AddExp(SeasonPassConfigSO.Instance.expPerLevel * 10);

            // Act
            var unclaimed = spManager.GetUnclaimedPremiumLevels();

            // Assert — no premium pass, so premium levels should be empty
            Assert.IsEmpty(unclaimed, "Premium unclaimed should be empty without premium pass");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_GetUnclaimedPremiumLevels_ReturnsLevelsWithPremium()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            var config = SeasonPassConfigSO.Instance;

            // Buy premium pass
            var pdm = PlayerDataManager.Instance;
            pdm.AddGems(config.premiumPassCostGems + 500);
            spManager.PurchasePremiumPass();

            // Reach level 3
            spManager.AddExp(config.expPerLevel * 3);

            // Act
            var unclaimed = spManager.GetUnclaimedPremiumLevels();

            // Assert — premium has rewards at every level, so levels 1,2,3 should be unclaimed
            Assert.Contains(1, unclaimed, "Level 1 premium should be unclaimed");
            Assert.Contains(2, unclaimed, "Level 2 premium should be unclaimed");
            Assert.Contains(3, unclaimed, "Level 3 premium should be unclaimed");
            Assert.IsFalse(unclaimed.Contains(4), "Level 4 should not be unclaimed (past current level)");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ClaimEndOfSeason_SuccessPath_GrantsReward()
        {
            // Arrange — simulate expired season by manipulating saveData directly
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();

            // Purchase premium pass (required for end-of-season reward)
            var pdm = PlayerDataManager.Instance;
            pdm.AddGems(SeasonPassConfigSO.Instance.premiumPassCostGems + 500);
            spManager.PurchasePremiumPass();

            // Directly manipulate saveData to simulate expired season (30 days ago)
            var data = spManager.GetSaveData();
            data.seasonStartDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            // GetSaveData() returns a REFERENCE to internal saveData, so IsExpired() reads it directly

            // Act
            bool result = spManager.ClaimEndOfSeasonReward();

            // Assert
            Assert.IsTrue(result, "End-of-season claim should succeed when expired and premium owned");
            Assert.IsTrue(spManager.GetSaveData().seasonEndedClaimed, "Should be marked as claimed");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeasonPassManager_ClaimEndOfSeason_AlreadyClaimed_ReturnsFalse()
        {
            // Arrange
            var spManager = SeasonPassManager.Instance;
            spManager.ResetSeason();
            var data = spManager.GetSaveData();
            data.seasonStartDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            data.hasPremiumPass = true;
            data.seasonEndedClaimed = true; // already claimed

            // Act
            bool result = spManager.ClaimEndOfSeasonReward();

            // Assert
            Assert.IsFalse(result, "Should return false when already claimed");
            yield return null;
        }
    }
}
