using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BatsSolitaire.Editor
{
    public sealed class DeckIntegrity : IPreprocessBuildWithReport
    {
        [Serializable] private sealed class Entry { public string key; public int page; public string sha256; }
        [Serializable] private sealed class Baseline { public Entry[] entries; }
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Validate();
        public static void Validate()
        {
            var baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText("Assets/Art/Cards/original-sha256.json"));
            if (baseline?.entries == null || baseline.entries.Length != 53 || baseline.entries.Select(e => e.key).Distinct().Count() != 53)
                throw new BuildFailedException("Baseline do baralho ausente ou inválida.");
            using var hash = SHA256.Create();
            foreach (var entry in baseline.entries)
            {
                var path = $"Assets/Art/Cards/{entry.key}.png";
                if (!File.Exists(path) || BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "") != entry.sha256)
                    throw new BuildFailedException($"Original ausente ou alterado: {entry.key}, página Canva {entry.page}.");
            }
            Debug.Log("DeckIntegrity: 53 originais preservados, hashes SHA256 conferidos.");
        }
    }
}
