using UnityEngine;
using UnityEditor;

public class AtlasHelper
{
    [MenuItem("KingdomWar/Build Card Atlas")]
    static void BuildAtlas()
    {
        // The .spriteatlas file already exists at Assets/Resources/UI/CardIconsAtlas.spriteatlas
        // Just need to trigger Unity to reimport it
        AssetDatabase.ImportAsset("Assets/Resources/UI/CardIconsAtlas.spriteatlas");
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Info", "Atlas asset reimported.\n\nIf it doesn't work:\n1. Delete Assets/Resources/UI/CardIconsAtlas.spriteatlas\n2. Right-click Project → Create → Sprite Atlas\n3. Name it 'CardIconsAtlas'\n4. Drag Assets/Resources/UI/CardIcons/ into 'Objects for Packing'\n\nNo code changes needed.", "OK");
    }
}
