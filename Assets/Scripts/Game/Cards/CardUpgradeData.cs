using UnityEngine;

namespace KingdomWar.Game.Cards
{
    [System.Serializable]
    public class UpgradeLevel
    {
        public int level;
        public int fragmentsRequired;
        public int goldRequired;
        public float statMultiplier;
    }

    public static class CardUpgradeTable
    {
        private static UpgradeLevel[] levels;

        static CardUpgradeTable()
        {
            levels = new UpgradeLevel[13];
            levels[0] = new UpgradeLevel { level = 1, fragmentsRequired = 0, goldRequired = 0, statMultiplier = 1.0f };
            levels[1] = new UpgradeLevel { level = 2, fragmentsRequired = 2, goldRequired = 100, statMultiplier = 1.1f };
            levels[2] = new UpgradeLevel { level = 3, fragmentsRequired = 4, goldRequired = 200, statMultiplier = 1.21f };
            levels[3] = new UpgradeLevel { level = 4, fragmentsRequired = 10, goldRequired = 400, statMultiplier = 1.331f };
            levels[4] = new UpgradeLevel { level = 5, fragmentsRequired = 20, goldRequired = 800, statMultiplier = 1.464f };
            levels[5] = new UpgradeLevel { level = 6, fragmentsRequired = 50, goldRequired = 1600, statMultiplier = 1.611f };
            levels[6] = new UpgradeLevel { level = 7, fragmentsRequired = 100, goldRequired = 3200, statMultiplier = 1.772f };
            levels[7] = new UpgradeLevel { level = 8, fragmentsRequired = 200, goldRequired = 6400, statMultiplier = 1.949f };
            levels[8] = new UpgradeLevel { level = 9, fragmentsRequired = 400, goldRequired = 12800, statMultiplier = 2.144f };
            levels[9] = new UpgradeLevel { level = 10, fragmentsRequired = 800, goldRequired = 25600, statMultiplier = 2.358f };
            levels[10] = new UpgradeLevel { level = 11, fragmentsRequired = 1000, goldRequired = 51200, statMultiplier = 2.594f };
            levels[11] = new UpgradeLevel { level = 12, fragmentsRequired = 2000, goldRequired = 102400, statMultiplier = 2.853f };
            levels[12] = new UpgradeLevel { level = 13, fragmentsRequired = 4000, goldRequired = 204800, statMultiplier = 3.138f };
        }

        public static UpgradeLevel GetLevel(int level)
        {
            if (level < 1 || level > 13)
            {
                Debug.LogError($"Invalid card level: {level}. Must be between 1 and 13.");
                return null;
            }
            return levels[level - 1];
        }

        public static int GetUpgradeCost(int currentLevel)
        {
            var levelData = GetLevel(currentLevel);
            return levelData != null ? levelData.goldRequired : 0;
        }

        public static int GetFragmentsRequired(int currentLevel)
        {
            var levelData = GetLevel(currentLevel);
            return levelData != null ? levelData.fragmentsRequired : 0;
        }

        public static float GetStatMultiplier(int level)
        {
            var levelData = GetLevel(level);
            return levelData != null ? levelData.statMultiplier : 1.0f;
        }

        public static int GetMaxLevel()
        {
            return 13;
        }
    }

    public static class CardUpgradeExtensions
    {
        public static int GetHealthAtLevel(this UnitData data, int level)
        {
            return Mathf.RoundToInt(data.health * CardUpgradeTable.GetStatMultiplier(level));
        }

        public static int GetDamageAtLevel(this UnitData data, int level)
        {
            return Mathf.RoundToInt(data.damage * CardUpgradeTable.GetStatMultiplier(level));
        }

        public static int GetHealthAtLevel(this BuildingData data, int level)
        {
            return Mathf.RoundToInt(data.health * CardUpgradeTable.GetStatMultiplier(level));
        }

        public static int GetDamageAtLevel(this BuildingData data, int level)
        {
            return Mathf.RoundToInt(data.damage * CardUpgradeTable.GetStatMultiplier(level));
        }

        public static int GetDamageAtLevel(this SpellData data, int level)
        {
            return Mathf.RoundToInt(data.damage * CardUpgradeTable.GetStatMultiplier(level));
        }
    }
}
