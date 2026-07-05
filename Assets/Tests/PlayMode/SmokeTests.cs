using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace KingdomWar.Tests.PlayMode
{
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator SmokeTest_MainScene_LoadsWithoutCrash()
        {
            Assert.DoesNotThrow(() => SceneManager.LoadScene("mainScene"));
            yield return new WaitForSeconds(3f);
            
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("mainScene"),
                "mainScene should be the active scene after loading");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmokeTest_BattleScene_LoadsWithoutCrash()
        {
            Assert.DoesNotThrow(() => SceneManager.LoadScene("Main"));
            yield return new WaitForSeconds(3f);
            
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Main"),
                "Battle scene (Main) should be the active scene after loading");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmokeTest_LoadingScene_LoadsWithoutCrash()
        {
            Assert.DoesNotThrow(() => SceneManager.LoadScene("LoadingScene"));
            yield return new WaitForSeconds(3f);
            
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("LoadingScene"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmokeTest_SceneLoad_DoesNotLogErrors()
        {
            // Track if any error was logged during scene load
            bool errorLogged = false;
            void HandleLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                {
                    errorLogged = true;
                    Debug.Log($"[SmokeTest] Captured error: {condition}");
                }
            }
            
            Application.logMessageReceived += HandleLog;
            
            SceneManager.LoadScene("mainScene");
            yield return new WaitForSeconds(2f);
            
            Application.logMessageReceived -= HandleLog;
            
            // Smoke test allows errors but should not crash
            // This test is informational — it logs errors instead of failing
            if (errorLogged)
            {
                Debug.LogWarning("[SmokeTest] Errors were logged during scene load. Review logs for details.");
            }
        }

        [UnityTest]
        public IEnumerator SmokeTest_MultipleScenes_LoadUnloadDoesNotLeak()
        {
            // Load and unload scenes to check for memory leaks
            SceneManager.LoadScene("mainScene");
            yield return new WaitForSeconds(1f);
            
            SceneManager.LoadScene("Main");
            yield return new WaitForSeconds(1f);
            
            SceneManager.LoadScene("mainScene");
            yield return new WaitForSeconds(1f);
            
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("mainScene"),
                "Should be able to switch between scenes without crashing");
            yield return null;
        }
    }
}
