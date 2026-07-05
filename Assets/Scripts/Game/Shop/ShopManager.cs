using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game.Cards;
using KingdomWar.HotUpdate;
using KingdomWar.Game.Config;

namespace KingdomWar.Game.Shop
{
    public class ShopManager : MonoBehaviour
    {
        private static ShopManager instance;
        public static ShopManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("ShopManager");
                    instance = obj.AddComponent<ShopManager>();
                }
                return instance;
            }
        }

        private List<ShopItem> dailyItems = new List<ShopItem>();
        private const string LastRefreshKey = "Shop_LastRefreshDate";
        private const string ShopDataKey = "Shop_DailyItems";

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                CheckDailyRefresh();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void CheckDailyRefresh()
        {
            string lastRefresh = PlayerPrefs.GetString(LastRefreshKey, "");
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (lastRefresh != today)
            {
                RefreshDailyShop();
            }
            else
            {
                LoadDailyItems();
            }
        }

        public void RefreshDailyShop()
        {
            dailyItems.Clear();
            var cardDatabase = CardDatabase.Instance;
            var allCards = cardDatabase.GetAllCards();

            if (allCards == null || allCards.Count == 0)
            {
                Debug.LogWarning("CardDatabase is empty, cannot generate shop items");
                return;
            }

            List<CardData> shuffled = new List<CardData>(allCards);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                CardData temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            int count = Mathf.Min(EconomyBalanceSO.Instance.shopDailyCardCount, shuffled.Count);
            for (int i = 0; i < count; i++)
            {
                ShopItem item = new ShopItem
                {
                    itemId = Guid.NewGuid().ToString(),
                    cardName = shuffled[i].cardName,
                    currencyType = CurrencyType.Gold,
                    price = EconomyBalanceSO.Instance.shopCardBasePrice + (i * EconomyBalanceSO.Instance.shopCardPriceMultiplier),
                    quantity = 1,
                    itemType = ShopItemType.Card,
                    purchased = false
                };
                dailyItems.Add(item);
            }

            PlayerPrefs.SetString(LastRefreshKey, DateTime.Now.ToString("yyyy-MM-dd"));
            SaveDailyItems();
            PlayerPrefs.Save();
        }

        private void SaveDailyItems()
        {
            ShopSaveData saveData = new ShopSaveData
            {
                items = dailyItems,
                lastRefreshDate = DateTime.Now.ToString("yyyy-MM-dd")
            };
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(ShopDataKey, json);
        }

        private void LoadDailyItems()
        {
            string json = PlayerPrefs.GetString(ShopDataKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                ShopSaveData saveData = JsonUtility.FromJson<ShopSaveData>(json);
                if (saveData != null)
                {
                    dailyItems = saveData.items;
                }
            }
        }

        public List<ShopItem> GetDailyItems()
        {
            if (dailyItems == null || dailyItems.Count == 0)
            {
                RefreshDailyShop();
            }
            return new List<ShopItem>(dailyItems);
        }

        public bool PurchaseItem(ShopItem item)
        {
            if (item == null)
            {
                Debug.LogError("Cannot purchase null item");
                return false;
            }

            ShopItem shopItem = dailyItems.Find(i => i.itemId == item.itemId);
            if (shopItem == null)
            {
                Debug.LogError("Item not found in daily shop");
                return false;
            }

            if (shopItem.purchased)
            {
                Debug.LogWarning("Item already purchased");
                return false;
            }

            var playerData = PlayerDataManager.Instance;

            bool success = item.currencyType == CurrencyType.Gold
                ? playerData.SpendGold(item.price)
                : playerData.SpendGems(item.price);

            if (!success)
            {
                Debug.LogWarning("Not enough currency");
                return false;
            }

            if (item.itemType == ShopItemType.Card && !string.IsNullOrEmpty(item.cardName))
            {
                playerData.AddCard(item.cardName);
            }

            shopItem.purchased = true;
            SaveDailyItems();
            return true;
        }
    }
}
