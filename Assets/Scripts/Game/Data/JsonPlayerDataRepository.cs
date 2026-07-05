using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using KingdomWar.Server;

namespace KingdomWar.Game.Data
{
    /// <summary>
    /// File-based player data repository with AES encryption.
    /// Stores data to Application.persistentDataPath/KingdomWar/save.json
    /// Features atomic saves (write temp file then rename) and backup recovery.
    /// </summary>
    public class JsonPlayerDataRepository : IPlayerDataRepository
    {
        private readonly string _savePath;
        private readonly string _backupPath;
        private readonly string _tempPath;
        private Dictionary<string, string> _data;
        private bool _dirty;

        private const string SAVE_FILE = "save.json";
        private const string BACKUP_FILE = "save.json.bak";
        private const string TEMP_FILE = "save.json.tmp";
        private const string ENCRYPTION_KEY = "KingdomWar_Save_Key_2024!@#$";

        // Type tags for value serialization
        private const string TAG_INT = "I:";
        private const string TAG_FLOAT = "F:";
        private const string TAG_STRING = "S:";
        private const string TAG_DICT = "D:";
        private const string TAG_LIST = "L:";

        public JsonPlayerDataRepository()
        {
            string dir = Path.Combine(Application.persistentDataPath, "KingdomWar");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _savePath = Path.Combine(dir, SAVE_FILE);
            _backupPath = Path.Combine(dir, BACKUP_FILE);
            _tempPath = Path.Combine(dir, TEMP_FILE);
            _data = new Dictionary<string, string>();
            _dirty = false;

            LoadFromDisk();
        }

        public void Save(string key, int value)
        {
            _data[key] = TAG_INT + value.ToString();
            _dirty = true;
        }

        public void Save(string key, string value)
        {
            _data[key] = TAG_STRING + (value ?? "");
            _dirty = true;
        }

        public void Save(string key, float value)
        {
            _data[key] = TAG_FLOAT + value.ToString("G");
            _dirty = true;
        }

        public int LoadInt(string key, int defaultValue = 0)
        {
            if (_data.TryGetValue(key, out string raw) && raw.StartsWith(TAG_INT))
            {
                if (int.TryParse(raw.Substring(TAG_INT.Length), out int result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        public string LoadString(string key, string defaultValue = "")
        {
            if (_data.TryGetValue(key, out string raw) && raw.StartsWith(TAG_STRING))
            {
                return raw.Substring(TAG_STRING.Length);
            }
            return defaultValue;
        }

        public float LoadFloat(string key, float defaultValue = 0f)
        {
            if (_data.TryGetValue(key, out string raw) && raw.StartsWith(TAG_FLOAT))
            {
                if (float.TryParse(raw.Substring(TAG_FLOAT.Length), out float result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        public void SaveDictionary(string key, Dictionary<string, int> dict)
        {
            DictWrapper wrapper = new DictWrapper();
            foreach (var kvp in dict)
            {
                wrapper.keys.Add(kvp.Key);
                wrapper.values.Add(kvp.Value);
            }
            _data[key] = TAG_DICT + JsonUtility.ToJson(wrapper);
            _dirty = true;
        }

        public Dictionary<string, int> LoadDictionary(string key)
        {
            if (_data.TryGetValue(key, out string raw) && raw.StartsWith(TAG_DICT))
            {
                string json = raw.Substring(TAG_DICT.Length);
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
            return new Dictionary<string, int>();
        }

        public void SaveList(string key, List<string> list)
        {
            StringListWrapper wrapper = new StringListWrapper();
            wrapper.items.AddRange(list);
            _data[key] = TAG_LIST + JsonUtility.ToJson(wrapper);
            _dirty = true;
        }

        public List<string> LoadList(string key)
        {
            if (_data.TryGetValue(key, out string raw) && raw.StartsWith(TAG_LIST))
            {
                string json = raw.Substring(TAG_LIST.Length);
                StringListWrapper wrapper = JsonUtility.FromJson<StringListWrapper>(json);
                if (wrapper != null && wrapper.items != null)
                {
                    return new List<string>(wrapper.items);
                }
            }
            return new List<string>();
        }

        public bool HasKey(string key)
        {
            return _data.ContainsKey(key);
        }

        public void DeleteKey(string key)
        {
            if (_data.Remove(key))
            {
                _dirty = true;
            }
        }

        public void SaveAll()
        {
            if (!_dirty) return;

            try
            {
                DataWrapper wrapper = new DataWrapper();
                foreach (var kvp in _data)
                {
                    wrapper.keys.Add(kvp.Key);
                    wrapper.values.Add(kvp.Value);
                }

                string json = JsonUtility.ToJson(wrapper);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                byte[] encrypted = AES.AESEncrypt(jsonBytes, ENCRYPTION_KEY);

                // Atomic write: write to temp, then rename
                File.WriteAllBytes(_tempPath, encrypted);
                if (File.Exists(_savePath))
                {
                    if (File.Exists(_backupPath))
                    {
                        File.Delete(_backupPath);
                    }
                    File.Move(_savePath, _backupPath);
                }
                File.Move(_tempPath, _savePath);

                _dirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonRepo] Failed to save: {ex.Message}");
                // Try to restore from backup
                try
                {
                    if (File.Exists(_backupPath) && !File.Exists(_savePath))
                    {
                        File.Copy(_backupPath, _savePath);
                    }
                }
                catch (Exception backupEx)
                {
                    Debug.LogError($"[JsonRepo] Backup restore failed: {backupEx.Message}");
                }
            }
        }

        public void Clear()
        {
            _data.Clear();
            _dirty = true;
            SaveAll();
        }

        private void LoadFromDisk()
        {
            byte[] encrypted = TryReadFile(_savePath) ?? TryReadFile(_backupPath);
            if (encrypted == null) return;

            try
            {
                byte[] decrypted = AES.AESDecrypt(encrypted, ENCRYPTION_KEY);
                string json = Encoding.UTF8.GetString(decrypted);
                DataWrapper wrapper = JsonUtility.FromJson<DataWrapper>(json);
                if (wrapper != null && wrapper.keys != null && wrapper.values != null)
                {
                    _data.Clear();
                    for (int i = 0; i < wrapper.keys.Count && i < wrapper.values.Count; i++)
                    {
                        _data[wrapper.keys[i]] = wrapper.values[i];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonRepo] Failed to load save data: {ex.Message}. Starting fresh.");
                _data.Clear();
            }
        }

        private static byte[] TryReadFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        [Serializable]
        private class DataWrapper
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();
        }

        [Serializable]
        private class DictWrapper
        {
            public List<string> keys = new List<string>();
            public List<int> values = new List<int>();
        }

        [Serializable]
        private class StringListWrapper
        {
            public List<string> items = new List<string>();
        }
    }
}
