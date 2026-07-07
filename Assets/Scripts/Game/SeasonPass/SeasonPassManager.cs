using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.HotUpdate;

namespace KingdomWar.Game.SeasonPass
{
    public class SeasonPassManager : MonoBehaviour
    {
        private static SeasonPassManager instance;
        public static SeasonPassManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("SeasonPassManager");
                    instance = obj.AddComponent<SeasonPassManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        private const string SaveKey = "SeasonPassData";

        private SeasonPassSaveData saveData;
        private SeasonPassConfigSO config;

        /// <summary>Total exp changed. Args: (currentLevel, totalExp).</summary>
        public event Action<int, int> OnExpChanged;

        /// <summary>Reward claimed. Args: (level, tier).</summary>
        public event Action<int, SeasonPassTier> OnRewardClaimed;

        /// <summary>Premium pass purchased.</summary>
        public event Action OnPremiumPurchased;

        /// <summary>Season was reset to fresh state.</summary>
        public event Action OnSeasonReset;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                config = SeasonPassConfigSO.Instance;
                LoadData();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        // ── Save / Load ────────────────────────────────────────────────

        private void LoadData()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                SeasonPassSaveWrapper wrapper = JsonUtility.FromJson<SeasonPassSaveWrapper>(json);
                if (wrapper?.data != null)
                {
                    saveData = wrapper.data;
                    return;
                }
            }

            saveData = new SeasonPassSaveData();
            SaveData();
        }

        private void SaveData()
        {
            SeasonPassSaveWrapper wrapper = new SeasonPassSaveWrapper { data = saveData };
            string json = JsonUtility.ToJson(wrapper);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        // ── Exp / Level ────────────────────────────────────────────────

        /// <summary>Add season-pass experience (called from battle results).</summary>
        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            saveData.totalExp += amount;
            int level = GetCurrentLevel();
            SaveData();
            OnExpChanged?.Invoke(level, saveData.totalExp);
        }

        /// <summary>Current pass level = totalExp / expPerLevel, capped at maxLevel.</summary>
        public int GetCurrentLevel()
        {
            int level = saveData.totalExp / config.expPerLevel;
            return Mathf.Min(level, config.maxLevel);
        }

        /// <summary>Exp progress within the current level (0 … expPerLevel-1).</summary>
        public int GetCurrentLevelExp()
        {
            return saveData.totalExp % config.expPerLevel;
        }

        /// <summary>Exp required to reach the next level (always expPerLevel).</summary>
        public int GetExpForNextLevel()
        {
            return config.expPerLevel;
        }

        /// <summary>Progress ratio (0f … 1f) for the current level bar.</summary>
        public float GetLevelProgress()
        {
            int current = GetCurrentLevelExp();
            int needed = GetExpForNextLevel();
            return needed > 0 ? (float)current / needed : 1f;
        }

        // ── Claiming ───────────────────────────────────────────────────

        /// <summary>
        /// Claim reward at a given level and tier.
        /// Returns false if not eligible (already claimed, level locked, expired, or premium missing).
        /// </summary>
        public bool ClaimReward(int level, SeasonPassTier tier)
        {
            if (level <= 0) return false;
            if (level > config.maxLevel) return false;
            if (level > GetCurrentLevel()) return false;
            if (IsExpired()) return false;

            List<int> claimedLevels = ClaimedLevelsForTier(tier);
            if (claimedLevels.Contains(level)) return false;

            if (tier == SeasonPassTier.Premium && !saveData.hasPremiumPass) return false;

            List<SeasonPassReward> rewards = config.GetRewardsAtLevel(level, tier);
            if (rewards == null || rewards.Count == 0) return false;

            PlayerDataManager pData = PlayerDataManager.Instance;
            foreach (SeasonPassReward reward in rewards)
            {
                GrantReward(reward, pData);
            }

            claimedLevels.Add(level);
            SaveData();
            OnRewardClaimed?.Invoke(level, tier);
            return true;
        }

        /// <summary>Whether a specific tier-level reward has already been claimed.</summary>
        public bool IsRewardClaimed(int level, SeasonPassTier tier)
        {
            return ClaimedLevelsForTier(tier).Contains(level);
        }

        // ── Premium Pass ───────────────────────────────────────────────

        /// <summary>Purchase premium pass. Spends gems and sets hasPremiumPass flag.</summary>
        public bool PurchasePremiumPass()
        {
            if (saveData.hasPremiumPass) return false;
            if (IsExpired()) return false;

            if (!PlayerDataManager.Instance.SpendGems(config.premiumPassCostGems))
                return false;

            saveData.hasPremiumPass = true;
            SaveData();
            OnPremiumPurchased?.Invoke();
            return true;
        }

        /// <summary>Whether the player owns the premium pass this season.</summary>
        public bool HasPremiumPass()
        {
            return saveData.hasPremiumPass;
        }

        // ── Season Time ────────────────────────────────────────────────

        /// <summary>Whether the current season has expired (>= seasonDurationDays since start).</summary>
        public bool IsExpired()
        {
            TimeSpan elapsed = DateTime.Now - GetStartDateTime();
            return elapsed.TotalDays >= config.seasonDurationDays;
        }

        /// <summary>Days remaining before season expiry (0 if expired), rounded up.</summary>
        public int GetRemainingDays()
        {
            if (IsExpired()) return 0;
            TimeSpan elapsed = DateTime.Now - GetStartDateTime();
            double remainingDays = config.seasonDurationDays - elapsed.TotalDays;
            return Mathf.Max(0, Mathf.CeilToInt((float)remainingDays));
        }

        /// <summary>Remaining hours (approximate, 0 if expired).</summary>
        public int GetRemainingHours()
        {
            if (IsExpired()) return 0;
            TimeSpan elapsed = DateTime.Now - GetStartDateTime();
            double remainingHours = (config.seasonDurationDays * 24.0) - elapsed.TotalHours;
            return Mathf.Max(0, (int)remainingHours);
        }

        /// <summary>
        /// Claim the end-of-season reward (only if premium pass was purchased).
        /// </summary>
        public bool ClaimEndOfSeasonReward()
        {
            if (!IsExpired())
            {
                Debug.LogWarning("[SeasonPassManager] Season not yet expired, cannot claim end-of-season reward");
                return false;
            }
            if (saveData.seasonEndedClaimed)
            {
                Debug.LogWarning("[SeasonPassManager] End-of-season reward already claimed");
                return false;
            }
            if (!saveData.hasPremiumPass)
            {
                Debug.LogWarning("[SeasonPassManager] Premium pass required for end-of-season reward");
                return false;
            }

            var reward = config.endOfSeasonReward;
            var pData = PlayerDataManager.Instance;
            GrantReward(reward, pData);
            saveData.seasonEndedClaimed = true;
            SaveData();
            OnRewardClaimed?.Invoke(0, SeasonPassTier.Premium); // level 0 = end-of-season
            return true;
        }

        // ── End-of-Season Mass Claim ───────────────────────────────────

        /// <summary>All free-tier levels that have rewards and are not yet claimed, up to current level.</summary>
        public List<int> GetUnclaimedFreeLevels()
        {
            return GetUnclaimedLevels(SeasonPassTier.Free);
        }

        /// <summary>All premium-tier levels that have rewards and are not yet claimed, up to current level.</summary>
        public List<int> GetUnclaimedPremiumLevels()
        {
            return GetUnclaimedLevels(SeasonPassTier.Premium);
        }

        // ── Reset ──────────────────────────────────────────────────────

        /// <summary>Wipe all season pass progress and start a fresh season.</summary>
        public void ResetSeason()
        {
            saveData = new SeasonPassSaveData();
            SaveData();
            OnSeasonReset?.Invoke();
        }

        // ── Raw save data (for debug / migration) ──────────────────────

        public SeasonPassSaveData GetSaveData()
        {
            return saveData;
        }

        // ── Internal helpers ───────────────────────────────────────────

        private List<int> ClaimedLevelsForTier(SeasonPassTier tier)
        {
            return tier == SeasonPassTier.Free
                ? saveData.freeClaimedLevels
                : saveData.premiumClaimedLevels;
        }

        private void GrantReward(SeasonPassReward reward, PlayerDataManager pData)
        {
            switch (reward.rewardType)
            {
                case SeasonPassRewardType.Gold:
                    pData.AddGold(reward.quantity);
                    break;

                case SeasonPassRewardType.Gems:
                    pData.AddGems(reward.quantity);
                    break;

                case SeasonPassRewardType.Card:
                    string cardId = reward.rewardId;
                    if (string.IsNullOrEmpty(cardId))
                    {
                        // Pick a random card from player's collection
                        var owned = pData.GetOwnedCards();
                        if (owned.Count > 0)
                            cardId = owned[UnityEngine.Random.Range(0, owned.Count)];
                        else
                        {
                            Debug.LogWarning("[SeasonPassManager] No owned cards to grant, skipping Card reward");
                            break;
                        }
                    }
                    pData.AddCard(cardId);
                    break;

                case SeasonPassRewardType.CardFragments:
                    if (!string.IsNullOrEmpty(reward.rewardId))
                        pData.AddCardFragments(reward.rewardId, reward.quantity);
                    break;

                case SeasonPassRewardType.Experience:
                    pData.AddExperience(reward.quantity);
                    break;

                // ponytail: Chest/Emote integration deferred until chest & emote systems exist
                case SeasonPassRewardType.Chest:
                case SeasonPassRewardType.Emote:
                default:
                    Debug.Log($"[SeasonPassManager] Reward type {reward.rewardType} not implemented, skipped.");
                    break;
            }
        }

        private DateTime GetStartDateTime()
        {
            if (DateTime.TryParseExact(saveData.seasonStartDate, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime parsed))
            {
                return parsed;
            }
            return DateTime.Now;
        }

        private List<int> GetUnclaimedLevels(SeasonPassTier tier)
        {
            // Premium unclaimed requires premium pass
            if (tier == SeasonPassTier.Premium && !saveData.hasPremiumPass)
                return new List<int>();

            List<int> result = new List<int>();
            int currentLevel = GetCurrentLevel();
            List<int> claimed = ClaimedLevelsForTier(tier);

            for (int lvl = 1; lvl <= currentLevel; lvl++)
            {
                if (claimed.Contains(lvl)) continue;

                List<SeasonPassReward> rewards = config.GetRewardsAtLevel(lvl, tier);
                if (rewards != null && rewards.Count > 0)
                {
                    result.Add(lvl);
                }
            }
            return result;
        }
    }
}
