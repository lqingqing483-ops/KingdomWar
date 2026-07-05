using UnityEngine;
using KingdomWar.HotUpdate;

namespace KingdomWar.Game.Arena
{
    public class TrophyManager : MonoBehaviour
    {
        private static TrophyManager instance;

        public static TrophyManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("TrophyManager");
                    instance = obj.AddComponent<TrophyManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Calculate trophy change after a battle.
        /// Formula: baseWin/Lose + bonus based on opponent trophy difference
        /// </summary>
        /// <param name="isVictory">true if player won</param>
        /// <param name="isDraw">true if draw</param>
        /// <param name="playerTrophies">player's current trophies</param>
        /// <param name="opponentTrophies">opponent's current trophies</param>
        /// <returns>net trophy change (positive=gain, negative=loss)</returns>
        public int CalculateTrophyChange(bool isVictory, bool isDraw, int playerTrophies, int opponentTrophies)
        {
            if (isDraw)
                return ArenaConfig.TROPHY_DRAW;

            int trophyDiff = opponentTrophies - playerTrophies;

            if (isVictory)
            {
                // Base win + bonus for beating higher-trophy opponents
                int bonus = Mathf.Clamp(trophyDiff / 30 * 2, 0, ArenaConfig.TROPHY_DIFF_MAX_BONUS);
                int result = ArenaConfig.TROPHY_WIN_BASE + bonus;
                // Penalty for beating much lower-trophy opponents (minimum win)
                if (trophyDiff < -300)
                    result = Mathf.Max(result - 10, 15);  // minimum 15 trophies for a win
                return result;
            }
            else
            {
                // Base loss - penalty reduction when losing to higher-trophy opponents
                int reduction = Mathf.Clamp(trophyDiff / 30 * 2, 0, ArenaConfig.TROPHY_DIFF_MAX_BONUS);
                int result = ArenaConfig.TROPHY_LOSE_BASE + reduction;
                // Extra penalty when losing to much lower-trophy opponents
                if (trophyDiff < -300)
                    result = Mathf.Max(result - 10, -40);  // max -40 for bad loss
                return Mathf.Min(result, 0);  // never gain trophies on loss
            }
        }

        /// <summary>
        /// Apply trophy change result to player data and return result struct.
        /// Handles trophy gates (cannot drop below arena threshold).
        /// </summary>
        public TrophyChangeResult ApplyTrophyChange(bool isVictory, bool isDraw, int opponentTrophies)
        {
            var playerData = PlayerDataManager.Instance;
            int currentTrophies = playerData.GetTrophies();
            ArenaDefinition currentArena = ArenaConfig.GetArenaByTrophies(currentTrophies);

            int trophyChange = CalculateTrophyChange(isVictory, isDraw, currentTrophies, opponentTrophies);

            // Trophy gate: cannot drop below the minimum trophies of current arena
            // unless already at lowest arena
            int newTrophies = currentTrophies + trophyChange;
            if (currentArena.arenaId != ArenaId.TrainingCamp && newTrophies < currentArena.minTrophies)
            {
                newTrophies = currentArena.minTrophies;
                trophyChange = newTrophies - currentTrophies;  // actual change may be less
            }

            // Clamp to valid range
            newTrophies = Mathf.Clamp(newTrophies, ArenaConfig.TROPHY_MIN, ArenaConfig.TROPHY_MAX);

            // Batch update player data (avoid redundant SaveData per setter)
            playerData.SetTrophies(newTrophies, false);

            // Update highest trophies
            if (newTrophies > playerData.GetHighestTrophies())
            {
                playerData.SetHighestTrophies(newTrophies, false);
            }

            // Update season highest
            bool seasonHighBroken = false;
            if (newTrophies > playerData.GetSeasonHighest())
            {
                playerData.SetSeasonHighest(newTrophies, false);
                seasonHighBroken = true;
            }

            // Check arena change
            ArenaDefinition newArena = ArenaConfig.GetArenaByTrophies(newTrophies);
            bool arenaChanged = newArena.arenaId != currentArena.arenaId;

            // Update arena in player data
            playerData.SetCurrentArena((int)newArena.arenaId, false);

            // Single save at end of batch
            playerData.SaveData();

            return new TrophyChangeResult(
                trophyChange,
                newTrophies,
                newArena.arenaId,
                arenaChanged,
                seasonHighBroken
            );
        }

        /// <summary>
        /// Get the current arena definition for the player.
        /// </summary>
        public ArenaDefinition GetPlayerArena()
        {
            int trophies = PlayerDataManager.Instance.GetTrophies();
            return ArenaConfig.GetArenaByTrophies(trophies);
        }

        /// <summary>
        /// Get the player's current trophy count.
        /// </summary>
        public int GetPlayerTrophies()
        {
            return PlayerDataManager.Instance.GetTrophies();
        }

        /// <summary>
        /// Get the player's highest-ever trophy count.
        /// </summary>
        public int GetPlayerHighestTrophies()
        {
            return PlayerDataManager.Instance.GetHighestTrophies();
        }

        /// <summary>
        /// Get the player's season highest trophy count.
        /// </summary>
        public int GetPlayerSeasonHighest()
        {
            return PlayerDataManager.Instance.GetSeasonHighest();
        }
    }
}
