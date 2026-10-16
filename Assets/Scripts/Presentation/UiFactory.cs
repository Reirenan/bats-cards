using UnityEngine;
using UnityEngine.UI;

namespace BatsSolitaire.Presentation
{
    public static class UiFactory
    {
        private static Font font;
        public static readonly Color Ivory = new(0.965f, 0.93f, 0.86f);
        public const float TouchSize = 44f;
        public const int BodySize = 28;
        public static float PixelDensity => !Application.isMobilePlatform ? 1f :
            Mathf.Clamp(Screen.dpi >= 120 ? Screen.dpi / 160f : Mathf.Min(Screen.width, Screen.height) / 360f, 1f, 4f);
        public static float ScreenWidth => Screen.width / PixelDensity;
        public static float ScreenHeight => Screen.height / PixelDensity;
        public static float Pixels(Transform parent, float pixels) => pixels * PixelDensity / Mathf.Max(0.1f, parent.GetComponentInParent<Canvas>()?.scaleFactor ?? 1f);

        public static void Border(Transform parent, Color color, float thickness = 2f)
        {
            for (var i = 0; i < 4; i++)
            {
                var vertical = i < 2;
                var edge = i % 2;
                var strip = Panel(parent, color, vertical ? new Vector2(edge, 0) : new Vector2(0, edge),
                    vertical ? new Vector2(edge, 1) : new Vector2(1, edge));
                strip.name = "Border";
                strip.GetComponent<Image>().raycastTarget = false;
                var rect = strip.GetComponent<RectTransform>();
                rect.sizeDelta = vertical ? new Vector2(thickness, 0) : new Vector2(0, thickness);
            }
        }

        public static GameObject Panel(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            gameObject.GetComponent<Image>().color = color;
            return gameObject;
        }

        public static GameObject Background(Transform parent, Sprite sprite)
        {
            var gameObject = new GameObject("TableBackground", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sprite != null ? sprite.rect.size : new Vector2(1080, 1920);
            var image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            var fitter = gameObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null ? sprite.rect.width / sprite.rect.height : 9f / 16f;
            return gameObject;
        }

        public static Text Label(Transform parent, string value, int size, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var gameObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = gameObject.GetComponent<Text>();
            text.font = size >= 40 ? (font != null ? font : font = Resources.Load<Font>("Fonts/Cinzel") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")) : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            var owner = parent.GetComponentInParent<Canvas>();
            var scale = (owner != null ? Mathf.Max(0.1f, owner.scaleFactor) : 1f) / PixelDensity;
            text.resizeTextMinSize = Mathf.CeilToInt(14f / scale);
            text.resizeTextMaxSize = Mathf.Max(size, Mathf.CeilToInt(16f / scale));
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string value, Vector2 anchorMin, Vector2 anchorMax)
        {
            var gameObject = Panel(parent, new Color(0.06f, 0.10f, 0.11f, 0.88f), anchorMin, anchorMax);
            gameObject.name = "Button_" + value;
            var button = gameObject.AddComponent<Button>();
            Border(gameObject.transform, new Color(0.50f, 0.66f, 0.64f, 0.58f));
            var colors = button.colors;
            colors.highlightedColor = new Color(0.88f, 0.36f, 0.42f, 0.96f);
            colors.pressedColor = new Color(0.68f, 0.22f, 0.28f, 0.96f);
            colors.selectedColor = new Color(0.9f, 0.72f, 0.43f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            button.colors = colors;
            var label = Label(gameObject.transform, value, BodySize, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Ivory);
            label.rectTransform.offsetMin = new Vector2(Pixels(parent, 6), 2);
            label.rectTransform.offsetMax = new Vector2(-Pixels(parent, 6), -2);
            return button;
        }

        public static void Icon(Button button, string kind)
        {
            var icon = new GameObject("Icon_" + kind, typeof(RectTransform), typeof(UiIcon));
            icon.transform.SetParent(button.transform, false);
            var r = icon.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0, 0.5f);
            r.anchoredPosition = new Vector2(Pixels(button.transform, 22), 0);
            r.sizeDelta = Vector2.one * Pixels(button.transform, 20);
            icon.GetComponent<UiIcon>().Kind = kind;
            icon.GetComponent<UiIcon>().color = Ivory;
            icon.GetComponent<UiIcon>().raycastTarget = false;
            button.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(Pixels(button.transform, 40), 2);
        }

        public static void SetAccent(Button button)
        {
            button.GetComponent<Image>().color = new Color(0.54f, 0.05f, 0.13f, 0.96f);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.72f, 0.08f, 0.16f, 1f);
            colors.pressedColor = new Color(0.38f, 0.025f, 0.07f, 1f);
            button.colors = colors;
            foreach (Transform child in button.transform)
                if (child.name == "Border") child.GetComponent<Image>().color = new Color(0.85f, 0.33f, 0.26f, 0.9f);
        }
    }
}
