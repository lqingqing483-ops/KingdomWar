using UnityEngine;
using KingdomWar.HotUpdate;

namespace KingdomWar.Game.Cards
{
    public class CardUpgradeManager : MonoBehaviour
    {
        public static CardUpgradeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool CanUpgrade(CardData card)
        {
            if (card == null) return false;

            PlayerDataManager playerData = PlayerDataManager.Instance;
            int currentLevel = playerData.GetCardLevel(card.cardName);

            if (currentLevel >= CardUpgradeTable.GetMaxLevel()) return false;

            int requiredFragments = CardUpgradeTable.GetFragmentsRequired(currentLevel);
            int requiredGold = CardUpgradeTable.GetUpgradeCost(currentLevel);

            int ownedFragments = playerData.GetCardFragments(card.cardName);
            int ownedGold = playerData.GetGold();

            return ownedFragments >= requiredFragments && ownedGold >= requiredGold;
        }

        public bool UpgradeCard(CardData card)
        {
            if (!CanUpgrade(card)) return false;

            PlayerDataManager playerData = PlayerDataManager.Instance;
            int currentLevel = playerData.GetCardLevel(card.cardName);
            int requiredFragments = CardUpgradeTable.GetFragmentsRequired(currentLevel);
            int requiredGold = CardUpgradeTable.GetUpgradeCost(currentLevel);

            if (!playerData.SpendFragments(card.cardName, requiredFragments)) return false;
            if (!playerData.SpendGold(requiredGold)) return false;

            playerData.SetCardLevel(card.cardName, currentLevel + 1);

            Debug.Log($"Card upgraded: {card.cardName} to level {currentLevel + 1}");
            return true;
        }

        public int GetNextLevelHealth(CardData card)
        {
            if (card == null) return 0;

            int currentLevel = PlayerDataManager.Instance.GetCardLevel(card.cardName);
            int nextLevel = Mathf.Min(currentLevel + 1, CardUpgradeTable.GetMaxLevel());

            switch (card.cardType)
            {
                case CardType.Unit:
                    return card.unitData.GetHealthAtLevel(nextLevel);
                case CardType.Building:
                    return card.buildingData.GetHealthAtLevel(nextLevel);
                default:
                    return 0;
            }
        }

        public int GetNextLevelDamage(CardData card)
        {
            if (card == null) return 0;

            int currentLevel = PlayerDataManager.Instance.GetCardLevel(card.cardName);
            int nextLevel = Mathf.Min(currentLevel + 1, CardUpgradeTable.GetMaxLevel());

            switch (card.cardType)
            {
                case CardType.Unit:
                    return card.unitData.GetDamageAtLevel(nextLevel);
                case CardType.Spell:
                    return card.spellData.GetDamageAtLevel(nextLevel);
                case CardType.Building:
                    return card.buildingData.GetDamageAtLevel(nextLevel);
                default:
                    return 0;
            }
        }
    }
}
