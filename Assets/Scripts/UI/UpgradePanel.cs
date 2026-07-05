using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using KingdomWar.Game.Cards;
using KingdomWar.HotUpdate;

namespace KingdomWar.UI
{
    public class upgradePanel : basePanel
    {
        public Image cardIcon;
        public Text cardNameText;
        public Text currentLevelText;
        public Text currentStatsText;
        public Text nextStatsText;
        public Text costText;
        public Button upgradeBtn;

        private CardData currentCard;
        private int currentLevel;
        private int nextLevel;

        protected override void Awake()
        {
            base.Awake();
            if (cardIcon == null)
                cardIcon = transform.Find("CardIcon")?.GetComponent<Image>();
            if (cardNameText == null)
                cardNameText = transform.Find("CardName")?.GetComponent<Text>();
            if (currentLevelText == null)
                currentLevelText = transform.Find("CurrentLevel")?.GetComponent<Text>();
            if (currentStatsText == null)
                currentStatsText = transform.Find("CurrentStats")?.GetComponent<Text>();
            if (nextStatsText == null)
                nextStatsText = transform.Find("NextStats")?.GetComponent<Text>();
            if (costText == null)
                costText = transform.Find("CostText")?.GetComponent<Text>();
            if (upgradeBtn == null)
                upgradeBtn = transform.Find("UpgradeBtn")?.GetComponent<Button>();

            if (upgradeBtn != null)
                upgradeBtn.onClick.AddListener(OnUpgradeClick);
        }

        public void Show(CardData card)
        {
            currentCard = card;
            RefreshDisplay();
            OnEnter();
        }

        private void RefreshDisplay()
        {
            if (currentCard == null) return;

            PlayerDataManager playerData = PlayerDataManager.Instance;
            currentLevel = playerData.GetCardLevel(currentCard.cardName);
            int maxLevel = CardUpgradeTable.GetMaxLevel();

            if (cardIcon != null && currentCard.cardIcon != null)
                cardIcon.sprite = currentCard.cardIcon;

            if (cardNameText != null)
                cardNameText.text = currentCard.cardName;

            if (currentLevelText != null)
                currentLevelText.text = $"Lv.{currentLevel}";

            nextLevel = Mathf.Min(currentLevel + 1, maxLevel);
            bool isMaxLevel = currentLevel >= maxLevel;

            string currentStats = BuildStatsString(currentCard, currentLevel);
            if (currentStatsText != null)
                currentStatsText.text = currentStats;

            if (nextStatsText != null)
            {
                if (isMaxLevel)
                {
                    nextStatsText.text = "MAX LEVEL";
                    nextStatsText.color = Color.yellow;
                }
                else
                {
                    string nextStats = BuildNextStatsString(currentCard, currentLevel, nextLevel);
                    nextStatsText.text = nextStats;
                    nextStatsText.color = Color.green;
                }
            }

            int requiredFragments = CardUpgradeTable.GetFragmentsRequired(currentLevel);
            int requiredGold = CardUpgradeTable.GetUpgradeCost(currentLevel);
            int ownedFragments = playerData.GetCardFragments(currentCard.cardName);
            int ownedGold = playerData.GetGold();

            if (costText != null)
            {
                if (isMaxLevel)
                {
                    costText.text = "MAX LEVEL";
                }
                else
                {
                    costText.text = $"Fragments: {ownedFragments}/{requiredFragments}  Gold: {requiredGold}";
                }
            }

            if (upgradeBtn != null)
            {
                upgradeBtn.interactable = !isMaxLevel && ownedFragments >= requiredFragments && ownedGold >= requiredGold;
            }
        }

        private string BuildStatsString(CardData card, int level)
        {
            switch (card.cardType)
            {
                case CardType.Unit:
                    int unitHp = card.unitData.GetHealthAtLevel(level);
                    int unitDmg = card.unitData.GetDamageAtLevel(level);
                    return $"HP: {unitHp}\nDMG: {unitDmg}";
                case CardType.Building:
                    int buildingHp = card.buildingData.GetHealthAtLevel(level);
                    int buildingDmg = card.buildingData.GetDamageAtLevel(level);
                    return $"HP: {buildingHp}\nDMG: {buildingDmg}";
                case CardType.Spell:
                    int spellDmg = card.spellData.GetDamageAtLevel(level);
                    return $"DMG: {spellDmg}";
                default:
                    return string.Empty;
            }
        }

        private string BuildNextStatsString(CardData card, int current, int next)
        {
            switch (card.cardType)
            {
                case CardType.Unit:
                    int curHp = card.unitData.GetHealthAtLevel(current);
                    int nextHp = card.unitData.GetHealthAtLevel(next);
                    int curDmg = card.unitData.GetDamageAtLevel(current);
                    int nextDmg = card.unitData.GetDamageAtLevel(next);
                    return $"HP: {curHp} -> {nextHp}\nDMG: {curDmg} -> {nextDmg}";
                case CardType.Building:
                    int curBHp = card.buildingData.GetHealthAtLevel(current);
                    int nextBHp = card.buildingData.GetHealthAtLevel(next);
                    int curBDmg = card.buildingData.GetDamageAtLevel(current);
                    int nextBDmg = card.buildingData.GetDamageAtLevel(next);
                    return $"HP: {curBHp} -> {nextBHp}\nDMG: {curBDmg} -> {nextBDmg}";
                case CardType.Spell:
                    int curDmgS = card.spellData.GetDamageAtLevel(current);
                    int nextDmgS = card.spellData.GetDamageAtLevel(next);
                    return $"DMG: {curDmgS} -> {nextDmgS}";
                default:
                    return string.Empty;
            }
        }

        private void OnUpgradeClick()
        {
            if (currentCard == null) return;

            bool success = CardUpgradeManager.Instance.UpgradeCard(currentCard);
            if (success)
            {
                if (upgradeBtn != null)
                    upgradeBtn.transform.DOScale(1.2f, 0.15f).SetLoops(2, LoopType.Yoyo).OnComplete(() =>
                    {
                        RefreshDisplay();
                    });
                else
                    RefreshDisplay();

                UIManager.Instance.CreatePromptMessageAsync($"Upgrade success! {currentCard.cardName} -> Lv.{currentLevel + 1}");
            }
            else
            {
                UIManager.Instance.CreatePromptMessageAsync("Upgrade failed!");
            }
        }
    }
}
