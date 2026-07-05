using UnityEditor;
using UnityEngine;
using System.IO;
using KingdomWar.Game.Cards;
namespace KingdomWar.Editor
{
public class CardAssetCreator
{
    [MenuItem("Tools/Card System/Create Test Cards")]
    public static void CreateTestCards()
    {
        // 确保Cards目录存在
        string cardsPath = "Assets/Resources/Cards";
        if (!Directory.Exists(cardsPath))
        {
            Directory.CreateDirectory(cardsPath);
        }
        
        // 创建测试卡片
        CreateCardAsset("骑士", CardType.Unit, 3, 1, 
            new UnitData { health = 2000, damage = 200, attackSpeed = 1.2f, moveSpeed = 1.5f, attackRange = 1.0f, deployTime = 1.0f });
        
        CreateCardAsset("Archer", CardType.Unit, 3, 1, 
            new UnitData { health = 700, damage = 120, attackSpeed = 1.0f, moveSpeed = 1.2f, attackRange = 5.0f, deployTime = 1.0f });
        
        CreateCardAsset("火球", CardType.Spell, 4, 1, 
            new SpellData { damage = 1200, radius = 2.5f, duration = 0.5f, deployTime = 0.0f });
        
        CreateCardAsset("巨人", CardType.Unit, 5, 2, 
            new UnitData { health = 3000, damage = 150, attackSpeed = 1.5f, moveSpeed = 1.0f, attackRange = 1.0f, deployTime = 1.5f });
        
        CreateCardAsset("闪电", CardType.Spell, 6, 3, 
            new SpellData { damage = 1000, radius = 3.0f, duration = 0.5f, deployTime = 0.0f });
        
        CreateCardAsset("皮卡超人", CardType.Unit, 7, 4, 
            new UnitData { health = 3200, damage = 400, attackSpeed = 1.8f, moveSpeed = 1.0f, attackRange = 1.0f, deployTime = 1.5f });
        
        CreateCardAsset("ElixirCollector", CardType.Building, 6, 3, 
            new BuildingData { health = 1200, damage = 0, attackSpeed = 0, attackRange = 0, duration = 60.0f, deployTime = 2.0f });
        
        CreateCardAsset("电击", CardType.Spell, 2, 1, 
            new SpellData { damage = 200, radius = 3.0f, duration = 0.5f, deployTime = 0.0f });
        
        Debug.Log("Created test cards successfully!");
        AssetDatabase.Refresh();
    }
    
    [MenuItem("Tools/Card System/Clear All Cards")]
    public static void ClearAllCards()
    {
        string cardsPath = "Assets/Resources/Cards";
        if (Directory.Exists(cardsPath))
        {
            string[] cardAssets = Directory.GetFiles(cardsPath, "*.asset");
            foreach (string assetPath in cardAssets)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
            Debug.Log("Cleared all card assets!");
            AssetDatabase.Refresh();
        }
    }
    
    /// <summary>
    /// 创建卡片资源文件
    /// </summary>
    /// <param name="cardName">卡片名称</param>
    /// <param name="cardType">卡片类型</param>
    /// <param name="elixirCost">圣水消�?/param>
    /// <param name="rarity">稀有度</param>
    /// <param name="cardData">卡片特定数据</param>
    public static CardData CreateCardAsset(string cardName, CardType cardType, int elixirCost, int rarity, object cardData)
    {
        // 确保Cards目录存在
        string cardsPath = "Assets/Resources/Cards";
        if (!Directory.Exists(cardsPath))
        {
            Directory.CreateDirectory(cardsPath);
        }
        
        // 检查卡片是否已存在
        string assetPath = cardsPath + "/" + cardName + ".asset";
        CardData existingCard = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
        if (existingCard != null)
        {
            Debug.LogWarning("Card already exists: " + cardName + ". Overwriting...");
            AssetDatabase.DeleteAsset(assetPath);
        }
        
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
        
        // 保存资源
        AssetDatabase.CreateAsset(card, assetPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log("Created card asset: " + assetPath);
        return card;
    }
    
    /// <summary>
    /// 批量创建卡片
    /// </summary>
    /// <param name="cards">卡片数据数组</param>
    public static void BatchCreateCards(CardCreationData[] cards)
    {
        foreach (CardCreationData cardData in cards)
        {
            CreateCardAsset(cardData.cardName, cardData.cardType, cardData.elixirCost, cardData.rarity, cardData.cardSpecificData);
        }
        
        AssetDatabase.Refresh();
        Debug.Log("Batch created " + cards.Length + " cards!");
    }
    
    // 卡片创建数据结构
    public struct CardCreationData
    {
        public string cardName;
        public CardType cardType;
        public int elixirCost;
        public int rarity;
        public object cardSpecificData;
    }
}

}
