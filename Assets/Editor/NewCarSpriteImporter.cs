using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class NewCarSpriteImporter : AssetPostprocessor
{
    private static readonly HashSet<string> GarageSprites = new HashSet<string>
    {
        "GarageCarPhantomX.png",
        "GarageCarRaptor4X.png",
        "GarageCarBlazeRS.png",
        "GarageCarNovaLM.png",
        "GarageCarZenithQ.png"
    };

    private static readonly HashSet<string> TrackSprites = new HashSet<string>
    {
        "TrackCarPhantomX.png",
        "TrackCarRaptor4X.png",
        "TrackCarBlazeRS.png",
        "TrackCarNovaLM.png",
        "TrackCarZenithQ.png"
    };

    private void OnPreprocessTexture()
    {
        string fileName = Path.GetFileName(assetPath);
        TextureImporter importer = (TextureImporter)assetImporter;

        if (GarageSprites.Contains(fileName))
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            return;
        }

        if (!TrackSprites.Contains(fileName))
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 640f;
        importer.isReadable = false;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 64;
    }
}
