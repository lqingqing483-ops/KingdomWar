using System;

namespace KingdomWar.Game.Quests
{
    public enum QuestType
    {
        WinBattles,         // Win X battles
        PlayBattles,        // Play X battles
        DestroyTowers,      // Destroy X towers
        EarnGold,           // Earn X gold
        EarnTrophies,       // Gain X trophies
        UpgradeCard,        // Upgrade X cards
        OpenChests,         // Open X chests
        UseCard,            // Use X card type in battle
        DealDamage,         // Deal X total damage
        SeasonPassLevel,    // Reach season pass level X
    }

    public enum QuestRarity
    {
        Daily,      // 1 day
        Weekly,     // 7 days
        Event       // special duration
    }

    public enum QuestRewardType
    {
        Gold,
        Gems,
        Card,
        Experience,
        SeasonPassExp
    }

    [Serializable]
    public class QuestReward
    {
        public QuestRewardType rewardType;
        public string rewardId;     // card name if Card type
        public int quantity;
    }

    [Serializable]
    public class QuestDefinition
    {
        public string questId;              // unique id
        public QuestType questType;
        public QuestRarity rarity;
        public string title;                // "Win 3 Battles"
        public string description;          // "Win 3 battles in the arena"
        public int targetCount;             // how many to complete (e.g. 3)
        public QuestReward reward;
        public int durationHours;           // 24 for daily, 168 for weekly
    }

    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public int currentCount;            // how many done so far
        public bool completed;
        public bool rewardClaimed;
        public string expirationTime;       // DateTime.ToString("o")

        public bool IsExpired()
        {
            if (string.IsNullOrEmpty(expirationTime)) return false;
            if (DateTime.TryParse(expirationTime, out DateTime expiry))
                return DateTime.Now > expiry;
            return false;
        }
    }

    [Serializable]
    public class QuestSaveData
    {
        public System.Collections.Generic.List<QuestProgress> activeQuests;
        public string lastDailyRefresh;
        public string lastWeeklyRefresh;

        public QuestSaveData()
        {
            activeQuests = new System.Collections.Generic.List<QuestProgress>();
            lastDailyRefresh = DateTime.Now.ToString("yyyy-MM-dd");
            lastWeeklyRefresh = DateTime.Now.ToString("yyyy-MM-dd");
        }
    }
}
