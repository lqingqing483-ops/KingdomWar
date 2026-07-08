using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KingdomWar.Game.SeasonPass;

namespace KingdomWar.UI
{
    public class seasonPassPanel : basePanel
    {
        // ── Serialized references (optional for prefab binding) ──
        public Transform rewardContainer;
        public GameObject rewardCellPrefab;
        public Transform buyPremiumButton;

        // ── Internal references ──
        private Text countdownText;
        private Text levelText;
        private Image progressBar;
        private Text expText;
        private Transform contentRoot;
        private Transform buyPremiumBtnTransform;
        private Button claimAllButton;

        private List<GameObject> rowObjects = new List<GameObject>();
        private float nextCountdownUpdate;

        protected override void Awake()
        {
            base.Awake();

            // Ensure panel has a valid size (for self-building without prefab)
            RectTransform rt = GetComponent<RectTransform>();
            if (rt.sizeDelta == Vector2.zero)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(900f, 1600f);
                rt.anchoredPosition = Vector2.zero;
            }

            if (rewardContainer == null)
                rewardContainer = transform;

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
        }

        public override void OnEnter()
        {
            base.OnEnter();

            SeasonPassManager mgr = SeasonPassManager.Instance;
            mgr.OnExpChanged += OnExpChanged;
            mgr.OnRewardClaimed += OnRewardClaimed;
            mgr.OnPremiumPurchased += OnPremiumPurchased;
            mgr.OnSeasonReset += OnSeasonReset;

            RefreshDisplay();
            nextCountdownUpdate = Time.realtimeSinceStartup + 60f;
        }

        public override void OnExit()
        {
            base.OnExit();

            if (SeasonPassManager.Instance != null)
            {
                SeasonPassManager mgr = SeasonPassManager.Instance;
                mgr.OnExpChanged -= OnExpChanged;
                mgr.OnRewardClaimed -= OnRewardClaimed;
                mgr.OnPremiumPurchased -= OnPremiumPurchased;
                mgr.OnSeasonReset -= OnSeasonReset;
            }
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup >= nextCountdownUpdate)
            {
                UpdateCountdown();
                nextCountdownUpdate = Time.realtimeSinceStartup + 60f;
            }
        }

        // ── Event handlers ──────────────────────────────────────────────

        private void OnExpChanged(int level, int totalExp) { RefreshDisplay(); }
        private void OnRewardClaimed(int level, SeasonPassTier tier) { RefreshDisplay(); }
        private void OnPremiumPurchased() { RefreshDisplay(); }
        private void OnSeasonReset() { RefreshDisplay(); }

        // ── Public API ──────────────────────────────────────────────────

        public void RefreshDisplay()
        {
            ClearRewardRows();
            BuildHeader();
            BuildProgressBar();
            BuildRewardGrid();
            BuildBottomButtons();
        }

        // ── Header ──────────────────────────────────────────────────────

        private void BuildHeader()
        {
            Transform header = transform.Find("HeaderArea");
            if (header == null)
            {
                GameObject headerObj = new GameObject("HeaderArea");
                headerObj.transform.SetParent(transform, false);
                RectTransform hrt = headerObj.AddComponent<RectTransform>();
                hrt.anchorMin = new Vector2(0, 0.85f);
                hrt.anchorMax = new Vector2(1, 1);
                hrt.offsetMin = Vector2.zero;
                hrt.offsetMax = Vector2.zero;
                header = headerObj.transform;

                // Title
                GameObject titleObj = new GameObject("TitleText");
                titleObj.transform.SetParent(header, false);
                Text titleText = titleObj.AddComponent<Text>();
                titleText.text = "Season Pass";
                titleText.font = FontHelper.GetUIFont();
                titleText.fontSize = 26;
                titleText.alignment = TextAnchor.MiddleCenter;
                titleText.color = Color.white;
                RectTransform trt = titleObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0, 0.4f);
                trt.anchorMax = new Vector2(1, 1);
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                // Countdown
                GameObject countObj = new GameObject("CountdownText");
                countObj.transform.SetParent(header, false);
                countdownText = countObj.AddComponent<Text>();
                countdownText.font = FontHelper.GetUIFont();
                countdownText.fontSize = 14;
                countdownText.alignment = TextAnchor.MiddleCenter;
                countdownText.color = Color.yellow;
                RectTransform crt = countObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 0);
                crt.anchorMax = new Vector2(1, 0.4f);
                crt.offsetMin = Vector2.zero;
                crt.offsetMax = Vector2.zero;
            }
            else
            {
                countdownText = header.Find("CountdownText")?.GetComponent<Text>();
            }

            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (countdownText == null) return;
            SeasonPassManager mgr = SeasonPassManager.Instance;
            if (mgr.IsExpired())
            {
                countdownText.text = "Season expired";
            }
            else
            {
                int days = mgr.GetRemainingDays();
                countdownText.text = $"Days remaining: {days}";
            }
        }

        // ── Progress Bar ────────────────────────────────────────────────

        private void BuildProgressBar()
        {
            Transform area = transform.Find("ProgressArea");
            if (area == null)
            {
                GameObject areaObj = new GameObject("ProgressArea");
                areaObj.transform.SetParent(transform, false);
                RectTransform art = areaObj.AddComponent<RectTransform>();
                art.anchorMin = new Vector2(0.05f, 0.75f);
                art.anchorMax = new Vector2(0.95f, 0.85f);
                art.offsetMin = Vector2.zero;
                art.offsetMax = Vector2.zero;
                area = areaObj.transform;

                // Level text (top-left)
                GameObject lvlObj = new GameObject("LevelText");
                lvlObj.transform.SetParent(area, false);
                levelText = lvlObj.AddComponent<Text>();
                levelText.font = FontHelper.GetUIFont();
                levelText.fontSize = 16;
                levelText.alignment = TextAnchor.MiddleLeft;
                levelText.color = Color.white;
                RectTransform lrt = lvlObj.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0.5f);
                lrt.anchorMax = new Vector2(0.5f, 1);
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                // Exp text (top-right)
                GameObject expObj = new GameObject("ExpText");
                expObj.transform.SetParent(area, false);
                expText = expObj.AddComponent<Text>();
                expText.font = FontHelper.GetUIFont();
                expText.fontSize = 12;
                expText.alignment = TextAnchor.MiddleRight;
                expText.color = Color.gray;
                RectTransform ert = expObj.GetComponent<RectTransform>();
                ert.anchorMin = new Vector2(0.5f, 0.5f);
                ert.anchorMax = new Vector2(1, 1);
                ert.offsetMin = Vector2.zero;
                ert.offsetMax = Vector2.zero;

                // Progress bar background (bottom)
                GameObject barBgObj = new GameObject("ProgressBarBg");
                barBgObj.transform.SetParent(area, false);
                Image barBg = barBgObj.AddComponent<Image>();
                barBg.color = new Color(0.2f, 0.2f, 0.25f, 1);
                RectTransform brt = barBgObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0, 0);
                brt.anchorMax = new Vector2(1, 0.45f);
                brt.offsetMin = new Vector2(0, 2);
                brt.offsetMax = new Vector2(0, -2);

                // Progress bar fill
                GameObject barObj = new GameObject("ProgressBarFill");
                barObj.transform.SetParent(barBgObj.transform, false);
                progressBar = barObj.AddComponent<Image>();
                progressBar.color = new Color(0.2f, 0.8f, 1f, 1);
                progressBar.type = Image.Type.Filled;
                progressBar.fillMethod = Image.FillMethod.Horizontal;
                RectTransform prt = barObj.GetComponent<RectTransform>();
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = Vector2.one;
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
            }
            else
            {
                levelText = area.Find("LevelText")?.GetComponent<Text>();
                progressBar = area.Find("ProgressBarBg/ProgressBarFill")?.GetComponent<Image>();
                expText = area.Find("ExpText")?.GetComponent<Text>();
            }

            SeasonPassManager mgr = SeasonPassManager.Instance;
            if (levelText != null)
                levelText.text = $"Level {mgr.GetCurrentLevel()}";
            if (progressBar != null)
                progressBar.fillAmount = mgr.GetLevelProgress();
            if (expText != null)
                expText.text = $"{mgr.GetCurrentLevelExp()}/{mgr.GetExpForNextLevel()}";
        }

        // ── Reward Grid ─────────────────────────────────────────────────

        private void BuildRewardGrid()
        {
            Transform scrollView = transform.Find("ScrollView");
            Transform content;
            if (scrollView == null)
            {
                GameObject svObj = new GameObject("ScrollView");
                svObj.transform.SetParent(transform, false);
                RectTransform svrt = svObj.AddComponent<RectTransform>();
                svrt.anchorMin = new Vector2(0, 0.10f);
                svrt.anchorMax = new Vector2(1, 0.75f);
                svrt.offsetMin = Vector2.zero;
                svrt.offsetMax = Vector2.zero;

                Image bg = svObj.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.2f);

                ScrollRect sr = svObj.AddComponent<ScrollRect>();
                sr.horizontal = false;
                sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.inertia = true;
                sr.scrollSensitivity = 30;

                // Viewport
                GameObject vpObj = new GameObject("Viewport");
                vpObj.transform.SetParent(svObj.transform, false);
                RectTransform vprt = vpObj.AddComponent<RectTransform>();
                vprt.anchorMin = Vector2.zero;
                vprt.anchorMax = Vector2.one;
                vprt.offsetMin = Vector2.zero;
                vprt.offsetMax = Vector2.zero;
                vpObj.AddComponent<Image>().color = new Color(0, 0, 0, 0);
                vpObj.AddComponent<Mask>().showMaskGraphic = false;

                // Content
                GameObject contentObj = new GameObject("Content");
                contentObj.transform.SetParent(vpObj.transform, false);
                content = contentObj.transform;
                RectTransform crt = contentObj.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                crt.sizeDelta = new Vector2(0, 0);
                VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 4;
                vlg.padding = new RectOffset(10, 10, 5, 5);
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childForceExpandWidth = true;
                ContentSizeFitter csf = contentObj.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                sr.content = crt;
                scrollView = svObj.transform;
            }

            content = scrollView.Find("Viewport/Content");
            if (content == null) return;

            SeasonPassConfigSO config = SeasonPassConfigSO.Instance;
            int currentLevel = SeasonPassManager.Instance.GetCurrentLevel();

            for (int level = config.maxLevel; level >= 1; level--)
            {
                GameObject row = CreateRewardRow(content, level, currentLevel);
                rowObjects.Add(row);
            }
        }

        private GameObject CreateRewardRow(Transform parent, int level, int currentLevel)
        {
            // Column headers on first row (at top = first created)
            if (level == SeasonPassConfigSO.Instance.maxLevel)
            {
                GameObject headerRow = new GameObject("RewardHeader");
                headerRow.transform.SetParent(parent, false);
                HorizontalLayoutGroup hlg = headerRow.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.padding = new RectOffset(4, 4, 2, 2);
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childForceExpandWidth = true;
                hlg.childControlWidth = false;

                LayoutElement le = headerRow.AddComponent<LayoutElement>();
                le.minHeight = 24;

                // Spacer for level label column
                GameObject spacerObj = new GameObject("Spacer");
                spacerObj.transform.SetParent(headerRow.transform, false);
                RectConstraint(spacerObj, 60, 24);
                LayoutElement sle = spacerObj.AddComponent<LayoutElement>();
                sle.minWidth = 60;

                // Free column header
                GameObject freeHeader = new GameObject("FreeHeader");
                freeHeader.transform.SetParent(headerRow.transform, false);
                Text freeText = freeHeader.AddComponent<Text>();
                freeText.text = "Free";
                freeText.font = FontHelper.GetUIFont();
                freeText.fontSize = 14;
                freeText.alignment = TextAnchor.MiddleCenter;
                freeText.color = new Color(0.5f, 0.8f, 1f, 1);
                RectConstraint(freeHeader, 0, 24);
                LayoutElement fle = freeHeader.AddComponent<LayoutElement>();
                fle.flexibleWidth = 1;

                // Premium column header
                GameObject premiumHeader = new GameObject("PremiumHeader");
                premiumHeader.transform.SetParent(headerRow.transform, false);
                Text premiumText = premiumHeader.AddComponent<Text>();
                premiumText.text = "Premium";
                premiumText.font = FontHelper.GetUIFont();
                premiumText.fontSize = 14;
                premiumText.alignment = TextAnchor.MiddleCenter;
                premiumText.color = new Color(1f, 0.8f, 0.2f, 1);
                RectConstraint(premiumHeader, 0, 24);
                LayoutElement ple = premiumHeader.AddComponent<LayoutElement>();
                ple.flexibleWidth = 1;

                rowObjects.Add(headerRow);
            }

            // Actual reward row
            GameObject row = new GameObject($"RewardRow_Lv{level}");
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup rhlg = row.AddComponent<HorizontalLayoutGroup>();
            rhlg.spacing = 8;
            rhlg.padding = new RectOffset(4, 4, 2, 2);
            rhlg.childAlignment = TextAnchor.MiddleCenter;
            rhlg.childForceExpandWidth = true;
            rhlg.childControlWidth = false;

            LayoutElement rle = row.AddComponent<LayoutElement>();
            rle.minHeight = 36;

            // Level label
            GameObject lvlLabelObj = new GameObject("LevelLabel");
            lvlLabelObj.transform.SetParent(row.transform, false);
            Text lvlLabel = lvlLabelObj.AddComponent<Text>();
            lvlLabel.text = $"Lv.{level}";
            lvlLabel.font = FontHelper.GetUIFont();
            lvlLabel.fontSize = 13;
            lvlLabel.alignment = TextAnchor.MiddleCenter;
            lvlLabel.color = level <= currentLevel ? Color.white : Color.gray;
            RectConstraint(lvlLabelObj, 60, 30);
            LayoutElement lle = lvlLabelObj.AddComponent<LayoutElement>();
            lle.minWidth = 60;

            // Free cell
            CreateRewardCell(row.transform, level, currentLevel, SeasonPassTier.Free);

            // Premium cell
            CreateRewardCell(row.transform, level, currentLevel, SeasonPassTier.Premium);

            return row;
        }

        private void CreateRewardCell(Transform parent, int level, int currentLevel, SeasonPassTier tier)
        {
            SeasonPassConfigSO config = SeasonPassConfigSO.Instance;
            SeasonPassManager mgr = SeasonPassManager.Instance;

            List<SeasonPassReward> rewards = config.GetRewardsAtLevel(level, tier);
            bool hasReward = rewards != null && rewards.Count > 0;

            bool isPremium = (tier == SeasonPassTier.Premium);
            bool isLocked = level > currentLevel;
            bool isClaimed = !isLocked && mgr.IsRewardClaimed(level, tier);
            bool canClaim = !isLocked && !isClaimed && hasReward;

            // Check if premium is available for this tier
            if (isPremium && !mgr.HasPremiumPass())
            {
                canClaim = false;
                // Don't override isClaimed — may have been claimed before data loss
            }

            GameObject cellObj = new GameObject($"{tier}Cell_Lv{level}");
            cellObj.transform.SetParent(parent, false);

            LayoutElement le = cellObj.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minWidth = 100;

            // Background
            Image bg = cellObj.AddComponent<Image>();
            if (isLocked)
                bg.color = new Color(0.08f, 0.08f, 0.12f, 0.8f);
            else if (isClaimed)
                bg.color = new Color(0.1f, 0.2f, 0.1f, 0.8f);
            else if (canClaim)
                bg.color = new Color(0.15f, 0.25f, 0.15f, 0.9f);
            else
                bg.color = new Color(0.12f, 0.12f, 0.15f, 0.8f);

            if (!hasReward)
            {
                Text noRewardText = AddCellText(cellObj, "---", 11, Color.gray);
                RectTransform nrt = noRewardText.GetComponent<RectTransform>();
                nrt.anchorMin = Vector2.zero;
                nrt.anchorMax = Vector2.one;
                nrt.offsetMin = Vector2.zero;
                nrt.offsetMax = Vector2.zero;
                return;
            }

            // Build reward description string
            string desc = "";
            foreach (SeasonPassReward r in rewards)
            {
                if (desc.Length > 0) desc += "\n";
                desc += RewardToShortString(r);
            }

            // Description text
            Text descText = AddCellText(cellObj, desc, 10, Color.white);
            RectTransform drt = descText.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0.35f);
            drt.anchorMax = new Vector2(1, 0.95f);
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;

            if (isClaimed)
            {
                Text claimedText = AddCellText(cellObj, "Claimed", 11, Color.green);
                RectTransform claimedRt = claimedText.GetComponent<RectTransform>();
                claimedRt.anchorMin = new Vector2(0, 0);
                claimedRt.anchorMax = new Vector2(1, 0.35f);
                claimedRt.offsetMin = Vector2.zero;
                claimedRt.offsetMax = Vector2.zero;
            }
            else if (isLocked)
            {
                Text lockedText = AddCellText(cellObj, "Locked", 10, Color.gray);
                RectTransform lockedRt = lockedText.GetComponent<RectTransform>();
                lockedRt.anchorMin = new Vector2(0, 0);
                lockedRt.anchorMax = new Vector2(1, 0.35f);
                lockedRt.offsetMin = Vector2.zero;
                lockedRt.offsetMax = Vector2.zero;
            }
            else if (isPremium && !mgr.HasPremiumPass())
            {
                Text lockedText = AddCellText(cellObj, "Premium", 10, Color.gray);
                RectTransform lockedRt = lockedText.GetComponent<RectTransform>();
                lockedRt.anchorMin = new Vector2(0, 0);
                lockedRt.anchorMax = new Vector2(1, 0.35f);
                lockedRt.offsetMin = Vector2.zero;
                lockedRt.offsetMax = Vector2.zero;
            }
            else if (canClaim)
            {
                // Claim button
                GameObject btnObj = new GameObject("ClaimBtn");
                btnObj.transform.SetParent(cellObj.transform, false);

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.2f, 0.8f, 0.2f, 1);

                Button btn = btnObj.AddComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                btn.targetGraphic = btnImg;

                ColorBlock colors = btn.colors;
                colors.highlightedColor = new Color(0.3f, 1f, 0.3f, 1);
                colors.pressedColor = new Color(0.1f, 0.5f, 0.1f, 1);
                btn.colors = colors;

                RectTransform brt = btnObj.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.1f, 0.02f);
                brt.anchorMax = new Vector2(0.9f, 0.32f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Text btnText = AddCellText(btnObj, "Claim", 11, Color.white);
                RectTransform btrt = btnText.GetComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;

                int capturedLevel = level;
                SeasonPassTier capturedTier = tier;
                btn.onClick.AddListener(() => OnClaimReward(capturedLevel, capturedTier));
            }
        }

        // ── Bottom Buttons ──────────────────────────────────────────────

        private void BuildBottomButtons()
        {
            SeasonPassManager mgr = SeasonPassManager.Instance;
            bool isExpired = mgr.IsExpired();

            // ── Buy Premium Pass button ──
            Transform buyBtn = transform.Find("BuyPremiumBtn");
            if (buyBtn == null)
            {
                GameObject btnObj = new GameObject("BuyPremiumBtn");
                btnObj.transform.SetParent(transform, false);
                RectTransform brt = btnObj.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.1f, 0.02f);
                brt.anchorMax = new Vector2(0.9f, 0.09f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(1f, 0.6f, 0f, 1);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnImg;
                ColorBlock colors = btn.colors;
                colors.highlightedColor = new Color(1f, 0.8f, 0f, 1);
                colors.pressedColor = new Color(0.7f, 0.4f, 0f, 1);
                btn.colors = colors;

                GameObject btnTextObj = new GameObject("Text");
                btnTextObj.transform.SetParent(btnObj.transform, false);
                Text btnText = btnTextObj.AddComponent<Text>();
                btnText.font = FontHelper.GetUIFont();
                btnText.fontSize = 15;
                btnText.alignment = TextAnchor.MiddleCenter;
                btnText.color = Color.white;
                RectTransform btrt = btnTextObj.GetComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;

                btn.onClick.AddListener(OnBuyPremium);

                buyPremiumBtnTransform = btnObj.transform;
            }

            bool showBuy = !mgr.HasPremiumPass() && !isExpired;
            if (buyPremiumBtnTransform != null)
            {
                buyPremiumBtnTransform.gameObject.SetActive(showBuy);
                if (showBuy)
                {
                    Text btnText = buyPremiumBtnTransform.Find("Text")?.GetComponent<Text>();
                    if (btnText != null)
                        btnText.text = $"Buy Premium Pass - {SeasonPassConfigSO.Instance.premiumPassCostGems} Gems";
                }
            }

            // ── Claim All Rewards button ──
            Transform claimAll = transform.Find("ClaimAllBtn");
            if (claimAll == null)
            {
                GameObject btnObj = new GameObject("ClaimAllBtn");
                btnObj.transform.SetParent(transform, false);
                RectTransform brt = btnObj.AddComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.1f, 0.12f);
                brt.anchorMax = new Vector2(0.9f, 0.19f);
                brt.offsetMin = Vector2.zero;
                brt.offsetMax = Vector2.zero;

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.3f, 0.5f, 1f, 1);

                Button btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = btnImg;

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

                claimAllButton = btn;
                claimAll = btnObj.transform;
            }

            // Show claim all if there's anything to claim
            bool hasUnclaimed = mgr.GetUnclaimedFreeLevels().Count > 0
                || (mgr.HasPremiumPass() && mgr.GetUnclaimedPremiumLevels().Count > 0);
            bool showClaimAll = hasUnclaimed && !isExpired;

            // Show end-of-season claim button when season expired
            bool showEndOfSeason = isExpired && mgr.HasPremiumPass()
                && !mgr.GetSaveData().seasonEndedClaimed;

            if (claimAll != null)
            {
                claimAll.gameObject.SetActive(showClaimAll || showEndOfSeason);
                if ((showClaimAll || showEndOfSeason) && claimAllButton != null)
                {
                    Text btnText = claimAll.Find("Text")?.GetComponent<Text>();
                    if (btnText != null)
                        btnText.text = showEndOfSeason ? "Claim End-of-Season Reward" : "Claim All Rewards";
                    claimAllButton.onClick.RemoveAllListeners();
                    claimAllButton.onClick.AddListener(OnClaimAll);
                }
            }
        }

        // ── Button actions ──────────────────────────────────────────────

        private void OnBuyPremium()
        {
            bool success = SeasonPassManager.Instance.PurchasePremiumPass();
            if (success)
                UIManager.Instance.CreatePromptMessageAsync("Premium Pass purchased!");
            else
                UIManager.Instance.CreatePromptMessageAsync("Failed to purchase Premium Pass.");

            RefreshDisplay();
        }

        private void OnClaimReward(int level, SeasonPassTier tier)
        {
            bool success = SeasonPassManager.Instance.ClaimReward(level, tier);
            if (!success)
                UIManager.Instance.CreatePromptMessageAsync("Reward already claimed or locked.");
            RefreshDisplay();
        }

        private void OnClaimAll()
        {
            SeasonPassManager mgr = SeasonPassManager.Instance;
            if (mgr.IsExpired())
            {
                bool eosClaimed = mgr.ClaimEndOfSeasonReward();
                if (eosClaimed)
                    UIManager.Instance.CreatePromptMessageAsync("End-of-season reward claimed!");
                else
                    UIManager.Instance.CreatePromptMessageAsync("No end-of-season reward available.");
                RefreshDisplay();
                return;
            }
            List<int> freeLevels = mgr.GetUnclaimedFreeLevels();
            List<int> premiumLevels = mgr.GetUnclaimedPremiumLevels();

            int claimed = 0;
            foreach (int lvl in freeLevels)
            {
                if (mgr.ClaimReward(lvl, SeasonPassTier.Free))
                    claimed++;
            }
            foreach (int lvl in premiumLevels)
            {
                if (mgr.ClaimReward(lvl, SeasonPassTier.Premium))
                    claimed++;
            }

            if (claimed > 0)
                UIManager.Instance.CreatePromptMessageAsync($"Claimed {claimed} rewards!");
            RefreshDisplay();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void ClearRewardRows()
        {
            foreach (GameObject obj in rowObjects)
            {
                if (obj != null) Destroy(obj);
            }
            rowObjects.Clear();
        }

        private string RewardToShortString(SeasonPassReward reward)
        {
            switch (reward.rewardType)
            {
                case SeasonPassRewardType.Gold:
                    return $"{reward.quantity} Gold";
                case SeasonPassRewardType.Gems:
                    return $"{reward.quantity} Gems";
                case SeasonPassRewardType.Card:
                    return $"Card: {(!string.IsNullOrEmpty(reward.rewardId) ? reward.rewardId : "Random Card")}";
                case SeasonPassRewardType.CardFragments:
                    return $"Frags: {reward.rewardId} x{reward.quantity}";
                case SeasonPassRewardType.Chest:
                    return $"Chest: {(!string.IsNullOrEmpty(reward.rewardId) ? reward.rewardId : "???")}";
                case SeasonPassRewardType.Emote:
                    return "Emote";
                case SeasonPassRewardType.Experience:
                    return $"{reward.quantity} Exp";
                default:
                    return "Reward";
            }
        }

        private Text AddCellText(GameObject parent, string text, int fontSize, Color color)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent.transform, false);
            Text txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.font = FontHelper.GetUIFont();
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            return txt;
        }

        private static void RectConstraint(GameObject obj, float w, float h)
        {
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(w, h);
            }
        }

        private T FindChildComponent<T>(Transform parent, string name) where T : Component
        {
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
