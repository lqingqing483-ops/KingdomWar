using UnityEngine;
using System.IO;

using KingdomWar.Game.Cards;
namespace KingdomWar.Game.Cards
{
public class CardDataCreator : MonoBehaviour
{
    [Header("Card Data Creation")]
    public bool createTestCards = true;
    public string cardsResourcePath = "Assets/Resources/Cards";
    
    private void Awake()
    {
        if (createTestCards)
        {
            CreateTestCardData();
        }
    }
    
    private void CreateTestCardData()
    {
        // 确保Cards目录存在
        if (!Directory.Exists(cardsResourcePath))
        {
            Directory.CreateDirectory(cardsResourcePath);
        }
        
        // 创建测试卡片
        CreateCardData("Knight", CardType.Unit, 3, 1, 
            new UnitData { health = 2000, damage = 200, attackSpeed = 1.2f, moveSpeed = 1.5f, attackRange = 1.0f, deployTime = 1.0f });
        
        CreateCardData("Archer", CardType.Unit, 3, 1, 
            new UnitData { health = 700, damage = 120, attackSpeed = 1.0f, moveSpeed = 1.2f, attackRange = 5.0f, deployTime = 1.0f });
        
        CreateCardData("Fireball", CardType.Spell, 4, 1, 
            new SpellData { damage = 1200, radius = 2.5f, duration = 0.5f, deployTime = 0.0f });
        
        CreateCardData("Giant", CardType.Unit, 5, 2, 
            new UnitData { health = 3000, damage = 150, attackSpeed = 1.5f, moveSpeed = 1.0f, attackRange = 1.0f, deployTime = 1.5f });
        
        CreateCardData("Lightning", CardType.Spell, 6, 3, 
            new SpellData { damage = 1000, radius = 3.0f, duration = 0.5f, deployTime = 0.0f });
        
        CreateCardData("PEKKA", CardType.Unit, 7, 4, 
            new UnitData { health = 3200, damage = 400, attackSpeed = 1.8f, moveSpeed = 1.0f, attackRange = 1.0f, deployTime = 1.5f });
        
        CreateCardData("ElixirCollector", CardType.Building, 6, 3, 
            new BuildingData { health = 1200, damage = 0, attackSpeed = 0, attackRange = 0, duration = 60.0f, deployTime = 2.0f });
        
        CreateCardData("Zap", CardType.Spell, 2, 1, 
            new SpellData { damage = 200, radius = 3.0f, duration = 0.5f, deployTime = 0.0f });
        
        Debug.Log("Created test card data files");
    }
    
    private void CreateCardData(string cardName, CardType cardType, int elixirCost, int rarity, object cardSpecificData)
    {
        string assetPath = cardsResourcePath + "/" + cardName + ".asset";
        
        // 检查资源是否已存在
        CardData existingCard = Resources.Load<CardData>("Cards/" + cardName);
        if (existingCard != null)
        {
            Debug.Log("Card already exists: " + cardName);
            return;
        }
        
        // 创建新的CardData
        CardData cardData = ScriptableObject.CreateInstance<CardData>();
        cardData.cardName = cardName;
        cardData.cardType = cardType;
        cardData.elixirCost = elixirCost;
        cardData.rarity = rarity;
        
        // 设置卡片特定数据
        switch (cardType)
        {
            case CardType.Unit:
                cardData.unitData = (UnitData)cardSpecificData;
                break;
            case CardType.Spell:
                cardData.spellData = (SpellData)cardSpecificData;
                break;
            case CardType.Building:
                cardData.buildingData = (BuildingData)cardSpecificData;
                break;
        }
        
        // 注意：在编辑器中，我们通常使用AssetDatabase.CreateAsset来创建资�?        // 但在运行时，我们可以使用Resources.Load和Instantiate来管�?        // 这里我们只是创建数据结构，实际的资源创建需要在编辑器中完成
        
        Debug.Log("Created card data for: " + cardName);
    }
}

}
