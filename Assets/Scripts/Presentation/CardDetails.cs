using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace BatsSolitaire.Presentation
{
    // Uses the original sprite, never reconstructed or translated card content.
    public sealed class CardDetails : MonoBehaviour
    {
        private RectTransform viewport;
        private RectTransform content;
        private ScrollRect scroll;
        private Sprite original;
        private float zoom = 1;
        private Vector2 lastSize;
        private List<Sprite> sequence;
        private int index;
        private Text title;
        private bool reduceMotion;
        public int CurrentIndex => index;
        public Sprite CurrentSprite => original;

        public static void Open(Transform parent, Sprite sprite, bool english, List<Sprite> cards = null, int selected = 0, bool reduced = false)
        {
            if (sprite == null) return;
            var root = UiFactory.Panel(parent, new Color(0.01f, 0.025f, 0.03f, 1f), Vector2.zero, Vector2.one);
            root.name = "OriginalCardDetails";
            var details = root.AddComponent<CardDetails>();
            details.original = sprite;
            details.sequence = cards == null ? null : new List<Sprite>(cards);
            details.index = selected; details.reduceMotion = reduced;
            var area = UiFactory.Panel(root.transform, Color.clear, new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.88f));
            details.viewport = area.GetComponent<RectTransform>();
            area.AddComponent<RectMask2D>();
            details.scroll = area.AddComponent<ScrollRect>();
            details.scroll.viewport = details.viewport;
            details.scroll.movementType = ScrollRect.MovementType.Clamped;
            var card = new GameObject("Original", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(area.transform, false);
            details.content = card.GetComponent<RectTransform>();
            details.content.anchorMin = details.content.anchorMax = new Vector2(0.5f, 0.5f);
            var image = card.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            details.scroll.content = details.content;
            details.title = UiFactory.Label(root.transform, english ? "Original card · drag to pan when zoomed" : "Carta original · arraste para explorar no zoom", 24,
                TextAnchor.MiddleCenter, new Vector2(0.04f, 0.9f), new Vector2(0.96f, 0.99f), Color.white);
            UiFactory.Button(root.transform, "−", new Vector2(0.04f, 0.02f), new Vector2(0.24f, 0.10f)).onClick.AddListener(() => details.SetZoom(details.zoom - 0.5f));
            UiFactory.Button(root.transform, "1:1", new Vector2(0.27f, 0.02f), new Vector2(0.47f, 0.10f)).onClick.AddListener(() => details.SetZoom(1));
            UiFactory.Button(root.transform, "+", new Vector2(0.50f, 0.02f), new Vector2(0.70f, 0.10f)).onClick.AddListener(() => details.SetZoom(details.zoom + 0.5f));
            UiFactory.Button(root.transform, cards != null ? (english ? "Gallery" : "Galeria") : (english ? "Close" : "Fechar"), new Vector2(0.73f, 0.02f), new Vector2(0.96f, 0.10f)).onClick.AddListener(() => Destroy(root));
            if (cards != null)
            {
                details.viewport.anchorMin = new Vector2(0.03f, 0.21f);
                UiFactory.Button(root.transform, english ? "Previous" : "Anterior", new Vector2(0.04f, 0.12f), new Vector2(0.47f, 0.20f)).onClick.AddListener(() => details.Navigate(-1));
                UiFactory.Button(root.transform, english ? "Next" : "Próxima", new Vector2(0.53f, 0.12f), new Vector2(0.96f, 0.20f)).onClick.AddListener(() => details.Navigate(1));
                area.AddComponent<CardSwipe>().Swiped = direction => { if (details.zoom == 1) details.Navigate(direction); };
                details.Navigate(0);
            }
        }

        public void Navigate(int direction)
        {
            if (sequence == null || sequence.Count == 0) return;
            index = (index + direction + sequence.Count) % sequence.Count;
            original = sequence[index]; content.GetComponent<Image>().sprite = original;
            title.text = $"{index + 1} / {sequence.Count}";
            SetZoom(1);
            StopAllCoroutines();
            if (!reduceMotion && direction != 0) StartCoroutine(Reveal());
        }
        private IEnumerator Reveal()
        {
            var image = content.GetComponent<Image>();
            for (var elapsed = 0f; elapsed < 0.12f; elapsed += Time.unscaledDeltaTime)
            {
                image.color = new Color(1, 1, 1, Mathf.Lerp(0.5f, 1, elapsed / 0.12f)); yield return null;
            }
            image.color = Color.white;
        }

        private void Update()
        {
            var height = Mathf.Max(1, ((RectTransform)transform).rect.height);
            var unit = UiFactory.Pixels(transform, 1) / height;
            viewport.anchorMin = new Vector2(0.03f, (sequence == null ? 60 : 112) * unit);
            viewport.anchorMax = new Vector2(0.97f, 1 - 52 * unit);
            title.rectTransform.anchorMin = new Vector2(0.04f, 1 - 48 * unit);
            title.rectTransform.anchorMax = new Vector2(0.96f, 1 - 4 * unit);
            var buttons = GetComponentsInChildren<Button>();
            for (var i = 0; i < buttons.Length; i++)
            {
                var r = (RectTransform)buttons[i].transform;
                r.anchorMin = new Vector2(r.anchorMin.x, (i < 4 ? 8 : 60) * unit);
                r.anchorMax = new Vector2(r.anchorMax.x, (i < 4 ? 52 : 104) * unit);
            }
            if (lastSize != viewport.rect.size) SetZoom(zoom);
            var keys = Keyboard.current;
            if (keys == null) return;
            if (keys.escapeKey.wasPressedThisFrame) Destroy(gameObject);
            if (keys.leftArrowKey.wasPressedThisFrame) Navigate(-1);
            if (keys.rightArrowKey.wasPressedThisFrame) Navigate(1);
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, 1, 3);
            lastSize = viewport.rect.size;
            var fit = Mathf.Min(lastSize.x / original.rect.width, lastSize.y / original.rect.height);
            content.sizeDelta = original.rect.size * fit * zoom;
            content.anchoredPosition = Vector2.zero;
            scroll.StopMovement();
        }
    }
}
