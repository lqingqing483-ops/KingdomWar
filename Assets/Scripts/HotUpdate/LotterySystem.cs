using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KingdomWar.Game.Cards;
namespace KingdomWar.HotUpdate
{
/// <summary>
/// 抽奖系统核心�?/// 负责管理抽奖逻辑、卡片抽取、玩家数据同步等功能
/// 支持每日免费抽奖和金币付费抽奖两种模�?/// </summary>
public class LotterySystem : MonoBehaviour
{
    #region 单例模式
    
    private static LotterySystem instance;
    
    /// <summary>
    /// 获取抽奖系统单例实例
    /// 如果实例不存在则自动创建
    /// </summary>
    public static LotterySystem Instance
    {
        get
        {
                if (instance == null)
                {
                    GameObject obj = new GameObject("LotterySystem");
                    instance = obj.AddComponent<LotterySystem>();
                }
            return instance;
        }
    }
    
    #endregion

    #region 配置参数
    
    /// <summary>
    /// 每日免费抽奖次数
    /// </summary>
    [Header("抽奖配置")]
    public int dailyFreeDraws = 1;
    
    /// <summary>
    /// 单次抽奖消耗的金币数量
    /// </summary>
    public int drawCost = 100;
    
    #endregion

    #region 私有字段
    
    /// <summary>
    /// 剩余免费抽奖次数
    /// </summary>
    private int remainingFreeDraws;
    
    /// <summary>
    /// 累计抽奖总次�?    /// </summary>
    private int totalDraws;
    
    /// <summary>
    /// 已获得的卡片列表
    /// </summary>
    private List<CardData> obtainedCards = new List<CardData>();
    
    #endregion

    #region 事件定义
    
    /// <summary>
    /// 卡片获得事件
    /// 当玩家通过抽奖获得卡片时触�?    /// </summary>
    public event Action<CardData> OnCardObtained;
    
    /// <summary>
    /// 抽奖次数变化事件
    /// 当免费抽奖次数发生变化时触发
    /// </summary>
    public event Action<int> OnDrawCountChanged;
    
    #endregion

    #region 生命周期方法
    
    /// <summary>
    /// Unity Awake方法
    /// 初始化单例实例和抽奖系统
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化抽奖系�?    /// 设置默认值并加载玩家数据
    /// </summary>
    private void Initialize()
    {
        remainingFreeDraws = dailyFreeDraws;
        totalDraws = 0;
        LoadPlayerData();
    }
    
    #endregion

    #region 抽奖次数管理
    
    /// <summary>
    /// 重置每日免费抽奖次数
    /// 通常在每日首次登录时调用
    /// </summary>
    public void ResetDailyDraws()
    {
        remainingFreeDraws = dailyFreeDraws;
        OnDrawCountChanged?.Invoke(remainingFreeDraws);
        SavePlayerData();
    }

    /// <summary>
    /// 检查玩家是否可以抽�?    /// </summary>
    /// <returns>如果有免费次数或足够金币返回true，否则返回false</returns>
    public bool CanDraw()
    {
        return remainingFreeDraws > 0 || HasEnoughCurrency();
    }

    /// <summary>
    /// 检查玩家是否有足够的金币进行付费抽�?    /// </summary>
    /// <returns>金币足够返回true，否则返回false</returns>
    public bool HasEnoughCurrency()
    {
        return PlayerDataManager.Instance.GetGold() >= drawCost;
    }

    /// <summary>
    /// 检查是否有免费抽奖次数
    /// </summary>
    /// <returns>有免费次数返回true，否则返回false</returns>
    public bool IsFreeDrawAvailable()
    {
        return remainingFreeDraws > 0;
    }
    
    #endregion

    #region 抽奖核心逻辑
    
    /// <summary>
    /// 执行抽奖
    /// 根据玩家状态选择免费或付费抽奖，并返回抽奖结�?    /// </summary>
    /// <returns>抽奖结果对象，包含成功状态、卡片信息和消息</returns>
    public LotteryResult Draw()
    {
        // 检查是否可以抽奖
        if (!CanDraw())
        {
            return new LotteryResult { success = false, message = "insufficient draw count or coins" };
        }

        // 消耗免费次数或金币
        if (remainingFreeDraws > 0)
        {
            remainingFreeDraws--;
        }
        else if (HasEnoughCurrency())
        {
            SpendGold();
        }

        // 增加抽奖总次�?        totalDraws++;
        
        // 执行随机抽卡
        CardData drawnCard = DrawRandomCard();
        
        if (drawnCard != null)
        {
            // 抽卡成功，保存数�?            obtainedCards.Add(drawnCard);
            PlayerDataManager.Instance.AddCard(drawnCard.cardName);
            
            // 触发事件
            OnCardObtained?.Invoke(drawnCard);
            OnDrawCountChanged?.Invoke(remainingFreeDraws);
            SavePlayerData();

            return new LotteryResult 
            {
                success = true, 
                card = drawnCard,
                message = $"恭喜获得: {drawnCard.cardName}"
            };
        }

        return new LotteryResult { success = false, message = "抽奖失败" };
    }

    /// <summary>
    /// 随机抽取一张卡�?    /// 使用权重随机算法，稀有度越高的卡片概率越�?    /// </summary>
    /// <returns>抽取到的卡片数据，如果卡牌库为空则返回null</returns>
    private CardData DrawRandomCard()
    {
        // get all available cards
        List<CardData> allCards = CardDatabase.Instance.GetAllCards();
        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogError("No cards available in database");
            return null;
        }

        // calculate total weight
        int totalWeight = 0;
        List<int> weights = new List<int>();
        
        foreach (var card in allCards)
        {
            int weight = GetRarityWeight(card.rarity);
            totalWeight += weight;
            weights.Add(weight);
        }

        // 根据权重随机选择卡片
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < allCards.Count; i++)
        {
            cumulativeWeight += weights[i];
            if (randomValue < cumulativeWeight)
            {
                return allCards[i];
            }
        }

        // 默认返回随机卡片（理论上不会执行到这里）
        return allCards[UnityEngine.Random.Range(0, allCards.Count)];
    }

    /// <summary>
    /// 根据稀有度获取权重�?    /// 权重值越高，抽中概率越大
    /// </summary>
    /// <param name="rarity">稀有度等级 (1-普�? 2-稀�? 3-史诗, 4-传说)</param>
    /// <returns>对应的权重�?/returns>
    private int GetRarityWeight(int rarity)
    {
        switch (rarity)
        {
            case 1: return 100;     // 普�?- 最高概�?
            case 2: return 50;      // 稀�?- 中等概率
            case 3: return 20;      // 史诗 - 较低概率
            case 4: return 5;       // Legendary - lowest probability
            default:
                return 100;
        }
    }
    
    /// <summary>
    /// 消耗金币进行抽�?    /// </summary>
    /// <returns>是否消耗成�?/returns>
    private void SpendGold()
    {
        PlayerDataManager.Instance.SpendGold(drawCost);
    }
    
    #endregion

    #region 数据获取方法
    
    /// <summary>
    /// 获取剩余免费抽奖次数
    /// </summary>
    /// <returns>剩余免费次数</returns>
    public int GetRemainingFreeDraws()
    {
        return remainingFreeDraws;
    }

    /// <summary>
    /// 获取累计抽奖总次�?    /// </summary>
    /// <returns>抽奖总次�?/returns>
    public int GetTotalDraws()
    {
        return totalDraws;
    }

    /// <summary>
    /// 获取已获得的卡片列表副本
    /// </summary>
    /// <returns>卡片列表的新副本</returns>
    public List<CardData> GetObtainedCards()
    {
        return new List<CardData>(obtainedCards);
    }
    
    #endregion

    #region 数据持久�?    
    /// <summary>
    /// 保存玩家抽奖数据到本�?    /// 使用PlayerPrefs进行持久化存�?    /// </summary>
    private void SavePlayerData()
    {
        PlayerPrefs.SetInt("Lottery_FreeDraws", remainingFreeDraws);
        PlayerPrefs.SetInt("Lottery_TotalDraws", totalDraws);
        PlayerPrefs.SetString("Lottery_LastSave", DateTime.Now.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从本地加载玩家抽奖数�?    /// 如果是新的一天，会自动重置免费抽奖次�?    /// </summary>
    private void LoadPlayerData()
    {
        remainingFreeDraws = PlayerPrefs.GetInt("Lottery_FreeDraws", dailyFreeDraws);
        totalDraws = PlayerPrefs.GetInt("Lottery_TotalDraws", 0);

        // check if daily free draws need reset
        string lastSave = PlayerPrefs.GetString("Lottery_LastSave", "");
        if (!string.IsNullOrEmpty(lastSave))
        {
            if (DateTime.TryParse(lastSave, out DateTime saveTime))
            {
                // if last save was before today, reset free draws
                if (DateTime.Now.Date > saveTime.Date)
                {
                    ResetDailyDraws();
                }
            }
        }
    }
    
    #endregion
}

/// <summary>
/// 抽奖结果�?/// 封装单次抽奖的结果信�?/// </summary>
public class LotteryResult
{
    /// <summary>
    /// 抽奖是否成功
    /// </summary>
    public bool success;
    
    /// <summary>
    /// 抽到的卡片数据（成功时有效）
    /// </summary>
    public CardData card;
    
    /// <summary>
    /// 结果消息（用于UI显示�?    /// </summary>
    public string message;
}

}
