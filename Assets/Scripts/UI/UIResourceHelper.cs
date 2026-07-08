using UnityEngine;

namespace KingdomWar.UI
{
    /// <summary>
    /// Provides typed access to imported Heroes Arena UI resources.
    /// All assets live under Assets/Resources/UI/ and are loaded at runtime.
    /// </summary>
    public static class UIResourceHelper
    {
        // ── Battle Result ────────────────────────────────────────────
        public static Sprite LoadWinSprite(int index)
        {
            return Resources.Load<Sprite>($"UI/BattleResult/win_{index}");
        }
        public static Sprite LoadLoseSprite()
        {
            return Resources.Load<Sprite>("UI/BattleResult/Lose");
        }
        public static Sprite LoadVSSprite()
        {
            return Resources.Load<Sprite>("UI/BattleResult/VS");
        }
        public static Sprite LoadBattleResultBg()
        {
            return Resources.Load<Sprite>("UI/BattleResult/background");
        }
        public static Sprite LoadRewardIcon()
        {
            return Resources.Load<Sprite>("UI/BattleResult/reward");
        }
        public static Sprite LoadExpIcon()
        {
            return Resources.Load<Sprite>("UI/BattleResult/exp");
        }
        public static Sprite LoadChestIcon(string variant = "ruong_nau")
        {
            return Resources.Load<Sprite>($"UI/BattleResult/{variant}");
        }

        // ── Shop ─────────────────────────────────────────────────────
        public static Sprite LoadGoldIcon(int tier = 1)
        {
            return Resources.Load<Sprite>($"UI/Shop/gold{tier}");
        }
        public static Sprite LoadGemIcon()
        {
            return Resources.Load<Sprite>("UI/Shop/gema");
        }
        public static Sprite LoadShopButtonNormal()
        {
            return Resources.Load<Sprite>("UI/Shop/nap_normal");
        }
        public static Sprite LoadShopButtonDown()
        {
            return Resources.Load<Sprite>("UI/Shop/nap_down");
        }

        // ── Cards ────────────────────────────────────────────────────
        public static Sprite LoadCardIcon(string cardId)
        {
            return Resources.Load<Sprite>($"UI/CardIcons/{cardId}");
        }
        public static Sprite LoadCardIcon(int cardId)
        {
            return Resources.Load<Sprite>($"UI/CardIcons/{cardId}");
        }

        // ── Icons ────────────────────────────────────────────────────
        public static Sprite LoadIcon(string name)
        {
            return Resources.Load<Sprite>($"UI/Icons/{name}");
        }
        public static Sprite LoadAttackIcon()
        {
            return LoadIcon("attack-icon");
        }
        public static Sprite LoadHealthIcon()
        {
            return LoadIcon("heart-icon1");
        }
        public static Sprite LoadDamageIcon()
        {
            return LoadIcon("Icon-Damage");
        }
        public static Sprite LoadSwordIcon()
        {
            return LoadIcon("sword-icon");
        }
    }
}
