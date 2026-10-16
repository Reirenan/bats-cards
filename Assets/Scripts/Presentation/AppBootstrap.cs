using System;
using BatsSolitaire.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BatsSolitaire.Presentation
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        private SaveService saves;
        private GameSession session;
        private TextCatalog text;
        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private AudioService audio;
        private GameObject currentScreen;
        private Vector2 displaySize;
        private Rect safeArea;

        private void Update()
        {
            var size = new Vector2(Screen.width, Screen.height);
            if (displaySize == size && safeArea == Screen.safeArea) return;
            displaySize = size;
            safeArea = Screen.safeArea;
            ApplyUiScale();
            if (currentScreen == null) return;
            if (currentScreen.name == "Menu") ShowMenu();
            else if (currentScreen.name == "Settings") ShowSettings();
            else ApplySafeArea(currentScreen.GetComponent<RectTransform>());
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            saves = new SaveService();
            text = new TextCatalog { Language = saves.Data.Settings.Language };
            session = new GameSession(saves);
            audio = gameObject.AddComponent<AudioService>();
            audio.Initialize(saves.Data.Settings);
            BuildEventSystem();
            BuildCanvas();
            ShowMenu();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) session.Save();
        }

        private void OnApplicationQuit() => session.Save();

        private void BuildEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("Canvas");
            DontDestroyOnLoad(canvasObject);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            ApplyUiScale();
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = Screen.width < Screen.height ? 0 : 1;
        }

        private void ShowMenu()
        {
            ReplaceScreen("Menu");
            var panel = BuildBrandedPanel(0.7f);
            var title = UiFactory.Label(panel.transform, "BATS", 104, TextAnchor.MiddleCenter,
                new Vector2(0.055f, 0.75f), new Vector2(0.43f, 0.94f), new Color(0.96f, 0.92f, 0.84f));
            title.fontStyle = FontStyle.Bold;
            UiFactory.Label(panel.transform, "—  C A R D S  —", 35, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.69f), new Vector2(0.43f, 0.78f), new Color(0.96f, 0.92f, 0.84f));
            UiFactory.Label(panel.transform, "♣   ♥   ♦   ♠", 25, TextAnchor.MiddleCenter,
                new Vector2(0.11f, 0.65f), new Vector2(0.38f, 0.72f), new Color(0.69f, 0.08f, 0.15f));
            UiFactory.Label(panel.transform, text.Get("tagline"), 26, TextAnchor.MiddleCenter,
                new Vector2(0.06f, 0.60f), new Vector2(0.43f, 0.67f), new Color(0.96f, 0.92f, 0.84f));
            AddMenuCardFan(panel.transform);

            var y = 0.59f;
            if (saves.Data.CurrentGame != null && !saves.Data.CurrentGame.Won)
            {
                var resume = AddMenuButton(panel.transform, text.Get("resume"), y, ResumeGame, ref y);
                UiFactory.Icon(resume, "play");
                UiFactory.SetAccent(resume);
            }
            UiFactory.Icon(AddMenuButton(panel.transform, text.Get("random"), y, () => StartGame(Core.GameMode.Random), ref y), "shuffle");
            UiFactory.Icon(AddMenuButton(panel.transform, text.Get("winnable"), y, () => StartGame(Core.GameMode.Winnable), ref y), "trophy");
            UiFactory.Icon(AddMenuButton(panel.transform, text.Get("daily"), y, () => StartGame(Core.GameMode.Daily), ref y), "calendar");
            UiFactory.Icon(AddMenuButton(panel.transform, text.Get("settings"), y, ShowSettings, ref y), "settings");
            UiFactory.Icon(AddMenuButton(panel.transform, text.Language.StartsWith("en") ? "Card Gallery" : "Galeria de Cartas", y, ShowGallery, ref y), "gallery");

            var language = UiFactory.Button(panel.transform, text.Language.StartsWith("en") ? "Português" : "English",
                new Vector2(0.065f, 0.018f), new Vector2(0.25f, 0.075f));
            language.onClick.AddListener(() =>
            {
                text.Language = text.Language.StartsWith("en") ? "pt-BR" : "en";
                saves.Data.Settings.Language = text.Language;
                saves.Write();
                ShowMenu();
            });
            if (Screen.width < Screen.height)
            {
                foreach (RectTransform child in panel.transform)
                {
                    if (child.name == "TableBackground" || child.name == "Panel") continue;
                    var min = child.anchorMin;
                    var max = child.anchorMax;
                    child.anchorMin = new Vector2(Mathf.Clamp(min.x * 2, 0.04f, 0.96f), min.y);
                    child.anchorMax = new Vector2(Mathf.Clamp(max.x * 2, 0.04f, 0.96f), max.y);
                }
            }
            else if (UiFactory.ScreenHeight < 500)
            {
                // Short landscape screens use two rows of controls across a wider menu area.
                var buttons = panel.GetComponentsInChildren<Button>();
                for (var i = 0; i < buttons.Length - 1; i++)
                {
                    var r = (RectTransform)buttons[i].transform;
                    var column = i % 2; var row = i / 2;
                    r.anchorMin = new Vector2(0.03f + column * 0.36f, 0.43f - row * 0.16f);
                    r.anchorMax = new Vector2(0.37f + column * 0.36f, 0.57f - row * 0.16f);
                }
                var languageRect = (RectTransform)language.transform;
                languageRect.anchorMin = new Vector2(0.75f, 0.03f);
                languageRect.anchorMax = new Vector2(0.98f, 0.17f);
                foreach (Transform child in panel.transform)
                    if (child.name == "MenuCard") child.gameObject.SetActive(false);
            }
        }

        private Button AddMenuButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction action, ref float nextY)
        {
            var button = UiFactory.Button(parent, label, new Vector2(0.065f, y - 0.08f), new Vector2(0.43f, y));
            button.onClick.AddListener(action);
            nextY -= 0.085f;
            return button;
        }

        private void StartGame(Core.GameMode mode)
        {
            session.NewGame(mode);
            ShowGame();
        }

        private void ResumeGame()
        {
            if (!session.Resume()) session.NewGame(Core.GameMode.Random);
            ShowGame();
        }

        private void ShowGame()
        {
            ReplaceScreen("Game");
            var screen = currentScreen.AddComponent<GameScreen>();
            screen.Initialize(session, saves, text, canvas, audio, ShowMenu);
        }

        private void ShowSettings()
        {
            ReplaceScreen("Settings");
            var panel = BuildBrandedPanel(0.74f);
            UiFactory.Label(panel.transform, text.Get("settings"), 58, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.79f), new Vector2(0.8f, 0.93f), new Color(1f, 0.88f, 0.85f));
            var y = 0.76f;
            AddToggle(panel.transform, text.Get("reduce_motion"), () => saves.Data.Settings.ReduceMotion,
                value => saves.Data.Settings.ReduceMotion = value, ref y);
            AddToggle(panel.transform, text.Get("high_contrast"), () => saves.Data.Settings.HighContrast,
                value => saves.Data.Settings.HighContrast = value, ref y);
            AddToggle(panel.transform, text.Get("left_handed"), () => saves.Data.Settings.LeftHanded,
                value => saves.Data.Settings.LeftHanded = value, ref y);
            AddToggle(panel.transform, text.Get("haptics"), () => saves.Data.Settings.Haptics,
                value => saves.Data.Settings.Haptics = value, ref y);
            AddValue(panel.transform, text.Get("music"), () => saves.Data.Settings.MusicVolume,
                value => saves.Data.Settings.MusicVolume = value, ref y);
            AddValue(panel.transform, text.Get("effects"), () => saves.Data.Settings.EffectsVolume,
                value => saves.Data.Settings.EffectsVolume = value, ref y);
            AddUiScale(panel.transform, ref y);
            var back = UiFactory.Button(panel.transform, text.Get("back"), new Vector2(0.36f, 0.025f), new Vector2(0.64f, 0.095f));
            back.onClick.AddListener(() => { saves.Write(); audio.ApplyVolumes(); ShowMenu(); });
            if (Screen.width < Screen.height)
                foreach (var button in panel.GetComponentsInChildren<Button>())
                {
                    var r = button.GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.05f, r.anchorMin.y);
                    r.anchorMax = new Vector2(0.95f, r.anchorMax.y);
                }
        }

        private void ShowGallery()
        {
            ReplaceScreen("Gallery");
            currentScreen.AddComponent<CardGallery>().Initialize(Resources.Load<DeckManifest>("DeckManifest"),
                text.Language.StartsWith("en"), saves.Data.Settings.ReduceMotion, ShowMenu);
        }

        private void AddToggle(Transform parent, string label, Func<bool> get, Action<bool> set, ref float y)
        {
            var button = UiFactory.Button(parent, $"{label}: {(get() ? "✓" : "—")}", new Vector2(0.28f, y - 0.06f), new Vector2(0.72f, y));
            button.onClick.AddListener(() => { set(!get()); ShowSettings(); });
            y -= 0.082f;
        }

        private void AddValue(Transform parent, string label, Func<float> get, Action<float> set, ref float y)
        {
            var portrait = Screen.width < Screen.height;
            var row = UiFactory.Panel(parent, new Color(0.02f, 0.06f, 0.07f, 0.9f),
                new Vector2(portrait ? 0.05f : 0.28f, y - 0.06f), new Vector2(portrait ? 0.95f : 0.72f, y));
            var valueLabel = UiFactory.Label(row.transform, $"{label}: {Mathf.RoundToInt(get() * 100)}%", 25,
                TextAnchor.MiddleLeft, new Vector2(0.03f, 0.05f), new Vector2(0.47f, 0.95f), Color.white);
            var control = UiFactory.Panel(row.transform, Color.clear, new Vector2(0.50f, 0), new Vector2(0.96f, 1));
            var track = UiFactory.Panel(control.transform, new Color(0.15f, 0.24f, 0.24f), new Vector2(0, 0.44f), new Vector2(1, 0.56f));
            track.GetComponent<Image>().raycastTarget = false;
            var handle = UiFactory.Panel(control.transform, new Color(0.96f, 0.86f, 0.70f), new Vector2(0, 0.20f), new Vector2(0, 0.80f));
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24, 0);
            var slider = control.AddComponent<Slider>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.SetValueWithoutNotify(get());
            slider.onValueChanged.AddListener(value =>
            {
                set(value);
                valueLabel.text = $"{label}: {Mathf.RoundToInt(value * 100)}%";
                audio.ApplyVolumes();
            });
            y -= 0.082f;
        }

        private void AddUiScale(Transform parent, ref float y)
        {
            var value = saves.Data.Settings.UiScale;
            var button = UiFactory.Button(parent, $"{text.Get("ui_scale")}: {Mathf.RoundToInt(value * 100)}%",
                new Vector2(0.28f, y - 0.06f), new Vector2(0.72f, y));
            button.onClick.AddListener(() =>
            {
                saves.Data.Settings.UiScale = value >= 1.2f ? 0.9f : value + 0.1f;
                ApplyUiScale();
                ShowSettings();
            });
            y -= 0.082f;
        }

        private void ApplyUiScale()
        {
            var scale = saves?.Data.Settings.UiScale ?? 1f;
            canvasScaler.referenceResolution = Screen.width < Screen.height ? new Vector2(720f / scale, 1280f / scale) : new Vector2(1920f / scale, 1080f / scale);
            canvasScaler.matchWidthOrHeight = Screen.width < Screen.height ? 0 : 1;
        }

        private GameObject BuildBrandedPanel(float overlayAlpha)
        {
            var panel = UiFactory.Panel(currentScreen.transform, Color.white, Vector2.zero, Vector2.one);
            UiFactory.Background(panel.transform, Resources.Load<Sprite>("TableBackground"));
            var overlay = UiFactory.Panel(panel.transform, new Color(0.01f, 0.04f, 0.045f, overlayAlpha), Vector2.zero, Vector2.one);
            overlay.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private void AddMenuCardFan(Transform parent)
        {
            if (Screen.width < Screen.height) return;
            var manifest = Resources.Load<DeckManifest>("DeckManifest");
            if (manifest == null || manifest.Cards.Count < 41) return;
            var virtualWidth = 1080f * Screen.width / Screen.height;
            var width = Mathf.Min(315f, virtualWidth * 0.16f);
            CreateMenuCard(parent, manifest.Cards[3].Sprite, new Vector2(virtualWidth * 0.15f, -10), 13f, width, 0);
            CreateMenuCard(parent, manifest.Cards[22].Sprite, new Vector2(virtualWidth * 0.285f, 15), 0f, width, 1);
            CreateMenuCard(parent, manifest.Cards[40].Sprite, new Vector2(virtualWidth * 0.42f, -10), -13f, width, 0);
        }

        private static void CreateMenuCard(Transform parent, Sprite sprite, Vector2 position, float angle, float width, int order)
        {
            if (sprite == null) return;
            var card = new GameObject("MenuCard", typeof(RectTransform), typeof(Image), typeof(Shadow));
            card.transform.SetParent(parent, false);
            var rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, width * 1.416f);
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            card.transform.SetSiblingIndex(Mathf.Max(2, order + 2));
            var image = card.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var shadow = card.GetComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.72f);
            shadow.effectDistance = new Vector2(12, -12);
        }

        private void ReplaceScreen(string name)
        {
            if (currentScreen != null) { currentScreen.SetActive(false); Destroy(currentScreen); }
            currentScreen = new GameObject(name);
            currentScreen.transform.SetParent(canvas.transform, false);
            var rect = currentScreen.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            ApplySafeArea(rect);
            if (!saves.Data.Settings.ReduceMotion) StartCoroutine(FadeIn(currentScreen.AddComponent<CanvasGroup>()));
        }

        private System.Collections.IEnumerator FadeIn(CanvasGroup group)
        {
            for (var elapsed = 0f; elapsed < 0.12f; elapsed += Time.unscaledDeltaTime)
            {
                if (group == null) yield break;
                group.alpha = Mathf.Lerp(0.65f, 1, elapsed / 0.12f);
                yield return null;
            }
            if (group != null) group.alpha = 1;
        }

        private static void ApplySafeArea(RectTransform rect)
        {
            var area = Screen.safeArea;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
