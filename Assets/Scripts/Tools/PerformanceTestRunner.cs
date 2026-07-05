using UnityEngine;
using UnityEngine.Profiling;

namespace KingdomWar.Tools
{
    public class PerformanceTestRunner : MonoBehaviour
    {
        [System.Serializable]
        public struct Metrics
        {
            public float avgFPS;
            public float minFPS;
            public float maxFPS;
            public long totalMemoryMB;
            public long gcAllocPerFrame;
            public float timestamp;
        }

        private float[] frameTimes = new float[300];
        private int frameIndex = 0;
        private float testDuration = 5f;
        private bool isRunning = false;
        private float startTime;
        private long memBefore;
        private long memAfter;

        public bool IsTestComplete { get; private set; }
        public Metrics LastResults { get; private set; }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!isRunning)
                return;

            if (frameIndex == 0)
            {
                memBefore = Profiler.GetTotalAllocatedMemoryLong();
            }

            frameTimes[frameIndex++] = deltaTime;

            if (frameIndex >= frameTimes.Length || Time.unscaledTime - startTime >= testDuration)
            {
                memAfter = Profiler.GetTotalAllocatedMemoryLong();
                isRunning = false;
                LastResults = CalculateMetrics();
                IsTestComplete = true;
            }
        }

        public void BeginTest()
        {
            IsTestComplete = false;
            isRunning = false;
            frameIndex = 0;
            startTime = Time.unscaledTime;
            System.Array.Clear(frameTimes, 0, frameTimes.Length);
            isRunning = true;
        }

        private Metrics CalculateMetrics()
        {
            Metrics m = new Metrics();
            int count = frameIndex;
            if (count == 0)
            {
                m.minFPS = m.maxFPS = m.avgFPS = 0;
                m.totalMemoryMB = 0;
                m.gcAllocPerFrame = 0;
                m.timestamp = Time.unscaledTime;
                return m;
            }

            float sum = 0;
            float min = float.MaxValue;
            float max = 0;

            for (int i = 0; i < count; i++)
            {
                float dt = frameTimes[i];
                if (dt > 0)
                {
                    float fps = 1f / dt;
                    sum += fps;
                    if (fps < min) min = fps;
                    if (fps > max) max = fps;
                }
            }

            m.avgFPS = sum / count;
            m.minFPS = min;
            m.maxFPS = max;

            long allocDelta = memAfter - memBefore;
            m.totalMemoryMB = memAfter / 1048576L;
            m.gcAllocPerFrame = count > 0 ? allocDelta / count : 0;
            m.timestamp = Time.unscaledTime;

            return m;
        }

        public Metrics GetResults()
        {
            return LastResults;
        }
    }
}
