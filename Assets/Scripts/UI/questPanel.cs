using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.Game.Quests;

namespace KingdomWar.UI
{
    public class questPanel : basePanel
    {
        public Transform questContainer;
        public GameObject questItemPrefab;

        private List<GameObject> questObjects = new List<GameObject>();

        protected override void Awake()
        {
            base.Awake();

            RectTransform rt = GetComponent<RectTransform>();
            if (rt.sizeDelta == Vector2.zero)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(800f, 900f);
                rt.anchoredPosition = Vector2.zero;
            }

            if (questContainer == null)
                questContainer = transform.Find("QuestList");
            if (questContainer == null)
                questContainer = transform;

            // Create CloseBtn for basePanel.Start() to find
            Transform existing = transform.Find("CloseBtn");
            if (existing == null)
            {
                GameObject closeObj = new GameObject("CloseBtn");
                closeObj.transform.SetParent(transform, false);
                RectTransform crt = closeObj.AddComponent<RectTransform>();
                crt.sizeDelta = new Vector2(40, 40);
                crt.anchorMin = new Vector2(1, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(1, 1);
                crt.anchoredPosition = new Vector2(-10, -10);

                Image closeImg = closeObj.AddComponent<Image>();
                closeImg.color = new Color(0.8f, 0.2f, 0.2f, 1);

                Button closeBtn = closeObj.AddComponent<Button>();
                closeBtn.targetGraphic = closeImg;
                ColorBlock colors = closeBtn.colors;
                colors.highlightedColor = new Color(1f, 0.3f, 0.3f, 1);
                closeBtn.colors = colors;

                GameObject closeTextObj = new GameObject("Text");
                closeTextObj.transform.SetParent(closeObj.transform, false);
                Text closeText = closeTextObj.AddComponent<Text>();
                closeText.text = "X";
                closeText.font = FontHelper.GetUIFont();
                closeText.fontSize = 20;
                closeText.alignment = TextAnchor.MiddleCenter;
                closeText.color = Color.white;
                RectTransform ctrt = closeTextObj.GetComponent<RectTransform>();
                ctrt.anchorMin = Vector2.zero;
                ctrt.anchorMax = Vector2.one;
                ctrt.offsetMin = Vector2.zero;
                ctrt.offsetMax = Vector2.zero;
            }

            // Create title
            Transform titleTrans = transform.Find("TitleText");
            if (titleTrans == null)
            {
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(transform, false);
                Text titleText = titleObj.AddComponent<Text>();
                titleText.text = "Quests";
                titleText.font = FontHelper.GetUIFont();
                titleText.fontSize = 28;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
                RectTransform trt = titleObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0, 0.9f);
                trt.anchorMax = new Vector2(1, 1);
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            QuestManager.Instance.OnQuestsChanged += OnQuestsChanged;
            RefreshDisplay();
        }

        public override void OnExit()
        {
            base.OnExit();
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestsChanged -= OnQuestsChanged;
            ClearItems();
        }

        private void OnQuestsChanged()
        {
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            ClearItems();
            BuildHeader();
            BuildQuestList();
        }

        private void BuildHeader()
        {
            Transform header = transform.Find("HeaderText");
            if (header == null)
            {
                GameObject headerObj = new GameObject("HeaderText");
                headerObj.transform.SetParent(transform, false);
                Text headerText = headerObj.AddComponent<Text>();
                headerText.text = "Complete quests to earn rewards!";
                headerText.font = FontHelper.GetUIFont();
                headerText.fontSize = 14;
                headerText.alignment = TextAnchor.MiddleCenter;
                headerText.color = Color.gray;
                RectTransform hrt = headerObj.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0.1f, 0.84f);
                hrt.anchorMax = new Vector2(0.9f, 0.90f);
                hrt.offsetMin = Vector2.zero;
                hrt.offsetMax = Vector2.zero;
            }
        }

        private void BuildQuestList()
        {
            List<QuestProgress> activeQuests = QuestManager.Instance.GetActiveQuests();
            float yOffset = 0f;
            float itemHeight = 120f;
            float spacing = 10f;

            foreach (QuestProgress progress in activeQuests)
            {
                QuestDefinition def = QuestManager.Instance.GetDefinition(progress.questId);
                if (def == null) continue;

                GameObject itemObj = CreateQuestItem(progress, def, yOffset);
                questObjects.Add(itemObj);
                yOffset -= (itemHeight + spacing);
            }
        }

        private GameObject CreateQuestItem(QuestProgress progress, QuestDefinition def, float yOffset)
        {
            GameObject itemObj;
            if (questItemPrefab != null)
            {
                itemObj = Object.Instantiate(questItemPrefab, questContainer);
                itemObj.SetActive(true);
            }
            else
            {
                itemObj = new GameObject("QuestItem_" + def.questId);
                itemObj.transform.SetParent(questContainer, false);

                RectTransform rt = itemObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1);
                rt.anchorMax = new Vector2(0.5f, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(700, 110);
                rt.anchoredPosition = new Vector2(0, yOffset);

                Image bg = itemObj.AddComponent<Image>();
                bg.color = progress.completed
                    ? new Color(0.1f, 0.25f, 0.1f, 0.9f)
                    : new Color(0.12f, 0.12f, 0.18f, 0.9f);
            }

            SetupQuestItem(itemObj, progress, def);
            return itemObj;
        }

        private void SetupQuestItem(GameObject obj, QuestProgress progress, QuestDefinition def)
        {
            // ── Title ──
            Text titleText = FindChildComponent<Text>(obj.transform, "TitleText");
            if (titleText == null)
            {
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(obj.transform, false);
                titleText = titleObj.AddComponent<Text>();
                titleText.font = FontHelper.GetUIFont();
                titleText.fontSize = 18;
                titleText.alignment = TextAnchor.MiddleLeft;
                titleText.color = progress.completed ? Color.green : Color.white;
                RectTransform trt = titleObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.02f, 0.55f);
                trt.anchorMax = new Vector2(0.7f, 0.95f);
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
            }
            titleText.text = def.title;
            titleText.color = progress.completed ? Color.green : Color.white;

            // ── Description / Progress text ──
            Text descText = FindChildComponent<Text>(obj.transform, "DescText");
            if (descText == null)
            {
                GameObject descObj = new GameObject("DescText");
                descObj.transform.SetParent(obj.transform, false);
                descText = descObj.AddComponent<Text>();
                descText.font = FontHelper.GetUIFont();
                descText.fontSize = 12;
                descText.alignment = TextAnchor.MiddleLeft;
                descText.color = Color.gray;
                RectTransform drt = descObj.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0.02f, 0.25f);
                drt.anchorMax = new Vector2(0.7f, 0.55f);
                drt.offsetMin = Vector2.zero;
                drt.offsetMax = Vector2.zero;
            }
            descText.text = $"{def.description} ({progress.currentCount}/{def.targetCount})";

            // ── Rarity label ──
            Text rarityText = FindChildComponent<Text>(obj.transform, "RarityText");
            if (rarityText == null)
            {
                GameObject rarityObj = new GameObject("RarityText");
                rarityObj.transform.SetParent(obj.transform, false);
                rarityText = rarityObj.AddComponent<Text>();
                rarityText.font = FontHelper.GetUIFont();
                rarityText.fontSize = 10;
                rarityText.alignment = TextAnchor.MiddleLeft;
                RectTransform rrt = rarityObj.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0.02f, 0f);
                rrt.anchorMax = new Vector2(0.3f, 0.25f);
                rrt.offsetMin = Vector2.zero;
                rrt.offsetMax = Vector2.zero;
            }
            rarityText.text = def.rarity.ToString();
            rarityText.color = def.rarity == QuestRarity.Weekly ? Color.yellow : Color.cyan;

            // ── Progress bar ──
            Transform barArea = obj.transform.Find("ProgressBar");
            Image barFill = null;
            if (barArea != null)
                barFill = barArea.Find("Fill")?.GetComponent<Image>();

            if (barFill == null)
            {
                // Create progress bar
                GameObject barObj = new GameObject("ProgressBar");
                barObj.transform.SetParent(obj.transform, false);
                RectTransform brt = barObj.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.02f, 0.05f);
                brt.anchorMax = new Vector2(0.7f, 0.22f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Image barBg = barObj.AddComponent<Image>();
                barBg.color = new Color(0.2f, 0.2f, 0.25f, 1);

                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(barObj.transform, false);
                barFill = fillObj.AddComponent<Image>();
                barFill.color = progress.completed ? new Color(0.2f, 0.8f, 0.2f, 1) : new Color(0.2f, 0.6f, 1f, 1);
                barFill.type = Image.Type.Filled;
                barFill.fillMethod = Image.FillMethod.Horizontal;
                RectTransform frt = fillObj.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = Vector2.zero;
                frt.offsetMax = Vector2.zero;
            }

            barFill.fillAmount = def.targetCount > 0 ? (float)progress.currentCount / def.targetCount : 0f;
            barFill.color = progress.completed ? new Color(0.2f, 0.8f, 0.2f, 1) : new Color(0.2f, 0.6f, 1f, 1);

            // ── Reward text ──
            Text rewardText = FindChildComponent<Text>(obj.transform, "RewardText");
            if (rewardText == null)
            {
                GameObject rewardObj = new GameObject("RewardText");
                rewardObj.transform.SetParent(obj.transform, false);
                rewardText = rewardObj.AddComponent<Text>();
                rewardText.font = FontHelper.GetUIFont();
                rewardText.fontSize = 12;
                rewardText.alignment = TextAnchor.MiddleRight;
                rewardText.color = Color.yellow;
                RectTransform rrt = rewardObj.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0.72f, 0.6f);
                rrt.anchorMax = new Vector2(0.98f, 0.95f);
                rrt.offsetMin = Vector2.zero;
                rrt.offsetMax = Vector2.zero;
            }
            rewardText.text = RewardToString(def.reward);

            // ── Claim / Status button ──
            Button actionBtn = FindChildComponent<Button>(obj.transform, "ActionBtn");
            if (actionBtn == null)
            {
                GameObject btnObj = new GameObject("ActionBtn");
                btnObj.transform.SetParent(obj.transform, false);
                RectTransform brt = btnObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.72f, 0.1f);
                brt.anchorMax = new Vector2(0.98f, 0.55f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Image btnImg = btnObj.AddComponent<Image>();
                actionBtn = btnObj.AddComponent<Button>();
                actionBtn.targetGraphic = btnImg;

                GameObject btnTextObj = new GameObject("Text");
                btnTextObj.transform.SetParent(btnObj.transform, false);
                Text btnText = btnTextObj.AddComponent<Text>();
                btnText.font = FontHelper.GetUIFont();
                btnText.fontSize = 13;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;
                RectTransform btrt = btnTextObj.GetComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;
            }

            Text btnLabel = actionBtn.GetComponentInChildren<Text>();
            bool isExpired = progress.IsExpired();

            if (progress.rewardClaimed)
            {
                actionBtn.interactable = false;
                Image btnImg = actionBtn.GetComponent<Image>();
                btnImg.color = Color.gray;
                if (btnLabel != null) btnLabel.text = "Claimed";
            }
            else if (progress.completed)
            {
                actionBtn.interactable = true;
                Image btnImg = actionBtn.GetComponent<Image>();
                btnImg.color = new Color(0.2f, 0.8f, 0.2f, 1);
                if (btnLabel != null) btnLabel.text = "Claim";
                ColorBlock colors = actionBtn.colors;
                colors.highlightedColor = new Color(0.3f, 1f, 0.3f, 1);
                colors.pressedColor = new Color(0.1f, 0.5f, 0.1f, 1);
                actionBtn.colors = colors;

                string capturedId = def.questId;
                actionBtn.onClick.RemoveAllListeners();
                actionBtn.onClick.AddListener(() => OnClaimClicked(capturedId));
            }
            else if (isExpired)
            {
                actionBtn.interactable = false;
                Image btnImg = actionBtn.GetComponent<Image>();
                btnImg.color = Color.gray;
                if (btnLabel != null) btnLabel.text = "Expired";
            }
            else
            {
                actionBtn.interactable = false;
                Image btnImg = actionBtn.GetComponent<Image>();
                btnImg.color = new Color(0.3f, 0.3f, 0.3f, 1);
                if (btnLabel != null) btnLabel.text = "In Progress";
            }
        }

        private void OnClaimClicked(string questId)
        {
            QuestManager.Instance.ClaimReward(questId);
            UIManager.Instance.CreatePromptMessageAsync("Reward claimed!");
            RefreshDisplay();
        }

        private string RewardToString(QuestReward reward)
        {
            if (reward == null) return "";
            string qty = reward.quantity > 0 ? reward.quantity.ToString() : "";
            switch (reward.rewardType)
            {
                case QuestRewardType.Gold: return $"{qty} Gold";
                case QuestRewardType.Gems: return $"{qty} Gems";
                case QuestRewardType.Card:
                    string card = !string.IsNullOrEmpty(reward.rewardId) ? reward.rewardId : "Random Card";
                    return $"{qty}x {card}";
                case QuestRewardType.Experience: return $"{qty} Exp";
                case QuestRewardType.SeasonPassExp: return $"{qty} Pass Exp";
                default: return "Reward";
            }
        }

        private void ClearItems()
        {
            foreach (GameObject obj in questObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            questObjects.Clear();
        }

        private T FindChildComponent<T>(Transform parent, string name) where T : Component
        {
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
