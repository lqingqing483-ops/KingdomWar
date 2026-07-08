using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class CardIconAtlasCombiner
{
    [MenuItem("KingdomWar/Combine Card Icons (Fallback)")]
    public static void CombineToTexture()
    {
        string[] guids = AssetDatabase.FindAssets("t:texture2D", new[] { "Assets/Resources/UI/CardIcons" });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No textures found", "OK");
            return;
        }

        // Load all textures and sort by name
        List<Texture2D> textures = new List<Texture2D>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t != null) textures.Add(t);
        }
        textures.Sort((a, b) => a.name.CompareTo(b.name));

        // Determine grid size
        int cols = Mathf.CeilToInt(Mathf.Sqrt(textures.Count));
        int rows = Mathf.CeilToInt((float)textures.Count / cols);
        int iconSize = 120;
        int padding = 4;
        int atlasWidth = cols * (iconSize + padding) + padding;
        int atlasHeight = rows * (iconSize + padding) + padding;

        // Create combined texture
        Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.RGBA32, false);
        Color clear = Color.clear;
        for (int x = 0; x < atlasWidth; x++)
            for (int y = 0; y < atlasHeight; y++)
                atlas.SetPixel(x, y, clear);

        // Pack icons
        for (int i = 0; i < textures.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            int x = padding + col * (iconSize + padding);
            int y = padding + row * (iconSize + padding);
            
            Texture2D src = textures[i];
            // Resize to iconSize
            Texture2D scaled = ScaleTexture(src, iconSize, iconSize);
            
            for (int px = 0; px < iconSize; px++)
                for (int py = 0; py < iconSize; py++)
                    atlas.SetPixel(x + px, y + py, scaled.GetPixel(px, py));
        }
        atlas.Apply();

        // Save as asset
        byte[] pngData = atlas.EncodeToPNG();
        string outputPath = "Assets/Resources/UI/CardIconsAtlas.png";
        File.WriteAllBytes(outputPath, pngData);
        
        AssetDatabase.Refresh();

        // Set texture import settings
        TextureImporter importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        // Generate sprite rects and slice
        string assetPath = outputPath;
        Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (savedTex != null)
        {
            // Set sprite pivot and borders (slicing)
            string importerPath = assetPath;
            TextureImporter ti = AssetImporter.GetAtPath(importerPath) as TextureImporter;
            if (ti != null)
            {
                List<SpriteMetaData> sprites = new List<SpriteMetaData>();
                for (int i = 0; i < textures.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    int x = padding + col * (iconSize + padding);
                    int y = padding + row * (iconSize + padding);
                    
                    SpriteMetaData smd = new SpriteMetaData();
                    smd.name = textures[i].name;
                    smd.rect = new Rect(x, atlasHeight - y - iconSize, iconSize, iconSize);
                    smd.alignment = (int)SpriteAlignment.Center;
                    sprites.Add(smd);
                }
                ti.spritesheet = sprites.ToArray();
                ti.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Combined atlas created!\n{textures.Count} icons\nSize: {atlasWidth}x{atlasHeight}\nPath: {outputPath}\n\nSprites are sliced by name.\nUse 'SpriteRenderer.sprite = Resources.Load<Sprite>(\"UI/CardIconsAtlas\")'", "OK");
    }

    static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(targetWidth, targetHeight);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
