#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using BatsSolitaire.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BatsSolitaire.Presentation
{
    public sealed class DevelopmentQa : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Launch()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-bats-qa") < 0) return;
            new GameObject("Development QA").AddComponent<DevelopmentQa>();
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            var directory = Path.GetFullPath("Builds/QA-Gallery");
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2);
            var app = FindFirstObjectByType<AppBootstrap>();
            var sizes = new[] { new Vector2Int(320, 720), new Vector2Int(360, 800), new Vector2Int(390, 844), new Vector2Int(430, 932), new Vector2Int(800, 360), new Vector2Int(768, 1024), new Vector2Int(1280, 720) };
            foreach (var size in sizes)
            {
                Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
                yield return new WaitForSecondsRealtime(1);
                app.SendMessage("ShowMenu");
                yield return new WaitForSecondsRealtime(0.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, $"{size.x}x{size.y}-menu.png"));
                app.SendMessage("ShowGallery");
                yield return new WaitForSecondsRealtime(0.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, $"{size.x}x{size.y}-gallery.png"));
                FindFirstObjectByType<CardGallery>().OpenCard(0);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, $"{size.x}x{size.y}-viewer.png"));
                app.SendMessage("StartGame", GameMode.Random);
                yield return new WaitForSecondsRealtime(0.7f);
                var views = FindObjectsByType<CardView>(FindObjectsSortMode.None);
                var corners = new Vector3[4];
                foreach (var view in views)
                {
                    view.GetComponent<RectTransform>().GetWorldCorners(corners);
                    if (corners.Any(p => p.x < -1 || p.x > Screen.width + 1 || p.y < -1 || p.y > Screen.height + 1))
                    {
                        File.WriteAllText(Path.Combine(directory, "FAILED.txt"), $"Card outside viewport: {size}, {view.State.Card.Key}");
                        Application.Quit(1);
                        yield break;
                    }
                }
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, $"{size.x}x{size.y}-table.png"));
                var original = views.First(v => v.State.FaceUp && v.OriginalSprite != null);
                CardDetails.Open(FindFirstObjectByType<GameScreen>().transform, original.OriginalSprite, false);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(directory, $"{size.x}x{size.y}-original.png"));
                yield return new WaitForSecondsRealtime(0.3f);
            }
            File.WriteAllText(Path.Combine(directory, "PASS.txt"), "7 window sizes: menu, table, gallery, viewer, original-card inspector. Rendered cards inside viewport. Not a physical Android performance test.");
            Application.Quit(0);
        }
    }
}
#endif
