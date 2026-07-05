using UnityEditor;
using UnityEngine;
using KingdomWar.Game.Cards;
namespace KingdomWar.Editor
{
[CustomEditor(typeof(CardData))]
public class CardDataEditor : UnityEditor.Editor
{
    private CardData cardData;
    private SerializedObject serializedObject;
    private SerializedProperty cardNameProp;
    private SerializedProperty cardTypeProp;
    private SerializedProperty elixirCostProp;
    private SerializedProperty rarityProp;
    private SerializedProperty unitDataProp;
    private SerializedProperty spellDataProp;
    private SerializedProperty buildingDataProp;
    private SerializedProperty cardIconProp;
    private SerializedProperty cardPrefabProp;
    
    private void OnEnable()
    {
        cardData = (CardData)target;
        serializedObject = new SerializedObject(cardData);
        
        // 获取序列化属�?        cardNameProp = serializedObject.FindProperty("cardName");
        cardTypeProp = serializedObject.FindProperty("cardType");
        elixirCostProp = serializedObject.FindProperty("elixirCost");
        rarityProp = serializedObject.FindProperty("rarity");
        unitDataProp = serializedObject.FindProperty("unitData");
        spellDataProp = serializedObject.FindProperty("spellData");
        buildingDataProp = serializedObject.FindProperty("buildingData");
        cardIconProp = serializedObject.FindProperty("cardIcon");
        cardPrefabProp = serializedObject.FindProperty("cardPrefab");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card Data Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // 基本属�?        EditorGUILayout.PropertyField(cardNameProp);
        EditorGUILayout.PropertyField(cardTypeProp);
        EditorGUILayout.PropertyField(elixirCostProp);
        EditorGUILayout.PropertyField(rarityProp);
        
        EditorGUILayout.Space();
        
        // show corresponding detailed data based on card type
        CardType cardType = (CardType)cardTypeProp.enumValueIndex;
        switch (cardType)
        {
            case CardType.Unit:
                EditorGUILayout.LabelField("Unit Data", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(unitDataProp, true);
                break;
            case CardType.Spell:
                EditorGUILayout.LabelField("Spell Data", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(spellDataProp, true);
                break;
            case CardType.Building:
                EditorGUILayout.LabelField("Building Data", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(buildingDataProp, true);
                break;
        }
        
        EditorGUILayout.Space();
        
        // 视觉属�?        EditorGUILayout.LabelField("Visual Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(cardIconProp);
        EditorGUILayout.PropertyField(cardPrefabProp);
        
        serializedObject.ApplyModifiedProperties();
        
        // 添加保存按钮
        if (GUILayout.Button("Save Changes"))
        {
            EditorUtility.SetDirty(cardData);
            AssetDatabase.SaveAssets();
            Debug.Log("Card data saved: " + cardData.cardName);
        }
    }
}

}
