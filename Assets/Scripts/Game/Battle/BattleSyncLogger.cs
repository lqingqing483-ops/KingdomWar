using UnityEngine;
using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public enum BattleSyncLogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public class BattleSyncLogEntry
    {
        public long timestamp;
        public BattleSyncLogLevel level;
        public string category;
        public string message;
        public string stackTrace;
        public Dictionary<string, object> context;

        public BattleSyncLogEntry(BattleSyncLogLevel logLevel, string cat, string msg)
        {
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            level = logLevel;
            category = cat;
            message = msg;
            stackTrace = System.Environment.StackTrace;
            context = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            var time = System.DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("HH:mm:ss.fff");
            return $"[{time}][{level}][{category}] {message}";
        }
    }

    public class BattleSyncLogger : MonoBehaviour
    {
        public static BattleSyncLogger Instance { get; private set; }

        [Header("日志配置")]
        public BattleSyncLogLevel minLogLevel = BattleSyncLogLevel.Info;
        public int maxLogEntries = 1000;
        public bool logToConsole = true;
        public bool logToFile = false;
        public string logFileName = "battle_sync.log";

        private Queue<BattleSyncLogEntry> logEntries;
        private Dictionary<string, int> errorCounts;
        private Dictionary<string, float> lastErrorTime;
        private float errorThrottleInterval = 1f;

        public event System.Action<BattleSyncLogEntry> OnLogEntryAdded;
        public event System.Action<int> OnErrorCountChanged;

        public int TotalErrors { get; private set; }
        public int TotalWarnings { get; private set; }

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
            logEntries = new Queue<BattleSyncLogEntry>();
            errorCounts = new Dictionary<string, int>();
            lastErrorTime = new Dictionary<string, float>();
            TotalErrors = 0;
            TotalWarnings = 0;

            Debug.Log("[BattleSyncLogger] Initialized");
        }

        public void Log(BattleSyncLogLevel level, string category, string message, Dictionary<string, object> context = null)
        {
            if (level < minLogLevel)
                return;

            var entry = new BattleSyncLogEntry(level, category, message);

            if (context != null)
            {
                foreach (var kvp in context)
                {
                    entry.context[kvp.Key] = kvp.Value;
                }
            }

            logEntries.Enqueue(entry);

            while (logEntries.Count > maxLogEntries)
            {
                logEntries.Dequeue();
            }

            if (level == BattleSyncLogLevel.Error || level == BattleSyncLogLevel.Critical)
            {
                TotalErrors++;
                IncrementErrorCount(category);
                OnErrorCountChanged?.Invoke(TotalErrors);
            }
            else if (level == BattleSyncLogLevel.Warning)
            {
                TotalWarnings++;
            }

            OnLogEntryAdded?.Invoke(entry);

            if (logToConsole)
            {
                LogToConsole(entry);
            }

            if (logToFile)
            {
                LogToFile(entry);
            }
        }

        public void LogDebug(string category, string message, Dictionary<string, object> context = null)
        {
            Log(BattleSyncLogLevel.Debug, category, message, context);
        }

        public void LogInfo(string category, string message, Dictionary<string, object> context = null)
        {
            Log(BattleSyncLogLevel.Info, category, message, context);
        }

        public void LogWarning(string category, string message, Dictionary<string, object> context = null)
        {
            Log(BattleSyncLogLevel.Warning, category, message, context);
        }

        public void LogError(string category, string message, Dictionary<string, object> context = null)
        {
            if (!ShouldThrottleError(category))
            {
                Log(BattleSyncLogLevel.Error, category, message, context);
            }
        }

        public void LogCritical(string category, string message, Dictionary<string, object> context = null)
        {
            Log(BattleSyncLogLevel.Critical, category, message, context);
        }

        public void LogException(string category, System.Exception exception, Dictionary<string, object> context = null)
        {
            var entry = new BattleSyncLogEntry(BattleSyncLogLevel.Critical, category, exception.Message)
            {
                stackTrace = exception.StackTrace
            };

            if (context != null)
            {
                foreach (var kvp in context)
                {
                    entry.context[kvp.Key] = kvp.Value;
                }
            }

            entry.context["ExceptionType"] = exception.GetType().Name;

            logEntries.Enqueue(entry);
            TotalErrors++;
            OnLogEntryAdded?.Invoke(entry);

            if (logToConsole)
            {
                Debug.LogError($"[BattleSync][{category}] Exception: {exception.Message}\n{exception.StackTrace}");
            }
        }

        private bool ShouldThrottleError(string category)
        {
            if (!lastErrorTime.ContainsKey(category))
            {
                lastErrorTime[category] = Time.time;
                return false;
            }

            float timeSinceLastError = Time.time - lastErrorTime[category];
            if (timeSinceLastError < errorThrottleInterval)
            {
                return true;
            }

            lastErrorTime[category] = Time.time;
            return false;
        }

        private void IncrementErrorCount(string category)
        {
            if (!errorCounts.ContainsKey(category))
            {
                errorCounts[category] = 0;
            }
            errorCounts[category]++;
        }

        private void LogToConsole(BattleSyncLogEntry entry)
        {
            string formattedMessage = entry.ToString();

            switch (entry.level)
            {
                case BattleSyncLogLevel.Debug:
                case BattleSyncLogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case BattleSyncLogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case BattleSyncLogLevel.Error:
                case BattleSyncLogLevel.Critical:
                    Debug.LogError(formattedMessage);
                    break;
            }
        }

        private void LogToFile(BattleSyncLogEntry entry)
        {
            try
            {
                string logPath = System.IO.Path.Combine(Application.persistentDataPath, logFileName);
                string logLine = entry.ToString() + "\n";
                System.IO.File.AppendAllText(logPath, logLine);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write log file: {e.Message}");
            }
        }

        public List<BattleSyncLogEntry> GetLogEntries(BattleSyncLogLevel? filterLevel = null, string filterCategory = null)
        {
            var result = new List<BattleSyncLogEntry>();

            foreach (var entry in logEntries)
            {
                if (filterLevel.HasValue && entry.level < filterLevel.Value)
                    continue;

                if (!string.IsNullOrEmpty(filterCategory) && entry.category != filterCategory)
                    continue;

                result.Add(entry);
            }

            return result;
        }

        public Dictionary<string, int> GetErrorCounts()
        {
            return new Dictionary<string, int>(errorCounts);
        }

        public void ClearLogs()
        {
            logEntries.Clear();
            errorCounts.Clear();
            lastErrorTime.Clear();
            TotalErrors = 0;
            TotalWarnings = 0;
        }

        public string ExportLogs()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Battle Sync Log Export ===");
            sb.AppendLine($"Export Time: {System.DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Errors: {TotalErrors}");
            sb.AppendLine($"Total Warnings: {TotalWarnings}");
            sb.AppendLine();

            foreach (var entry in logEntries)
            {
                sb.AppendLine(entry.ToString());
            }

            return sb.ToString();
        }
    }

    public static class BattleSyncLoggerExtensions
    {
        public static void LogSyncEvent(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogInfo("Sync", message, context);
        }

        public static void LogSyncError(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogError("Sync", message, context);
        }

        public static void LogNetworkEvent(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogInfo("Network", message, context);
        }

        public static void LogNetworkError(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogError("Network", message, context);
        }

        public static void LogConflict(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogWarning("Conflict", message, context);
        }

        public static void LogPerformance(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogDebug("Performance", message, context);
        }

        public static void LogStateChange(this BattleSyncLogger logger, string message, Dictionary<string, object> context = null)
        {
            logger?.LogInfo("State", message, context);
        }
    }
}
