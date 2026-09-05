using UnityEditor;

namespace BomberGhst.EditorTools
{
    /// The generated sprite sheet has to import as crisp, uncompressed pixel
    /// art. Doing it here means the .png needs no hand written .meta.
    public class GotchiTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("Resources/GotchiSprites")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Config.PPU;
            importer.filterMode = UnityEngine.FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = importer.GetDefaultPlatformTextureSettings();
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
