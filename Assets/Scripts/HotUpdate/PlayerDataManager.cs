using System;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game;
using KingdomWar.Game.Arena;
using KingdomWar.Game.Data;
namespace KingdomWar.HotUpdate
{
/// <summary>
/// 玩家数据管理�?/// 负责管理玩家的金币、宝石、卡片等核心数据
/// 使用PlayerPrefs实现数据持久�?/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    #region 单例模式
    
    private static PlayerDataManager instance;
    
    /// <summary>
    /// 获取玩家数据管理器单例实�?    /// 如果实例不存在则自动创建
    /// </summary>
    public static PlayerDataManager Instance
    {
        get
        {
                if (instance == null)
                {
                    GameObject obj = new GameObject("PlayerDataManager");
                    instance = obj.AddComponent<PlayerDataManager>();
                }
            return instance;
        }
    }
    
    #endregion

    #region PlayerPrefs键名
    
    private const string TrophiesKey = "Player_Trophies";
    private const string HighestTrophiesKey = "Player_HighestTrophies";
    private const string CurrentArenaKey = "Player_CurrentArena";
    private const string SeasonHighestKey = "Player_SeasonHighest";
    private const string SeasonRewardClaimedKey = "Player_SeasonRewardClaimed";
    private const string SeasonStartDateKey = "Player_SeasonStartDate";
    private const string NicknameKey = "Player_Nickname";
    private const string AvatarIndexKey = "Player_AvatarIndex";
    private const string TotalWinsKey = "Player_TotalWins";
    private const string TotalLossesKey = "Player_TotalLosses";
    private const string TotalDrawsKey = "Player_TotalDraws";
    
    #endregion
    
    #region 私有字段
    
    /// <summary>
    /// 玩家金币数量
    /// </summary>
    private int gold;
    
    /// <summary>
    /// 玩家宝石数量
    /// </summary>
    private int gems;
    
    /// <summary>
    /// 玩家拥有的卡片名称列�?    /// </summary>
    private List<string> ownedCards = new List<string>();
    
    /// <summary>
    /// 卡片数量统计字典
    /// Key: 卡片名称, Value: 拥有数量
    /// </summary>
    private Dictionary<string, int> cardCounts = new Dictionary<string, int>();
    
    /// <summary>
    /// 卡片等级字典
    /// Key: 卡片名称, Value: 等级 (1-13)
    /// </summary>
    private Dictionary<string, int> cardLevels = new Dictionary<string, int>();
    
    /// <summary>
    /// 卡片碎片字典
    /// Key: 卡片名称, Value: 碎片数量
    /// </summary>
    private Dictionary<string, int> cardFragments = new Dictionary<string, int>();
    
    /// <summary>
    /// 玩家当前奖杯数
    /// </summary>
    private int trophies;
    
    /// <summary>
    /// 玩家历史最高奖杯数
    /// </summary>
    private int highestTrophies;
    
    /// <summary>
    /// 玩家当前竞技场ID (存储为int)
    /// </summary>
    private int currentArena;
    
    /// <summary>
    /// 本赛季最高奖杯数
    /// </summary>
    private int seasonHighest;
    
    /// <summary>
    /// 本赛季奖励是否已领取
    /// </summary>
    private bool seasonRewardClaimed;
    
    /// <summary>
    /// 赛季开始日期
    /// </summary>
    private string seasonStartDate;
    private string nickname;
    private int avatarIndex;
    private int totalWins;
    private int totalLosses;
    private int totalDraws;
    
    /// <summary>
    /// Data repository for persistence. If non-null, used instead of PlayerPrefs.
    /// Falls back to PlayerPrefs if repository initialization fails.
    /// </summary>
    private IPlayerDataRepository dataRepository;
    
    #endregion

    #region 事件定义
    
    /// <summary>
    /// 金币数量变化事件
    /// 当金币数量发生改变时触发
    /// </summary>
    public event Action<int> OnGoldChanged;
    
    /// <summary>
    /// 宝石数量变化事件
    /// 当宝石数量发生改变时触发
    /// </summary>
    public event Action<int> OnGemsChanged;
    
    /// <summary>
    /// 卡片添加事件
    /// 当玩家获得新卡片时触�?    /// </summary>
    public event Action<string> OnCardAdded;
    
    /// <summary>
    /// 奖杯数量变化事件
    /// 当奖杯数量发生改变时触发
    /// </summary>
    public event Action<int> OnTrophiesChanged;
    
    /// <summary>
    /// 历史最高奖杯变化事件
    /// 当历史最高奖杯发生改变时触发
    /// </summary>
    public event Action<int> OnHighestTrophiesChanged;
    
    /// <summary>
    /// 赛季最高奖杯变化事件
    /// 当赛季最高奖杯发生改变时触发
    /// </summary>
    public event Action<int> OnSeasonHighestChanged;
    public event Action<string> OnNicknameChanged;
    public event Action<int> OnAvatarIndexChanged;
    public event Action<int, int, int> OnBattleStatsChanged;  // wins, losses, draws
    
    #endregion

    #region 生命周期方法
    
    /// <summary>
    /// Unity Awake方法
    /// 初始化单例实例并加载玩家数据
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRepository();
            LoadData();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Initialize data repository. Tries JsonPlayerDataRepository first,
    /// falls back to PlayerPrefs if it fails.
    /// </summary>
    private void InitializeRepository()
    {
        try
        {
            dataRepository = new JsonPlayerDataRepository();
            if (!dataRepository.HasKey("_migrated"))
            {
                MigrateFromPlayerPrefs();
            }
            Debug.Log("[PlayerDataManager] Using JsonPlayerDataRepository");
        }
        catch (Exception ex)
        {
            dataRepository = null;
            Debug.LogWarning($"[PlayerDataManager] JsonRepo failed: {ex.Message}, using PlayerPrefs");
        }
    }

    /// <summary>
    /// Migrate existing PlayerPrefs data to JSON file repository.
    /// Only runs once when first transitioning from PlayerPrefs to JSON.
    /// </summary>
    private void MigrateFromPlayerPrefs()
    {
        Debug.Log("[PlayerDataManager] Migrating PlayerPrefs data to JSON...");

        // Basic resources
        dataRepository.Save("Player_Gold", PlayerPrefs.GetInt("Player_Gold", GameConfig.Instance.defaultGold));
        dataRepository.Save("Player_Gems", PlayerPrefs.GetInt("Player_Gems", 100));

        // Card data (comma-separated string)
        string cardsData = PlayerPrefs.GetString("Player_Cards", "");
        if (!string.IsNullOrEmpty(cardsData))
        {
            string[] cards = cardsData.Split(',');
            List<string> cardList = new List<string>();
            foreach (string card in cards)
            {
                if (!string.IsNullOrEmpty(card))
                {
                    cardList.Add(card);
                }
            }
            dataRepository.SaveList("Player_Cards", cardList);
        }
        else
        {
            dataRepository.SaveList("Player_Cards", new List<string>());
        }

        // Card levels and fragments (JSON serialized int dictionaries)
        string levelsJson = PlayerPrefs.GetString("Player_CardLevels", "{}");
        Dictionary<string, int> levels = DeserializeIntDict(levelsJson);
        dataRepository.SaveDictionary("Player_CardLevels", levels);

        string fragmentsJson = PlayerPrefs.GetString("Player_CardFragments", "{}");
        Dictionary<string, int> fragments = DeserializeIntDict(fragmentsJson);
        dataRepository.SaveDictionary("Player_CardFragments", fragments);

        // Trophies and arena data
        dataRepository.Save(TrophiesKey, PlayerPrefs.GetInt(TrophiesKey, ArenaConfig.TROPHY_START));
        dataRepository.Save(HighestTrophiesKey, PlayerPrefs.GetInt(HighestTrophiesKey, 0));
        dataRepository.Save(CurrentArenaKey, PlayerPrefs.GetInt(CurrentArenaKey, (int)ArenaId.TrainingCamp));
        dataRepository.Save(SeasonHighestKey, PlayerPrefs.GetInt(SeasonHighestKey, 0));
        dataRepository.Save(SeasonRewardClaimedKey, PlayerPrefs.GetInt(SeasonRewardClaimedKey, 0));
        dataRepository.Save(SeasonStartDateKey, PlayerPrefs.GetString(SeasonStartDateKey, ""));
        dataRepository.Save(NicknameKey, PlayerPrefs.GetString(NicknameKey, "Player"));
        dataRepository.Save(AvatarIndexKey, PlayerPrefs.GetInt(AvatarIndexKey, 0));
        dataRepository.Save(TotalWinsKey, PlayerPrefs.GetInt(TotalWinsKey, 0));
        dataRepository.Save(TotalLossesKey, PlayerPrefs.GetInt(TotalLossesKey, 0));
        dataRepository.Save(TotalDrawsKey, PlayerPrefs.GetInt(TotalDrawsKey, 0));

        dataRepository.Save("_migrated", 1);
        dataRepository.SaveAll();

        Debug.Log("[PlayerDataManager] Migration complete");
    }

    /// <summary>
    /// Unity OnDestroy方法
    /// 清理事件订阅
    /// </summary>
    private void OnDestroy()
    {
        OnTrophiesChanged = null;
        OnHighestTrophiesChanged = null;
        OnSeasonHighestChanged = null;
        OnNicknameChanged = null;
        OnAvatarIndexChanged = null;
        OnBattleStatsChanged = null;
    }
    
    #endregion

    #region 数据加载与保�?    
    /// <summary>
    /// 从本地加载玩家数�?    /// 使用PlayerPrefs读取存储的数�?    /// </summary>
    private void LoadData()
    {
        if (dataRepository != null)
        {
            // 从JSON仓库加载数据
            gold = dataRepository.LoadInt("Player_Gold", GameConfig.Instance.defaultGold);
            gems = dataRepository.LoadInt("Player_Gems", 100);

            // 加载卡片数据（从列表加载，无需逗号分隔解析）
            List<string> cardsList = dataRepository.LoadList("Player_Cards");
            ownedCards = new List<string>(cardsList);
            cardCounts.Clear();
            foreach (string card in cardsList)
            {
                if (cardCounts.ContainsKey(card))
                    cardCounts[card]++;
                else
                    cardCounts[card] = 1;
            }

            // 加载卡片等级和碎片
            cardLevels = dataRepository.LoadDictionary("Player_CardLevels");
            cardFragments = dataRepository.LoadDictionary("Player_CardFragments");

            // 加载奖杯和竞技场数据
            trophies = dataRepository.LoadInt(TrophiesKey, ArenaConfig.TROPHY_START);
            highestTrophies = dataRepository.LoadInt(HighestTrophiesKey, 0);
            currentArena = dataRepository.LoadInt(CurrentArenaKey, (int)ArenaId.TrainingCamp);
            seasonHighest = dataRepository.LoadInt(SeasonHighestKey, 0);
            seasonRewardClaimed = dataRepository.LoadInt(SeasonRewardClaimedKey, 0) == 1;
            seasonStartDate = dataRepository.LoadString(SeasonStartDateKey, "");
            nickname = dataRepository.LoadString(NicknameKey, "Player");
            avatarIndex = dataRepository.LoadInt(AvatarIndexKey, 0);
            totalWins = dataRepository.LoadInt(TotalWinsKey, 0);
            totalLosses = dataRepository.LoadInt(TotalLossesKey, 0);
            totalDraws = dataRepository.LoadInt(TotalDrawsKey, 0);
        }
        else
        {
            // 原有PlayerPrefs加载逻辑（保持不变）
            gold = PlayerPrefs.GetInt("Player_Gold", GameConfig.Instance.defaultGold);
            gems = PlayerPrefs.GetInt("Player_Gems", 100);

            // load owned cards
            string cardsData = PlayerPrefs.GetString("Player_Cards", "");
            if (!string.IsNullOrEmpty(cardsData))
            {
                // parse card data (format: card names separated by comma)
                string[] cards = cardsData.Split(',');
                foreach (string card in cards)
                {
                    if (!string.IsNullOrEmpty(card))
                    {
                        Debug.Log($"加载卡片: {card}");
                        ownedCards.Add(card);
                        // 统计卡片数量
                        if (cardCounts.ContainsKey(card))
                            cardCounts[card]++;
                        else
                            cardCounts[card] = 1;
                    }
                }
            }

            // 加载卡片等级
            string levelsJson = PlayerPrefs.GetString("Player_CardLevels", "{}");
            cardLevels = DeserializeIntDict(levelsJson);

            // 加载卡片碎片
            string fragmentsJson = PlayerPrefs.GetString("Player_CardFragments", "{}");
            cardFragments = DeserializeIntDict(fragmentsJson);

            // 加载奖杯和竞技场数据
            trophies = PlayerPrefs.GetInt(TrophiesKey, ArenaConfig.TROPHY_START);
            highestTrophies = PlayerPrefs.GetInt(HighestTrophiesKey, 0);
            currentArena = PlayerPrefs.GetInt(CurrentArenaKey, (int)ArenaId.TrainingCamp);
            seasonHighest = PlayerPrefs.GetInt(SeasonHighestKey, 0);
            seasonRewardClaimed = PlayerPrefs.GetInt(SeasonRewardClaimedKey, 0) == 1;
            seasonStartDate = PlayerPrefs.GetString(SeasonStartDateKey, "");
            nickname = PlayerPrefs.GetString(NicknameKey, "Player");
            avatarIndex = PlayerPrefs.GetInt(AvatarIndexKey, 0);
            totalWins = PlayerPrefs.GetInt(TotalWinsKey, 0);
            totalLosses = PlayerPrefs.GetInt(TotalLossesKey, 0);
            totalDraws = PlayerPrefs.GetInt(TotalDrawsKey, 0);
        }
    }

    #region JSON序列化辅助
    
    /// <summary>
    /// 将字典序列化为JSON字符串
    /// </summary>
    private string SerializeIntDict(Dictionary<string, int> dict)
    {
        DictWrapper wrapper = new DictWrapper();
        foreach (var kvp in dict)
        {
            wrapper.keys.Add(kvp.Key);
            wrapper.values.Add(kvp.Value);
        }
        return JsonUtility.ToJson(wrapper);
    }
    
    /// <summary>
    /// 从JSON字符串反序列化字典
    /// </summary>
    private Dictionary<string, int> DeserializeIntDict(string json)
    {
        DictWrapper wrapper = JsonUtility.FromJson<DictWrapper>(json);
        Dictionary<string, int> result = new Dictionary<string, int>();
        if (wrapper != null && wrapper.keys != null)
        {
            for (int i = 0; i < wrapper.keys.Count; i++)
            {
                result[wrapper.keys[i]] = wrapper.values[i];
            }
        }
        return result;
    }
    
    [System.Serializable]
    private class DictWrapper
    {
        public List<string> keys = new List<string>();
        public List<int> values = new List<int>();
    }
    
    #endregion

    /// <summary>
    /// 保存玩家数据到本�?    /// 使用PlayerPrefs进行持久化存�?    /// </summary>
    public void SaveData()
    {
        if (dataRepository != null)
        {
            // 保存到JSON仓库
            dataRepository.Save("Player_Gold", gold);
            dataRepository.Save("Player_Gems", gems);

            // 保存卡片数据
            List<string> cardList = new List<string>();
            foreach (var kvp in cardCounts)
            {
                for (int i = 0; i < kvp.Value; i++)
                {
                    cardList.Add(kvp.Key);
                }
            }
            dataRepository.SaveList("Player_Cards", cardList);

            // 保存卡片等级和碎片
            dataRepository.SaveDictionary("Player_CardLevels", cardLevels);
            dataRepository.SaveDictionary("Player_CardFragments", cardFragments);

            // 保存奖杯和竞技场数据
            dataRepository.Save(TrophiesKey, trophies);
            dataRepository.Save(HighestTrophiesKey, highestTrophies);
            dataRepository.Save(CurrentArenaKey, currentArena);
            dataRepository.Save(SeasonHighestKey, seasonHighest);
            dataRepository.Save(SeasonRewardClaimedKey, seasonRewardClaimed ? 1 : 0);
            dataRepository.Save(SeasonStartDateKey, seasonStartDate ?? "");
            dataRepository.Save(NicknameKey, nickname ?? "Player");
            dataRepository.Save(AvatarIndexKey, avatarIndex);
            dataRepository.Save(TotalWinsKey, totalWins);
            dataRepository.Save(TotalLossesKey, totalLosses);
            dataRepository.Save(TotalDrawsKey, totalDraws);
            dataRepository.SaveAll();
        }
        else
        {
            // 原有PlayerPrefs保存逻辑（保持不变）
            PlayerPrefs.SetInt("Player_Gold", gold);
            PlayerPrefs.SetInt("Player_Gems", gems);

            // 保存卡片数据
            List<string> cardList = new List<string>();
            foreach (var kvp in cardCounts)
            {
                // 根据数量重复添加卡片名称
                for (int i = 0; i < kvp.Value; i++)
                {
                    cardList.Add(kvp.Key);
                }
            }
            PlayerPrefs.SetString("Player_Cards", string.Join(",", cardList));

            // 保存卡片等级和碎片
            PlayerPrefs.SetString("Player_CardLevels", SerializeIntDict(cardLevels));
            PlayerPrefs.SetString("Player_CardFragments", SerializeIntDict(cardFragments));

            // 保存奖杯和竞技场数据
            PlayerPrefs.SetInt(TrophiesKey, trophies);
            PlayerPrefs.SetInt(HighestTrophiesKey, highestTrophies);
            PlayerPrefs.SetInt(CurrentArenaKey, currentArena);
            PlayerPrefs.SetInt(SeasonHighestKey, seasonHighest);
            PlayerPrefs.SetInt(SeasonRewardClaimedKey, seasonRewardClaimed ? 1 : 0);
            PlayerPrefs.SetString(SeasonStartDateKey, seasonStartDate ?? "");
            PlayerPrefs.SetString(NicknameKey, nickname ?? "Player");
            PlayerPrefs.SetInt(AvatarIndexKey, avatarIndex);
            PlayerPrefs.SetInt(TotalWinsKey, totalWins);
            PlayerPrefs.SetInt(TotalLossesKey, totalLosses);
            PlayerPrefs.SetInt(TotalDrawsKey, totalDraws);
            PlayerPrefs.Save();
        }
    }
    
    #endregion

    #region 金币管理
    
    /// <summary>
    /// 获取当前金币数量
    /// </summary>
    /// <returns>金币数量</returns>
    public int GetGold() => gold;
    
    /// <summary>
    /// 增加金币
    /// </summary>
    /// <param name="amount">增加的数�?/param>
    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
        SaveData();
    }

    /// <summary>
    /// 消耗金�?    /// </summary>
    /// <param name="amount">消耗的数量</param>
    /// <returns>如果金币足够并成功消耗返回true，否则返回false</returns>
    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            OnGoldChanged?.Invoke(gold);
            SaveData();
            return true;
        }
        return false;
    }
    
    #endregion

    #region 宝石管理
    
    /// <summary>
    /// 获取当前宝石数量
    /// </summary>
    /// <returns>宝石数量</returns>
    public int GetGems() => gems;

    /// <summary>
    /// 增加宝石
    /// </summary>
    /// <param name="amount">增加的数�?/param>
    public void AddGems(int amount)
    {
        gems += amount;
        OnGemsChanged?.Invoke(gems);
        SaveData();
    }

    /// <summary>
    /// 消耗宝�?    /// </summary>
    /// <param name="amount">消耗的数量</param>
    /// <returns>如果宝石足够并成功消耗返回true，否则返回false</returns>
    public bool SpendGems(int amount)
    {
        if (gems >= amount)
        {
            gems -= amount;
            OnGemsChanged?.Invoke(gems);
            SaveData();
            return true;
        }
        return false;
    }
    
    #endregion

    #region 卡片管理
    
    /// <summary>
    /// 添加卡片到玩家背�?    /// </summary>
    /// <param name="cardName">卡片名称</param>
    public void AddCard(string cardName)
    {
        ownedCards.Add(cardName);
        
        // 更新卡片计数
        if (cardCounts.ContainsKey(cardName))
            cardCounts[cardName]++;
        else
            cardCounts[cardName] = 1;
        
        // 触发事件并保�?        OnCardAdded?.Invoke(cardName);
        SaveData();
    }

    /// <summary>
    /// 获取玩家拥有的所有卡片名称列�?    /// </summary>
    /// <returns>卡片名称列表的副�?/returns>
    public List<string> GetOwnedCards()
    {
        return new List<string>(ownedCards);
    }

    /// <summary>
    /// 获取指定卡片的拥有数�?    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <returns>拥有数量，如果没有则返回0</returns>
    public int GetCardCount(string cardName)
    {
        return cardCounts.ContainsKey(cardName) ? cardCounts[cardName] : 0;
    }
    
    #endregion

    #region 卡片等级管理
    
    /// <summary>
    /// 获取卡片等级
    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <returns>卡片等级，默认为1</returns>
    public int GetCardLevel(string cardName)
    {
        return cardLevels.ContainsKey(cardName) ? cardLevels[cardName] : 1;
    }
    
    /// <summary>
    /// 设置卡片等级
    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <param name="level">等级 (1-13)</param>
    public void SetCardLevel(string cardName, int level)
    {
        level = Mathf.Clamp(level, 1, 13);
        cardLevels[cardName] = level;
        SaveData();
    }
    
    #endregion

    #region 卡片碎片管理
    
    /// <summary>
    /// 获取卡片碎片数量
    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <returns>碎片数量，默认为0</returns>
    public int GetCardFragments(string cardName)
    {
        return cardFragments.ContainsKey(cardName) ? cardFragments[cardName] : 0;
    }
    
    /// <summary>
    /// 添加卡片碎片
    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <param name="count">增加的碎片数量</param>
    public void AddCardFragments(string cardName, int count)
    {
        if (cardFragments.ContainsKey(cardName))
            cardFragments[cardName] += count;
        else
            cardFragments[cardName] = count;
        SaveData();
    }
    
    /// <summary>
    /// 消耗卡片碎片
    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <param name="count">消耗的碎片数量</param>
    /// <returns>如果碎片足够并成功消耗返回true，否则返回false</returns>
    public bool SpendFragments(string cardName, int count)
    {
        int current = GetCardFragments(cardName);
        if (current >= count)
        {
            cardFragments[cardName] = current - count;
            SaveData();
            return true;
        }
        return false;
    }
    
    #endregion

    #region 奖杯与竞技场管理
    
    /// <summary>
    /// 获取当前奖杯数量
    /// </summary>
    /// <returns>奖杯数量</returns>
    public int GetTrophies() => trophies;
    
    /// <summary>
    /// 设置当前奖杯数量
    /// </summary>
    /// <param name="value">新的奖杯数量</param>
    /// <param name="saveImmediately">是否立即保存到PlayerPrefs，批量操作时可设为false后手动调用SaveData()</param>
    public void SetTrophies(int value, bool saveImmediately = true)
    {
        trophies = value;
        if (saveImmediately) SaveData();
        OnTrophiesChanged?.Invoke(trophies);
    }
    
    /// <summary>
    /// 获取历史最高奖杯数
    /// </summary>
    /// <returns>历史最高奖杯数</returns>
    public int GetHighestTrophies() => highestTrophies;
    
    /// <summary>
    /// 设置历史最高奖杯数
    /// </summary>
    /// <param name="value">新的历史最高奖杯数</param>
    /// <param name="saveImmediately">是否立即保存到PlayerPrefs，批量操作时可设为false后手动调用SaveData()</param>
    public void SetHighestTrophies(int value, bool saveImmediately = true)
    {
        highestTrophies = value;
        if (saveImmediately) SaveData();
        OnHighestTrophiesChanged?.Invoke(highestTrophies);
    }
    
    /// <summary>
    /// 获取本赛季最高奖杯数
    /// </summary>
    /// <returns>本赛季最高奖杯数</returns>
    public int GetSeasonHighest() => seasonHighest;
    
    /// <summary>
    /// 设置本赛季最高奖杯数
    /// </summary>
    /// <param name="value">新的赛季最高奖杯数</param>
    /// <param name="saveImmediately">是否立即保存到PlayerPrefs，批量操作时可设为false后手动调用SaveData()</param>
    public void SetSeasonHighest(int value, bool saveImmediately = true)
    {
        seasonHighest = value;
        if (saveImmediately) SaveData();
        OnSeasonHighestChanged?.Invoke(seasonHighest);
    }
    
    /// <summary>
    /// 获取当前竞技场ID
    /// </summary>
    /// <returns>当前竞技场ID (int)</returns>
    public int GetCurrentArena() => currentArena;
    
    /// <summary>
    /// 设置当前竞技场ID
    /// </summary>
    /// <param name="value">竞技场ID</param>
    /// <param name="saveImmediately">是否立即保存到PlayerPrefs，批量操作时可设为false后手动调用SaveData()</param>
    public void SetCurrentArena(int value, bool saveImmediately = true)
    {
        currentArena = value;
        if (saveImmediately) SaveData();
    }
    
    /// <summary>
    /// 检查本赛季奖励是否已领取
    /// </summary>
    /// <returns>是否已领取</returns>
    public bool IsSeasonRewardClaimed() => seasonRewardClaimed;
    
    /// <summary>
    /// 设置本赛季奖励领取状态
    /// </summary>
    /// <param name="value">是否已领取</param>
    public void SetSeasonRewardClaimed(bool value)
    {
        seasonRewardClaimed = value;
        SaveData();
    }
    
    /// <summary>
    /// 获取赛季开始日期
    /// </summary>
    /// <returns>赛季开始日期字符串</returns>
    public string GetSeasonStartDate() => seasonStartDate;
    
    /// <summary>
    /// 设置赛季开始日期
    /// </summary>
    /// <param name="date">赛季开始日期字符串</param>
    public void SetSeasonStartDate(string date)
    {
        seasonStartDate = date;
        SaveData();
    }
    
    #endregion

    #region 玩家资料与统计

    public string GetNickname() => nickname;

    public void SetNickname(string value)
    {
        nickname = value ?? "Player";
        SaveData();
        OnNicknameChanged?.Invoke(nickname);
    }

    public int GetAvatarIndex() => avatarIndex;

    public void SetAvatarIndex(int value)
    {
        avatarIndex = Mathf.Clamp(value, 0, 20);
        SaveData();
        OnAvatarIndexChanged?.Invoke(avatarIndex);
    }

    public int GetTotalWins() => totalWins;
    public int GetTotalLosses() => totalLosses;
    public int GetTotalDraws() => totalDraws;
    public int GetTotalBattles() => totalWins + totalLosses + totalDraws;

    public float GetWinRate()
    {
        int total = GetTotalBattles();
        return total > 0 ? (float)totalWins / total * 100f : 0f;
    }

    public void RecordBattleResult(bool isVictory, bool isDraw)
    {
        if (isDraw)
            totalDraws++;
        else if (isVictory)
            totalWins++;
        else
            totalLosses++;
        
        SaveData();
        OnBattleStatsChanged?.Invoke(totalWins, totalLosses, totalDraws);
    }

    #endregion
    
    #region 数据重置
    
    /// <summary>
    /// 重置所有玩家数�?    /// 将金币、宝石恢复默认值，清空所有卡�?    /// </summary>
    public void ResetData()
    {
        gold = GameConfig.Instance.defaultGold;
        gems = 100;
        ownedCards.Clear();
        cardCounts.Clear();
        cardLevels.Clear();
        cardFragments.Clear();
        trophies = 0;
        highestTrophies = 0;
        currentArena = (int)ArenaId.TrainingCamp;
        seasonHighest = 0;
        seasonRewardClaimed = false;
        seasonStartDate = "";
        nickname = "Player";
        avatarIndex = 0;
        totalWins = 0;
        totalLosses = 0;
        totalDraws = 0;
        SaveData();
    }
    
    #endregion
}

}
