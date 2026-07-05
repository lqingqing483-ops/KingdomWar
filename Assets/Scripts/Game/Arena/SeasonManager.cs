using System;
using UnityEngine;
using KingdomWar.HotUpdate;

namespace KingdomWar.Game.Arena
{
    public class SeasonManager : MonoBehaviour
    {
        // Singleton - same pattern as TrophyManager
        private static SeasonManager instance;

        public static SeasonManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("SeasonManager");
                    instance = obj.AddComponent<SeasonManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        public event Action OnSeasonChanged;  // fired when season resets
        public event Action<int, int> OnSeasonRewardReady;  // param: gold, gems

        private void OnDestroy()
        {
            if (instance == this)
            {
                OnSeasonChanged = null;
                OnSeasonRewardReady = null;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                CheckSeasonReset();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Get the current season start date as string (yyyy-MM-dd).
        /// </summary>
        public string GetSeasonStartDate()
        {
            return PlayerDataManager.Instance.GetSeasonStartDate();
        }

        /// <summary>
        /// Check if a new season should start, and if so, perform the reset.
        /// </summary>
        public void CheckSeasonReset()
        {
            var playerData = PlayerDataManager.Instance;
            string lastSeasonStart = playerData.GetSeasonStartDate();

            string currentSeasonStart = CalculateSeasonStartDate();

            // If no season recorded, or the season start date has changed → new season
            if (string.IsNullOrEmpty(lastSeasonStart) || lastSeasonStart != currentSeasonStart)
            {
                PerformSeasonReset();
            }
        }

        /// <summary>
        /// Calculate the season start date based on a 28-day cycle from a fixed epoch.
        /// </summary>
        private string CalculateSeasonStartDate()
        {
            DateTime epoch = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc); // Monday
            DateTime now = DateTime.UtcNow;
            TimeSpan elapsed = now - epoch;
            int daysSinceEpoch = (int)elapsed.TotalDays;
            int seasonNumber = daysSinceEpoch / ArenaConfig.SEASON_DAYS;
            DateTime seasonStart = epoch.AddDays(seasonNumber * ArenaConfig.SEASON_DAYS);
            return seasonStart.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Perform a season reset: calculate rewards based on season highest trophies,
        /// save reward data, update season start, mark reward as unclaimed.
        /// </summary>
        private void PerformSeasonReset()
        {
            var playerData = PlayerDataManager.Instance;
            int seasonHighest = playerData.GetSeasonHighest();

            // Calculate rewards based on highest arena reached this season
            ArenaDefinition arena = ArenaConfig.GetArenaByTrophies(seasonHighest);
            int rewardGold = arena.seasonRewardGold;
            int rewardGems = arena.seasonRewardGems;

            // Store reward info (gold/gems) and mark as unclaimed
            playerData.SetSeasonRewardClaimed(false);

            // Save the reward amount in PlayerPrefs for UI to show
            PlayerPrefs.SetInt("Season_RewardGold", rewardGold);
            PlayerPrefs.SetInt("Season_RewardGems", rewardGems);

            // Reset season highest to current trophies (not zero!)
            int currentTrophies = playerData.GetTrophies();
            playerData.SetSeasonHighest(currentTrophies);

            // Update season start date
            string newSeasonStart = CalculateSeasonStartDate();
            playerData.SetSeasonStartDate(newSeasonStart);

            PlayerPrefs.Save();

            // Fire event
            OnSeasonChanged?.Invoke();
            OnSeasonRewardReady?.Invoke(rewardGold, rewardGems);

            Debug.Log($"[SeasonManager] New season started! Highest trophies: {seasonHighest}, " +
                      $"Reward: {rewardGold} gold, {rewardGems} gems");
        }

        /// <summary>
        /// Claim the season reward. Called from UI when player taps the reward button.
        /// </summary>
        public bool ClaimSeasonReward()
        {
            var playerData = PlayerDataManager.Instance;
            if (playerData.IsSeasonRewardClaimed())
            {
                Debug.LogWarning("[SeasonManager] Season reward already claimed");
                return false;
            }

            int rewardGold = PlayerPrefs.GetInt("Season_RewardGold", 0);
            int rewardGems = PlayerPrefs.GetInt("Season_RewardGems", 0);

            if (rewardGold <= 0 && rewardGems <= 0)
            {
                Debug.LogWarning("[SeasonManager] No season reward to claim");
                return false;
            }

            playerData.AddGold(rewardGold);
            if (rewardGems > 0)
            {
                playerData.AddGems(rewardGems);
            }
            playerData.SetSeasonRewardClaimed(true);

            Debug.Log($"[SeasonManager] Season reward claimed: {rewardGold} gold, {rewardGems} gems");
            return true;
        }

        /// <summary>
        /// Get the unclaimed season reward info for display.
        /// Returns (gold, gems) or (0,0) if already claimed.
        /// </summary>
        public (int gold, int gems) GetUnclaimedReward()
        {
            var playerData = PlayerDataManager.Instance;
            if (playerData.IsSeasonRewardClaimed())
            {
                return (0, 0);
            }
            int gold = PlayerPrefs.GetInt("Season_RewardGold", 0);
            int gems = PlayerPrefs.GetInt("Season_RewardGems", 0);
            return (gold, gems);
        }

        /// <summary>
        /// Get days remaining in the current season.
        /// </summary>
        public int GetDaysRemainingInSeason()
        {
            string seasonStartStr = PlayerDataManager.Instance.GetSeasonStartDate();
            if (string.IsNullOrEmpty(seasonStartStr))
                return ArenaConfig.SEASON_DAYS;

            if (DateTime.TryParse(seasonStartStr, out DateTime seasonStart))
            {
                DateTime seasonEnd = seasonStart.AddDays(ArenaConfig.SEASON_DAYS);
                TimeSpan remaining = seasonEnd - DateTime.UtcNow;
                return Mathf.Max(0, (int)remaining.TotalDays);
            }
            return ArenaConfig.SEASON_DAYS;
        }

        /// <summary>
        /// Force a season reset (for testing).
        /// </summary>
        public void ForceSeasonReset()
        {
            PerformSeasonReset();
        }
    }
}
