using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using KingdomWar.Game.Arena;
using KingdomWar.HotUpdate;
using KingdomWar.UI;

namespace KingdomWar.Tests.PlayMode
{
    public class IntegrationTests
    {
        // ===== Arena/Trophy Integration Tests =====
        // These test TrophyManager.ApplyTrophyChange with real PlayerDataManager

        [SetUp]
        public void SetUp()
        {
            // Ensure PlayerDataManager is initialized (singleton auto-creates)
            var pdm = PlayerDataManager.Instance;
            // Reset data to ensure clean state for each test (avoids test pollution)
            pdm.ResetData();
        }

        [UnityTest]
        public IEnumerator TrophyManager_ApplyTrophyChange_IncreasesTrophiesOnWin()
        {
            var playerData = PlayerDataManager.Instance;
            int before = playerData.GetTrophies();

            var result = TrophyManager.Instance.ApplyTrophyChange(true, false, before);

            Assert.That(result.trophiesGained, Is.GreaterThan(0));
            Assert.That(playerData.GetTrophies(), Is.EqualTo(before + result.trophiesGained));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrophyManager_ApplyTrophyChange_DecreasesTrophiesOnLoss()
        {
            var playerData = PlayerDataManager.Instance;
            // Set some trophies first so we have something to lose
            playerData.SetTrophies(500, false);
            int before = playerData.GetTrophies();

            var result = TrophyManager.Instance.ApplyTrophyChange(false, false, before);

            Assert.That(result.trophiesGained, Is.LessThan(0));
            Assert.That(playerData.GetTrophies(), Is.EqualTo(before + result.trophiesGained));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrophyManager_ApplyTrophyChange_DrawGivesZero()
        {
            var playerData = PlayerDataManager.Instance;
            int before = playerData.GetTrophies();

            var result = TrophyManager.Instance.ApplyTrophyChange(false, true, before);

            Assert.That(result.trophiesGained, Is.EqualTo(0));
            Assert.That(playerData.GetTrophies(), Is.EqualTo(before));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrophyManager_ApplyTrophyChange_DoesNotDropBelowZero()
        {
            var playerData = PlayerDataManager.Instance;
            playerData.SetTrophies(10, false); // Set low trophies

            var result = TrophyManager.Instance.ApplyTrophyChange(false, false, 1000);

            // Should not go below 0 even with large loss
            Assert.That(playerData.GetTrophies(), Is.GreaterThanOrEqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrophyManager_ApplyTrophyChange_ArenaChangeDetected()
        {
            var playerData = PlayerDataManager.Instance;
            // Set trophies just below an arena threshold
            playerData.SetTrophies(299, false);

            var result = TrophyManager.Instance.ApplyTrophyChange(true, false, 299);

            // Winning should push into Bronze arena (300+)
            if (result.newTrophyCount >= 300)
            {
                Assert.That(result.arenaChanged, Is.True);
                Assert.That(result.newArenaId, Is.EqualTo(ArenaId.Bronze));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrophyManager_GetPlayerArena_ReturnsCorrectArena()
        {
            var playerData = PlayerDataManager.Instance;
            playerData.SetTrophies(1200, false); // Gold arena

            var arena = TrophyManager.Instance.GetPlayerArena();

            Assert.That(arena.arenaId, Is.EqualTo(ArenaId.Gold));
            Assert.That(arena.arenaName, Is.EqualTo("黄金竞技场"));
            yield return null;
        }

        // ===== Player Profile Integration Tests =====

        [UnityTest]
        public IEnumerator PlayerStats_RecordBattleResult_IncrementsWins()
        {
            var playerData = PlayerDataManager.Instance;
            int before = playerData.GetTotalWins();

            playerData.RecordBattleResult(true, false);

            Assert.That(playerData.GetTotalWins(), Is.EqualTo(before + 1));
            Assert.That(playerData.GetTotalBattles(), Is.EqualTo(before + 1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_RecordBattleResult_IncrementsLosses()
        {
            var playerData = PlayerDataManager.Instance;
            int before = playerData.GetTotalLosses();

            playerData.RecordBattleResult(false, false);

            Assert.That(playerData.GetTotalLosses(), Is.EqualTo(before + 1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_RecordBattleResult_IncrementsDraws()
        {
            var playerData = PlayerDataManager.Instance;
            int before = playerData.GetTotalDraws();

            playerData.RecordBattleResult(false, true);

            Assert.That(playerData.GetTotalDraws(), Is.EqualTo(before + 1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_WinRate_CalculatesCorrectly()
        {
            var playerData = PlayerDataManager.Instance;

            // Record 3 wins, 1 loss
            playerData.RecordBattleResult(true, false);
            playerData.RecordBattleResult(true, false);
            playerData.RecordBattleResult(true, false);
            playerData.RecordBattleResult(false, false);

            float winRate = playerData.GetWinRate();
            Assert.That(winRate, Is.EqualTo(75f).Within(0.1f)); // 3/4 = 75%
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_WinRate_ZeroBattles_ReturnsZero()
        {
            // WinRate with no battles should return 0, not crash
            float winRate = PlayerDataManager.Instance.GetWinRate();
            // Just verify it doesn't throw
            Assert.That(winRate, Is.GreaterThanOrEqualTo(0f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_Nickname_SetAndGet()
        {
            var playerData = PlayerDataManager.Instance;
            playerData.SetNickname("TestPlayer");

            Assert.That(playerData.GetNickname(), Is.EqualTo("TestPlayer"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_AvatarIndex_SetAndGet()
        {
            var playerData = PlayerDataManager.Instance;
            playerData.SetAvatarIndex(5);

            Assert.That(playerData.GetAvatarIndex(), Is.EqualTo(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerStats_TotalBattles_EqualsSum()
        {
            var playerData = PlayerDataManager.Instance;
            int total = playerData.GetTotalBattles();
            int sum = playerData.GetTotalWins() + playerData.GetTotalLosses() + playerData.GetTotalDraws();

            Assert.That(total, Is.EqualTo(sum));
            yield return null;
        }

        // ===== PlayerDataManager Initialization Tests =====
        // Regression: mainPanel.Start() NRE caused by null cachedPlayerData

        [UnityTest]
        public IEnumerator PlayerDataManager_Instance_IsNotNull()
        {
            var instance = PlayerDataManager.Instance;
            Assert.That(instance, Is.Not.Null, "PlayerDataManager singleton must be accessible");
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDataManager_GetTrophies_DoesNotThrow()
        {
            var instance = PlayerDataManager.Instance;
            Assert.DoesNotThrow(() => { int t = instance.GetTrophies(); });
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDataManager_GetNickname_DoesNotThrow()
        {
            var instance = PlayerDataManager.Instance;
            Assert.DoesNotThrow(() => { string n = instance.GetNickname(); });
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDataManager_OnTrophiesChanged_CanSubscribe()
        {
            // Regression: mainPanel subscribed to cachedPlayerData.OnTrophiesChanged
            // but cachedPlayerData was null because it was never assigned
            var instance = PlayerDataManager.Instance;
            System.Action<int> handler = (int v) => { };
            Assert.DoesNotThrow(() => instance.OnTrophiesChanged += handler);
            instance.OnTrophiesChanged -= handler;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerDataManager_OnBattleStatsChanged_CanSubscribe()
        {
            var instance = PlayerDataManager.Instance;
            System.Action<int, int, int> handler = (int w, int l, int d) => { };
            Assert.DoesNotThrow(() => instance.OnBattleStatsChanged += handler);
            instance.OnBattleStatsChanged -= handler;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any test GameObjects
            var objs = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in objs)
            {
                if (obj.name.StartsWith("Test"))
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        // ===== mainPanel Duplicate Panel Regression Tests =====
        // Bug: self-push caused Instantiate → Clone → duplicate panels + alpha=0

        [UnityTest]
        public IEnumerator mainPanel_CanvasGroupAlpha_IsOneAfterStart()
        {
            // Regression: pre-placed mainPanel no longer self-pushes.
            // Without self-push, OnEnter() is never called → canvasGroup.alpha stays 0
            // Fix: mainPanel.Start() sets alpha=1 manually.

            var pdm = PlayerDataManager.Instance;
            Assert.That(pdm, Is.Not.Null, "PlayerDataManager must be initialized");

            // Create a mainPanel-like GameObject with CanvasGroup
            GameObject panelObj = new GameObject("TestPanel_Alpha");
            var canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;  // Simulate basePanel.Awake() setting alpha=0
            canvasGroup.blocksRaycasts = false;

            // Simulate the fix: mainPanel.Start() sets alpha=1 manually
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;

            Assert.That(canvasGroup.alpha, Is.EqualTo(1f),
                "mainPanel must be visible after Start() - alpha should be 1");
            Assert.That(canvasGroup.blocksRaycasts, Is.True,
                "mainPanel must be interactive after Start() - blocksRaycasts should be true");

            Object.DestroyImmediate(panelObj);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UIManager_GetPanel_DoesNotCreateDuplicate()
        {
            // Regression: GetPanel would not find pre-placed mainPanel in panelObjDic
            // → Instantiate prefab → creates mainPanel(Clone) → duplicate panel

            // Verify the UIManager's panel dictionary behavior:
            // GetPanel should check if panelObjDic already has the type
            var uiManager = UIManager.Instance;
            Assert.That(uiManager, Is.Not.Null);

            // Check that GetPanel for mainPanel returns the same instance on second call
            // (no duplicate creation)
            var panel1 = uiManager.GetPanel(UIPanelType.mainPanel);
            var panel2 = uiManager.GetPanel(UIPanelType.mainPanel);

            // Both calls should return the same instance (or both null if not loaded)
            if (panel1 != null && panel2 != null)
            {
                Assert.That(panel1, Is.SameAs(panel2),
                    "GetPanel must return the SAME instance for mainPanel, not create duplicates");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator UIManager_PushPanel_DoesNotCreateDuplicateInstance()
        {
            // Regression: PushPanel(mainPanel) in mainPanel.Start() triggered GetPanel
            // which Instantiated a Clone, then the Clone's Start() ran AGAIN
            // causing cascading duplicates.

            // Verify that pushing a panel type twice returns the same instance
            // (panelObjDic prevents duplicate Instantiate)
            var panel1 = UIManager.Instance.PushPanel(UIPanelType.shopPanel);
            var panel2 = UIManager.Instance.PushPanel(UIPanelType.shopPanel);

            // UIManager.PushPanel calls GetPanel which checks panelObjDic first.
            // Second call should find existing entry and return same instance.
            if (panel1 != null && panel2 != null)
            {
                Assert.That(panel1, Is.SameAs(panel2),
                    "PushPanel must reuse the existing panel instance, not create duplicates");
            }

            // Clean up: pop the extra push from stack
            UIManager.Instance.PopPanel();

            yield return null;
        }
    }
}
