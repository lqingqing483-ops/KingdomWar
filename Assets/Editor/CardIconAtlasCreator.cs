using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CardIconAtlasCreator
{
    [MenuItem("KingdomWar/Create Card Icon Atlas")]
    public static void CreateAtlas()
    {
        string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/Resources/UI/CardIcons" });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No textures found in Assets/Resources/UI/CardIcons/", "OK");
            return;
        }

        string atlasPath = "Assets/Resources/UI/CardIconsAtlas.spriteatlas";

        // Check for existing atlas
        UnityEngine.U2D.SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new UnityEngine.U2D.SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        // Collect textures
        List<Texture2D> textures = new List<Texture2D>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) textures.Add(t);
        }

        // Pack into atlas
        Object[] objects = textures.ToArray();
        atlas.Add(objects);

        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int count = textures.Count;
        EditorUtility.DisplayDialog("Done", $"Card Icon Atlas created!\n{count} icons packed.\n\nNo code changes needed - Unity auto-resolves sprites.", "OK");
    }
}
