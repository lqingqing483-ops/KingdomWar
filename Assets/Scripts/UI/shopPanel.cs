using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.Game.Shop;
using KingdomWar.Game.Cards;

namespace KingdomWar.UI
{
    public class shopPanel : basePanel
    {
        public Transform itemContainer;
        public GameObject itemPrefab;

        private List<GameObject> itemObjects = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();
            if (itemContainer == null)
                itemContainer = transform.Find("ItemContainer");
            if (itemContainer == null)
                itemContainer = transform;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            ClearItems();
            List<ShopItem> items = ShopManager.Instance.GetDailyItems();
            foreach (ShopItem item in items)
            {
                CreateItemUI(item);
            }
        }

        private void CreateItemUI(ShopItem item)
        {
            GameObject itemObj;
            if (itemPrefab != null)
            {
                itemObj = Object.Instantiate(itemPrefab, itemContainer);
            }
            else
            {
                itemObj = new GameObject("ShopItem_" + item.cardName);
                itemObj.transform.SetParent(itemContainer, false);
                Image bg = itemObj.AddComponent<Image>();
                bg.color = new Color(0.15f, 0.15f, 0.25f, 0.9f);
                RectTransform rt = itemObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(200, 120);
            }

            SetupItemUI(itemObj, item);
            itemObjects.Add(itemObj);
        }

        private void SetupItemUI(GameObject obj, ShopItem item)
        {
            Text nameText = FindChildComponent<Text>(obj.transform, "NameText");
            if (nameText == null)
            {
                GameObject nameObj = new GameObject("NameText");
                nameObj.transform.SetParent(obj.transform, false);
                nameText = nameObj.AddComponent<Text>();
                nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameText.fontSize = 18;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = Color.white;
                RectTransform nrt = nameObj.GetComponent<RectTransform>();
                nrt.sizeDelta = new Vector2(180, 30);
                nrt.anchoredPosition = new Vector2(0, 35);
            }
            nameText.text = item.cardName;

            Text priceText = FindChildComponent<Text>(obj.transform, "PriceText");
            if (priceText == null)
            {
                GameObject priceObj = new GameObject("PriceText");
                priceObj.transform.SetParent(obj.transform, false);
                priceText = priceObj.AddComponent<Text>();
                priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                priceText.fontSize = 16;
                priceText.alignment = TextAnchor.MiddleCenter;
                priceText.color = Color.yellow;
                RectTransform prt = priceObj.GetComponent<RectTransform>();
                prt.sizeDelta = new Vector2(180, 30);
                prt.anchoredPosition = new Vector2(0, 5);
            }

            if (item.purchased)
            {
                priceText.text = "\u5DF2\u8D2D\u4E70";
            }
            else
            {
                string currency = item.currencyType == CurrencyType.Gold ? "\u91D1\u5E01" : "\u5B9D\u77F3";
                priceText.text = $"{item.price} {currency}";
            }

            Button buyBtn = FindChildComponent<Button>(obj.transform, "BuyButton");
            if (buyBtn == null)
            {
                GameObject btnObj = new GameObject("BuyButton");
                btnObj.transform.SetParent(obj.transform, false);
                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = item.purchased ? Color.gray : new Color(0.2f, 0.6f, 1f, 1f);
                buyBtn = btnObj.AddComponent<Button>();
                RectTransform brt = btnObj.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(100, 30);
                brt.anchoredPosition = new Vector2(0, -25);

                GameObject btnTextObj = new GameObject("Text");
                btnTextObj.transform.SetParent(btnObj.transform, false);
                Text btnText = btnTextObj.AddComponent<Text>();
                btnText.text = item.purchased ? "\u5DF2\u62E5\u6709" : "\u8D2D\u4E70";
                btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                btnText.fontSize = 14;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;
                RectTransform btrt = btnTextObj.GetComponent<RectTransform>();
                btrt.sizeDelta = new Vector2(100, 30);
                btrt.anchoredPosition = Vector2.zero;
            }
            buyBtn.interactable = !item.purchased;

            ShopItem capturedItem = item;
            GameObject capturedObj = obj;
            buyBtn.onClick.AddListener(() => OnBuyClicked(capturedItem, capturedObj));
        }

        private void OnBuyClicked(ShopItem item, GameObject obj)
        {
            bool success = ShopManager.Instance.PurchaseItem(item);
            if (success)
            {
                UIManager.Instance.CreatePromptMessageAsync($"\u6210\u529F\u8D2D\u4E70 {item.cardName}\uFF01");
            }
            else
            {
                UIManager.Instance.CreatePromptMessageAsync("\u8D2D\u4E70\u5931\u8D25\uFF01\u8D27\u5E01\u4E0D\u8DB3\u6216\u5DF2\u8D2D\u4E70\u3002");
            }

            UpdateItemUI(obj, item);
        }

        private void UpdateItemUI(GameObject obj, ShopItem item)
        {
            Text priceText = FindChildComponent<Text>(obj.transform, "PriceText");
            if (priceText != null)
            {
                if (item.purchased)
                {
                    priceText.text = "\u5DF2\u8D2D\u4E70";
                }
                else
                {
                    string currency = item.currencyType == CurrencyType.Gold ? "\u91D1\u5E01" : "\u5B9D\u77F3";
                    priceText.text = $"{item.price} {currency}";
                }
            }

            Button buyBtn = FindChildComponent<Button>(obj.transform, "BuyButton");
            if (buyBtn != null)
            {
                buyBtn.interactable = !item.purchased;
                Image btnImg = buyBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = item.purchased ? Color.gray : new Color(0.2f, 0.6f, 1f, 1f);
                }
                Text btnText = FindChildComponent<Text>(buyBtn.transform, "Text");
                if (btnText != null)
                {
                    btnText.text = item.purchased ? "\u5DF2\u62E5\u6709" : "\u8D2D\u4E70";
                }
            }
        }

        private void ClearItems()
        {
            foreach (GameObject obj in itemObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            itemObjects.Clear();
        }

        private T FindChildComponent<T>(Transform parent, string name) where T : Component
        {
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
