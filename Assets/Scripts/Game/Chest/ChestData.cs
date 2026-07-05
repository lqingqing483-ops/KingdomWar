using System;

namespace KingdomWar.Game.Chest
{
    public enum ChestType { Free, Victory, Crown, Special, Legendary }

    [Serializable]
    public class ChestData
    {
        public ChestType chestType;
        public string chestName;
        public int goldReward;
        public int gemReward;
        public int cardCount;
        public int[] cardRarityWeights;
        public int unlockTimeMinutes;
    }

    [Serializable]
    public class ChestSlot
    {
        public ChestData chest;
        public long unlockStartTime;
        public bool isUnlocking;
        public bool isUnlocked;
    }
}
