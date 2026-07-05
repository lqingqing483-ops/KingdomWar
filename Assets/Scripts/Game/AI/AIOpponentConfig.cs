using System.Collections.Generic;
using UnityEngine;

namespace KingdomWar.Game.AI
{
    public static class AIOpponentConfig
    {
        // ===== Pre-built AI Decks =====
        // Each deck has 8 cards that exist in CardDatabase

        public static readonly List<AIDeckData> Decks = new List<AIDeckData>
        {
            new AIDeckData
            {
                deckName = "Starter Push",
                minDifficulty = AIDifficulty.Easy,
                cardNames = new List<string> { "Knight", "Archers", "Goblins", "Fireball", "Arrows", "Skeleton Army", "Baby Dragon", "Cannon" }
            },
            new AIDeckData
            {
                deckName = "Giant Beatdown",
                minDifficulty = AIDifficulty.Medium,
                cardNames = new List<string> { "Giant", "Musketeer", "Mini P.E.K.K.A", "Fireball", "Zap", "Skeleton Army", "Baby Dragon", "Tombstone" }
            },
            new AIDeckData
            {
                deckName = "Control Cycle",
                minDifficulty = AIDifficulty.Hard,
                cardNames = new List<string> { "Hog Rider", "Ice Spirit", "Fire Spirit", "Zap", "Log", "Inferno Tower", "Ice Golem", "Musketeer" }
            },
            new AIDeckData
            {
                deckName = "Siege Master",
                minDifficulty = AIDifficulty.Expert,
                cardNames = new List<string> { "X-Bow", "Ice Golem", "Archers", "Fireball", "Log", "Tornado", "Ice Wizard", "Tesla" }
            }
        };

        // ===== Decision Timing (seconds) =====
        public static float GetDecisionInterval(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:    return 3.0f;  // slow
                case AIDifficulty.Medium:  return 2.0f;  // normal
                case AIDifficulty.Hard:    return 1.5f;  // fast
                case AIDifficulty.Expert:  return 1.0f;  // optimal
                default:                   return 2.0f;
            }
        }

        // ===== Elixir Management =====
        public static float GetMinElixirToAttack(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:    return 8f;   // waits for near-full elixir
                case AIDifficulty.Medium:  return 6f;   // moderate
                case AIDifficulty.Hard:    return 5f;   // efficient
                case AIDifficulty.Expert:  return 4f;   // capitalizes on openings
                default:                   return 6f;
            }
        }

        // ===== Placement Accuracy =====
        // How much random offset is added to placements (0 = perfect, higher = worse)
        public static float GetPlacementRandomRadius(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:    return 3f;   // very inaccurate
                case AIDifficulty.Medium:  return 1.5f; // somewhat inaccurate
                case AIDifficulty.Hard:    return 0.5f; // accurate
                case AIDifficulty.Expert:  return 0f;   // perfect
                default:                   return 1.5f;
            }
        }

        // ===== Reaction Delay When Player Plays Card =====
        public static float GetReactionDelay(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:    return 2.5f;  // slow reaction
                case AIDifficulty.Medium:  return 1.5f;  // normal reaction
                case AIDifficulty.Hard:    return 0.8f;  // fast reaction
                case AIDifficulty.Expert:  return 0.3f;  // almost instant
                default:                   return 1.5f;
            }
        }

        // ===== Get Deck for Difficulty =====
        public static AIDeckData GetDeckForDifficulty(AIDifficulty difficulty)
        {
            // Get all decks valid for this difficulty
            List<AIDeckData> valid = new List<AIDeckData>();
            foreach (var deck in Decks)
            {
                if ((int)deck.minDifficulty <= (int)difficulty)
                {
                    valid.Add(deck);
                }
            }

            // Pick random from valid decks
            if (valid.Count > 0)
            {
                int idx = Random.Range(0, valid.Count);
                return valid[idx];
            }

            return Decks[0]; // fallback
        }

        // ===== Hand Size =====
        public const int HAND_SIZE = 4;
        public const int DECK_SIZE = 8;
    }
}
