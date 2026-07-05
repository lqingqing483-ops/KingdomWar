using NUnit.Framework;
using KingdomWar.Game.Arena;
using UnityEngine;

namespace KingdomWar.Tests.EditMode
{
    public class ArenaSystemTests
    {
        private TrophyManager testManager;

        [SetUp]
        public void SetUp()
        {
            testManager = ArenaTestHelper.CreateTestTrophyManager();
        }

        [TearDown]
        public void TearDown()
        {
            ArenaTestHelper.DestroyTestTrophyManager(testManager);
            testManager = null;
        }

        // ==================== ArenaConfig Tests ====================
        
        [Test]
        public void ArenaConfig_Has8Arenas()
        {
            Assert.That(ArenaConfig.GetArenaCount(), Is.EqualTo(8));
        }
        
        [Test]
        public void ArenaConfig_TrainingCamp_StartsAtZero()
        {
            var arena = ArenaConfig.GetArena(ArenaId.TrainingCamp);
            Assert.That(arena.minTrophies, Is.EqualTo(0));
            Assert.That(arena.arenaLevel, Is.EqualTo(0));
        }
        
        [Test]
        public void ArenaConfig_Legendary_HasMaxIntMax()
        {
            var arena = ArenaConfig.GetArena(ArenaId.Legendary);
            Assert.That(arena.maxTrophies, Is.EqualTo(int.MaxValue));
        }
        
        [Test]
        public void ArenaConfig_GetArenaByTrophies_ReturnsCorrectArena()
        {
            // Mid-range trophies → Gold arena
            Assert.That(ArenaConfig.GetArenaByTrophies(1200).arenaId, Is.EqualTo(ArenaId.Gold));
            // Edge: exactly at threshold → higher arena
            Assert.That(ArenaConfig.GetArenaByTrophies(1000).arenaId, Is.EqualTo(ArenaId.Gold));
            // Edge: just below threshold → lower arena
            Assert.That(ArenaConfig.GetArenaByTrophies(999).arenaId, Is.EqualTo(ArenaId.Silver));
            // Lowest
            Assert.That(ArenaConfig.GetArenaByTrophies(0).arenaId, Is.EqualTo(ArenaId.TrainingCamp));
            // Highest
            Assert.That(ArenaConfig.GetArenaByTrophies(9000).arenaId, Is.EqualTo(ArenaId.Legendary));
        }
        
        [Test]
        public void ArenaConfig_GetArenaByTrophies_HandlesNegative()
        {
            // Negative trophies should return TrainingCamp
            Assert.That(ArenaConfig.GetArenaByTrophies(-1).arenaId, Is.EqualTo(ArenaId.TrainingCamp));
        }
        
        [Test]
        public void ArenaConfig_GetArenaIdByLevel_ReturnsHighestUnlocked()
        {
            // Level 0 → TrainingCamp
            Assert.That(ArenaConfig.GetArenaIdByLevel(0), Is.EqualTo(ArenaId.TrainingCamp));
            // Level 5 → Gold
            Assert.That(ArenaConfig.GetArenaIdByLevel(5), Is.EqualTo(ArenaId.Gold));
            // Level 13 → Legendary
            Assert.That(ArenaConfig.GetArenaIdByLevel(13), Is.EqualTo(ArenaId.Legendary));
        }
        
        [Test]
        public void ArenaConfig_AllArenas_HaveIncreasingTrophyThresholds()
        {
            var arenas = ArenaConfig.GetAllArenas();
            for (int i = 1; i < arenas.Count; i++)
            {
                Assert.That(arenas[i].minTrophies, Is.GreaterThan(arenas[i - 1].minTrophies),
                    $"{arenas[i].arenaName} should have higher minTrophies than {arenas[i-1].arenaName}");
            }
        }
        
        // ==================== TrophyManager Tests ====================
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_WinGainsTrophies()
        {
            int change = testManager.CalculateTrophyChange(true, false, 1000, 1000);
            Assert.That(change, Is.GreaterThan(0));
            Assert.That(change, Is.EqualTo(30)); // base win
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_LossLosesTrophies()
        {
            int change = testManager.CalculateTrophyChange(false, false, 1000, 1000);
            Assert.That(change, Is.LessThan(0));
            Assert.That(change, Is.EqualTo(-30)); // base loss
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_DrawIsZero()
        {
            int change = testManager.CalculateTrophyChange(false, true, 1000, 1000);
            Assert.That(change, Is.EqualTo(0));
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_WinVsHigher_GainsBonus()
        {
            int equalWin = testManager.CalculateTrophyChange(true, false, 1000, 1000);
            int higherWin = testManager.CalculateTrophyChange(true, false, 1000, 1300);
            Assert.That(higherWin, Is.GreaterThan(equalWin));
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_LossVsHigher_LosesLess()
        {
            int equalLoss = testManager.CalculateTrophyChange(false, false, 1000, 1000);
            int higherLoss = testManager.CalculateTrophyChange(false, false, 1000, 1300);
            Assert.That(higherLoss, Is.GreaterThan(equalLoss)); // less negative
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_BonusCappedAt10()
        {
            // Very large trophy difference
            int change = testManager.CalculateTrophyChange(true, false, 1000, 2000);
            int baseWin = 30;
            Assert.That(change - baseWin, Is.EqualTo(10)); // max bonus
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_LossNeverPositive()
        {
            int change = testManager.CalculateTrophyChange(false, false, 1000, 9999);
            Assert.That(change, Is.LessThanOrEqualTo(0));
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_WinVsMuchLower_Minimum15()
        {
            int change = testManager.CalculateTrophyChange(true, false, 1000, 500);
            Assert.That(change, Is.GreaterThanOrEqualTo(15));
        }
        
        [Test]
        public void TrophyManager_CalculateTrophyChange_LossVsMuchLower_Max40Loss()
        {
            int change = testManager.CalculateTrophyChange(false, false, 1000, 500);
            Assert.That(change, Is.GreaterThanOrEqualTo(-40));
        }

        // ==================== TrophyManager Edge Case Tests ====================

        [Test]
        public void TrophyManager_CalculateTrophyChange_ZeroTrophies_WinGainsBase()
        {
            // Edge case: both players at 0 trophies
            int change = testManager.CalculateTrophyChange(true, false, 0, 0);
            Assert.That(change, Is.EqualTo(30));
        }

        [Test]
        public void TrophyManager_CalculateTrophyChange_ZeroTrophies_LossLosesBase()
        {
            // Edge case: both players at 0 trophies, loss
            int change = testManager.CalculateTrophyChange(false, false, 0, 0);
            Assert.That(change, Is.EqualTo(-30));
        }

        [Test]
        public void TrophyManager_CalculateTrophyChange_HugePositiveDiff_BonusCappedAt10()
        {
            // Opponent has 10x more trophies — bonus should still be capped
            int change = testManager.CalculateTrophyChange(true, false, 1000, 20000);
            int baseWin = 30;
            Assert.That(change - baseWin, Is.EqualTo(10)); // max bonus
        }

        [Test]
        public void TrophyManager_CalculateTrophyChange_HugeNegativeDiff_LossMaxPenalty()
        {
            // Opponent has far fewer trophies — max penalty applies
            // Loss vs much lower: TROPHY_LOSE_BASE(-30) + penalty capped at -40
            int change = testManager.CalculateTrophyChange(false, false, 1000, 100);
            Assert.That(change, Is.GreaterThanOrEqualTo(-40));
            Assert.That(change, Is.LessThanOrEqualTo(-30));
        }

        [Test]
        public void TrophyManager_CalculateTrophyChange_Draw_AlwaysZero()
        {
            // Draw should be 0 regardless of trophy difference
            int equalDraw = testManager.CalculateTrophyChange(false, true, 1000, 1000);
            int higherDraw = testManager.CalculateTrophyChange(false, true, 1000, 3000);
            int lowerDraw = testManager.CalculateTrophyChange(false, true, 3000, 1000);
            Assert.That(equalDraw, Is.EqualTo(0));
            Assert.That(higherDraw, Is.EqualTo(0));
            Assert.That(lowerDraw, Is.EqualTo(0));
        }
    }
}
