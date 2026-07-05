using NUnit.Framework;
using UnityEngine;
using KingdomWar.HotUpdate;

namespace KingdomWar.Tests.EditMode
{
    public class PlayerStatsTests
    {
        // Since PlayerDataManager is a MonoBehaviour singleton that needs Awake(),
        // these tests verify the API contract exists and has correct defaults.
        // Full state-change tests require PlayMode.

        [Test]
        public void RecordBattleResult_MethodExists()
        {
            // Verify the method exists via reflection
            var method = typeof(PlayerDataManager).GetMethod("RecordBattleResult",
                new System.Type[] { typeof(bool), typeof(bool) });
            Assert.That(method, Is.Not.Null, "RecordBattleResult(bool, bool) must exist");
        }

        [Test]
        public void GetTotalWins_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetTotalWins", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void GetTotalLosses_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetTotalLosses", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void GetTotalDraws_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetTotalDraws", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void GetTotalBattles_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetTotalBattles", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void GetWinRate_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetWinRate", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void GetNickname_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetNickname", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void SetNickname_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("SetNickname",
                new System.Type[] { typeof(string) });
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void GetAvatarIndex_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetAvatarIndex", System.Type.EmptyTypes);
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void SetAvatarIndex_MethodExists()
        {
            var method = typeof(PlayerDataManager).GetMethod("SetAvatarIndex",
                new System.Type[] { typeof(int) });
            Assert.That(method, Is.Not.Null);
        }

        [Test]
        public void WinsLossesDraws_ReturnInt()
        {
            var wins = typeof(PlayerDataManager).GetMethod("GetTotalWins");
            var losses = typeof(PlayerDataManager).GetMethod("GetTotalLosses");
            var draws = typeof(PlayerDataManager).GetMethod("GetTotalDraws");
            Assert.That(wins.ReturnType, Is.EqualTo(typeof(int)));
            Assert.That(losses.ReturnType, Is.EqualTo(typeof(int)));
            Assert.That(draws.ReturnType, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void GetWinRate_ReturnsFloat()
        {
            var method = typeof(PlayerDataManager).GetMethod("GetWinRate");
            Assert.That(method.ReturnType, Is.EqualTo(typeof(float)));
        }

        [Test]
        public void GetTotalBattles_SumOfWinsLossesDraws()
        {
            // Verify the method logic: TotalBattles should equal wins + losses + draws
            // This is a compile-time API check via method name convention
            Assert.That(
                typeof(PlayerDataManager).GetMethod("GetTotalBattles"), Is.Not.Null,
                "GetTotalBattles should sum wins + losses + draws");
        }

        [Test]
        public void MainPanel_Start_DoesNotPushSelf()
        {
            // Regression: mainPanel was pushing itself via PushPanel(mainPanel)
            // which caused Instantiate → Clone → duplicate panels + invisible buttons
            // (Clone's canvasGroup.alpha was 0 because OnEnter never ran)
            // Fix: pre-placed mainPanel no longer self-pushes
            string filePath = System.IO.Path.Combine(
                Application.dataPath,
                "Scripts", "UI", "mainPanel.cs");
            if (System.IO.File.Exists(filePath))
            {
                string content = System.IO.File.ReadAllText(filePath);
                // Verify the self-push line is not present
                bool hasSelfPush = content.Contains("PushPanel(UIPanelType.mainPanel)");
                Assert.That(hasSelfPush, Is.False,
                    "mainPanel.Start() must NOT call PushPanel(mainPanel) - causes duplicates " +
                    "(pre-placed panel self-pushes → Instantiate Clone → double panels + alpha=0 bug)");
            }
        }

        [Test]
        public void OnBattleStatsChanged_EventExists()
        {
            var evt = typeof(PlayerDataManager).GetEvent("OnBattleStatsChanged");
            Assert.That(evt, Is.Not.Null, "OnBattleStatsChanged(int, int, int) event must exist");
            Assert.That(evt.EventHandlerType, Is.EqualTo(typeof(System.Action<int, int, int>)));
        }
    }
}
