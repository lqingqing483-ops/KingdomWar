using System.Collections.Generic;
using UnityEngine;

namespace KingdomWar.Game.AI
{
    [System.Serializable]
    public class AIDeckData
    {
        public string deckName;             // e.g. "Giant Beatdown"
        public AIDifficulty minDifficulty;  // minimum difficulty that uses this deck
        public List<string> cardNames;      // 8 card names matching CardData.cardName
    }
}
