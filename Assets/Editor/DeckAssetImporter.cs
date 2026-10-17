using System;
using System.Collections.Generic;
using System.IO;
using BatsSolitaire.Core;
using BatsSolitaire.Presentation;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace BatsSolitaire.Editor
{
    public static class DeckAssetImporter
    {
        private const string ManifestPath = "Assets/Resources/DeckManifest.asset";
        private const string AtlasPath = "Assets/Art/Cards/BatsCards.spriteatlas";
        private const string MetadataPath = "Assets/Art/Cards/deck-metadata.csv";

        [MenuItem("Bats Cards/Refresh Deck Manifest")]
        public static void RefreshFromMenu() => RefreshManifest(true);

        public static bool RefreshManifest(bool showDialog)
        {
            var manifest = AssetDatabase.LoadAssetAtPath<DeckManifest>(ManifestPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<DeckManifest>();
                AssetDatabase.CreateAsset(manifest, ManifestPath);
            }

            manifest.Cards = new List<DeckManifest.Entry>();
            var metadata = LoadMetadata();
            var missing = new List<string>();
            var sprites = new List<UnityEngine.Object>();
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                for (var rankValue = 1; rankValue <= 13; rankValue++)
                {
                    var rank = (CardRank)rankValue;
                    var path = $"Assets/Art/Cards/{suit}_{rank}.png";
                    ConfigureTexture(path);
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite == null) missing.Add(path);
                    else sprites.Add(sprite);
                    metadata.TryGetValue($"{suit}_{rank}", out var details);
                    manifest.Cards.Add(new DeckManifest.Entry
                    {
                        Suit = suit,
                        Rank = rank,
                        Sprite = sprite,
                        CommonNamePt = details?.CommonNamePt ?? string.Empty,
                        CommonNameEn = details?.CommonNameEn ?? string.Empty,
                        ScientificName = details?.ScientificName ?? string.Empty
                    });
                }
            }
            ConfigureTexture("Assets/Art/Cards/Back.png");
            manifest.Back = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Cards/Back.png");
            if (manifest.Back == null) missing.Add("Assets/Art/Cards/Back.png");
            else sprites.Add(manifest.Back);
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            if (missing.Count == 0) BuildAtlas(sprites);

            if (showDialog)
            {
                var message = missing.Count == 0
                    ? "52 faces e verso encontrados."
                    : $"Manifesto atualizado. Faltam {missing.Count} arquivos; serão usados placeholders.";
                EditorUtility.DisplayDialog("Bats Cards", message, "OK");
            }
            return missing.Count == 0;
        }

        private static Dictionary<string, Metadata> LoadMetadata()
        {
            var result = new Dictionary<string, Metadata>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(MetadataPath)) return result;
            var lines = File.ReadAllLines(MetadataPath);
            for (var i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].Split(';');
                if (fields.Length != 4 || string.IsNullOrWhiteSpace(fields[0])) continue;
                result[fields[0]] = new Metadata(fields[1], fields[2], fields[3]);
            }
            return result;
        }

        private sealed class Metadata
        {
            public readonly string CommonNamePt;
            public readonly string CommonNameEn;
            public readonly string ScientificName;

            public Metadata(string commonNamePt, string commonNameEn, string scientificName)
            {
                CommonNamePt = commonNamePt;
                CommonNameEn = commonNameEn;
                ScientificName = scientificName;
            }
        }

        private static void ConfigureTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            var changed = importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != 2048;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency = true;
            if (changed) importer.SaveAndReimport();
        }

        private static void BuildAtlas(List<UnityEngine.Object> sprites)
        {
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
            }
            var current = atlas.GetPackables();
            if (current.Length > 0) SpriteAtlasExtensions.Remove(atlas, current);
            SpriteAtlasExtensions.Add(atlas, sprites.ToArray());
            var settings = atlas.GetPackingSettings();
            settings.enableRotation = false;
            settings.enableTightPacking = false;
            settings.padding = 4;
            atlas.SetPackingSettings(settings);
            // Cards are strongly reduced on phones. Mipmaps prevent noisy text/photos at that scale.
            var textureSettings = atlas.GetTextureSettings();
            textureSettings.generateMipMaps = true;
            textureSettings.filterMode = FilterMode.Trilinear;
            atlas.SetTextureSettings(textureSettings);
            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
        }
    }
}
