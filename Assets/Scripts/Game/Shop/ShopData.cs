using System.Collections.Generic;

namespace KingdomWar.Game.Shop
{
    public enum ShopItemType { Card, Chest, Gold, Gems }
    public enum CurrencyType { Gold, Gems }

    [System.Serializable]
    public class ShopItem
    {
        public string itemId;
        public string cardName;
        public CurrencyType currencyType;
        public int price;
        public int quantity;
        public ShopItemType itemType;
        public bool purchased;
    }

    [System.Serializable]
    public class ShopSaveData
    {
        public List<ShopItem> items = new List<ShopItem>();
        public string lastRefreshDate;
    }
}
