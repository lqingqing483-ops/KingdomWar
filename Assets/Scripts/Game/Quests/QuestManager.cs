using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.HotUpdate;
using KingdomWar.Game.SeasonPass;

namespace KingdomWar.Game.Quests
{
    public class QuestManager : MonoBehaviour
    {
        private static QuestManager instance;
        public static QuestManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("QuestManager");
                    instance = obj.AddComponent<QuestManager>();
                }
                return instance;
            }
        }

        private const string SaveKey = "QuestData";

        private QuestSaveData saveData;
        private List<QuestDefinition> activeQuestDefs = new List<QuestDefinition>();

        public event Action OnQuestsChanged;

        // ── Default quest pool ──────────────────────────────────────────

        private static readonly QuestDefinition[] DefaultQuestPool = new QuestDefinition[]
        {
            new QuestDefinition { questId = "daily_win_1", questType = QuestType.WinBattles, rarity = QuestRarity.Daily, title = "Win 3 Battles", description = "Win 3 battles in the arena", targetCount = 3, durationHours = 24, reward = new QuestReward { rewardType = QuestRewardType.Gold, quantity = 200 } },
            new QuestDefinition { questId = "daily_play_1", questType = QuestType.PlayBattles, rarity = QuestRarity.Daily, title = "Play 5 Battles", description = "Play 5 battles (win or lose)", targetCount = 5, durationHours = 24, reward = new QuestReward { rewardType = QuestRewardType.Experience, quantity = 50 } },
            new QuestDefinition { questId = "daily_gold_1", questType = QuestType.EarnGold, rarity = QuestRarity.Daily, title = "Earn 500 Gold", description = "Earn gold from battles and chests", targetCount = 500, durationHours = 24, reward = new QuestReward { rewardType = QuestRewardType.Gems, quantity = 10 } },
            new QuestDefinition { questId = "daily_trophy_1", questType = QuestType.EarnTrophies, rarity = QuestRarity.Daily, title = "Gain 30 Trophies", description = "Gain trophies from winning battles", targetCount = 30, durationHours = 24, reward = new QuestReward { rewardType = QuestRewardType.Card, rewardId = "", quantity = 1 } },
            new QuestDefinition { questId = "daily_damage_1", questType = QuestType.DealDamage, rarity = QuestRarity.Daily, title = "Deal 2000 Damage", description = "Deal 2000 damage to enemy towers and units", targetCount = 2000, durationHours = 24, reward = new QuestReward { rewardType = QuestRewardType.Gold, quantity = 300 } },
            new QuestDefinition { questId = "weekly_win_1", questType = QuestType.WinBattles, rarity = QuestRarity.Weekly, title = "Win 20 Battles", description = "Win 20 battles this week", targetCount = 20, durationHours = 168, reward = new QuestReward { rewardType = QuestRewardType.Gems, quantity = 100 } },
            new QuestDefinition { questId = "weekly_level_1", questType = QuestType.SeasonPassLevel, rarity = QuestRarity.Weekly, title = "Season Pass Level 10", description = "Reach season pass level 10", targetCount = 10, durationHours = 168, reward = new QuestReward { rewardType = QuestRewardType.Gems, quantity = 50 } },
        };

        // ── Daily / Weekly pool subsets ────────────────────────────────

        private static QuestDefinition[] DailyPool
        {
            get
            {
                List<QuestDefinition> list = new List<QuestDefinition>();
                foreach (var q in DefaultQuestPool)
                    if (q.rarity == QuestRarity.Daily)
                        list.Add(q);
                return list.ToArray();
            }
        }

        private static QuestDefinition[] WeeklyPool
        {
            get
            {
                List<QuestDefinition> list = new List<QuestDefinition>();
                foreach (var q in DefaultQuestPool)
                    if (q.rarity == QuestRarity.Weekly)
                        list.Add(q);
                return list.ToArray();
            }
        }

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadData();
                AutoRefresh();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        // ── Save / Load ────────────────────────────────────────────────

        private void LoadData()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    saveData = JsonUtility.FromJson<QuestSaveData>(json);
                }
                catch
                {
                    // fall through to default
                }
            }

            if (saveData == null)
            {
                saveData = new QuestSaveData();
                SaveData();
            }

            // Build active quest definitions from saved progress
            RebuildActiveDefs();
        }

        private void SaveData()
        {
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        private void RebuildActiveDefs()
        {
            activeQuestDefs.Clear();
            if (saveData.activeQuests == null) return;

            foreach (var progress in saveData.activeQuests)
            {
                QuestDefinition def = FindDefinition(progress.questId);
                if (def != null)
                    activeQuestDefs.Add(def);
            }
        }

        private QuestDefinition FindDefinition(string questId)
        {
            foreach (var q in DefaultQuestPool)
                if (q.questId == questId)
                    return q;
            return null;
        }

        // ── Auto-Refresh ───────────────────────────────────────────────

        private void AutoRefresh()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            bool changed = false;

            if (saveData.lastDailyRefresh != today)
            {
                GenerateDailyQuests();
                saveData.lastDailyRefresh = today;
                changed = true;
            }

            // Weekly refresh every Monday
            string monday = GetCurrentWeekMonday();
            if (saveData.lastWeeklyRefresh != monday)
            {
                GenerateWeeklyQuests();
                saveData.lastWeeklyRefresh = monday;
                changed = true;
            }

            if (changed)
            {
                SaveData();
                DispatchChanged();
            }
        }

        private static string GetCurrentWeekMonday()
        {
            DateTime now = DateTime.Now;
            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            return now.AddDays(-diff).ToString("yyyy-MM-dd");
        }

        // ── Quest Generation ──────────────────────────────────────────

        public void GenerateDailyQuests()
        {
            saveData.activeQuests.RemoveAll(p => FindDefinition(p.questId)?.rarity == QuestRarity.Daily);
            activeQuestDefs.RemoveAll(d => d.rarity == QuestRarity.Daily);

            var pool = DailyPool;
            var pick = PickRandom(pool, Mathf.Min(3, pool.Length));
            foreach (var def in pick)
            {
                AddQuestProgress(def);
            }
        }

        public void GenerateWeeklyQuests()
        {
            saveData.activeQuests.RemoveAll(p => FindDefinition(p.questId)?.rarity == QuestRarity.Weekly);
            activeQuestDefs.RemoveAll(d => d.rarity == QuestRarity.Weekly);

            var pool = WeeklyPool;
            var pick = PickRandom(pool, Mathf.Min(2, pool.Length));
            foreach (var def in pick)
            {
                AddQuestProgress(def);
            }
        }

        private void AddQuestProgress(QuestDefinition def)
        {
            QuestProgress progress = new QuestProgress
            {
                questId = def.questId,
                currentCount = 0,
                completed = false,
                rewardClaimed = false,
                expirationTime = DateTime.Now.AddHours(def.durationHours).ToString("o")
            };
            saveData.activeQuests.Add(progress);
            activeQuestDefs.Add(def);
        }

        private static List<T> PickRandom<T>(T[] array, int count)
        {
            List<T> source = new List<T>(array);
            List<T> result = new List<T>();
            for (int i = 0; i < count && source.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, source.Count);
                result.Add(source[idx]);
                source.RemoveAt(idx);
            }
            return result;
        }

        // ── Progress ──────────────────────────────────────────────────

        public void ProgressQuest(QuestType type, int amount = 1)
        {
            if (amount <= 0) return;
            bool changed = false;

            for (int i = 0; i < saveData.activeQuests.Count; i++)
            {
                QuestProgress p = saveData.activeQuests[i];
                if (p.completed || p.rewardClaimed) continue;
                if (p.IsExpired()) continue;

                QuestDefinition def = FindDefinition(p.questId);
                if (def != null && def.questType == type)
                {
                    p.currentCount += amount;
                    if (p.currentCount >= def.targetCount)
                    {
                        p.currentCount = def.targetCount;
                        p.completed = true;
                    }
                    changed = true;
                }
            }

            if (changed)
            {
                SaveData();
                DispatchChanged();
            }
        }

        // ── Claim ─────────────────────────────────────────────────────

        public void ClaimReward(string questId)
        {
            QuestProgress progress = null;
            foreach (var p in saveData.activeQuests)
            {
                if (p.questId == questId)
                {
                    progress = p;
                    break;
                }
            }

            if (progress == null)
            {
                Debug.LogWarning($"[QuestManager] Quest {questId} not found");
                return;
            }

            if (!progress.completed)
            {
                Debug.LogWarning($"[QuestManager] Quest {questId} not completed yet");
                return;
            }

            if (progress.rewardClaimed)
            {
                Debug.LogWarning($"[QuestManager] Quest {questId} reward already claimed");
                return;
            }

            QuestDefinition def = FindDefinition(questId);
            if (def == null)
            {
                Debug.LogWarning($"[QuestManager] Definition for {questId} not found");
                return;
            }

            GrantReward(def.reward);
            progress.rewardClaimed = true;
            SaveData();
            DispatchChanged();
        }

        private void GrantReward(QuestReward reward)
        {
            if (reward == null) return;

            PlayerDataManager pData = PlayerDataManager.Instance;
            switch (reward.rewardType)
            {
                case QuestRewardType.Gold:
                    pData.AddGold(reward.quantity);
                    break;
                case QuestRewardType.Gems:
                    pData.AddGems(reward.quantity);
                    break;
                case QuestRewardType.Card:
                    string cardId = reward.rewardId;
                    if (string.IsNullOrEmpty(cardId))
                    {
                        var owned = pData.GetOwnedCards();
                        if (owned.Count > 0)
                            cardId = owned[UnityEngine.Random.Range(0, owned.Count)];
                        else
                        {
                            Debug.LogWarning("[QuestManager] No owned cards, skipping Card reward");
                            break;
                        }
                    }
                    pData.AddCard(cardId);
                    break;
                case QuestRewardType.Experience:
                    pData.AddExperience(reward.quantity);
                    break;
                case QuestRewardType.SeasonPassExp:
                    var sp = SeasonPassManager.Instance;
                    if (sp != null)
                        sp.AddExp(reward.quantity);
                    break;
            }
        }

        // ── Public Query ──────────────────────────────────────────────

        public List<QuestProgress> GetActiveQuests()
        {
            if (saveData == null || saveData.activeQuests == null)
                return new List<QuestProgress>();
            return new List<QuestProgress>(saveData.activeQuests);
        }

        public QuestDefinition GetDefinition(string questId)
        {
            return FindDefinition(questId);
        }

        public QuestProgress GetProgress(string questId)
        {
            if (saveData == null || saveData.activeQuests == null) return null;
            foreach (var p in saveData.activeQuests)
                if (p.questId == questId)
                    return p;
            return null;
        }

        public int GetCompletedQuestCount()
        {
            int count = 0;
            if (saveData == null || saveData.activeQuests == null) return 0;
            foreach (var p in saveData.activeQuests)
                if (p.completed && !p.rewardClaimed)
                    count++;
            return count;
        }

        // ── Static Integration Helper ─────────────────────────────────

        /// <summary>Call this from BattleManager when a battle ends to progress battle-related quests.</summary>
        public static void OnBattleEnded(bool isVictory, int trophiesGained, int goldGained, int damageDealt)
        {
            var mgr = Instance;
            mgr.ProgressQuest(QuestType.PlayBattles);
            if (isVictory) mgr.ProgressQuest(QuestType.WinBattles);
            if (trophiesGained > 0) mgr.ProgressQuest(QuestType.EarnTrophies, trophiesGained);
            if (goldGained > 0) mgr.ProgressQuest(QuestType.EarnGold, goldGained);
            if (damageDealt > 0) mgr.ProgressQuest(QuestType.DealDamage, damageDealt);
        }

        // ── Dispatch ──────────────────────────────────────────────────

        private void DispatchChanged()
        {
            if (OnQuestsChanged != null)
                OnQuestsChanged.Invoke();
        }
    }
}
