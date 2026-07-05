using System;
using UnityEngine;
using KingdomWar.Game.Arena;
using KingdomWar.UI;
using Photon.Pun;

namespace KingdomWar.Server
{
    /// <summary>
    /// Matchmaking service with trophy-based matching, timeout, and range expansion.
    /// Wraps NetworkManager's Photon matchmaking with trophy-aware logic.
    /// </summary>
    public class MatchmakingService
    {
        // ===== Configuration =====
        private const int INITIAL_TROPHY_RANGE = 100;    // start with +/-100
        private const int RANGE_EXPANSION_PER_STEP = 100; // expand by 100 each step
        private const float EXPANSION_INTERVAL = 10f;     // expand every 10 seconds
        private const float MAX_WAIT_TIME = 60f;          // max 60 seconds before offering bot
        private const float SEARCH_RETRY_INTERVAL = 3f;   // retry JoinRandomRoom every 3s

        // ===== State =====
        public enum MatchmakingState { Idle, Searching, MatchFound, TimedOut, Cancelled }
        public MatchmakingState State { get; private set; }
        public int PlayerTrophies { get; private set; }
        public int CurrentTrophyRange { get; private set; }
        public float ElapsedTime { get; private set; }

        // ===== Events =====
        public event Action OnSearchStarted;
        public event Action<int, int> OnRangeUpdated;        // currentRange, elapsedSeconds
        public event Action OnMatchFound;                     // match found!
        public event Action OnTimedOut;                       // max wait reached, offer bot
        public event Action OnCancelled;
        public event Action<string> OnError;                  // error message

        private float rangeExpansionTimer;
        private float retryTimer;
        private bool waitingForRoom;

        public MatchmakingService()
        {
            State = MatchmakingState.Idle;
        }

        /// <summary>
        /// Start matchmaking with the player's current trophy count.
        /// </summary>
        public void StartSearch()
        {
            if (State == MatchmakingState.Searching)
            {
                Debug.LogWarning("[Matchmaking] Already searching");
                return;
            }

            PlayerTrophies = TrophyManager.Instance.GetPlayerTrophies();
            CurrentTrophyRange = INITIAL_TROPHY_RANGE;
            ElapsedTime = 0f;
            rangeExpansionTimer = 0f;
            retryTimer = 0f;
            waitingForRoom = false;
            State = MatchmakingState.Searching;

            Debug.Log($"[Matchmaking] Start search. Trophies: {PlayerTrophies}, Range: +/-{CurrentTrophyRange}");
            OnSearchStarted?.Invoke();

            StartPhotonMatchmaking();
        }

        /// <summary>
        /// Cancel current matchmaking search.
        /// </summary>
        public void CancelSearch()
        {
            if (State != MatchmakingState.Searching)
                return;

            State = MatchmakingState.Cancelled;
            Debug.Log("[Matchmaking] Search cancelled");

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.LeaveRoom();
            }

            OnCancelled?.Invoke();
        }

        /// <summary>
        /// Called every frame to update matchmaking state.
        /// </summary>
        public void UpdateMatchmaking(float deltaTime)
        {
            if (State != MatchmakingState.Searching)
                return;

            ElapsedTime += deltaTime;

            // Check for timeout (offer bot match)
            if (ElapsedTime >= MAX_WAIT_TIME)
            {
                State = MatchmakingState.TimedOut;
                Debug.Log("[Matchmaking] Timed out after 60s");
                OnTimedOut?.Invoke();
                return;
            }

            // Trophy range expansion
            rangeExpansionTimer += deltaTime;
            if (rangeExpansionTimer >= EXPANSION_INTERVAL)
            {
                rangeExpansionTimer = 0f;
                CurrentTrophyRange += RANGE_EXPANSION_PER_STEP;
                Debug.Log($"[Matchmaking] Range expanded to +/-{CurrentTrophyRange}");
                OnRangeUpdated?.Invoke(CurrentTrophyRange, (int)ElapsedTime);
            }

            // Retry JoinRandomRoom if not already waiting for a room
            if (!waitingForRoom)
            {
                retryTimer += deltaTime;
                if (retryTimer >= SEARCH_RETRY_INTERVAL)
                {
                    retryTimer = 0f;
                    StartPhotonMatchmaking();
                }
            }

            // Check if we've joined a room (Photon callback handled in NetworkManager)
            if (PhotonNetwork.InRoom)
            {
                int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
                int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

                if (playerCount >= maxPlayers)
                {
                    State = MatchmakingState.MatchFound;
                    Debug.Log("[Matchmaking] Match found! Room is full.");
                    OnMatchFound?.Invoke();
                }
            }
        }

        /// <summary>
        /// Start Photon matchmaking via NetworkManager.
        /// </summary>
        private void StartPhotonMatchmaking()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogError("[Matchmaking] NetworkManager not available");
                OnError?.Invoke("Network not available");
                return;
            }

            waitingForRoom = true;
            NetworkManager.Instance.StartMatching();
        }

        /// <summary>
        /// Called by NetworkManager when join random room failed.
        /// </summary>
        public void OnJoinRandomRoomFailed()
        {
            waitingForRoom = false;
            // Will retry in UpdateMatchmaking after SEARCH_RETRY_INTERVAL
        }

        /// <summary>
        /// Called by NetworkManager when room was created (host waits for opponent).
        /// </summary>
        public void OnRoomCreated()
        {
            waitingForRoom = false;
            // Now waiting for another player to join
            Debug.Log("[Matchmaking] Room created, waiting for opponent...");
        }

        /// <summary>
        /// Called by NetworkManager when another player joined the room.
        /// </summary>
        public void OnPlayerJoined()
        {
            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                State = MatchmakingState.MatchFound;
                Debug.Log("[Matchmaking] Match found!");
                OnMatchFound?.Invoke();
            }
        }

        /// <summary>
        /// Reset the service to idle state.
        /// </summary>
        public void Reset()
        {
            State = MatchmakingState.Idle;
            ElapsedTime = 0f;
            CurrentTrophyRange = 0;
            waitingForRoom = false;
        }

        /// <summary>
        /// Get a formatted status string for the UI.
        /// </summary>
        public string GetStatusText()
        {
            switch (State)
            {
                case MatchmakingState.Searching:
                    int seconds = (int)ElapsedTime;
                    int minTrophy = Mathf.Max(0, PlayerTrophies - CurrentTrophyRange);
                    int maxTrophy = PlayerTrophies + CurrentTrophyRange;
                    return $"Searching... ({seconds}s)\nTrophy range: {minTrophy}-{maxTrophy}";
                case MatchmakingState.MatchFound:
                    return "Match found!";
                case MatchmakingState.TimedOut:
                    return "No opponent found.\nTry vs AI?";
                case MatchmakingState.Cancelled:
                    return "Search cancelled";
                default:
                    return "Ready";
            }
        }
    }
}
