using System;
using System.Collections;
using System.Collections.Generic;
using BatsSolitaire.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BatsSolitaire.Presentation
{
    public sealed class CardView : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rect;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Image image;
        private Color baseColor;
        private Vector2 homePosition;
        private readonly List<CardView> dragging = new();
        private int homeSibling;
        private GameObject selection;
        private bool suppressClick;
        public bool ReduceMotion { get; set; }
        public Sprite OriginalSprite => image.sprite;
        public bool IsDragging { get; private set; }

        public CardState State { get; private set; }
        public PileRef Source { get; private set; }
        public int CardIndex { get; private set; }
        public Action<CardView, bool> Clicked;
        public Action<CardView, Vector2> DragEnded;

        public void Initialize(CardState state, PileRef source, int cardIndex, Vector2 size, DeckManifest manifest, Canvas owner)
        {
            State = state;
            Source = source;
            CardIndex = cardIndex;
            canvas = owner;
            rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            image = gameObject.AddComponent<Image>();
            image.raycastTarget = true;
            var entry = manifest != null ? manifest.Find(state.Card) : null;
            var sprite = state.FaceUp ? entry?.Sprite : manifest?.Back;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = sprite != null ? Color.white : state.FaceUp ? new Color(0.98f, 0.97f, 0.91f) : new Color(0.36f, 0.66f, 0.61f);
            var hitPadding = UiFactory.Pixels(transform, 8);
            image.raycastPadding = new Vector4(-hitPadding, -hitPadding, -hitPadding, -hitPadding);
            baseColor = image.color;
            selection = UiFactory.Panel(transform, Color.clear, Vector2.zero, Vector2.one);
            selection.name = "SelectionBorder";
            selection.GetComponent<Image>().raycastTarget = false;
            UiFactory.Border(selection.transform, new Color(1f, 0.75f, 0.35f, 0.85f), 3f);
            selection.SetActive(false);

            if (sprite == null) BuildFallbackLabel(state);
            else if (state.FaceUp)
            {
                var badge = UiFactory.Panel(transform, new Color(0.985f, 0.97f, 0.91f), new Vector2(0, 1), Vector2.one);
                badge.name = "ReadableIndex";
                badge.GetComponent<Image>().raycastTarget = true;
                var badgeRect = badge.GetComponent<RectTransform>();
                badgeRect.pivot = new Vector2(0.5f, 0);
                badgeRect.sizeDelta = new Vector2(0, UiFactory.Pixels(transform, 22));
                UiFactory.Border(badge.transform, new Color(0.24f, 0.16f, 0.10f, 0.72f), 1f);
                var color = state.Card.IsRed ? new Color(0.72f, 0.02f, 0.08f) : new Color(0.025f, 0.025f, 0.025f);
                var label = UiFactory.Label(badge.transform, state.Card.RankSymbol,
                    30, TextAnchor.MiddleRight, new Vector2(0.08f, 0), new Vector2(0.50f, 1), color);
                label.resizeTextMinSize = label.resizeTextMaxSize = Mathf.RoundToInt(UiFactory.Pixels(transform, 15));
                label.fontStyle = FontStyle.Bold;
                var suitObject = new GameObject("Suit_" + state.Card.Suit, typeof(RectTransform), typeof(UiIcon));
                suitObject.transform.SetParent(badge.transform, false);
                var suitRect = suitObject.GetComponent<RectTransform>();
                suitRect.anchorMin = new Vector2(0.52f, 0.14f);
                suitRect.anchorMax = new Vector2(0.78f, 0.86f);
                suitRect.offsetMin = suitRect.offsetMax = Vector2.zero;
                var suit = suitObject.GetComponent<UiIcon>();
                suit.Kind = state.Card.Suit.ToString().ToLowerInvariant();
                suit.color = color;
                suit.raycastTarget = false;
            }
        }

        public void SetAnchoredPosition(Vector2 position)
        {
            rect.anchoredPosition = position;
            homePosition = position;
        }

        public void SetHighlighted(bool highlighted)
        {
            selection.SetActive(highlighted);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressClick) { suppressClick = false; return; }
            if (!State.FaceUp && Source.Kind != PileKind.Stock) return;
            Clicked?.Invoke(this, eventData.clickCount >= 2);
        }

        public void OnPointerDown(PointerEventData eventData) => suppressClick = false;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!State.FaceUp) return;
            IsDragging = true;
            suppressClick = true;
            dragging.Clear();
            foreach (var view in transform.parent.GetComponentsInChildren<CardView>())
                if (view == this || (Source.Kind == PileKind.Tableau && view.Source.Kind == Source.Kind &&
                    view.Source.Index == Source.Index && view.CardIndex > CardIndex)) dragging.Add(view);
            dragging.Sort((a, b) => a.CardIndex.CompareTo(b.CardIndex));
            foreach (var view in dragging)
            {
                view.StopAllCoroutines();
                view.homePosition = view.rect.anchoredPosition;
                view.homeSibling = view.transform.GetSiblingIndex();
            }
            foreach (var view in dragging)
            {
                view.canvasGroup.blocksRaycasts = false;
                view.transform.SetAsLastSibling();
                view.SetHighlighted(true);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!State.FaceUp) return;
            foreach (var view in dragging) view.rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!State.FaceUp) return;
            IsDragging = false;
            DragEnded?.Invoke(this, eventData.position);
            foreach (var view in dragging)
            {
                if (view == null || !view.gameObject.activeInHierarchy) continue;
                view.canvasGroup.blocksRaycasts = true;
                view.SetHighlighted(false);
                view.transform.SetSiblingIndex(view.homeSibling);
                view.AnimateFrom(view.rect.anchoredPosition, view.homePosition, 0);
            }
            dragging.Clear();
        }

        public void AnimateFrom(Vector2 from, Vector2 to, float delay)
        {
            StopAllCoroutines();
            homePosition = to;
            if (ReduceMotion) { rect.anchoredPosition = to; return; }
            StartCoroutine(Slide(from, to, delay));
        }

        private IEnumerator Slide(Vector2 from, Vector2 to, float delay)
        {
            rect.anchoredPosition = from;
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            for (var elapsed = 0f; elapsed < 0.18f; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / 0.18f);
                rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }
            rect.anchoredPosition = to;
        }

        private void BuildFallbackLabel(CardState state)
        {
            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            var labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5, 5);
            labelRect.offsetMax = new Vector2(-5, -5);
            var label = labelObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.UpperLeft;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 42;
            label.color = state.FaceUp && state.Card.IsRed ? new Color(0.78f, 0.08f, 0.12f) : Color.black;
            label.text = $"{state.Card.RankSymbol}\n{state.Card.SuitSymbol}\nARTE AUSENTE";
        }
    }
}
