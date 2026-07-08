using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateAtlasDirectly : EditorWindow
{
    [MenuItem("KingdomWar/Build Card Atlas")]
    static void BuildAtlas()
    {
        string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/Resources/UI/CardIcons" });
        string atlasPath = "Assets/Resources/UI/CardIconsAtlas";

        // Check if spriteatlas exists
        var atlasType = System.Type.GetType("UnityEngine.U2D.SpriteAtlas, UnityEngine.U2DModule");
        if (atlasType == null) { EditorUtility.DisplayDialog("Error", "SpriteAtlas type not found", "OK"); return; }

        // Create atlas asset
        var atlas = ScriptableObject.CreateInstance(atlasType);
        if (atlas == null) { EditorUtility.DisplayDialog("Error", "Failed to create SpriteAtlas", "OK"); return; }

        AssetDatabase.CreateAsset(atlas, atlasPath + ".spriteatlas");

        // Use SerializedObject to add textures
        SerializedObject so = new SerializedObject(atlas);
        SerializedProperty packables = so.FindProperty("m_Packables");
        if (packables == null) { EditorUtility.DisplayDialog("Error", "m_Packables not found. Trying alternate approach...", "OK"); TryAlternate(atlas, guids, atlasPath); return; }

        packables.ClearArray();
        int index = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            packables.InsertArrayElementAtIndex(index);
            packables.GetArrayElementAtIndex(index).objectReferenceValue = tex;
            index++;
        }
        so.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Success", $"SpriteAtlas created with {index} textures\n{atlasPath}.spriteatlas", "OK");
    }

    static void TryAlternate(ScriptableObject atlas, string[] guids, string atlasPath)
    {
        // Save with default objects
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Partial Success", $"Created empty atlas at {atlasPath}.spriteatlas\nDrag CardIcons folder into 'Objects for Packing' in the Inspector.", "OK");
    }
}
