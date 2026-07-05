using System.Collections.Generic;
using UnityEngine;
using System.IO;

using KingdomWar.Game.Cards;
namespace KingdomWar.Game.Decks
{
[System.Serializable]
public class SavedDeckList
{
    public List<SavedDeckEntry> entries = new List<SavedDeckEntry>();
}

[System.Serializable]
public class SavedDeckEntry
{
    public string key;
    public SavedDeckData value;
}

public class DeckManager : MonoBehaviour
{
    private static DeckManager instance;
    public static DeckManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DeckManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("DeckManager");
                    instance = obj.AddComponent<DeckManager>();
                }
            }
            return instance;
        }
    }
    
    private string decksSavePath;
    private Dictionary<string, SavedDeckData> savedDecks = new Dictionary<string, SavedDeckData>();
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            decksSavePath = Application.persistentDataPath + "/decks.json";
            LoadSavedDecks();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void LoadSavedDecks()
    {
        if (File.Exists(decksSavePath))
        {
            try
            {
                string json = File.ReadAllText(decksSavePath);
                var list = JsonUtility.FromJson<SavedDeckList>(json);
                savedDecks = new Dictionary<string, SavedDeckData>();
                if (list != null)
                {
                    foreach (var entry in list.entries)
                    {
                        savedDecks[entry.key] = entry.value;
                    }
                }
                Debug.Log($"Loaded {savedDecks.Count} saved decks");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading saved decks: {e.Message}");
                savedDecks = new Dictionary<string, SavedDeckData>();
            }
        }
    }
    
    private void SaveDecksToFile()
    {
        try
        {
            var list = new SavedDeckList();
            foreach (var kvp in savedDecks)
            {
                list.entries.Add(new SavedDeckEntry { key = kvp.Key, value = kvp.Value });
            }
            string json = JsonUtility.ToJson(list, prettyPrint: true);
            File.WriteAllText(decksSavePath, json);
            Debug.Log($"Saved {savedDecks.Count} decks to file");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving decks: {e.Message}");
        }
    }
    
    public void SaveDeck(string deckName, List<CardData> cards)
    {
        if (string.IsNullOrEmpty(deckName))
        {
            Debug.LogError("Deck name cannot be empty");
            return;
        }
        
        if (cards == null || cards.Count == 0)
        {
            Debug.LogError("Deck cannot be empty");
            return;
        }
        
        // create save deck data
        SavedDeckData deckData = new SavedDeckData();
        deckData.deckName = deckName;
        deckData.cardNames = new List<string>();
        
        foreach (CardData card in cards)
        {
            deckData.cardNames.Add(card.cardName);
        }
        
        // 保存到字�?        savedDecks[deckName] = deckData;
        
        // 保存到文�?        SaveDecksToFile();
        
        Debug.Log($"Deck saved: {deckName}");
    }
    
    public List<CardData> LoadDeck(string deckName)
    {
        if (string.IsNullOrEmpty(deckName) || !savedDecks.ContainsKey(deckName))
        {
            Debug.LogError($"Deck not found: {deckName}");
            return null;
        }
        
        SavedDeckData deckData = savedDecks[deckName];
        List<CardData> loadedDeck = new List<CardData>();
        
        foreach (string cardName in deckData.cardNames)
        {
            CardData card = CardDatabase.Instance.GetCardByName(cardName);
            if (card != null)
            {
                loadedDeck.Add(card);
            }
            else
            {
                Debug.LogWarning($"Card not found in database: {cardName}");
            }
        }
        
        Debug.Log($"Deck loaded: {deckName} with {loadedDeck.Count} cards");
        return loadedDeck;
    }
    
    public void DeleteDeck(string deckName)
    {
        if (savedDecks.ContainsKey(deckName))
        {
            savedDecks.Remove(deckName);
            SaveDecksToFile();
            Debug.Log($"Deck deleted: {deckName}");
        }
        else
        {
            Debug.LogError($"Deck not found: {deckName}");
        }
    }
    
    public List<string> GetAllDeckNames()
    {
        return new List<string>(savedDecks.Keys);
    }
    
    public bool DeckExists(string deckName)
    {
        return savedDecks.ContainsKey(deckName);
    }
}

[System.Serializable]
public class SavedDeckData
{
    public string deckName;
    public List<string> cardNames;
}

}
