using System.IO;
using BatsSolitaire.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BatsSolitaire.Editor
{
    public static class ProjectSetup
    {
        [MenuItem("Bats Cards/Configure Project")]
        public static void Configure()
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Art/Cards");
            Directory.CreateDirectory("Builds");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Bats Cards");
            bootstrap.AddComponent<AppBootstrap>();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };

            PlayerSettings.companyName = "Bats Cards";
            PlayerSettings.productName = "Bats Cards";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.batscards.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.batscards.game");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.androidIsGame = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            EditorUserBuildSettings.buildAppBundle = true;

            ConfigureBranding();
            ConfigureTableBackground();
            SetActiveInputHandling();
            DeckAssetImporter.RefreshManifest(false);
            AssetDatabase.SaveAssets();
            Debug.Log("Bats Cards configurado com sucesso.");
        }

        private static void ConfigureBranding()
        {
            const string path = "Assets/Art/Brand/AppIcon.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return;
            var icons = new[] { texture };
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Application);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) return;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.035f, 0.16f, 0.17f);
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, sprite) };
        }

        private static void ConfigureTableBackground()
        {
            const string path = "Assets/Resources/TableBackground.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void SetActiveInputHandling()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) return;
            var settings = new SerializedObject(assets[0]);
            var activeInputHandler = settings.FindProperty("activeInputHandler");
            if (activeInputHandler == null) return;
            activeInputHandler.intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
