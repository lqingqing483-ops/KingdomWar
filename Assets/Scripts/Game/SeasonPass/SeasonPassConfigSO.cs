using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.SeasonPass
{
    [CreateAssetMenu(fileName = "SeasonPassConfig", menuName = "Config/Season Pass Config")]
    public class SeasonPassConfigSO : ScriptableObject
    {
        [Header("===== Season Duration =====")]
        public int seasonDurationDays = 28;

        [Header("===== Progression =====")]
        public int maxLevel = 50;
        public int expPerLevel = 100;           // exp needed per level
        public int battleWinExp = 50;           // exp earned per battle win
        public int battleLoseExp = 10;          // exp earned per battle loss

        [Header("===== Premium Pass Cost =====")]
        public int premiumPassCostGems = 800;

        [Header("===== Free Tier Rewards (level 1~50) =====")]
        public List<SeasonPassReward> freeTierRewards;

        [Header("===== Premium Tier Rewards (level 1~50) =====")]
        public List<SeasonPassReward> premiumTierRewards;

        [Header("===== End-of-Season Reward (if premium purchased) =====")]
        public SeasonPassReward endOfSeasonReward;

        private static SeasonPassConfigSO instance;
        public static SeasonPassConfigSO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<SeasonPassConfigSO>("Config/SeasonPassConfig");
                    if (instance == null)
                    {
                        instance = CreateInstance<SeasonPassConfigSO>();
                        instance.InitializeDefaults();
                        Debug.LogWarning("[SeasonPassConfig] Not found in Resources/Config/, using defaults!");
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Build sensible default rewards when no ScriptableObject is configured.
        /// Free: every 5 levels get gold/cards, every 10 get gems
        /// Premium: every level gets something, premium at 50 gets special chest
        /// </summary>
        private void InitializeDefaults()
        {
            seasonDurationDays = 28;
            maxLevel = 50;
            expPerLevel = 100;
            battleWinExp = 50;
            battleLoseExp = 10;
            premiumPassCostGems = 800;

            freeTierRewards = new List<SeasonPassReward>();
            premiumTierRewards = new List<SeasonPassReward>();

            // Free tier rewards (one per level, roughly)
            // Gems at level 10, 20, 30, 40, 50; Gold at level 5, 15, 25, 35, 45
            for (int i = 1; i <= maxLevel; i++)
            {
                if (i % 10 == 0)
                {
                    freeTierRewards.Add(new SeasonPassReward
                    {
                        level = i,
                        rewardType = SeasonPassRewardType.Gems,
                        quantity = 10 + i / 10
                    });
                }
                else if (i % 5 == 0)
                {
                    freeTierRewards.Add(new SeasonPassReward
                    {
                        level = i,
                        rewardType = SeasonPassRewardType.Gold,
                        quantity = 100 + i * 10
                    });
                }
            }

            // Premium tier rewards (every level)
            for (int i = 1; i <= maxLevel; i++)
            {
                if (i == 50)
                {
                    premiumTierRewards.Add(new SeasonPassReward
                    {
                        level = i,
                        rewardType = SeasonPassRewardType.Chest,
                        rewardId = "LegendaryChest",
                        quantity = 1
                    });
                }
                else if (i % 5 == 0)
                {
                    premiumTierRewards.Add(new SeasonPassReward
                    {
                        level = i,
                        rewardType = SeasonPassRewardType.Card,
                        quantity = 1,
                        rewardId = "" // random card
                    });
                }
                else
                {
                    premiumTierRewards.Add(new SeasonPassReward
                    {
                        level = i,
                        rewardType = SeasonPassRewardType.Gold,
                        quantity = 200 + i * 20
                    });
                }
            }

            endOfSeasonReward = new SeasonPassReward
            {
                level = 0,
                rewardType = SeasonPassRewardType.Chest,
                rewardId = "SeasonEndChest",
                quantity = 1
            };
        }

        /// <summary>
        /// Get all rewards for a specific level and tier.
        /// Returns empty list if no rewards at that level.
        /// </summary>
        public List<SeasonPassReward> GetRewardsAtLevel(int level, SeasonPassTier tier)
        {
            List<SeasonPassReward> source = (tier == SeasonPassTier.Free) ? freeTierRewards : premiumTierRewards;
            return source.FindAll(r => r.level == level);
        }

        /// <summary>
        /// Get ALL rewards up to (and including) a level for a tier.
        /// Used for end-of-season mass claim.
        /// </summary>
        public List<SeasonPassReward> GetRewardsUpToLevel(int level, SeasonPassTier tier)
        {
            List<SeasonPassReward> source = (tier == SeasonPassTier.Free) ? freeTierRewards : premiumTierRewards;
            return source.FindAll(r => r.level <= level);
        }
    }
}
