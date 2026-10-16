using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BatsSolitaire.Editor
{
    public static class BuildAutomation
    {
        [MenuItem("Bats Cards/Build/Android Test APK")]
        public static void BuildAndroidTest()
        {
            EnsureConfigured();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = false;
            EnsureSuccess(BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EnabledScenes(), locationPathName = "Builds/Android/BatsCards-Test.apk",
                target = BuildTarget.Android, options = BuildOptions.Development
            }));
        }
        [MenuItem("Bats Cards/Build/Windows Development")]
        public static void BuildWindowsDevelopment()
        {
            EnsureConfigured();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = "Builds/WindowsDevelopment/BatsCards.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            EnsureSuccess(report);
        }

        [MenuItem("Bats Cards/Build/Windows")]
        public static void BuildWindows()
        {
            EnsureConfigured();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = "Builds/Windows/BatsCards.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            EnsureSuccess(report);
        }

        [MenuItem("Bats Cards/Build/Android AAB")]
        public static void BuildAndroid()
        {
            EnsureConfigured();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            EditorUserBuildSettings.buildAppBundle = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EnabledScenes(),
                locationPathName = "Builds/Android/BatsCards.aab",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            EnsureSuccess(report);
        }

        private static void EnsureConfigured()
        {
            DeckIntegrity.Validate();
            if (!File.Exists("Assets/Scenes/Main.unity")) ProjectSetup.Configure();
            Directory.CreateDirectory("Builds/Windows");
            Directory.CreateDirectory("Builds/WindowsDevelopment");
            Directory.CreateDirectory("Builds/Android");
            if (!DeckAssetImporter.RefreshManifest(false))
                throw new BuildFailedException("Build bloqueado: faltam artes originais do baralho.");
        }

        private static string[] EnabledScenes()
        {
            var scenes = Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled);
            return Array.ConvertAll(scenes, scene => scene.path);
        }

        private static void EnsureSuccess(BuildReport report)
        {
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Build falhou: {report.summary.result}");
            Debug.Log($"Build concluído: {report.summary.outputPath}");
        }
    }
}
