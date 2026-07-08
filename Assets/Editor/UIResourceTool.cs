using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using KingdomWar.UI;
using KingdomWar.Game.Cards;

/// <summary>
/// Editor tool to preview and assign imported Heroes Arena UI resources.
/// Tools > UI Resources > Resource Browser
/// </summary>
public class UIResourceTool : EditorWindow
{
    private Vector2 scrollPos;
    private string searchFilter = "";

    // Known resource categories
    private readonly List<ResourceCategory> categories = new List<ResourceCategory>
    {
        new ResourceCategory("Battle Result", "UI/BattleResult", new[] {
            "background", "Win", "Lose", "VS", "reward", "exp", "exp_bg", "exp_light",
            "blue2", "green2", "button", "button_down", "bg_bar", "item_bg", "point_bg", "point_icon",
            "ruong_nau", "ruong_nau_open", "levelup", "levelup_vn",
            "win_1", "win_2", "win_3", "win_4", "win_5", "win_6", "win_7", "win_8"
        }),
        new ResourceCategory("Shop", "UI/Shop", new[] {
            "gold1", "gold2", "gold3", "gold4", "gold5", "gold6",
            "nap_normal", "nap_down", "bar_chose", "bar_normal",
            "tieude_bg", "inf_bg", "khuyenmai_bg", "vnd_bg", "giatien_bg", "nap_tb_bg",
            "the_dt", "UpDown", "ggp_ios", "xuong_down"
        }),
        new ResourceCategory("Icons", "UI/Icons", new[] {
            "attack-icon", "sword-icon", "heart-icon1", "Icon-Damage",
            "Icon_Physical_Attack", "Constitution", "CrossCutIcon", "HeatedBladeIcon",
            "TemporalStrikeIcon", "Shield_and_swords", "shield and swords",
            "strength_icon-13162009-100px", "target-icon-14133",
            "iconCritDMG", "icon-item-icon_item_Seed_ATK1", "celi", "gang", "4021619_orig"
        }),
    };

    [MenuItem("Tools/UI Resources/Resource Browser")]
    private static void ShowWindow()
    {
        GetWindow<UIResourceTool>("UI Resources");
    }

    [MenuItem("Tools/UI Resources/Assign BattleResult to SettlementPanel &#B")]
    private static void AssignBattleResultToSettlement()
    {
        var settlement = FindObjectOfType<settlementPanel>();
        if (settlement == null)
        {
            // Try the prefab
            var prefab = Resources.Load<GameObject>("Prefabs/UIPrefab/settlementPanel");
            if (prefab != null)
            {
                var comp = prefab.GetComponent<settlementPanel>();
                if (comp != null)
                {
                    Debug.Log("[UIResourceTool] settlementPanel prefab found, assign in Prefab mode");
                }
            }
            EditorUtility.DisplayDialog("Not Found", "No settlementPanel found in current scene", "OK");
            return;
        }

        Undo.RecordObject(settlement, "Assign BattleResult Resources");

        // Try to assign images if the panel has them
        // The settlementPanel might have Images in its hierarchy
        var allImages = settlement.GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            switch (img.name.ToLower())
            {
                case "bg":
                case "background":
                    TryAssignSprite(img, "UI/BattleResult/background");
                    break;
                case "win":
                case "winicon":
                    TryAssignSprite(img, "UI/BattleResult/Win");
                    break;
                case "lose":
                case "loseicon":
                    TryAssignSprite(img, "UI/BattleResult/Lose");
                    break;
                case "vs":
                case "vsicon":
                    TryAssignSprite(img, "UI/BattleResult/VS");
                    break;
                case "reward":
                case "rewardicon":
                    TryAssignSprite(img, "UI/BattleResult/reward");
                    break;
                case "chest":
                case "chesticon":
                    TryAssignSprite(img, "UI/BattleResult/ruong_nau");
                    break;
            }
        }

        Debug.Log("[UIResourceTool] BattleResult resources assigned to settlementPanel");
    }

    [MenuItem("Tools/UI Resources/Assign Card Icons to CardDatabase &#C")]
    private static void AssignCardIcons()
    {
        var db = FindObjectOfType<CardDatabase>();
        if (db == null)
        {
            EditorUtility.DisplayDialog("Not Found", "No CardDatabase found in scene", "OK");
            return;
        }

        Undo.RecordObject(db, "Assign Card Icons");

        // CardDatabase has a list of CardData, each with cardIcon
        // We can assign our imported card icons by card index
        var cards = db.GetAllCards();
        for (int i = 0; i < cards.Count && i < 50; i++)
        {
            var sprite = Resources.Load<Sprite>($"UI/CardIcons/{i + 1}");
            if (sprite != null)
            {
                cards[i].cardIcon = sprite;
            }
        }

        Debug.Log($"[UIResourceTool] Assigned card icons to {cards.Count} cards");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI Resources Browser", EditorStyles.boldLabel);
        GUILayout.Space(5);

        searchFilter = EditorGUILayout.TextField("Search", searchFilter);
        GUILayout.Space(5);

        if (GUILayout.Button("Refresh", GUILayout.Width(100)))
        {
            Repaint();
        }

        GUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var category in categories)
        {
            bool hasResults = string.IsNullOrEmpty(searchFilter) ||
                              category.name.ToLower().Contains(searchFilter.ToLower());

            if (!hasResults) continue;

            GUILayout.Label(category.name, EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();

            int count = 0;
            foreach (var assetName in category.assetNames)
            {
                if (!string.IsNullOrEmpty(searchFilter) &&
                    !assetName.ToLower().Contains(searchFilter.ToLower()))
                    continue;

                string path = $"{category.folder}/{assetName}";
                Sprite sprite = Resources.Load<Sprite>(path);

                if (sprite != null)
                {
                    GUILayout.BeginVertical(GUILayout.Width(80));
                    Rect r = GUILayoutUtility.GetRect(64, 64);
                    EditorGUI.DrawPreviewTexture(r, sprite.texture, null, ScaleMode.ScaleToFit);
                    GUI.Label(new Rect(r.x, r.y + 64, 80, 18), assetName, EditorStyles.miniLabel);

                    if (GUILayout.Button("Copy Path", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        EditorGUIUtility.systemCopyBuffer = path;
                        Debug.Log($"Copied: {path}");
                    }
                    GUILayout.EndVertical();

                    count++;
                    if (count % 4 == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label("Quick Assign", EditorStyles.boldLabel);
        if (GUILayout.Button("Assign BattleResult to settlementPanel", GUILayout.Height(30)))
        {
            AssignBattleResultToSettlement();
        }
        if (GUILayout.Button("Assign Card Icons to CardDatabase", GUILayout.Height(30)))
        {
            AssignCardIcons();
        }
    }

    private static void TryAssignSprite(Image img, string resourcesPath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcesPath);
        if (sprite != null)
        {
            img.sprite = sprite;
            Debug.Log($"[UIResourceTool] Assigned {resourcesPath} → {img.name}");
        }
    }

    private class ResourceCategory
    {
        public string name;
        public string folder;
        public string[] assetNames;

        public ResourceCategory(string name, string folder, string[] assetNames)
        {
            this.name = name;
            this.folder = folder;
            this.assetNames = assetNames;
        }
    }
}
