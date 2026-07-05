using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KingdomWar.Game.Cards
{
    public class CardDatabase : MonoBehaviour
{
    private static CardDatabase instance;
    public static CardDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CardDatabase>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("CardDatabase");
                    instance = obj.AddComponent<CardDatabase>();
                }
            }
            return instance;
        }
    }
    
    [Header("卡牌数据")]
    public List<CardData> allCards = new List<CardData>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCardData();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadCardData()
    {
        CardData[] cardDatas = Resources.LoadAll<CardData>("Cards");
        
        if (cardDatas != null && cardDatas.Length > 0)
        {
            allCards.AddRange(cardDatas);
        }
        
        Debug.Log("Loaded " + allCards.Count + " cards into database");
    }
    
    public List<CardData> GetAllCards()
    {
        return allCards;
    }
    
    public CardData GetCardByName(string cardName)
    {
        return allCards.Find(card => card.cardName == cardName);
    }
    
    public List<CardData> GetCardsByType(CardType type)
    {
        return allCards.FindAll(card => card.cardType == type);
    }
    
    public List<CardData> GetCardsByRarity(int rarity)
    {
        return allCards.FindAll(card => card.rarity == rarity);
    }
    
    public List<CardData> GetCardsByElixirCost(int minCost, int maxCost)
    {
        return allCards.FindAll(card => card.elixirCost >= minCost && card.elixirCost <= maxCost);
    }
    
    /// <summary>
    /// 添加卡片到数据库
    /// </summary>
    /// <param name="cardData">卡片数据</param>
    public void AddCard(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("Cannot add null card data!");
            return;
        }
        
        // 检查卡片是否已存在
        if (!allCards.Exists(card => card.cardName == cardData.cardName))
        {
            allCards.Add(cardData);
            Debug.Log("Added card to database: " + cardData.cardName);
        }
        else
        {
            Debug.LogWarning("Card already exists in database: " + cardData.cardName);
        }
    }
    
    /// <summary>
    /// 清除所有卡片
    /// </summary>
    public void ClearCards()
    {
        allCards.Clear();
        Debug.Log("Cleared all cards from database");
    }
    
    /// <summary>
    /// 重新加载卡片数据
    /// </summary>
    public void ReloadCardData()
    {
        allCards.Clear();
        LoadCardData();
        Debug.Log("Reloaded card data");
    }
    }
}