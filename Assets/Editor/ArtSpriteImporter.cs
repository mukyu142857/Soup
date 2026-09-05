using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps the supplied art usable by the runtime-built UI without requiring
/// manual Texture Type changes in the Inspector.
/// </summary>
public sealed class ArtSpriteImporter : AssetPostprocessor
{
    private const string ArtRoot = "Assets/Resources/Art/";

    [InitializeOnLoadMethod]
    private static void ConfigureExistingArtAfterReload()
    {
        EditorApplication.delayCall += ConfigureExistingArt;
    }

    [MenuItem("Tools/Mengpo Soup/Reimport Art Sprites")]
    private static void ConfigureExistingArt()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art" });
        for (int i = 0; i < textureGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !ApplySettings(importer, path)) continue;
            importer.SaveAndReimport();
        }
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot, System.StringComparison.Ordinal)) return;
        ApplySettings((TextureImporter)assetImporter, assetPath);
    }

    private static bool ApplySettings(TextureImporter importer, string path)
    {
        bool isStoryCg = path.Contains("/StoryCG/");
        bool changed = false;

        changed |= SetIfDifferent(importer.textureType, TextureImporterType.Sprite,
            value => importer.textureType = value);
        changed |= SetIfDifferent(importer.spriteImportMode, SpriteImportMode.Single,
            value => importer.spriteImportMode = value);
        changed |= SetIfDifferent(importer.alphaIsTransparency, true,
            value => importer.alphaIsTransparency = value);
        changed |= SetIfDifferent(importer.mipmapEnabled, false,
            value => importer.mipmapEnabled = value);
        changed |= SetIfDifferent(importer.wrapMode, TextureWrapMode.Clamp,
            value => importer.wrapMode = value);
        changed |= SetIfDifferent(importer.filterMode, isStoryCg ? FilterMode.Bilinear : FilterMode.Point,
            value => importer.filterMode = value);
        changed |= SetIfDifferent(
            importer.textureCompression,
            isStoryCg ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed,
            value => importer.textureCompression = value);

        if (!Mathf.Approximately(importer.spritePixelsPerUnit, 64f))
        {
            importer.spritePixelsPerUnit = 64f;
            changed = true;
        }

        return changed;
    }

    private static bool SetIfDifferent<T>(T current, T desired, System.Action<T> setter)
    {
        if (Equals(current, desired)) return false;
        setter(desired);
        return true;
    }
}
