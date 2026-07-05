using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KingdomWar.Game.Localization
{
    /// <summary>
    /// Lightweight localization manager.
    /// Loads translations from a CSV file at StreamingAssets/Localization/.
    /// Supports Chinese (default) and English.
    /// </summary>
    public static class LocalizationManager
    {
        private static Dictionary<string, string> _zhTable = new Dictionary<string, string>();
        private static Dictionary<string, string> _enTable = new Dictionary<string, string>();
        private static string _currentLang = "zh";
        private static bool _loaded = false;

        public static string CurrentLanguage => _currentLang;

        public static event Action<string> OnLanguageChanged;

        /// <summary>
        /// Set language (e.g. "zh", "en").
        /// </summary>
        public static void SetLanguage(string langCode)
        {
            if (_currentLang == langCode) return;
            _currentLang = langCode;
            OnLanguageChanged?.Invoke(langCode);
        }

        /// <summary>
        /// Initialize - load translations from CSV.
        /// </summary>
        public static void Initialize()
        {
            if (_loaded) return;

            // Try StreamingAssets first, fallback to Resources
            string csvPath = Path.Combine(Application.streamingAssetsPath, "Localization", "translations.csv");
            if (File.Exists(csvPath))
            {
                LoadFromCsv(csvPath);
            }
            else
            {
                // Fallback: try Resources
                TextAsset fallback = Resources.Load<TextAsset>("Localization/translations");
                if (fallback != null)
                {
                    LoadFromCsvLines(fallback.text.Split('\n'));
                }
                else
                {
                    Debug.LogWarning("[Localization] No translation file found. Using hardcoded defaults.");
                    LoadDefaults();
                }
            }

            _loaded = true;
            Debug.Log($"[Localization] Loaded {_zhTable.Count} keys for zh, {_enTable.Count} keys for en");
        }

        /// <summary>
        /// Get localized text for a key.
        /// </summary>
        public static string Get(string key, params object[] args)
        {
            if (!_loaded) Initialize();

            Dictionary<string, string> table = _currentLang == "en" ? _enTable : _zhTable;
            string fallbackKey = $"{{{key}}}";

            if (table.TryGetValue(key, out string text))
            {
                return args.Length > 0 ? string.Format(text, args) : text;
            }

            // Key not found - return the key itself as fallback
            return fallbackKey;
        }

        /// <summary>
        /// Convenience accessor for UI code.
        /// </summary>
        public static string T(string key) => Get(key);

        private static void LoadFromCsv(string path)
        {
            try
            {
                string[] lines = File.ReadAllLines(path);
                LoadFromCsvLines(lines);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Localization] Failed to load CSV: {ex.Message}");
                LoadDefaults();
            }
        }

        private static void LoadFromCsvLines(string[] lines)
        {
            _zhTable.Clear();
            _enTable.Clear();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                // Format: key,zh_text,en_text
                string[] parts = ParseCsvLine(trimmed);
                if (parts.Length >= 3)
                {
                    _zhTable[parts[0]] = parts[1];
                    _enTable[parts[0]] = parts[2];
                }
            }
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            foreach (char c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else { current += c; }
            }
            result.Add(current.Trim());
            return result.ToArray();
        }

        private static void LoadDefaults()
        {
            _zhTable["main_title"] = "王国战争";
            _enTable["main_title"] = "KingdomWar";
            _zhTable["battle_btn"] = "战斗";
            _enTable["battle_btn"] = "Battle";
            _zhTable["shop_btn"] = "商店";
            _enTable["shop_btn"] = "Shop";
            _zhTable["deck_btn"] = "卡组";
            _enTable["deck_btn"] = "Deck";
            _zhTable["profile_btn"] = "资料";
            _enTable["profile_btn"] = "Profile";
            _zhTable["trophies"] = "奖杯";
            _enTable["trophies"] = "Trophies";
            _zhTable["arena"] = "竞技场";
            _enTable["arena"] = "Arena";
            _zhTable["wins"] = "胜场";
            _enTable["wins"] = "Wins";
            _zhTable["losses"] = "负场";
            _enTable["losses"] = "Losses";
            _zhTable["win_rate"] = "胜率";
            _enTable["win_rate"] = "Win Rate";
            _zhTable["searching"] = "正在匹配对手";
            _enTable["searching"] = "Searching for opponent";
        }
    }
}
