using UnityEngine;
using System.IO;

namespace KingdomWar.Tools
{
    public static class PerformanceBaseline
    {
        private static string FilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "PerformanceBaseline.json"); }
        }

        [System.Serializable]
        public struct BaselineData
        {
            public float avgFPS;
            public float minFPS;
            public float maxFPS;
            public long totalMemoryMB;
            public long gcAllocPerFrame;
            public float timestamp;
        }

        public enum CompareResult
        {
            Pass,
            Warning,
            Fail
        }

        public struct CompareReport
        {
            public CompareResult result;
            public float avgFPSChangePercent;
            public float minFPSChangePercent;
            public float maxFPSChangePercent;
            public float memoryChangePercent;
            public float gcAllocChangePercent;
        }

        private static float ChangePercent(float baseline, float current)
        {
            if (baseline == 0)
                return current == 0 ? 0 : 100f;
            return ((current - baseline) / baseline) * 100f;
        }

        private static float ChangePercent(long baseline, long current)
        {
            if (baseline == 0)
                return current == 0 ? 0 : 100f;
            return ((float)(current - baseline) / baseline) * 100f;
        }

        public static void SaveBaseline(PerformanceTestRunner.Metrics m)
        {
            BaselineData data = new BaselineData
            {
                avgFPS = m.avgFPS,
                minFPS = m.minFPS,
                maxFPS = m.maxFPS,
                totalMemoryMB = m.totalMemoryMB,
                gcAllocPerFrame = m.gcAllocPerFrame,
                timestamp = m.timestamp
            };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
            Debug.Log("[PerformanceBaseline] Saved baseline to " + FilePath);
        }

        public static BaselineData? LoadBaseline()
        {
            if (!File.Exists(FilePath))
            {
                Debug.LogWarning("[PerformanceBaseline] No baseline file found at " + FilePath);
                return null;
            }

            string json = File.ReadAllText(FilePath);
            BaselineData data = JsonUtility.FromJson<BaselineData>(json);
            return data;
        }

        public static CompareReport CompareBaseline(PerformanceTestRunner.Metrics current)
        {
            CompareReport report = new CompareReport();
            BaselineData? maybeBaseline = LoadBaseline();

            if (maybeBaseline == null)
            {
                report.result = CompareResult.Pass;
                return report;
            }

            BaselineData baseline = maybeBaseline.Value;

            report.avgFPSChangePercent = ChangePercent(baseline.avgFPS, current.avgFPS);
            report.minFPSChangePercent = ChangePercent(baseline.minFPS, current.minFPS);
            report.maxFPSChangePercent = ChangePercent(baseline.maxFPS, current.maxFPS);
            report.memoryChangePercent = ChangePercent(baseline.totalMemoryMB, current.totalMemoryMB);
            report.gcAllocChangePercent = ChangePercent(baseline.gcAllocPerFrame, current.gcAllocPerFrame);

            float worstRegression = 0;
            float changes = Mathf.Min(report.avgFPSChangePercent, report.minFPSChangePercent);
            changes = Mathf.Min(changes, report.maxFPSChangePercent);
            changes = Mathf.Min(changes, -report.memoryChangePercent);
            changes = Mathf.Min(changes, -report.gcAllocChangePercent);

            worstRegression = changes;

            if (worstRegression <= -30f)
                report.result = CompareResult.Fail;
            else if (worstRegression <= -10f)
                report.result = CompareResult.Warning;
            else
                report.result = CompareResult.Pass;

            return report;
        }
    }
}
