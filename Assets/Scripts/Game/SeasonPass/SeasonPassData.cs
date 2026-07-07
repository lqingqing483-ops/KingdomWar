using System;
using System.Collections.Generic;

namespace KingdomWar.Game.SeasonPass
{
    public enum SeasonPassRewardType
    {
        Gold,
        Gems,
        Card,
        CardFragments,
        Chest,
        Emote,
        Experience
    }

    public enum SeasonPassTier
    {
        Free,
        Premium
    }

    [Serializable]
    public class SeasonPassReward
    {
        public int level;                    // at what level this reward is unlocked
        public SeasonPassRewardType rewardType;
        public string rewardId;              // card name, chest type, etc. (empty if gold/gems)
        public int quantity;                 // amount of gold/gems/cards/fragments
    }

    [Serializable]
    public class SeasonPassSaveData
    {
        public int totalExp;                 // accumulated exp this season
        public bool hasPremiumPass;          // premium track unlocked
        public List<int> freeClaimedLevels;  // which free-tier levels have been claimed
        public List<int> premiumClaimedLevels; // which premium-tier levels have been claimed
        public string seasonStartDate;       // "yyyy-MM-dd" format
        public bool seasonEndedClaimed;      // has end-of-season rewards been claimed

        public SeasonPassSaveData()
        {
            freeClaimedLevels = new List<int>();
            premiumClaimedLevels = new List<int>();
            seasonStartDate = DateTime.Now.ToString("yyyy-MM-dd");
            seasonEndedClaimed = false;
        }
    }

    // Wrapper for PlayerPrefs JSON serialization
    [Serializable]
    public class SeasonPassSaveWrapper
    {
        public SeasonPassSaveData data;
    }
}
