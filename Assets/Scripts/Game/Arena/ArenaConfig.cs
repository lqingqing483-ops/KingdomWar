using System.Collections.Generic;
using UnityEngine;

namespace KingdomWar.Game.Arena
{
    public static class ArenaConfig
    {
        // Trophy thresholds
        public const int TROPHY_MIN = 0;
        public const int TROPHY_MAX = 9000;
        public const int TROPHY_START = 0;     // new players start at 0

        // Trophy change constants
        public const int TROPHY_WIN_BASE = 30;
        public const int TROPHY_LOSE_BASE = -30;
        public const int TROPHY_DRAW = 0;
        public const int TROPHY_DIFF_MAX_BONUS = 10;  // max extra when beating higher trophy opponent

        // Season config
        public const int SEASON_DAYS = 28;      // 4 weeks per season

        // Arena definitions (0-7)
        private static readonly ArenaDefinition[] ArenaTable = new ArenaDefinition[]
        {
            new ArenaDefinition { arenaId = ArenaId.TrainingCamp, arenaName = "训练营", arenaNameEn = "Training Camp", minTrophies = 0, maxTrophies = 300, arenaLevel = 0, seasonRewardGold = 0, seasonRewardGems = 0, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Bronze, arenaName = "青铜竞技场", arenaNameEn = "Bronze Arena", minTrophies = 300, maxTrophies = 600, arenaLevel = 1, seasonRewardGold = 100, seasonRewardGems = 10, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Silver, arenaName = "白银竞技场", arenaNameEn = "Silver Arena", minTrophies = 600, maxTrophies = 1000, arenaLevel = 3, seasonRewardGold = 250, seasonRewardGems = 25, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Gold, arenaName = "黄金竞技场", arenaNameEn = "Gold Arena", minTrophies = 1000, maxTrophies = 1500, arenaLevel = 5, seasonRewardGold = 500, seasonRewardGems = 50, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Platinum, arenaName = "白金竞技场", arenaNameEn = "Platinum Arena", minTrophies = 1500, maxTrophies = 2100, arenaLevel = 7, seasonRewardGold = 800, seasonRewardGems = 80, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Diamond, arenaName = "钻石竞技场", arenaNameEn = "Diamond Arena", minTrophies = 2100, maxTrophies = 2800, arenaLevel = 9, seasonRewardGold = 1200, seasonRewardGems = 120, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Master, arenaName = "大师竞技场", arenaNameEn = "Master Arena", minTrophies = 2800, maxTrophies = 3600, arenaLevel = 11, seasonRewardGold = 2000, seasonRewardGems = 200, iconPath = "" },
            new ArenaDefinition { arenaId = ArenaId.Legendary, arenaName = "传奇竞技场", arenaNameEn = "Legendary Arena", minTrophies = 3600, maxTrophies = int.MaxValue, arenaLevel = 13, seasonRewardGold = 5000, seasonRewardGems = 500, iconPath = "" }
        };

        public static ArenaDefinition GetArena(ArenaId id)
        {
            foreach (ArenaDefinition arena in ArenaTable)
            {
                if (arena.arenaId == id)
                {
                    return arena;
                }
            }

            Debug.LogWarning($"[ArenaConfig] Arena not found for id: {id}, returning default TrainingCamp");
            return ArenaTable[0];
        }

        public static ArenaDefinition GetArenaByTrophies(int trophies)
        {
            for (int i = ArenaTable.Length - 1; i >= 0; i--)
            {
                ArenaDefinition arena = ArenaTable[i];
                if (trophies >= arena.minTrophies && trophies < arena.maxTrophies)
                {
                    return arena;
                }
            }

            Debug.LogWarning($"[ArenaConfig] No arena found for trophies: {trophies}, returning default TrainingCamp");
            return ArenaTable[0];
        }

        public static ArenaId GetArenaIdByLevel(int arenaLevel)
        {
            // Find the highest arena whose arenaLevel <= given level
            ArenaId result = ArenaId.TrainingCamp;
            foreach (ArenaDefinition arena in ArenaTable)
            {
                if (arena.arenaLevel <= arenaLevel)
                {
                    result = arena.arenaId;
                }
            }

            return result;
        }

        public static IReadOnlyList<ArenaDefinition> GetAllArenas()
        {
            return System.Array.AsReadOnly(ArenaTable);
        }

        public static int GetArenaCount()
        {
            return ArenaTable.Length;
        }
    }
}
