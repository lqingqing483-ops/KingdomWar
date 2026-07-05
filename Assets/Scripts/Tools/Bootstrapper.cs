using UnityEngine;
using KingdomWar.Core;

namespace KingdomWar.Tools
{
    /// <summary>
    /// Runs once at game startup to initialize all cross-cutting services.
    /// Uses RuntimeInitializeOnLoadMethod to ensure early execution.
    /// </summary>
    public static class Bootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // 1. Initialize crash reporting
            CrashReporter.Initialize();

            // 2. Register core services with ServiceLocator
            RegisterCoreServices();

            Debug.Log("[Bootstrapper] Services initialized");
        }

        private static void RegisterCoreServices()
        {
            // Register BattleManager if it exists (may be null if not yet created)
            if (KingdomWar.Game.Battle.BattleManager.Instance != null)
            {
                ServiceLocator.Register<KingdomWar.Game.Battle.IBattleManager>(
                    KingdomWar.Game.Battle.BattleManager.Instance);
            }

            // More services will be registered here as they implement interfaces
            // ServiceLocator.Register<IUIManager>(UIManager.Instance);
            // ServiceLocator.Register<INetworkService>(NetworkManager.Instance);
        }
    }
}
