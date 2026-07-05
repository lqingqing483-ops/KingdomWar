using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game.Cards;
using KingdomWar.HotUpdate;
using KingdomWar.Game.Config;

namespace KingdomWar.Game.Chest
{
    public class ChestManager : MonoBehaviour
    {
        private static ChestManager instance;
        public static ChestManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("ChestManager");
                    instance = obj.AddComponent<ChestManager>();
                }
                return instance;
            }
        }

        private const int MaxSlots = 4;
        private const string SlotsSaveKey = "Chest_Slots";
        private const string FreeChestCooldownKey = "Chest_FreeLastClaim";
        // (FreeChestCooldownSeconds and GemCostPerMinute now come from EconomyBalanceSO.Instance)

        private List<ChestSlot> slots = new List<ChestSlot>();

        private static ChestData FreeChestData
        {
            get
            {
                var config = EconomyBalanceSO.Instance;
                return new ChestData
                {
                    chestType = ChestType.Free,
                    chestName = "Free Chest",
                    goldReward = config.freeChestGold,
                    gemReward = config.freeChestGems,
                    cardCount = config.freeChestCardCount,
                    cardRarityWeights = config.freeChestRarityWeights,
                    unlockTimeMinutes = 0
                };
            }
        }

        private static ChestData VictoryChestData
        {
            get
            {
                var config = EconomyBalanceSO.Instance;
                return new ChestData
                {
                    chestType = ChestType.Victory,
                    chestName = "Victory Chest",
                    goldReward = config.victoryChestGold,
                    gemReward = config.victoryChestGems,
                    cardCount = config.victoryChestCardCount,
                    cardRarityWeights = config.victoryChestRarityWeights,
                    unlockTimeMinutes = config.victoryChestUnlockMinutes
                };
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSlots();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        public List<ChestSlot> GetSlots()
        {
            return slots;
        }

        public int GetFreeChestCooldownRemaining()
        {
            string lastClaimStr = PlayerPrefs.GetString(FreeChestCooldownKey, "");
            if (string.IsNullOrEmpty(lastClaimStr))
                return 0;

            DateTime lastClaim = DateTime.Parse(lastClaimStr);
            double elapsed = (DateTime.Now - lastClaim).TotalSeconds;
            int remaining = EconomyBalanceSO.Instance.freeChestCooldownSeconds - (int)elapsed;
            return Mathf.Max(0, remaining);
        }

        public bool CanClaimFreeChest()
        {
            return GetFreeChestCooldownRemaining() <= 0;
        }

        public bool GetFreeChest()
        {
            if (!CanClaimFreeChest())
            {
                Debug.LogWarning("Free chest cooldown not over yet");
                return false;
            }

            int emptyIndex = FindEmptySlot();
            if (emptyIndex < 0)
            {
                Debug.LogWarning("No empty slots for free chest");
                return false;
            }

            slots[emptyIndex].chest = FreeChestData;
            slots[emptyIndex].unlockStartTime = 0;
            slots[emptyIndex].isUnlocking = false;
            slots[emptyIndex].isUnlocked = true;

            PlayerPrefs.SetString(FreeChestCooldownKey, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            PlayerPrefs.Save();
            SaveSlots();

            return true;
        }

        public bool GetVictoryChest()
        {
            int emptyIndex = FindEmptySlot();
            if (emptyIndex < 0)
            {
                Debug.LogWarning("No empty slots for victory chest");
                return false;
            }

            slots[emptyIndex].chest = VictoryChestData;
            slots[emptyIndex].unlockStartTime = 0;
            slots[emptyIndex].isUnlocking = false;
            slots[emptyIndex].isUnlocked = false;

            SaveSlots();
            return true;
        }

        public bool StartUnlock(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return false;

            ChestSlot slot = slots[slotIndex];
            if (slot.chest == null)
            {
                Debug.LogWarning("Slot is empty, cannot start unlock");
                return false;
            }

            if (slot.isUnlocked)
            {
                Debug.LogWarning("Chest already unlocked");
                return false;
            }

            if (slot.isUnlocking)
            {
                Debug.LogWarning("Chest already unlocking");
                return false;
            }

            slot.isUnlocking = true;
            slot.isUnlocked = false;
            slot.unlockStartTime = DateTime.Now.Ticks;

            SaveSlots();
            return true;
        }

        public bool SpeedUpUnlock(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return false;

            ChestSlot slot = slots[slotIndex];
            if (slot.chest == null)
            {
                Debug.LogWarning("Slot is empty, cannot speed up");
                return false;
            }

            if (slot.isUnlocked)
            {
                Debug.LogWarning("Chest already unlocked");
                return false;
            }

            if (!slot.isUnlocking)
            {
                Debug.LogWarning("Chest not unlocking, start unlock first");
                return false;
            }

            int remainingMinutes = GetRemainingUnlockMinutes(slotIndex);
            int gemCost = remainingMinutes * EconomyBalanceSO.Instance.chestSpeedUpGemCostPerMinute;

            if (!PlayerDataManager.Instance.SpendGems(gemCost))
            {
                Debug.LogWarning("Not enough gems to speed up unlock");
                return false;
            }

            slot.isUnlocking = false;
            slot.isUnlocked = true;

            SaveSlots();
            return true;
        }

        public int GetRemainingUnlockMinutes(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return 0;

            ChestSlot slot = slots[slotIndex];
            if (!slot.isUnlocking || slot.chest == null)
                return 0;

            DateTime startTime = new DateTime(slot.unlockStartTime);
            double elapsedMinutes = (DateTime.Now - startTime).TotalMinutes;
            int remaining = slot.chest.unlockTimeMinutes - (int)elapsedMinutes;
            return Mathf.Max(0, remaining);
        }

        public bool IsUnlockComplete(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return false;

            ChestSlot slot = slots[slotIndex];
            if (!slot.isUnlocking || slot.chest == null)
                return false;

            return GetRemainingUnlockMinutes(slotIndex) <= 0;
        }

        public Dictionary<string, int> OpenChest(int slotIndex)
        {
            if (!IsValidSlot(slotIndex))
                return null;

            ChestSlot slot = slots[slotIndex];
            if (slot.chest == null)
            {
                Debug.LogWarning("Slot is empty, cannot open");
                return null;
            }

            if (!slot.isUnlocked)
            {
                Debug.LogWarning("Chest is not unlocked yet");
                return null;
            }

            ChestData chest = slot.chest;
            var playerData = PlayerDataManager.Instance;

            playerData.AddGold(chest.goldReward);

            if (chest.gemReward > 0)
            {
                playerData.AddGems(chest.gemReward);
            }

            Dictionary<string, int> rewards = new Dictionary<string, int>();
            rewards["gold"] = chest.goldReward;
            if (chest.gemReward > 0)
            {
                rewards["gems"] = chest.gemReward;
            }

            List<CardData> allCards = CardDatabase.Instance.GetAllCards();
            if (allCards != null && allCards.Count > 0 && chest.cardCount > 0)
            {
                List<CardData> weightedPool = BuildWeightedPool(allCards, chest.cardRarityWeights);

                for (int i = 0; i < chest.cardCount && weightedPool.Count > 0; i++)
                {
                    int idx = UnityEngine.Random.Range(0, weightedPool.Count);
                    CardData picked = weightedPool[idx];

                    playerData.AddCard(picked.cardName);

                    if (rewards.ContainsKey(picked.cardName))
                        rewards[picked.cardName]++;
                    else
                        rewards[picked.cardName] = 1;

                    weightedPool.RemoveAt(idx);
                }
            }

            slot.chest = null;
            slot.unlockStartTime = 0;
            slot.isUnlocking = false;
            slot.isUnlocked = false;

            SaveSlots();
            return rewards;
        }

        private int FindEmptySlot()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (slots[i].chest == null)
                    return i;
            }
            return -1;
        }

        private bool IsValidSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slots.Count;
        }

        private List<CardData> BuildWeightedPool(List<CardData> allCards, int[] rarityWeights)
        {
            List<CardData> pool = new List<CardData>();

            if (rarityWeights == null || rarityWeights.Length == 0)
            {
                return new List<CardData>(allCards);
            }

            foreach (CardData card in allCards)
            {
                int weight = card.rarity >= 0 && card.rarity < rarityWeights.Length
                    ? rarityWeights[card.rarity]
                    : 1;

                for (int w = 0; w < weight; w++)
                {
                    pool.Add(card);
                }
            }

            if (pool.Count == 0)
            {
                pool.AddRange(allCards);
            }

            return pool;
        }

        private void SaveSlots()
        {
            ChestSaveData saveData = new ChestSaveData
            {
                slots = slots
            };
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SlotsSaveKey, json);
            PlayerPrefs.Save();
        }

        private void LoadSlots()
        {
            slots.Clear();
            for (int i = 0; i < MaxSlots; i++)
            {
                slots.Add(new ChestSlot());
            }

            string json = PlayerPrefs.GetString(SlotsSaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                ChestSaveData saveData = JsonUtility.FromJson<ChestSaveData>(json);
                if (saveData != null && saveData.slots != null && saveData.slots.Count == MaxSlots)
                {
                    slots = saveData.slots;
                }
            }
        }
    }

    [Serializable]
    public class ChestSaveData
    {
        public List<ChestSlot> slots;
    }
}
