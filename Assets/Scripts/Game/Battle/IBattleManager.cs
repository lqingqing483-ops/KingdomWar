using System.Collections.Generic;

namespace KingdomWar.Game.Battle
{
    public interface IBattleManager
    {
        bool IsNetworkBattle { get; }
        byte LocalPlayerTeam { get; }
        IReadOnlyList<Unit> Units { get; }
        IReadOnlyList<Building> Buildings { get; }
        void AddUnit(Unit unit);
        void AddBuilding(Building building);
        void EndBattle();
    }
}
