using UnityEngine;

namespace KingdomWar.Game.Arena
{
    public static class ArenaTestHelper
    {
        /// <summary>
        /// Creates a temporary TrophyManager GameObject for testing.
        /// Caller must destroy it after test.
        /// </summary>
        public static TrophyManager CreateTestTrophyManager()
        {
            GameObject obj = new GameObject("TestTrophyManager");
            TrophyManager manager = obj.AddComponent<TrophyManager>();
            return manager;
        }
        
        /// <summary>
        /// Clean up test manager.
        /// </summary>
        public static void DestroyTestTrophyManager(TrophyManager manager)
        {
            if (manager != null && manager.gameObject != null)
            {
                Object.DestroyImmediate(manager.gameObject);
            }
        }
    }
}
