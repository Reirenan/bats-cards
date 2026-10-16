using System;
using System.IO;
using BatsSolitaire.Core;
using UnityEngine;

namespace BatsSolitaire.Services
{
    [Serializable]
    public sealed class PlayerSettings
    {
        public string Language = "pt-BR";
        public float MusicVolume = 0.6f;
        public float EffectsVolume = 0.8f;
        public bool Haptics = true;
        public bool ReduceMotion;
        public bool HighContrast;
        public bool LeftHanded;
        public float UiScale = 1f;
    }

    [Serializable]
    public sealed class PlayerStats
    {
        public int GamesStarted;
        public int GamesWon;
        public int BestScore;
        public float BestTimeSeconds;
        public int CurrentWinStreak;
        public int BestWinStreak;
        public string LastDailyDate;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int SchemaVersion = 1;
        public GameState CurrentGame;
        public PlayerSettings Settings = new();
        public PlayerStats Stats = new();
    }

    public sealed class SaveService
    {
        private readonly string path;
        public SaveData Data { get; private set; }

        public SaveService(string customPath = null)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (customPath == null && Array.IndexOf(Environment.GetCommandLineArgs(), "-bats-qa") >= 0)
                customPath = Path.Combine(Application.temporaryCachePath, "bats-qa-save.json");
#endif
            path = customPath ?? Path.Combine(Application.persistentDataPath, "bats-solitaire-save.json");
            Data = Load();
        }

        public void SaveGame(GameState state)
        {
            if (state == null) return;
            Data.CurrentGame = state?.Clone();
            Write();
        }

        public void Write()
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(Data, true));
            if (File.Exists(path)) File.Replace(temporaryPath, path, path + ".bak");
            else File.Move(temporaryPath, path);
        }

        private SaveData Load()
        {
            foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
            {
            if (!File.Exists(candidate)) continue;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(candidate));
                if (data == null || data.SchemaVersion != 1) continue;
                data.Settings ??= new PlayerSettings();
                data.Stats ??= new PlayerStats();
                data.CurrentGame?.EnsurePiles();
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Não foi possível carregar o save: {exception.Message}");
            }
            }
            return new SaveData();
        }
    }
}
