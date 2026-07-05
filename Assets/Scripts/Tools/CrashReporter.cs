using System;
using UnityEngine;

namespace KingdomWar.Tools
{
    /// <summary>
    /// Crash and error reporting service.
    /// Uses Bugly (tencent) for crash collection in release builds.
    /// Falls back to Debug.LogError if Bugly SDK is not available.
    /// 
    /// SETUP:
    /// 1. Download Bugly Unity SDK from: https://bugly.qq.com/docs/user-guide/unity-guide/
    /// 2. Import BuglyPlugin.unitypackage into the project
    /// 3. BuglyAgent will be auto-detected at runtime
    /// </summary>
    public static class CrashReporter
    {
        private static bool _initialized = false;
        private static bool _buglyAvailable = false;
        private const string BUGLY_APP_ID = "6617eaa29d";

        /// <summary>
        /// Initialize crash reporting. Call once at app startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            // Try to detect Bugly SDK by checking for BuglyAgent type
            Type buglyType = Type.GetType("BuglyAgent, Assembly-CSharp");
            if (buglyType != null)
            {
                try
                {
                    // BuglyAgent.InitWithAppId(BUGLY_APP_ID);
                    var initMethod = buglyType.GetMethod("InitWithAppId",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (initMethod != null)
                    {
                        initMethod.Invoke(null, new object[] { BUGLY_APP_ID });
                        _buglyAvailable = true;
                        Debug.Log("[CrashReporter] Bugly initialized successfully");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CrashReporter] Bugly init failed: {ex.Message}");
                    _buglyAvailable = false;
                }
            }
            else
            {
                Debug.Log("[CrashReporter] Bugly SDK not found. Crash reports will use Debug.LogError fallback.");
                _buglyAvailable = false;
            }

            // Hook unhandled exceptions (works with or without Bugly)
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        /// <summary>
        /// Report an error. In release builds with Bugly, this sends to cloud.
        /// In editor/debug, uses Debug.LogError.
        /// </summary>
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            if (_buglyAvailable)
            {
                // BuglyAgent.PrintLog(LogSeverityLevel.Error, message);
                try
                {
                    Type buglyType = Type.GetType("BuglyAgent, Assembly-CSharp");
                    if (buglyType != null)
                    {
                        var printLog = buglyType.GetMethod("PrintLog",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (printLog != null)
                        {
                            // LogSeverityLevel.Error = 3
                            printLog.Invoke(null, new object[] { 3, message });
                        }
                    }
                }
                catch { /* silently fall through */ }
            }
            else
            {
                Debug.LogError(message, context);
            }
        }

        /// <summary>
        /// Report an exception with stack trace.
        /// </summary>
        public static void LogException(Exception ex, UnityEngine.Object context = null)
        {
            if (_buglyAvailable)
            {
                // BuglyAgent.ReportException(ex, "PlayerDataManager.cs", "LoadData");
                try
                {
                    Type buglyType = Type.GetType("BuglyAgent, Assembly-CSharp");
                    if (buglyType != null)
                    {
                        var reportException = buglyType.GetMethod("ReportException",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (reportException != null)
                        {
                            reportException.Invoke(null, new object[] { ex, ex.Source ?? "Unknown", ex.StackTrace ?? "" });
                        }
                    }
                }
                catch { /* silently fall through */ }
            }
            else
            {
                Debug.LogException(ex, context);
            }
        }

        /// <summary>
        /// Set a user identifier for this session (e.g. player ID).
        /// Shows in Bugly dashboard to identify affected users.
        /// </summary>
        public static void SetUserId(string userId)
        {
            if (_buglyAvailable)
            {
                try
                {
                    Type buglyType = Type.GetType("BuglyAgent, Assembly-CSharp");
                    if (buglyType != null)
                    {
                        var setUserId = buglyType.GetMethod("SetUserId",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (setUserId != null)
                        {
                            setUserId.Invoke(null, new object[] { userId });
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Set a custom tag for the current session (e.g. "scene=Main").
        /// </summary>
        public static void SetTag(string key, string value)
        {
            if (_buglyAvailable)
            {
                try
                {
                    Type buglyType = Type.GetType("BuglyAgent, Assembly-CSharp");
                    if (buglyType != null)
                    {
                        var setTag = buglyType.GetMethod("SetTag",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (setTag != null)
                        {
                            setTag.Invoke(null, new object[] { $"{key}={value}" });
                        }
                    }
                }
                catch { }
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception && _buglyAvailable)
            {
                try
                {
                    Type buglyType = Type.GetType("BuglyAgent, Assembly-CSharp");
                    if (buglyType != null)
                    {
                        var reportException = buglyType.GetMethod("ReportException",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (reportException != null)
                        {
                            reportException.Invoke(null, new object[] { new Exception(condition), "", stackTrace ?? "" });
                        }
                    }
                }
                catch { }
            }
        }
    }
}
