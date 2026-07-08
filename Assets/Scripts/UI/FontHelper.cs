using UnityEngine;

namespace KingdomWar.UI
{
    /// <summary>
    /// Provides the game UI font: loads supercell-magic_0.ttf from Resources/Fonts/
    /// with fallback to the built-in LegacyRuntime.ttf if the custom font is missing.
    /// </summary>
    public static class FontHelper
    {
        private static Font cachedFont;
        private static bool attemptedLoad;

        public static Font GetUIFont()
        {
            if (cachedFont != null) return cachedFont;
            if (attemptedLoad) return GetFallbackFont();

            attemptedLoad = true;
            Font font = Resources.Load<Font>("Fonts/supercell-magic_0");
            if (font != null)
            {
                cachedFont = font;
                Debug.Log("[FontHelper] Loaded supercell-magic_0.ttf");
                return cachedFont;
            }

            Debug.LogWarning("[FontHelper] supercell-magic_0.ttf not found, using LegacyRuntime fallback");
            return GetFallbackFont();
        }

        private static Font GetFallbackFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
