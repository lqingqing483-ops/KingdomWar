using UnityEditor;
using UnityEngine;
using KingdomWar.Tools;

namespace KingdomWar.Editor
{
    public static class PerformanceTestRunnerEntry
    {
        private static PerformanceTestRunner runner;
        private static double lastTickTime;

        public static void RunAndCompare()
        {
            GameObject go = new GameObject("PerformanceTestRunner");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<PerformanceTestRunner>();
            runner.BeginTest();
            lastTickTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += PollAndComplete;
        }

        private static void PollAndComplete()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - lastTickTime);
            lastTickTime = now;

            if (delta <= 0f) delta = 0.016f;
            if (delta > 0.1f) delta = 0.016f;

            runner.Tick(delta);

            if (!runner.IsTestComplete)
                return;

            EditorApplication.update -= PollAndComplete;

            PerformanceTestRunner.Metrics results = runner.GetResults();

            var baseline = PerformanceBaseline.LoadBaseline();
            if (baseline == null)
            {
                PerformanceBaseline.SaveBaseline(results);
                Debug.LogWarning("[Perf] No baseline existed. Saved current measurements as baseline.");
                Debug.Log("[Perf] avgFPS=" + results.avgFPS.ToString("F1")
                    + " minFPS=" + results.minFPS.ToString("F1")
                    + " maxFPS=" + results.maxFPS.ToString("F1")
                    + " Memory=" + results.totalMemoryMB + "MB"
                    + " GC/frame=" + results.gcAllocPerFrame);
                EditorApplication.Exit(0);
                return;
            }

            PerformanceBaseline.CompareReport report = PerformanceBaseline.CompareBaseline(results);
            LogReport(report, results);
            EditorApplication.Exit(report.result == PerformanceBaseline.CompareResult.Fail ? 1 : 0);
        }

        private static void LogReport(PerformanceBaseline.CompareReport report, PerformanceTestRunner.Metrics results)
        {
            string status = report.result == PerformanceBaseline.CompareResult.Pass ? "PASS" :
                            report.result == PerformanceBaseline.CompareResult.Warning ? "WARNING" : "FAIL";
            Debug.Log("=== Performance Baseline Report ===");
            Debug.Log("Result: " + status);
            Debug.Log(string.Format("  avgFPS: {0:F1} ({1:F1}%)", results.avgFPS, report.avgFPSChangePercent));
            Debug.Log(string.Format("  minFPS: {0:F1} ({1:F1}%)", results.minFPS, report.minFPSChangePercent));
            Debug.Log(string.Format("  maxFPS: {0:F1} ({1:F1}%)", results.maxFPS, report.maxFPSChangePercent));
            Debug.Log(string.Format("  Memory: {0}MB ({1:F1}%)", results.totalMemoryMB, report.memoryChangePercent));
            Debug.Log(string.Format("  GC/frame: {0} ({1:F1}%)", results.gcAllocPerFrame, report.gcAllocChangePercent));
            Debug.Log("===================================");
        }
    }
}
