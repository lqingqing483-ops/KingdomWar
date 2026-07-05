using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public class BattleSyncOptimizer : MonoBehaviour
    {
        public static BattleSyncOptimizer Instance { get; private set; }

        [Header("优化配置")]
        public bool enableOptimization = true;
        public bool enableAdaptiveSync = true;
        public bool enableBandwidthOptimization = true;
        public bool enablePrediction = true;

        [Header("带宽限制")]
        public int maxBytesPerSecond = 64000;
        public int maxEventsPerFrame = 10;
        public int maxEventsPerSecond = 100;

        [Header("预测配置")]
        public float predictionThreshold = 0.2f;
        public int maxPredictionFrames = 3;

        [Header("批处理配置")]
        public float batchInterval = 0.05f;
        public int maxBatchSize = 20;

        private Queue<BattleSyncEvent> eventQueue;
        private Queue<BattleSyncEvent> priorityQueue;
        private Dictionary<int, PredictedState> predictedStates;
        private BandwidthController bandwidthController;
        private AdaptiveSyncController adaptiveController;

        private float lastBatchTime;
        private int eventsThisFrame;
        private int eventsThisSecond;
        private float secondTimer;

        public event System.Action<List<BattleSyncEvent>> OnBatchReady;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            eventQueue = new Queue<BattleSyncEvent>();
            priorityQueue = new Queue<BattleSyncEvent>();
            predictedStates = new Dictionary<int, PredictedState>();
            bandwidthController = new BandwidthController(maxBytesPerSecond);
            adaptiveController = new AdaptiveSyncController();

            lastBatchTime = Time.time;
            eventsThisFrame = 0;
            eventsThisSecond = 0;
            secondTimer = 0f;

            Debug.Log("[BattleSyncOptimizer] Initialized");
        }

        private void Update()
        {
            eventsThisFrame = 0;
            secondTimer += Time.deltaTime;

            if (secondTimer >= 1f)
            {
                eventsThisSecond = 0;
                secondTimer = 0f;
            }

            if (enableOptimization)
            {
                ProcessBatching();
                UpdatePredictions();
            }

            if (enableAdaptiveSync)
            {
                adaptiveController.Update();
            }
        }

        public bool QueueEvent(BattleSyncEvent syncEvent)
        {
            if (!enableOptimization)
            {
                return true;
            }

            if (eventsThisFrame >= maxEventsPerFrame)
            {
                BattleSyncLogger.Instance?.LogPerformance("Event dropped: frame limit reached");
                return false;
            }

            if (eventsThisSecond >= maxEventsPerSecond)
            {
                BattleSyncLogger.Instance?.LogPerformance("Event dropped: second limit reached");
                return false;
            }

            int eventSize = syncEvent.eventData?.Length ?? 0;
            if (!bandwidthController.CanSend(eventSize))
            {
                BattleSyncLogger.Instance?.LogPerformance("Event dropped: bandwidth limit reached");
                return false;
            }

            if (syncEvent.priority >= SyncPriority.High)
            {
                priorityQueue.Enqueue(syncEvent);
            }
            else
            {
                eventQueue.Enqueue(syncEvent);
            }

            eventsThisFrame++;
            eventsThisSecond++;
            bandwidthController.RecordSend(eventSize);

            return true;
        }

        public List<BattleSyncEvent> GetEventsToSend()
        {
            var result = new List<BattleSyncEvent>();

            while (priorityQueue.Count > 0 && result.Count < maxBatchSize)
            {
                result.Add(priorityQueue.Dequeue());
            }

            while (eventQueue.Count > 0 && result.Count < maxBatchSize)
            {
                result.Add(eventQueue.Dequeue());
            }

            return result;
        }

        private void ProcessBatching()
        {
            if (Time.time - lastBatchTime >= batchInterval)
            {
                var batch = GetEventsToSend();
                if (batch.Count > 0)
                {
                    OnBatchReady?.Invoke(batch);
                }
                lastBatchTime = Time.time;
            }
        }

        #region Prediction

        private void UpdatePredictions()
        {
            if (!enablePrediction) return;

            var toRemove = new List<int>();
            foreach (var kvp in predictedStates)
            {
                var prediction = kvp.Value;
                prediction.age += Time.deltaTime;

                if (prediction.age > predictionThreshold * maxPredictionFrames)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                predictedStates.Remove(key);
            }
        }

        public void RecordState(int entityId, Vector3 position, Vector3 velocity)
        {
            if (!enablePrediction) return;

            if (!predictedStates.ContainsKey(entityId))
            {
                predictedStates[entityId] = new PredictedState();
            }

            var state = predictedStates[entityId];
            state.position = position;
            state.velocity = velocity;
            state.timestamp = Time.time;
            state.age = 0f;
        }

        public Vector3 GetPredictedPosition(int entityId)
        {
            if (!enablePrediction || !predictedStates.ContainsKey(entityId))
            {
                return Vector3.zero;
            }

            var state = predictedStates[entityId];
            float predictionTime = Mathf.Min(state.age, predictionThreshold * maxPredictionFrames);
            return state.position + state.velocity * predictionTime;
        }

        public void ClearPrediction(int entityId)
        {
            predictedStates.Remove(entityId);
        }

        #endregion

        #region Bandwidth Optimization

        public byte[] CompressData(byte[] data)
        {
            if (!enableBandwidthOptimization || data == null || data.Length == 0)
            {
                return data;
            }

            return CompressionHelper.Compress(data);
        }

        public byte[] DecompressData(byte[] compressedData)
        {
            if (!enableBandwidthOptimization || compressedData == null || compressedData.Length == 0)
            {
                return compressedData;
            }

            return CompressionHelper.Decompress(compressedData);
        }

        public bool ShouldSyncPosition(Vector3 current, Vector3 last, float threshold)
        {
            if (!enableOptimization)
            {
                return true;
            }

            return Vector3.Distance(current, last) > threshold;
        }

        public bool ShouldSyncRotation(Quaternion current, Quaternion last, float threshold)
        {
            if (!enableOptimization)
            {
                return true;
            }

            return Quaternion.Angle(current, last) > threshold;
        }

        #endregion

        #region Adaptive Sync

        public float GetAdaptiveSyncInterval()
        {
            if (!enableAdaptiveSync)
            {
                return BattleSyncConfig.DEFAULT_SYNC_INTERVAL;
            }

            return adaptiveController.GetSyncInterval();
        }

        public void RecordNetworkQuality(int quality)
        {
            if (enableAdaptiveSync)
            {
                adaptiveController.RecordQuality(quality);
            }
        }

        public void RecordLatency(float latency)
        {
            if (enableAdaptiveSync)
            {
                adaptiveController.RecordLatency(latency);
            }
        }

        #endregion

        public OptimizationStats GetStats()
        {
            return new OptimizationStats
            {
                queuedEvents = eventQueue.Count + priorityQueue.Count,
                predictedStates = predictedStates.Count,
                bytesThisSecond = bandwidthController.GetBytesThisSecond(),
                eventsThisSecond = eventsThisSecond,
                currentSyncInterval = adaptiveController.GetSyncInterval(),
                averageLatency = adaptiveController.GetAverageLatency()
            };
        }

        public void Reset()
        {
            eventQueue.Clear();
            priorityQueue.Clear();
            predictedStates.Clear();
            bandwidthController.Reset();
            adaptiveController.Reset();
            eventsThisFrame = 0;
            eventsThisSecond = 0;
            secondTimer = 0f;
        }
    }

    public class PredictedState
    {
        public Vector3 position;
        public Vector3 velocity;
        public float timestamp;
        public float age;
    }

    public class BandwidthController
    {
        private int maxBytesPerSecond;
        private int bytesThisSecond;
        private float secondTimer;

        public BandwidthController(int maxBytes)
        {
            maxBytesPerSecond = maxBytes;
            bytesThisSecond = 0;
            secondTimer = 0f;
        }

        public bool CanSend(int byteCount)
        {
            return bytesThisSecond + byteCount <= maxBytesPerSecond;
        }

        public void RecordSend(int byteCount)
        {
            bytesThisSecond += byteCount;
        }

        public void Update()
        {
            secondTimer += Time.deltaTime;
            if (secondTimer >= 1f)
            {
                bytesThisSecond = 0;
                secondTimer = 0f;
            }
        }

        public int GetBytesThisSecond()
        {
            return bytesThisSecond;
        }

        public void Reset()
        {
            bytesThisSecond = 0;
            secondTimer = 0f;
        }
    }

    public class AdaptiveSyncController
    {
        private float minInterval;
        private float maxInterval;
        private float currentInterval;
        private Queue<float> latencyHistory;
        private Queue<int> qualityHistory;
        private int historySize = 10;

        public AdaptiveSyncController()
        {
            minInterval = BattleSyncConfig.MIN_SYNC_INTERVAL;
            maxInterval = BattleSyncConfig.MAX_SYNC_INTERVAL;
            currentInterval = BattleSyncConfig.DEFAULT_SYNC_INTERVAL;
            latencyHistory = new Queue<float>();
            qualityHistory = new Queue<int>();
        }

        public void Update()
        {
            if (latencyHistory.Count > 0)
            {
                float avgLatency = GetAverageLatency();
                int avgQuality = GetAverageQuality();

                float targetInterval = Mathf.Lerp(minInterval, maxInterval, (5 - avgQuality) / 4f);

                if (avgLatency > 0.15f)
                {
                    targetInterval = Mathf.Min(targetInterval * 1.2f, maxInterval);
                }
                else if (avgLatency < 0.05f)
                {
                    targetInterval = Mathf.Max(targetInterval * 0.9f, minInterval);
                }

                currentInterval = Mathf.Lerp(currentInterval, targetInterval, 0.1f);
            }
        }

        public void RecordLatency(float latency)
        {
            latencyHistory.Enqueue(latency);
            while (latencyHistory.Count > historySize)
            {
                latencyHistory.Dequeue();
            }
        }

        public void RecordQuality(int quality)
        {
            qualityHistory.Enqueue(quality);
            while (qualityHistory.Count > historySize)
            {
                qualityHistory.Dequeue();
            }
        }

        public float GetSyncInterval()
        {
            return currentInterval;
        }

        public float GetAverageLatency()
        {
            if (latencyHistory.Count == 0) return 0f;

            float sum = 0f;
            foreach (var latency in latencyHistory)
            {
                sum += latency;
            }
            return sum / latencyHistory.Count;
        }

        public int GetAverageQuality()
        {
            if (qualityHistory.Count == 0) return 3;

            int sum = 0;
            foreach (var quality in qualityHistory)
            {
                sum += quality;
            }
            return sum / qualityHistory.Count;
        }

        public void Reset()
        {
            latencyHistory.Clear();
            qualityHistory.Clear();
            currentInterval = BattleSyncConfig.DEFAULT_SYNC_INTERVAL;
        }
    }

    public static class CompressionHelper
    {
        public static byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            using (var output = new System.IO.MemoryStream())
            {
                using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return compressedData;

            using (var input = new System.IO.MemoryStream(compressedData))
            using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new System.IO.MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
    }

    public struct OptimizationStats
    {
        public int queuedEvents;
        public int predictedStates;
        public int bytesThisSecond;
        public int eventsThisSecond;
        public float currentSyncInterval;
        public float averageLatency;
    }
}
