namespace KingdomWar.Game.Data
{
    /// <summary>
    /// Marker interface for game services that can be registered in DI container.
    /// All services that were previously singletons should implement this.
    /// </summary>
    public interface IGameService
    {
        void Initialize();
        void Shutdown();
    }
}
