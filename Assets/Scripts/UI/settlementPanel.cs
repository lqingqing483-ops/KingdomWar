using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using KingdomWar.Game;
using KingdomWar.Game.Battle;
using KingdomWar.Game.Arena;
using KingdomWar.Server;

namespace KingdomWar.UI
{
    public class settlementPanel : basePanel
    {
        public Button exitButton;
        public List<Transform> honScoreList;
        public List<Transform> lanScoreList;
        public List<Transform> winTra;

        // NEW: Trophy display
        public Text trophyChangeText;    // e.g. "+30" or "-20"
        public Text trophyCountText;     // e.g. "当前奖杯: 1250"
        public Text arenaNameText;       // e.g. "黄金竞技场"

        protected override void Start()
        {
            base.Start();
            exitButton.onClick.AddListener(() =>
            {
                UIManager.Instance.ClearPanel();
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.LeaveRoomAndReturnToMainScene();
                    return;
                }
                SceneManager.LoadScene(SceneNames.MainMenu);
            });
        }

        public void Init(int id)
        {
            // Auto-load sprites from imported resources if serialized references are null
            AutoLoadSprites();

            if (id == 1)
            {
                winTra[0].gameObject.SetActive(true);
                winTra[1].gameObject.SetActive(false);
            }
            else
            {
                winTra[0].gameObject.SetActive(false);
                winTra[1].gameObject.SetActive(true);
            }

            // NEW: Show trophy result
            UpdateTrophyDisplay();
        }

        private void AutoLoadSprites()
        {
            // Assign BattleResult sprites to child Image components if they have no sprite
            var images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.sprite != null) continue;
                switch (img.name.ToLower())
                {
                    case "bg":
                    case "background":
                        img.sprite = UIResourceHelper.LoadBattleResultBg();
                        break;
                    case "winicon":
                        img.sprite = UIResourceHelper.LoadWinSprite(1);
                        break;
                    case "loseicon":
                        img.sprite = UIResourceHelper.LoadLoseSprite();
                        break;
                    case "chesticon":
                    case "chest":
                        img.sprite = UIResourceHelper.LoadChestIcon();
                        break;
                    case "rewardicon":
                    case "reward":
                        img.sprite = UIResourceHelper.LoadRewardIcon();
                        break;
                }
                if (img.sprite != null)
                    img.enabled = true;
            }
        }

        // NEW: Private helper to display trophy result
        private void UpdateTrophyDisplay()
        {
            if (BattleManager.Instance != null && BattleManager.Instance.LastTrophyResult.HasValue)
            {
                TrophyChangeResult result = BattleManager.Instance.LastTrophyResult.Value;

                if (trophyChangeText != null)
                {
                    string sign = result.trophiesGained >= 0 ? "+" : "";
                    trophyChangeText.text = $"{sign}{result.trophiesGained}";
                    trophyChangeText.color = result.trophiesGained >= 0 ? Color.green : Color.red;
                }

                if (trophyCountText != null)
                {
                    trophyCountText.text = $"当前奖杯: {result.newTrophyCount}";
                }

                if (arenaNameText != null)
                {
                    ArenaDefinition arena = ArenaConfig.GetArena(result.newArenaId);
                    arenaNameText.text = arena.arenaName;
                }
            }
            else
            {
                // No trophy change (e.g. training mode)
                if (trophyChangeText != null)
                    trophyChangeText.text = "--";
                if (trophyCountText != null)
                    trophyCountText.text = "";
                if (arenaNameText != null)
                    arenaNameText.text = "";
            }
        }
    }
}
