using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BatsSolitaire.Core;

namespace BatsSolitaire.Presentation
{
    // Browsing is intentionally independent of GameSession and SaveService.
    public sealed class CardGallery : MonoBehaviour
    {
        private DeckManifest deck;
        private bool english, reduceMotion;
        private RectTransform viewport, content;
        private GridLayoutGroup grid;
        private Vector2 lastSize;
        private Vector2 lastRootSize;
        private Text heading;
        private Button backButton;
        private readonly List<Sprite> visible = new();
        private readonly List<Button> filters = new();
        public int CardCount => visible.Count;
        public void Initialize(DeckManifest manifest, bool useEnglish, bool reduced, Action back)
        {
            deck = manifest; english = useEnglish; reduceMotion = reduced;
            UiFactory.Panel(transform, new Color(0.025f, 0.07f, 0.08f), Vector2.zero, Vector2.one);
            heading = UiFactory.Label(transform, english ? "Card Gallery" : "Galeria de Cartas", 36, TextAnchor.MiddleCenter,
                new Vector2(0.04f, 0.9f), new Vector2(0.96f, 1), UiFactory.Ivory);
            for (var i = 0; i < 5; i++)
            {
                var suit = i - 1;
                var title = i == 0 ? (english ? "All" : "Todas") : new CardId((CardSuit)suit, CardRank.Ace).SuitSymbol;
                var button = UiFactory.Button(transform, title, new Vector2(0.03f + i * 0.19f, 0.81f), new Vector2(0.21f + i * 0.19f, 0.89f));
                button.onClick.AddListener(() => Filter(suit)); filters.Add(button);
            }
            var area = UiFactory.Panel(transform, Color.clear, new Vector2(0.03f, 0.11f), new Vector2(0.97f, 0.79f));
            viewport = area.GetComponent<RectTransform>(); area.AddComponent<RectMask2D>();
            var scroll = area.AddComponent<ScrollRect>(); scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewport;
            content = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0.5f, 1);
            grid = content.gameObject.AddComponent<GridLayoutGroup>(); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            scroll.content = content;
            backButton = UiFactory.Button(transform, english ? "Main menu" : "Menu principal", new Vector2(0.12f, 0.02f), new Vector2(0.88f, 0.09f));
            backButton.onClick.AddListener(() => back());
            Filter(-1);
        }
        public void Filter(int suit)
        {
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            visible.Clear();
            for (var i = 0; i < filters.Count; i++) filters[i].GetComponent<Image>().color = i == suit + 1 ? new Color(0.54f, 0.05f, 0.13f) : new Color(0.06f, 0.1f, 0.11f);
            foreach (var entry in deck.Cards.Where(c => suit < 0 || (int)c.Suit == suit))
            {
                var index = visible.Count; visible.Add(entry.Sprite);
                var thumb = new GameObject($"Gallery_{entry.Suit}_{entry.Rank}", typeof(RectTransform), typeof(Image), typeof(Button));
                thumb.transform.SetParent(content, false);
                var image = thumb.GetComponent<Image>(); image.sprite = entry.Sprite; image.preserveAspect = true;
                thumb.GetComponent<Button>().onClick.AddListener(() => OpenCard(index));
            }
            content.anchoredPosition = Vector2.zero; Layout();
        }
        public void OpenCard(int index) => CardDetails.Open(transform, visible[index], english, visible, index, reduceMotion);
        private void Update() { if (viewport.rect.size != lastSize || ((RectTransform)transform).rect.size != lastRootSize) Layout(); }
        private void Layout()
        {
            lastRootSize = ((RectTransform)transform).rect.size;
            var unit = UiFactory.Pixels(transform, 1);
            var height = Mathf.Max(1, lastRootSize.y);
            heading.rectTransform.anchorMin = new Vector2(0.04f, 1 - 52 * unit / height);
            foreach (var button in filters)
            {
                var r = (RectTransform)button.transform;
                r.anchorMin = new Vector2(r.anchorMin.x, 1 - 104 * unit / height);
                r.anchorMax = new Vector2(r.anchorMax.x, 1 - 60 * unit / height);
            }
            viewport.anchorMin = new Vector2(0.03f, 64 * unit / height);
            viewport.anchorMax = new Vector2(0.97f, 1 - 116 * unit / height);
            ((RectTransform)backButton.transform).anchorMin = new Vector2(0.12f, 8 * unit / height);
            ((RectTransform)backButton.transform).anchorMax = new Vector2(0.88f, 52 * unit / height);
            lastSize = viewport.rect.size;
            var scale = GetComponentInParent<Canvas>().scaleFactor / UiFactory.PixelDensity;
            grid.spacing = Vector2.one * UiFactory.Pixels(transform, 8);
            grid.constraintCount = Mathf.Max(2, Mathf.FloorToInt(lastSize.x * scale / 130f));
            var width = (lastSize.x - grid.spacing.x * (grid.constraintCount - 1)) / grid.constraintCount;
            grid.cellSize = new Vector2(width, width * 1.415f);
            content.sizeDelta = new Vector2(0, Mathf.CeilToInt(visible.Count / (float)grid.constraintCount) * (grid.cellSize.y + grid.spacing.y));
        }
    }
}
