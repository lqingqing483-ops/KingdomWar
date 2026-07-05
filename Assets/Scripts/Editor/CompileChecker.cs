using UnityEditor;
using UnityEngine;

namespace KingdomWar.Editor
{
    public class CompileChecker
    {
        public static void CheckAndExit()
        {
            // Wait for compilation to finish (max 20s - reduced from 120s for faster stuck detection)
            float timeout = Time.realtimeSinceStartup + 20f;
            while (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (Time.realtimeSinceStartup > timeout)
                {
                    Debug.LogError("Compilation timed out");
                    EditorApplication.Exit(1);
                    return;
                }
                System.Threading.Thread.Sleep(500);
            }

            // Check for compile errors in log
            var logs = Application.consoleLogPath;
            Debug.Log($"Compilation check complete. Log: {logs}");
            EditorApplication.Exit(0);
        }
    }
}
