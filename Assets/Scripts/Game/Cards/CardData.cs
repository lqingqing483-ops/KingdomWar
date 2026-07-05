using UnityEngine;

namespace KingdomWar.Game.Cards
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
    public class CardData : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public int elixirCost;
    public int rarity;
    
    public UnitData unitData;
    public SpellData spellData;
    public BuildingData buildingData;
    
    public Sprite cardIcon;
    public GameObject cardPrefab;
}

public enum CardType
{
    Unit,
    Spell,
    Building
}

[System.Serializable]
public class UnitData
{
    public int health;
    public int damage;
    public float attackSpeed;
    public float moveSpeed;
    public float attackRange;
    public float deployTime;
    public GameObject projectilePrefab; // 远程攻击物体预制体
}

[System.Serializable]
public class SpellData
{
    public int damage;
    public float radius;
    public float duration;
    public float deployTime;
    public GameObject draggedPrefab; // 拖动时的模型预制体
}

[System.Serializable]
public class BuildingData
{
    public int health;
    public int damage;
    public float attackSpeed;
    public float attackRange;
    public float duration;
    public float deployTime;
}
}