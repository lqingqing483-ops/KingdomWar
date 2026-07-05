using System.Collections.Generic;

namespace KingdomWar.Game.Data
{
    /// <summary>
    /// Data repository abstraction for player data persistence.
    /// Implementations: JsonPlayerDataRepository (file-based), PlayerPrefsDataRepository (legacy), SqlitePlayerDataRepository (future).
    /// </summary>
    public interface IPlayerDataRepository
    {
        void Save(string key, int value);
        void Save(string key, string value);
        void Save(string key, float value);
        int LoadInt(string key, int defaultValue = 0);
        string LoadString(string key, string defaultValue = "");
        float LoadFloat(string key, float defaultValue = 0f);
        void SaveDictionary(string key, Dictionary<string, int> dict);
        Dictionary<string, int> LoadDictionary(string key);
        void SaveList(string key, List<string> list);
        List<string> LoadList(string key);
        bool HasKey(string key);
        void DeleteKey(string key);
        void SaveAll();
        void Clear();
    }
}
