namespace KingdomWar.Core
{
    /// <summary>
    /// Service with explicit lifecycle management.
    /// Implementations: NetworkManager, AudioManager, BattleManager
    /// </summary>
    public interface IDisposableService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
    }
}
