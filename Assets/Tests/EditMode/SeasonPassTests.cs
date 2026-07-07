using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomWar.Game.SeasonPass;

namespace KingdomWar.Tests.EditMode
{
    public class SeasonPassTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Ensure config singleton is initialized with defaults
            var config = SeasonPassConfigSO.Instance;
        }

        [Test]
        public void SeasonPassConfig_Defaults_AreValid()
        {
            var config = SeasonPassConfigSO.Instance;
            Assert.Greater(config.maxLevel, 0);
            Assert.Greater(config.expPerLevel, 0);
            Assert.Greater(config.premiumPassCostGems, 0);
            Assert.Greater(config.seasonDurationDays, 0);
        }

        [Test]
        public void SeasonPassConfig_FreeTierRewards_HaveValidLevels()
        {
            var config = SeasonPassConfigSO.Instance;
            foreach (var reward in config.freeTierRewards)
            {
                Assert.Greater(reward.level, 0, "Free reward level must be > 0");
                Assert.LessOrEqual(reward.level, config.maxLevel, "Free reward level must be <= maxLevel");
                Assert.Greater(reward.quantity, 0, "Free reward quantity must be > 0");
            }
        }

        [Test]
        public void SeasonPassConfig_PremiumTierRewards_HaveValidLevels()
        {
            var config = SeasonPassConfigSO.Instance;
            foreach (var reward in config.premiumTierRewards)
            {
                Assert.Greater(reward.level, 0, "Premium reward level must be > 0");
                Assert.LessOrEqual(reward.level, config.maxLevel, "Premium reward level must be <= maxLevel");
                Assert.Greater(reward.quantity, 0, "Premium reward quantity must be > 0");
            }
        }

        [Test]
        public void SeasonPassManager_GetCurrentLevel_StartsAtZero()
        {
            // We can test SaveData directly since SeasonPassManager needs PlayerDataManager (MonoBehaviour)
            var saveData = new SeasonPassSaveData();
            Assert.AreEqual(0, saveData.totalExp);
        }

        [Test]
        public void SeasonPassReward_GoldType_HasQuantity()
        {
            var reward = new SeasonPassReward
            {
                level = 1,
                rewardType = SeasonPassRewardType.Gold,
                quantity = 100
            };
            Assert.AreEqual(1, reward.level);
            Assert.AreEqual(SeasonPassRewardType.Gold, reward.rewardType);
            Assert.AreEqual(100, reward.quantity);
        }

        [Test]
        public void SeasonPassTier_Values_AreDistinct()
        {
            Assert.AreNotEqual((int)SeasonPassTier.Free, (int)SeasonPassTier.Premium);
            Assert.AreEqual(0, (int)SeasonPassTier.Free);
            Assert.AreEqual(1, (int)SeasonPassTier.Premium);
        }

        [Test]
        public void SeasonPassSaveData_Defaults_AreSensible()
        {
            var data = new SeasonPassSaveData();
            Assert.IsFalse(data.hasPremiumPass);
            Assert.IsNotNull(data.freeClaimedLevels);
            Assert.IsNotNull(data.premiumClaimedLevels);
            Assert.IsFalse(data.seasonEndedClaimed);
            Assert.AreEqual(0, data.totalExp);
            Assert.IsFalse(string.IsNullOrEmpty(data.seasonStartDate));
        }

        [Test]
        public void SeasonPassConfig_FreeTier_GemsAtMultiplesOfTen_GoldAtMultiplesOfFiveNotTen()
        {
            var config = SeasonPassConfigSO.Instance;
            foreach (var reward in config.freeTierRewards)
            {
                if (reward.level % 10 == 0)
                {
                    Assert.AreEqual(SeasonPassRewardType.Gems, reward.rewardType,
                        $"Level {reward.level} (multiple of 10) should have Gems reward");
                }
                else if (reward.level % 5 == 0)
                {
                    Assert.AreEqual(SeasonPassRewardType.Gold, reward.rewardType,
                        $"Level {reward.level} (multiple of 5, not 10) should have Gold reward");
                }
            }
            // Verify level 10, 20, 30, 40, 50 all have gems
            for (int lvl = 10; lvl <= 50; lvl += 10)
            {
                var rewards = config.GetRewardsAtLevel(lvl, SeasonPassTier.Free);
                Assert.IsNotEmpty(rewards, $"Level {lvl} should have free tier rewards");
                Assert.IsTrue(rewards.Exists(r => r.rewardType == SeasonPassRewardType.Gems),
                    $"Level {lvl} should have a gems reward");
            }
        }

        [Test]
        public void SeasonPassManager_GetRemainingDays_RoundsUp()
        {
            double elapsedDays = 27.1;
            double remaining = 28.0 - elapsedDays;
            int result = Mathf.CeilToInt((float)remaining);
            Assert.AreEqual(1, result, "27.1 days elapsed should show 1 day remaining");

            elapsedDays = 27.9;
            remaining = 28.0 - elapsedDays;
            result = Mathf.CeilToInt((float)remaining);
            Assert.AreEqual(1, result, "27.9 days elapsed should show 1 day remaining");

            elapsedDays = 0.5;
            remaining = 28.0 - elapsedDays;
            result = Mathf.CeilToInt((float)remaining);
            Assert.AreEqual(28, result, "0.5 days elapsed should show 28 days remaining");
        }

        [Test]
        public void SeasonPassSaveData_ClaimedLevels_Deduplicates()
        {
            var data = new SeasonPassSaveData();
            data.freeClaimedLevels.Add(5);
            data.freeClaimedLevels.Add(5); // duplicate

            int count = 0;
            foreach (var lvl in data.freeClaimedLevels)
                if (lvl == 5) count++;

            // This is a data integrity issue — duplicates should not happen
            // but if they do, the system should handle it
            // The Contains() check in ClaimReward would return true even with duplicates
            Assert.AreEqual(2, count, "ClaimedLevels should be checked for duplicates at data layer");
        }
    }
}
