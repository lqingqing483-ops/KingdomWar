using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.HotUpdate;
using KingdomWar.Game.Arena;
namespace KingdomWar.UI
{
public class mainPanel : basePanel
{
    public Toggle battleToggle;
    public Toggle shopToggle;
    public Toggle deckToggle;
    public Toggle profileToggle;
    public Text GoldsText;
    public Text GemsText;

    // NEW: Trophy/Arena display
    public Text trophiesText;    // shows trophy count
    public Text arenaText;       // shows arena name

    private PlayerDataManager cachedPlayerData;

    protected virtual void Start()
    {
        base.Start(); 

        // This panel is pre-placed in the scene, not created by UIManager.
        // Ensure visibility manually since we never get OnEnter() from a self-push.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
        }

        cachedPlayerData = PlayerDataManager.Instance;
        if (cachedPlayerData == null)
        {
            Debug.LogError("[mainPanel] PlayerDataManager.Instance is null!");
            return;
        }

        deckToggle.onValueChanged.AddListener((isOn) =>
        {
            UpdateToggleState(deckToggle, isOn);
            if (isOn)
            {
                PushPanel(UIPanelType.deckPanel);
            }
        });
        battleToggle.onValueChanged.AddListener((isOn) =>
        {
            UpdateToggleState(battleToggle, isOn);
            if (isOn)
            {
                PushPanel(UIPanelType.battlePanel);
            }
        });
        shopToggle.onValueChanged.AddListener((isOn) =>
        {
            UpdateToggleState(shopToggle, isOn);
            if (isOn)
            {
                PushPanel(UIPanelType.shopPanel);
            }
        });
        if (profileToggle != null)
        {
            profileToggle.onValueChanged.AddListener((isOn) =>
            {
                UpdateToggleState(profileToggle, isOn);
                if (isOn)
                {
                    PushPanel(UIPanelType.profilePanel);
                }
            });
        }
        //battleToggle.isOn = true;

        // 初始化金币和宝石显示
        UpdateGoldDisplay();
        UpdateGemsDisplay();
        // NEW
        UpdateTrophyDisplay();
        // 订阅金币和宝石变化事?
        PlayerDataManager.Instance.OnGoldChanged += OnGoldChanged;
        PlayerDataManager.Instance.OnGemsChanged += OnGemsChanged;
        // NEW
        cachedPlayerData.OnTrophiesChanged += OnTrophiesChanged;
        PlayerDataManager.Instance.OnBattleStatsChanged += OnBattleStatsChanged;

        // Push battlePanel as the default sub-view on top of mainPanel
        // Do NOT push mainPanel itself — it's pre-placed in the scene and self-push
        // would trigger Instantiate → Clone → duplicate panel bug.
        UIManager.Instance.PushPanel(UIPanelType.battlePanel);
    }

    private void OnDestroy()
    {
        // 取消订阅事件，防止内存泄?
        if (cachedPlayerData != null)
        {
            cachedPlayerData.OnGoldChanged -= OnGoldChanged;
            cachedPlayerData.OnGemsChanged -= OnGemsChanged;
            // NEW
            cachedPlayerData.OnTrophiesChanged -= OnTrophiesChanged;
        }
        PlayerDataManager.Instance.OnBattleStatsChanged -= OnBattleStatsChanged;
    }

    private void OnGoldChanged(int newGold)
    {
        UpdateGoldDisplay();
    }

    private void OnGemsChanged(int newGems)
    {
        UpdateGemsDisplay();
    }

    // NEW
    private void OnTrophiesChanged(int newTrophies)
    {
        UpdateTrophyDisplay();
    }

    private void OnBattleStatsChanged(int wins, int losses, int draws)
    {
        // Stats changed — could update mainPanel display if needed
        Debug.Log($"[Profile] Stats updated: {wins}W/{losses}L/{draws}D");
    }

    private void UpdateGoldDisplay()
    {
        if (GoldsText != null)
        {
            GoldsText.text = PlayerDataManager.Instance.GetGold().ToString();
        }
    }

    private void UpdateGemsDisplay()
    {
        if (GemsText != null)
        {
            GemsText.text = PlayerDataManager.Instance.GetGems().ToString();
        }
    }

    // NEW: Update trophy and arena display
    private void UpdateTrophyDisplay()
    {
        if (trophiesText != null)
        {
            trophiesText.text = PlayerDataManager.Instance.GetTrophies().ToString();
        }
        if (arenaText != null)
        {
            int trophies = PlayerDataManager.Instance.GetTrophies();
            ArenaDefinition arena = ArenaConfig.GetArenaByTrophies(trophies);
            arenaText.text = arena.arenaName;
        }
    }

    private void UpdateToggleState(Toggle toggle, bool isOn)
    {
        if (isOn)
        {
            Debug.Log("Selected: " + toggle.name);
            // 选中时，设置缩放�?.8*0.8*0.8，向上位�?5
            toggle.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            toggle.transform.localPosition = new Vector3(toggle.transform.localPosition.x, toggle.transform.localPosition.y + 18, toggle.transform.localPosition.z);
        }
        else
        {
            // 未选中时，恢复缩放�?.7*0.7*0.7，恢复位�?
            toggle.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            toggle.transform.localPosition = new Vector3(toggle.transform.localPosition.x, toggle.transform.localPosition.y - 18, toggle.transform.localPosition.z);
        }
    }

    private void PushPanel(UIPanelType panelType)
    {
        UIManager.Instance.PopPanel();
        UIManager.Instance.PushPanel(panelType);
    }

    public override void OnPause()
    {
        
    }
}
}
