using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game.Battle;
using KingdomWar.Game.Cards;

namespace KingdomWar.Game.AI
{
    /// <summary>
    /// AI opponent manager. Handles card management, decision making, and deployment.
    /// One instance per AI player, created by BattleManager when !isNetworkBattle.
    /// </summary>
    public class AIOpponentManager
    {
        // ===== Public State =====
        public AIDifficulty Difficulty { get; private set; }
        public int PlayerId { get; private set; }  // 1 or 2 (team)
        public float Elixir { get; set; }
        public float MaxElixir { get; set; }
        public float ElixirRegenRate { get; set; }

        // ===== Private State =====
        private List<string> fullDeck;       // 8 card names
        private List<string> hand;           // 4 cards in hand
        private Queue<string> drawPile;      // remaining cards to draw
        private float decisionTimer;
        private float decisionInterval;
        private float minElixirToAttack;
        private float placementRandomRadius;
        private float reactionDelay;
        private float reactionTimer;
        private bool waitingForReaction;
        private System.Random rng;

        // ===== Events =====
        // Fired when AI decides to play a card. BattleManager subscribes to this.
        public Action<string, Vector3> OnCardPlayed;

        public AIOpponentManager(AIDifficulty difficulty, int playerId)
        {
            this.Difficulty = difficulty;
            this.PlayerId = playerId;
            this.MaxElixir = 10f;
            this.ElixirRegenRate = 0.8f;
            this.rng = new System.Random();

            // Apply difficulty config
            this.decisionInterval = AIOpponentConfig.GetDecisionInterval(difficulty);
            this.minElixirToAttack = AIOpponentConfig.GetMinElixirToAttack(difficulty);
            this.placementRandomRadius = AIOpponentConfig.GetPlacementRandomRadius(difficulty);
            this.reactionDelay = AIOpponentConfig.GetReactionDelay(difficulty);

            // Initialize deck and hand
            InitializeDeck();
        }

        /// <summary>
        /// Initialize AI deck and draw initial hand.
        /// </summary>
        private void InitializeDeck()
        {
            AIDeckData deckData = AIOpponentConfig.GetDeckForDifficulty(Difficulty);
            fullDeck = new List<string>(deckData.cardNames);

            // Shuffle and create draw pile
            List<string> shuffled = new List<string>(fullDeck);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                string temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }
            drawPile = new Queue<string>(shuffled);

            // Draw initial hand (4 cards)
            hand = new List<string>(AIOpponentConfig.HAND_SIZE);
            for (int i = 0; i < AIOpponentConfig.HAND_SIZE; i++)
            {
                DrawCard();
            }

            decisionTimer = 0f;
            reactionTimer = 0f;
            waitingForReaction = false;
        }

        /// <summary>
        /// Draw next card from draw pile (cycles back when empty).
        /// </summary>
        private void DrawCard()
        {
            if (drawPile.Count == 0)
            {
                // Deck cycle: reshuffle all cards except current hand
                List<string> reshuffle = new List<string>();
                foreach (string cardName in fullDeck)
                {
                    if (!hand.Contains(cardName))
                    {
                        reshuffle.Add(cardName);
                    }
                }
                // Shuffle
                for (int i = reshuffle.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(0, i + 1);
                    string temp = reshuffle[i];
                    reshuffle[i] = reshuffle[j];
                    reshuffle[j] = temp;
                }
                drawPile = new Queue<string>(reshuffle);
            }

            if (drawPile.Count > 0)
            {
                hand.Add(drawPile.Dequeue());
            }
        }

        /// <summary>
        /// Called every frame by BattleManager during Fighting state.
        /// </summary>
        public void UpdateAI(float deltaTime)
        {
            if (hand == null || hand.Count == 0)
                return;

            // Handle reaction delay (AI waits before responding to player's card)
            if (waitingForReaction)
            {
                reactionTimer += deltaTime;
                if (reactionTimer >= reactionDelay)
                {
                    waitingForReaction = false;
                    reactionTimer = 0f;
                }
                return; // Don't make decisions during reaction delay
            }

            // Decision timer
            decisionTimer += deltaTime;
            if (decisionTimer < decisionInterval)
                return;
            decisionTimer = 0f;

            // Make a decision
            MakeDecision();
        }

        /// <summary>
        /// Main AI decision logic.
        /// </summary>
        private void MakeDecision()
        {
            // 1. Find playable cards (cost <= current elixir)
            List<string> playable = new List<string>();
            foreach (string cardName in hand)
            {
                CardData card = CardDatabase.Instance.GetCardByName(cardName);
                if (card != null && card.elixirCost <= Elixir)
                {
                    playable.Add(cardName);
                }
            }

            if (playable.Count == 0)
                return; // Can't afford anything, save elixir

            // 2. Prioritize: prefer higher cost cards when elixir is high
            string chosenCard = ChooseBestCard(playable);

            if (string.IsNullOrEmpty(chosenCard))
                return;

            // 3. Choose deployment position
            Vector3 position = ChooseDeployPosition(chosenCard);

            // 4. Play the card
            PlayCard(chosenCard, position);
        }

        /// <summary>
        /// Choose best card from playable options based on game situation.
        /// Simple heuristic: prefer higher cost cards (they're usually better value),
        /// but occasionally play cheap cards to cycle.
        /// </summary>
        private string ChooseBestCard(List<string> playable)
        {
            if (playable.Count == 0) return null;
            if (playable.Count == 1) return playable[0];

            // Get card costs
            List<(string name, int cost)> costs = new List<(string, int)>();
            foreach (string name in playable)
            {
                CardData card = CardDatabase.Instance.GetCardByName(name);
                if (card != null)
                {
                    costs.Add((name, card.elixirCost));
                }
            }

            if (costs.Count == 0) return playable[0];

            // Sort by cost descending (prefer expensive cards first)
            costs.Sort((a, b) => b.cost.CompareTo(a.cost));

            // 70% chance to pick the most expensive card
            if (rng.NextDouble() < 0.7f)
            {
                return costs[0].name;
            }

            // 30% chance to pick a random card (adds variety)
            return costs[rng.Next(0, costs.Count)].name;
        }

        /// <summary>
        /// Choose deployment position for the selected card.
        /// Uses player's spawn area and adds random offset based on difficulty.
        /// </summary>
        private Vector3 ChooseDeployPosition(string cardName)
        {
            CardData card = CardDatabase.Instance.GetCardByName(cardName);
            if (card == null)
                return GetDefaultSpawnPosition();

            Vector3 basePos = GetDefaultSpawnPosition();

            // For buildings, prefer defensive positions near towers
            if (card.cardType == CardType.Building)
            {
                basePos = GetDefensivePosition();
            }

            // Add random placement offset (difficulty-based inaccuracy)
            float offsetX = (float)(rng.NextDouble() * 2 - 1) * placementRandomRadius;
            float offsetZ = (float)(rng.NextDouble() * 2 - 1) * placementRandomRadius;

            basePos.x += offsetX;
            basePos.z += offsetZ;

            return basePos;
        }

        /// <summary>
        /// Default spawn: behind the king tower in the AI's lane.
        /// </summary>
        private Vector3 GetDefaultSpawnPosition()
        {
            // Player 2 spawns on the opposite side (red team, positive Z)
            if (PlayerId == 2)
            {
                return new Vector3(0f, 0f, 12f); // Behind red king tower
            }
            return new Vector3(0f, 0f, -12f); // Behind blue king tower
        }

        /// <summary>
        /// Defensive position: in front of king tower.
        /// </summary>
        private Vector3 GetDefensivePosition()
        {
            if (PlayerId == 2)
            {
                return new Vector3(0f, 0f, 10f);
            }
            return new Vector3(0f, 0f, -10f);
        }

        /// <summary>
        /// Calculate and apply the cost of playing a card.
        /// </summary>
        private void PlayCard(string cardName, Vector3 position)
        {
            CardData card = CardDatabase.Instance.GetCardByName(cardName);
            if (card == null) return;

            // Deduct elixir
            Elixir -= card.elixirCost;

            // Remove from hand
            hand.Remove(cardName);

            // Draw replacement card
            DrawCard();

            // Fire event for BattleManager to handle actual spawning
            OnCardPlayed?.Invoke(cardName, position);
        }

        /// <summary>
        /// Trigger AI reaction to an enemy card being played.
        /// </summary>
        public void ReactToEnemyPlay(string cardName, Vector3 position)
        {
            waitingForReaction = true;
            reactionTimer = 0f;
        }

        /// <summary>
        /// Get current hand card names (for UI display).
        /// </summary>
        public IReadOnlyList<string> GetHand()
        {
            return hand.AsReadOnly();
        }

        /// <summary>
        /// Get remaining deck size.
        /// </summary>
        public int GetDeckRemaining()
        {
            return drawPile.Count;
        }

        /// <summary>
        /// Reset AI state for new battle.
        /// </summary>
        public void Reset()
        {
            Elixir = 0f;
            decisionTimer = 0f;
            reactionTimer = 0f;
            waitingForReaction = false;
            InitializeDeck();
        }
    }
}
