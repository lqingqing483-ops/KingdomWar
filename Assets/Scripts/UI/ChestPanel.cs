using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using KingdomWar.Game.Chest;

namespace KingdomWar.UI
{
    public class ChestPanel : basePanel
    {
        [Header("Free Chest")]
        public Button freeChestButton;
        public Text freeChestCooldownText;

        [Header("Chest Slots")]
        public Transform[] slotRoots = new Transform[4];

        private Button[] openButtons = new Button[4];
        private Text[] slotTexts = new Text[4];
        private Text[] timerTexts = new Text[4];
        private Image[] chestIcons = new Image[4];
        private GameObject[] emptyGroups = new GameObject[4];
        private GameObject[] contentGroups = new GameObject[4];
        private Coroutine updateCoroutine;

        protected override void Awake()
        {
            base.Awake();
            ResolveReferences();
        }

        protected override void Start()
        {
            base.Start();
            BindEvents();
            updateCoroutine = StartCoroutine(TickCoroutine());
        }

        private void ResolveReferences()
        {
            if (freeChestButton == null)
                freeChestButton = transform.Find("FreeChestButton")?.GetComponent<Button>();
            if (freeChestCooldownText == null)
                freeChestCooldownText = transform.Find("FreeChestCooldownText")?.GetComponent<Text>();

            Transform container = transform.Find("SlotContainer");
            for (int i = 0; i < 4; i++)
            {
                if (slotRoots[i] == null)
                {
                    Transform t = container?.Find("ChestSlot_" + i);
                    if (t == null && container != null && i < container.childCount)
                        t = container.GetChild(i);
                    slotRoots[i] = t;
                }
                if (slotRoots[i] != null)
                {
                    chestIcons[i] = slotRoots[i].Find("ChestIcon")?.GetComponent<Image>();
                    slotTexts[i] = slotRoots[i].Find("SlotText")?.GetComponent<Text>();
                    timerTexts[i] = slotRoots[i].Find("TimerText")?.GetComponent<Text>();
                    openButtons[i] = slotRoots[i].Find("OpenButton")?.GetComponent<Button>();
                    emptyGroups[i] = slotRoots[i].Find("EmptyGroup")?.gameObject;
                    contentGroups[i] = slotRoots[i].Find("ContentGroup")?.gameObject;
                }
            }
        }

        private void BindEvents()
        {
            freeChestButton?.onClick.AddListener(OnFreeChestClicked);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                if (openButtons[i] != null)
                    openButtons[i].onClick.AddListener(() => OnOpenChestClicked(index));
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            RefreshAll();
        }

        private IEnumerator TickCoroutine()
        {
            while (true)
            {
                RefreshAll();
                yield return new WaitForSeconds(1f);
            }
        }

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshFreeChest();
        }

        private void RefreshSlots()
        {
            List<ChestSlot> slots = ChestManager.Instance.GetSlots();
            for (int i = 0; i < 4; i++)
            {
                if (i < slots.Count)
                    UpdateSlotView(i, slots[i]);
            }
        }

        private void UpdateSlotView(int index, ChestSlot slot)
        {
            bool isEmpty = slot.chest == null;
            bool isUnlocked = slot.isUnlocked;
            bool isUnlocking = slot.isUnlocking;

            if (emptyGroups[index] != null)
                emptyGroups[index].SetActive(isEmpty);
            if (contentGroups[index] != null)
                contentGroups[index].SetActive(!isEmpty);

            if (isEmpty)
            {
                if (slotTexts[index] != null)
                    slotTexts[index].text = "Empty";
                if (timerTexts[index] != null)
                    timerTexts[index].gameObject.SetActive(false);
                if (openButtons[index] != null)
                    openButtons[index].gameObject.SetActive(false);
                return;
            }

            if (slotTexts[index] != null)
                slotTexts[index].text = slot.chest.chestName;

            if (openButtons[index] != null)
                openButtons[index].gameObject.SetActive(isUnlocked);

            if (timerTexts[index] != null)
            {
                if (isUnlocking)
                {
                    int remaining = ChestManager.Instance.GetRemainingUnlockMinutes(index);
                    if (remaining > 0)
                        timerTexts[index].text = string.Format("{0:D2}:{1:D2}", remaining / 60, remaining % 60);
                    else
                        timerTexts[index].text = "Unlocking...";
                    timerTexts[index].gameObject.SetActive(true);
                }
                else if (!isUnlocked)
                {
                    timerTexts[index].text = "Locked";
                    timerTexts[index].gameObject.SetActive(true);
                }
                else
                {
                    timerTexts[index].gameObject.SetActive(false);
                }
            }
        }

        private void RefreshFreeChest()
        {
            int cooldown = ChestManager.Instance.GetFreeChestCooldownRemaining();
            bool canClaim = cooldown <= 0;

            if (freeChestButton != null)
                freeChestButton.interactable = canClaim;

            if (freeChestCooldownText != null)
            {
                if (canClaim)
                    freeChestCooldownText.text = "Free Chest Ready!";
                else
                    freeChestCooldownText.text = string.Format("{0:D2}:{1:D2}:{2:D2}",
                        cooldown / 3600, (cooldown % 3600) / 60, cooldown % 60);
            }
        }

        private void OnFreeChestClicked()
        {
            if (ChestManager.Instance.GetFreeChest())
            {
                RefreshAll();
                UIManager.Instance.CreatePromptMessageAsync("Free Chest claimed! Open it in the slot below.");
            }
            else
            {
                UIManager.Instance.CreatePromptMessageAsync("No empty slots available");
            }
        }

        private void OnOpenChestClicked(int index)
        {
            if (slotRoots[index] != null)
            {
                slotRoots[index].DOScale(1.3f, 0.15f).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        slotRoots[index].DOScale(1f, 0.15f).SetEase(Ease.InBack);
                    });
            }

            Dictionary<string, int> rewards = ChestManager.Instance.OpenChest(index);
            if (rewards != null)
            {
                string text = BuildRewardText(rewards);
                UIManager.Instance.CreatePromptMessageAsync(text);
                RefreshAll();
            }
            else
            {
                UIManager.Instance.CreatePromptMessageAsync("Failed to open chest");
            }
        }

        private static string BuildRewardText(Dictionary<string, int> rewards)
        {
            if (rewards == null || rewards.Count == 0)
                return "No rewards";

            var parts = new List<string>(rewards.Count);
            foreach (var kvp in rewards)
            {
                parts.Add(string.Format("{0} x{1}", kvp.Key, kvp.Value));
            }
            return string.Join(", ", parts);
        }

        public override void OnExit()
        {
            base.OnExit();
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }
        }
    }
}
