using System;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomWar.Game.Analytics
{
    /// <summary>
    /// Lightweight analytics service. Tracks local events (battle results, card plays, etc.)
    /// Stores events in memory and can be extended to flush to a remote server.
    /// </summary>
    public static class AnalyticsService
    {
        private static List<AnalyticsEvent> _events = new List<AnalyticsEvent>();
        private static bool _enabled = true;

        public static void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        /// <summary>
        /// Track a game event.
        /// </summary>
        public static void TrackEvent(string category, string action, string label = "", int value = 0)
        {
            if (!_enabled) return;

            var evt = new AnalyticsEvent
            {
                category = category,
                action = action,
                label = label,
                value = value,
                timestamp = DateTime.UtcNow
            };
            _events.Add(evt);

            // Log to console for debugging
            Debug.Log($"[Analytics] {category}.{action} | {label} | {value}");
        }

        /// <summary>
        /// Track battle result.
        /// </summary>
        public static void TrackBattleResult(bool isVictory, bool isDraw, int trophiesChanged, int totalTrophies)
        {
            TrackEvent("battle", isDraw ? "draw" : (isVictory ? "win" : "loss"),
                       $"trophies:{totalTrophies}", trophiesChanged);
        }

        /// <summary>
        /// Track card played in battle.
        /// </summary>
        public static void TrackCardPlayed(string cardName, int elixirCost)
        {
            TrackEvent("battle", "card_played", cardName, elixirCost);
        }

        /// <summary>
        /// Flush all pending events (currently just clears the buffer).
        /// Future: send to remote server via HTTP.
        /// </summary>
        public static void Flush()
        {
            if (_events.Count == 0) return;

            // TODO: POST _events to analytics server endpoint
            // For now, just log the count and clear
            Debug.Log($"[Analytics] Flushing {_events.Count} events...");

            _events.Clear();
        }

        public static int GetPendingEventCount() => _events.Count;
        public static void Clear() => _events.Clear();

        private class AnalyticsEvent
        {
            public string category;
            public string action;
            public string label;
            public int value;
            public DateTime timestamp;
        }
    }
}
