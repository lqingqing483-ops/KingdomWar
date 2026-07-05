using UnityEngine;

namespace KingdomWar.Game.Arena
{
    // Arena league definitions (0 =无段位训练营, 1=青铜, 2=白银, 3=黄金, 4=白金, 5=钻石, 6=大师, 7=传奇)
    public enum ArenaId
    {
        TrainingCamp = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Platinum = 4,
        Diamond = 5,
        Master = 6,
        Legendary = 7
    }

    [System.Serializable]
    public class ArenaDefinition
    {
        public ArenaId arenaId;
        public string arenaName;       // e.g. "训练营"
        public string arenaNameEn;     // e.g. "Training Camp"
        public int minTrophies;        // inclusive
        public int maxTrophies;        // exclusive (int.MaxValue for Legendary)
        public int arenaLevel;         // 0-13 (对应卡牌解锁等级)
        public int seasonRewardGold;   // 赛季结算金币奖励
        public int seasonRewardGems;   // 赛季结算宝石奖励
        public string iconPath;        // Addressables path to arena icon
    }

    // 奖杯变化结果
    public readonly struct TrophyChangeResult
    {
        public readonly int trophiesGained;        // positive for win, negative for loss
        public readonly int newTrophyCount;
        public readonly ArenaId newArenaId;
        public readonly bool arenaChanged;         // true if crossed arena threshold
        public readonly bool seasonHighBroken;     // true if new personal season record

        public TrophyChangeResult(
            int trophiesGained,
            int newTrophyCount,
            ArenaId newArenaId,
            bool arenaChanged,
            bool seasonHighBroken)
        {
            this.trophiesGained = trophiesGained;
            this.newTrophyCount = newTrophyCount;
            this.newArenaId = newArenaId;
            this.arenaChanged = arenaChanged;
            this.seasonHighBroken = seasonHighBroken;
        }
    }
}
