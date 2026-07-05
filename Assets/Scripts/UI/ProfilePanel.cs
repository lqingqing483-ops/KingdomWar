using UnityEngine;
using UnityEngine.UI;
using KingdomWar.HotUpdate;
using KingdomWar.Game.Arena;

namespace KingdomWar.UI
{
    public class ProfilePanel : basePanel
    {
        public Text nicknameText;
        public Text arenaText;
        public Text trophiesText;
        public Text winsText;
        public Text lossesText;
        public Text drawsText;
        public Text winRateText;
        public Text totalBattlesText;
        public Button closeButton;

        protected override void Start()
        {
            base.Start();

            if (closeButton != null)
                closeButton.onClick.AddListener(() =>
                {
                    UIManager.Instance.PopPanel();
                });

            UpdateDisplay();
        }

        public override void OnEnter()
        {
            base.OnEnter();
            gameObject.SetActive(true);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var data = PlayerDataManager.Instance;
            int trophies = data.GetTrophies();
            ArenaDefinition arena = ArenaConfig.GetArenaByTrophies(trophies);

            if (nicknameText != null)
                nicknameText.text = data.GetNickname();
            if (arenaText != null)
                arenaText.text = arena.arenaName;
            if (trophiesText != null)
                trophiesText.text = trophies.ToString();
            if (winsText != null)
                winsText.text = data.GetTotalWins().ToString();
            if (lossesText != null)
                lossesText.text = data.GetTotalLosses().ToString();
            if (drawsText != null)
                drawsText.text = data.GetTotalDraws().ToString();
            if (winRateText != null)
                winRateText.text = data.GetWinRate().ToString("F1") + "%";
            if (totalBattlesText != null)
                totalBattlesText.text = data.GetTotalBattles().ToString();
        }
    }
}
