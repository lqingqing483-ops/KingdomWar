using UnityEngine;
using System.IO;
using KingdomWar.Game.Cards;
namespace KingdomWar.Game.Cards
{
public class CardRuntimeCreator : MonoBehaviour
{
    [Header("Runtime Card Creation")]
    public bool createCardsOnStart = true;
    public bool overwriteExistingCards = false;
    
    private void Start()
    {
        if (createCardsOnStart)
        {
            CreateRuntimeCards();
        }
    }
    
    /// <summary>
    /// 在运行时创建卡片数据
    /// </summary>
    public void CreateRuntimeCards()
    {
        // 检查CardDatabase是否存在
        if (CardDatabase.Instance == null)
        {
            Debug.LogError("CardDatabase instance not found!");
            return;
        }
        
        // check if cards need to be created
        if (CardDatabase.Instance.GetAllCards().Count > 0 && !overwriteExistingCards)
        {
            Debug.Log("Cards already exist. Skipping creation.");
            return;
        }
        
        Debug.Log("Creating runtime cards...");
        
        // 创建测试卡片数据
        CreateRuntimeCard("骑士", CardType.Unit, 3, 1, 
            new UnitData { health = 2000, damage = 200, attackSpeed = 1.2f, moveSpeed = 1.5f, attackRange = 1.0f, deployTime = 1.0f });
        
        CreateRuntimeCard("Archer", CardType.Unit, 3, 1, 
            new UnitData { health = 700, damage = 120, attackSpeed = 1.0f, moveSpeed = 1.2f, attackRange = 5.0f, deployTime = 1.0f });
        
        CreateRuntimeCard("火球", CardType.Spell, 4, 1, 
            new SpellData { damage = 1200, radius = 2.5f, duration = 0.5f, deployTime = 0.0f });
        
        CreateRuntimeCard("巨人", CardType.Unit, 5, 2, 
            new UnitData { health = 3000, damage = 150, attackSpeed = 1.5f, moveSpeed = 1.0f, attackRange = 1.0f, deployTime = 1.5f });
        
        CreateRuntimeCard("闪电", CardType.Spell, 6, 3, 
            new SpellData { damage = 1000, radius = 3.0f, duration = 0.5f, deployTime = 0.0f });
        
        CreateRuntimeCard("皮卡超人", CardType.Unit, 7, 4, 
            new UnitData { health = 3200, damage = 400, attackSpeed = 1.8f, moveSpeed = 1.0f, attackRange = 1.0f, deployTime = 1.5f });
        
        CreateRuntimeCard("Elixir Collector", CardType.Building, 6, 3, 
            new BuildingData { health = 1200, damage = 0, attackSpeed = 0, attackRange = 0, duration = 60.0f, deployTime = 2.0f });
        
        CreateRuntimeCard("电击", CardType.Spell, 2, 1, 
            new SpellData { damage = 200, radius = 3.0f, duration = 0.5f, deployTime = 0.0f });
        
        Debug.Log("Created " + CardDatabase.Instance.GetAllCards().Count + " runtime cards");
    }
    
    /// <summary>
    /// 创建运行时卡片数�?    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <param name="cardType">卡片类型</param>
    /// <param name="elixirCost">圣水消�?/param>
    /// <param name="rarity">稀有度</param>
    /// <param name="cardData">卡片特定数据</param>
    private void CreateRuntimeCard(string cardName, CardType cardType, int elixirCost, int rarity, object cardData)
    {
        // 创建新的CardData
        CardData card = ScriptableObject.CreateInstance<CardData>();
        card.cardName = cardName;
        card.cardType = cardType;
        card.elixirCost = elixirCost;
        card.rarity = rarity;
        
        // 设置卡片特定数据
        switch (cardType)
        {
            case CardType.Unit:
                card.unitData = (UnitData)cardData;
                break;
            case CardType.Spell:
                card.spellData = (SpellData)cardData;
                break;
            case CardType.Building:
                card.buildingData = (BuildingData)cardData;
                break;
        }
        
        // 添加到CardDatabase
        CardDatabase.Instance.AddCard(card);
        Debug.Log("Created runtime card: " + cardName);
    }
    
    /// <summary>
    /// 清除所有运行时卡片
    /// </summary>
    public void ClearRuntimeCards()
    {
        if (CardDatabase.Instance != null)
        {
            CardDatabase.Instance.ClearCards();
            Debug.Log("Cleared all runtime cards");
        }
        else
        {
            Debug.LogError("CardDatabase instance not found!");
        }
    }
}

}
