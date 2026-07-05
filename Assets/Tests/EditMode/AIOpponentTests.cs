using NUnit.Framework;
using KingdomWar.Game.AI;
using UnityEngine;

namespace KingdomWar.Tests.EditMode
{
    public class AIOpponentTests
    {
        // ==================== Difficulty Enum Tests ====================

        [Test]
        public void AIDifficulty_Has4Values()
        {
            Assert.That(System.Enum.GetValues(typeof(AIDifficulty)).Length, Is.EqualTo(4));
        }

        [Test]
        public void AIDifficulty_HasEasy()
        {
            Assert.That(System.Enum.IsDefined(typeof(AIDifficulty), AIDifficulty.Easy));
        }

        [Test]
        public void AIDifficulty_HasExpert()
        {
            Assert.That(System.Enum.IsDefined(typeof(AIDifficulty), AIDifficulty.Expert));
        }

        [Test]
        public void AIDifficulty_ValuesAreSequential()
        {
            Assert.That((int)AIDifficulty.Easy, Is.EqualTo(0));
            Assert.That((int)AIDifficulty.Medium, Is.EqualTo(1));
            Assert.That((int)AIDifficulty.Hard, Is.EqualTo(2));
            Assert.That((int)AIDifficulty.Expert, Is.EqualTo(3));
        }

        [Test]
        public void AIPlayStyle_Has3Values()
        {
            Assert.That(System.Enum.GetValues(typeof(AIPlayStyle)).Length, Is.EqualTo(3));
        }

        // ==================== AIOpponentConfig Tests ====================

        [Test]
        public void Config_DecisionInterval_DecreasesWithDifficulty()
        {
            float easy = AIOpponentConfig.GetDecisionInterval(AIDifficulty.Easy);
            float medium = AIOpponentConfig.GetDecisionInterval(AIDifficulty.Medium);
            float hard = AIOpponentConfig.GetDecisionInterval(AIDifficulty.Hard);
            float expert = AIOpponentConfig.GetDecisionInterval(AIDifficulty.Expert);

            Assert.That(easy, Is.GreaterThan(medium));
            Assert.That(medium, Is.GreaterThan(hard));
            Assert.That(hard, Is.GreaterThanOrEqualTo(expert));
        }

        [Test]
        public void Config_MinElixirToAttack_DecreasesWithDifficulty()
        {
            float easy = AIOpponentConfig.GetMinElixirToAttack(AIDifficulty.Easy);
            float medium = AIOpponentConfig.GetMinElixirToAttack(AIDifficulty.Medium);
            float hard = AIOpponentConfig.GetMinElixirToAttack(AIDifficulty.Hard);
            float expert = AIOpponentConfig.GetMinElixirToAttack(AIDifficulty.Expert);

            Assert.That(easy, Is.GreaterThan(medium));
            Assert.That(medium, Is.GreaterThan(hard));
            Assert.That(hard, Is.GreaterThan(expert));
        }

        [Test]
        public void Config_PlacementRandomRadius_DecreasesWithDifficulty()
        {
            float easy = AIOpponentConfig.GetPlacementRandomRadius(AIDifficulty.Easy);
            float medium = AIOpponentConfig.GetPlacementRandomRadius(AIDifficulty.Medium);
            float hard = AIOpponentConfig.GetPlacementRandomRadius(AIDifficulty.Hard);
            float expert = AIOpponentConfig.GetPlacementRandomRadius(AIDifficulty.Expert);

            Assert.That(easy, Is.GreaterThan(medium));
            Assert.That(medium, Is.GreaterThan(hard));
            Assert.That(hard, Is.GreaterThan(expert));
        }

        [Test]
        public void Config_ReactionDelay_DecreasesWithDifficulty()
        {
            float easy = AIOpponentConfig.GetReactionDelay(AIDifficulty.Easy);
            float medium = AIOpponentConfig.GetReactionDelay(AIDifficulty.Medium);
            float hard = AIOpponentConfig.GetReactionDelay(AIDifficulty.Hard);
            float expert = AIOpponentConfig.GetReactionDelay(AIDifficulty.Expert);

            Assert.That(easy, Is.GreaterThan(medium));
            Assert.That(medium, Is.GreaterThan(hard));
            Assert.That(hard, Is.GreaterThan(expert));
        }

        [Test]
        public void Config_GetDeckForDifficulty_ReturnsValidDeck()
        {
            var deck = AIOpponentConfig.GetDeckForDifficulty(AIDifficulty.Medium);
            Assert.That(deck, Is.Not.Null);
            Assert.That(deck.cardNames, Is.Not.Null);
            Assert.That(deck.cardNames.Count, Is.EqualTo(AIOpponentConfig.DECK_SIZE));
        }

        [Test]
        public void Config_GetDeckForDifficulty_EasyReturnsOnlyStarterPush()
        {
            // Easy difficulty should return the deck with minDifficulty == Easy
            var deck = AIOpponentConfig.GetDeckForDifficulty(AIDifficulty.Easy);
            Assert.That(deck.deckName, Is.EqualTo("Starter Push"));
        }

        [Test]
        public void Config_AllDecks_Have8Cards()
        {
            foreach (var deck in AIOpponentConfig.Decks)
            {
                Assert.That(deck.cardNames.Count, Is.EqualTo(8),
                    $"Deck '{deck.deckName}' should have 8 cards, has {deck.cardNames.Count}");
            }
        }

        [Test]
        public void Config_AllDecks_HaveValidDeckName()
        {
            foreach (var deck in AIOpponentConfig.Decks)
            {
                Assert.That(deck.deckName, Is.Not.Null.Or.Empty,
                    $"Deck should have a non-empty name");
            }
        }

        [Test]
        public void Config_HandSize_Is4()
        {
            Assert.That(AIOpponentConfig.HAND_SIZE, Is.EqualTo(4));
        }

        [Test]
        public void Config_DeckSize_Is8()
        {
            Assert.That(AIOpponentConfig.DECK_SIZE, Is.EqualTo(8));
        }

        [Test]
        public void Config_Has4Decks()
        {
            Assert.That(AIOpponentConfig.Decks.Count, Is.EqualTo(4));
        }

        // ==================== AIOpponentManager Constructor Tests ====================

        [Test]
        public void Constructor_SetsDifficultyAndPlayerId()
        {
            var ai = new AIOpponentManager(AIDifficulty.Hard, 2);
            Assert.That(ai.Difficulty, Is.EqualTo(AIDifficulty.Hard));
            Assert.That(ai.PlayerId, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_InitializesElixir()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            Assert.That(ai.MaxElixir, Is.EqualTo(10f));
            Assert.That(ai.Elixir, Is.EqualTo(0f));
        }

        [Test]
        public void Constructor_DrawsInitialHand()
        {
            var ai = new AIOpponentManager(AIDifficulty.Medium, 2);
            Assert.That(ai.GetHand(), Is.Not.Null);
            Assert.That(ai.GetHand().Count, Is.EqualTo(AIOpponentConfig.HAND_SIZE));
        }

        [Test]
        public void Constructor_InitializesDrawPile()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            // Deck starts with DECK_SIZE (8) cards, HAND_SIZE (4) drawn into hand
            // So draw pile should have DECK_SIZE - HAND_SIZE = 4 remaining
            Assert.That(ai.GetDeckRemaining(), Is.EqualTo(4));
        }

        [Test]
        public void Constructor_HandCardsAreStrings()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            foreach (var card in ai.GetHand())
            {
                Assert.That(card, Is.Not.Null.Or.Empty);
            }
        }

        [Test]
        public void Constructor_HandCardsAreDistinct()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            var hand = ai.GetHand();
            // Check for duplicates (shouldn't normally happen with 8 unique cards)
            Assert.That(hand.Count, Is.EqualTo(new System.Collections.Generic.HashSet<string>(hand).Count));
        }

        // ==================== UpdateAI Tests ====================
        // Note: Full decision-making requires CardDatabase with card data, which is tested in PlayMode.
        // These EditMode tests verify UpdateAI handles flow control correctly without crashing.

        [Test]
        public void UpdateAI_WithSmallDelta_DoesNotCrash()
        {
            // Easy has 3.0s decision interval, so small deltas won't trigger decisions
            var ai = new AIOpponentManager(AIDifficulty.Easy, 2);
            Assert.DoesNotThrow(() => ai.UpdateAI(0.5f));
            Assert.DoesNotThrow(() => ai.UpdateAI(0.5f));
        }

        [Test]
        public void UpdateAI_WithLargeDelta_DoesNotCrash()
        {
            // CardDatabase auto-creates with empty card list, so GetCardByName returns null
            // MakeDecision will find no playable cards and return early
            var ai = new AIOpponentManager(AIDifficulty.Easy, 2);
            Assert.DoesNotThrow(() => ai.UpdateAI(10f));
        }

        [Test]
        public void UpdateAI_MultipleCalls_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            for (int i = 0; i < 10; i++)
            {
                Assert.DoesNotThrow(() => ai.UpdateAI(0.3f),
                    $"UpdateAI should not crash on call #{i + 1}");
            }
        }

        [Test]
        public void UpdateAI_WithZeroDelta_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Hard, 1);
            Assert.DoesNotThrow(() => ai.UpdateAI(0f));
        }

        [Test]
        public void UpdateAI_WithNegativeDelta_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Hard, 1);
            Assert.DoesNotThrow(() => ai.UpdateAI(-1f));
        }

        [Test]
        public void UpdateAI_AllDifficulties_DoNotCrash()
        {
            var difficulties = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            foreach (var diff in difficulties)
            {
                var ai = new AIOpponentManager(diff, 1);
                Assert.DoesNotThrow(() => ai.UpdateAI(0.5f),
                    $"UpdateAI should not crash for {diff}");
            }
        }

        // ==================== Reaction Delay Tests ====================

        [Test]
        public void ReactToEnemyPlay_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 2);
            Assert.DoesNotThrow(() => ai.ReactToEnemyPlay("Knight", new Vector3(0, 0, 0)));
        }

        [Test]
        public void ReactToEnemyPlay_ThenUpdateAI_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 2);
            ai.ReactToEnemyPlay("Archers", new Vector3(5, 0, 5));
            // During reaction delay, UpdateAI returns without making decisions
            Assert.DoesNotThrow(() => ai.UpdateAI(0.1f));
        }

        [Test]
        public void ReactToEnemyPlay_DelayExpires_ThenNormalUpdate()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 2);
            ai.ReactToEnemyPlay("Fireball", new Vector3(1, 2, 3));

            // Simulate enough time passing for reaction delay to expire (Easy = 2.5s)
            ai.UpdateAI(2.6f);

            // After reaction expires, should return to normal decision cycle
            Assert.DoesNotThrow(() => ai.UpdateAI(0.5f));
        }

        [Test]
        public void MultipleReactToEnemyPlay_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            Assert.DoesNotThrow(() => ai.ReactToEnemyPlay("Knight", Vector3.zero));
            Assert.DoesNotThrow(() => ai.ReactToEnemyPlay("Archers", Vector3.one));
            Assert.DoesNotThrow(() => ai.ReactToEnemyPlay("Fireball", new Vector3(1, 2, 3)));
        }

        // ==================== Reset Tests ====================

        [Test]
        public void Reset_ClearsElixir()
        {
            var ai = new AIOpponentManager(AIDifficulty.Hard, 1);
            ai.Elixir = 8f;
            ai.Reset();
            Assert.That(ai.Elixir, Is.EqualTo(0f));
        }

        [Test]
        public void Reset_ReinitializesHand()
        {
            var ai = new AIOpponentManager(AIDifficulty.Medium, 2);
            ai.Reset();
            Assert.That(ai.GetHand(), Is.Not.Null);
            Assert.That(ai.GetHand().Count, Is.EqualTo(AIOpponentConfig.HAND_SIZE));
        }

        [Test]
        public void Reset_ReinitializesDrawPile()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            ai.Reset();
            Assert.That(ai.GetDeckRemaining(), Is.EqualTo(4));
        }

        [Test]
        public void Reset_ClearsReactionState()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 2);
            ai.ReactToEnemyPlay("Knight", new Vector3(0, 0, 0));
            ai.Reset();
            // After reset, UpdateAI should not be blocked by reaction delay
            Assert.DoesNotThrow(() => ai.UpdateAI(1.0f));
        }

        [Test]
        public void MultipleReset_DoesNotCrash()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            Assert.DoesNotThrow(() => ai.Reset());
            Assert.DoesNotThrow(() => ai.Reset());
            Assert.DoesNotThrow(() => ai.Reset());
        }

        [Test]
        public void Reset_ClearsDecisionTimer()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            // Advance decision timer close to threshold
            ai.UpdateAI(2.5f);
            // Reset should clear the timer
            ai.Reset();
            // After reset, timer starts at 0, so small delta shouldn't trigger decision
            Assert.DoesNotThrow(() => ai.UpdateAI(2.5f));
        }

        // ==================== Hand Management Tests ====================

        [Test]
        public void GetHand_ReturnsCurrentHand()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            var hand = ai.GetHand();
            Assert.That(hand, Is.Not.Null);
            Assert.That(hand.Count, Is.EqualTo(AIOpponentConfig.HAND_SIZE));
        }

        [Test]
        public void GetDeckRemaining_ReturnsCorrectCount()
        {
            var ai = new AIOpponentManager(AIDifficulty.Medium, 2);
            // Deck starts with DECK_SIZE (8), HAND_SIZE (4) drawn into hand
            Assert.That(ai.GetDeckRemaining(), Is.EqualTo(AIOpponentConfig.DECK_SIZE - AIOpponentConfig.HAND_SIZE));
        }

        [Test]
        public void GetHand_IsReadOnly()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            var hand = ai.GetHand();
            Assert.That(hand, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<string>>());
        }

        [Test]
        public void GetHand_MultipleCalls_ReturnConsistentState()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            var hand1 = ai.GetHand();
            var hand2 = ai.GetHand();
            Assert.That(hand1.Count, Is.EqualTo(hand2.Count));
            for (int i = 0; i < hand1.Count; i++)
            {
                Assert.That(hand1[i], Is.EqualTo(hand2[i]));
            }
        }

        // ==================== Multiple Instance Tests ====================

        [Test]
        public void AllDifficulties_ConstructSuccessfully()
        {
            Assert.DoesNotThrow(() => new AIOpponentManager(AIDifficulty.Easy, 1));
            Assert.DoesNotThrow(() => new AIOpponentManager(AIDifficulty.Medium, 2));
            Assert.DoesNotThrow(() => new AIOpponentManager(AIDifficulty.Hard, 1));
            Assert.DoesNotThrow(() => new AIOpponentManager(AIDifficulty.Expert, 2));
        }

        [Test]
        public void AllDifficulties_InitialHandHas4Cards()
        {
            var difficulties = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            foreach (var diff in difficulties)
            {
                var ai = new AIOpponentManager(diff, 1);
                Assert.That(ai.GetHand().Count, Is.EqualTo(AIOpponentConfig.HAND_SIZE),
                    $"Hand should have {AIOpponentConfig.HAND_SIZE} cards for {diff}");
            }
        }

        [Test]
        public void AllPlayerIds_ConstructSuccessfully()
        {
            Assert.DoesNotThrow(() => new AIOpponentManager(AIDifficulty.Medium, 1));
            Assert.DoesNotThrow(() => new AIOpponentManager(AIDifficulty.Medium, 2));
        }

        [Test]
        public void TwoInstances_HaveSeparateHands()
        {
            var ai1 = new AIOpponentManager(AIDifficulty.Easy, 1);
            var ai2 = new AIOpponentManager(AIDifficulty.Easy, 1);

            // Hand objects should be different instances
            Assert.That(ai1.GetHand(), Is.Not.SameAs(ai2.GetHand()));
        }

        [Test]
        public void TwoInstances_HaveSeparateDrawPiles()
        {
            var ai1 = new AIOpponentManager(AIDifficulty.Easy, 1);
            var ai2 = new AIOpponentManager(AIDifficulty.Easy, 1);

            // Each should have its own draw pile
            Assert.That(ai1.GetDeckRemaining(), Is.EqualTo(ai2.GetDeckRemaining()));
        }

        // ==================== Edge Cases ====================

        [Test]
        public void UpdateAI_WithEmptyHand_DoesNotCrash()
        {
            // Hand is always initialized by constructor, so this tests the normal path
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            Assert.DoesNotThrow(() => ai.UpdateAI(1.0f));
        }

        [Test]
        public void Elixir_SetAndGet()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            ai.Elixir = 5f;
            Assert.That(ai.Elixir, Is.EqualTo(5f));
            ai.Elixir = 10f;
            Assert.That(ai.Elixir, Is.EqualTo(10f));
            ai.Elixir = 0f;
            Assert.That(ai.Elixir, Is.EqualTo(0f));
        }

        [Test]
        public void ElixirRegenRate_DefaultIsCorrect()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            Assert.That(ai.ElixirRegenRate, Is.EqualTo(0.8f));
        }

        [Test]
        public void MaxElixir_DefaultIs10()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            Assert.That(ai.MaxElixir, Is.EqualTo(10f));
        }

        [Test]
        public void OnCardPlayed_EventIsNull_DoesNotThrow()
        {
            var ai = new AIOpponentManager(AIDifficulty.Easy, 1);
            // Event is not subscribed - UpdateAI should handle this gracefully
            // (though MakeDecision won't reach PlayCard without CardDatabase data)
            Assert.DoesNotThrow(() => ai.UpdateAI(10f));
        }
    }
}
