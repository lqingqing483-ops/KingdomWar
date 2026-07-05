namespace KingdomWar.Game.AI
{
    // AI difficulty levels
    public enum AIDifficulty
    {
        Easy = 0,   // Slow reactions, poor placements
        Medium = 1, // Normal reactions, decent placements
        Hard = 2,   // Fast reactions, good placements
        Expert = 3  // Optimal play, counters player
    }

    // AI play style tendency (affects card choice)
    public enum AIPlayStyle
    {
        Balanced = 0,   // Mix of offense and defense
        Aggressive = 1, // Prefers offensive cards
        Defensive = 2   // Prefers defensive/building cards
    }
}
