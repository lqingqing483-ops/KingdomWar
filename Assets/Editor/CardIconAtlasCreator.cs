using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using System.Collections.Generic;

public class CardIconAtlasCreator
{
    [MenuItem("KingdomWar/Create Card Icon Atlas")]
    public static void CreateAtlas()
    {
        // Find all textures in CardIcons folder
        string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/Resources/UI/CardIcons" });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No textures found in Assets/Resources/UI/CardIcons/", "OK");
            return;
        }

        string atlasPath = "Assets/Resources/UI/CardIconsAtlas.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
            Debug.Log($"Created new SpriteAtlas at {atlasPath}");
        }
        else
        {
            // Clear existing objects
            atlas.Remove(atlas.GetPackables());
        }

        // Settings
        SpriteAtlasPackingSettings pack = atlas.GetPackingSettings();
        pack.enableTightPacking = false;
        pack.padding = 2;
        atlas.SetPackingSettings(pack);

        SpriteAtlasTextureSettings tex = atlas.GetTextureSettings();
        tex.readable = false;
        tex.generateMipMaps = false;
        tex.sRGB = true;
        atlas.SetTextureSettings(tex);

        // Add textures
        List<Texture2D> textures = new List<Texture2D>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) textures.Add(t);
        }
        atlas.Add(textures.ToArray());

        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Done", $"Card Icon Atlas created!\n{textures.Count} icons packed.\nPath: {atlasPath}\n\nNo code changes needed.", "OK");
    }
}
