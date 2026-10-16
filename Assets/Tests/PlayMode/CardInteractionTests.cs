using BatsSolitaire.Core;
using BatsSolitaire.Presentation;
using BatsSolitaire.Services;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BatsSolitaire.Tests.PlayMode
{
    public sealed class CardInteractionTests
    {
        private GameObject root;
        private CardView view;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TestCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var card = new GameObject("Card");
            card.transform.SetParent(root.transform, false);
            view = card.AddComponent<CardView>();
            view.Initialize(new CardState(new CardId(CardSuit.Hearts, CardRank.Ace), true),
                new PileRef(PileKind.Tableau, 0), 0, new Vector2(120, 170), null, canvas);
            view.SetAnchoredPosition(Vector2.zero);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void DoubleClick_RequestsAutomaticMove()
        {
            var wasDoubleClick = false;
            view.Clicked += (_, doubleClick) => wasDoubleClick = doubleClick;
            view.OnPointerClick(new PointerEventData(null) { clickCount = 2 });
            Assert.That(wasDoubleClick, Is.True);
        }

        [Test]
        public void DragEnd_ReportsDropPosition()
        {
            var reported = Vector2.zero;
            view.DragEnded += (_, position) => reported = position;
            view.OnBeginDrag(new PointerEventData(null));
            view.OnEndDrag(new PointerEventData(null) { position = new Vector2(320, 240) });
            Assert.That(reported, Is.EqualTo(new Vector2(320, 240)));
        }

        [Test]
        public void Selection_DoesNotTintOriginalArtwork()
        {
            var color = view.GetComponent<Image>().color;
            view.SetHighlighted(true);
            Assert.That(view.GetComponent<Image>().color, Is.EqualTo(color));
            Assert.That(view.GetComponent<Image>().preserveAspect, Is.True);
        }

        [Test]
        public void DragSequence_MovesAllCardsAndRestoresInvalidDrop()
        {
            var other = new GameObject("Next");
            other.transform.SetParent(root.transform, false);
            var next = other.AddComponent<CardView>();
            next.Initialize(new CardState(new CardId(CardSuit.Spades, CardRank.Two), true),
                new PileRef(PileKind.Tableau, 0), 1, new Vector2(120, 170), null, root.GetComponent<Canvas>());
            next.SetAnchoredPosition(new Vector2(0, -30));
            view.ReduceMotion = next.ReduceMotion = true;
            view.OnBeginDrag(new PointerEventData(null));
            view.OnDrag(new PointerEventData(null) { delta = new Vector2(40, 25) });
            Assert.That(next.GetComponent<RectTransform>().anchoredPosition - view.GetComponent<RectTransform>().anchoredPosition,
                Is.EqualTo(new Vector2(0, -30)));
            view.OnEndDrag(new PointerEventData(null));
            Assert.That(view.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(next.GetComponent<RectTransform>().anchoredPosition, Is.EqualTo(new Vector2(0, -30)));
        }

        [UnityTest]
        public IEnumerator Gallery_FiltersAllOriginalsAndNavigatesWithoutChangingSave()
        {
            var path = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid() + ".json");
            var saves = new SaveService(path);
            var session = new GameSession(saves);
            session.NewGame(GameMode.Random, 718);
            var before = File.ReadAllText(path);
            var stateBefore = JsonUtility.ToJson(session.State);
            var deck = Resources.Load<DeckManifest>("DeckManifest");
            var galleryObject = new GameObject("Gallery", typeof(RectTransform));
            galleryObject.transform.SetParent(root.transform, false);
            var rect = galleryObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var gallery = galleryObject.AddComponent<CardGallery>();
            var returned = false;
            gallery.Initialize(deck, false, true, () => returned = true);
            yield return null;
            Assert.That(gallery.CardCount, Is.EqualTo(52));
            gallery.OpenCard(0);
            yield return null;
            var detail = gallery.GetComponentInChildren<CardDetails>();
            for (var i = 0; i < 52; i++)
            {
                Assert.That(detail.CurrentIndex, Is.EqualTo(i));
                Assert.That(detail.CurrentSprite, Is.SameAs(deck.Cards[i].Sprite));
                detail.Navigate(1);
            }
            Assert.That(detail.CurrentIndex, Is.Zero);
            detail.Navigate(-1); Assert.That(detail.CurrentIndex, Is.EqualTo(51));
            var swipe = detail.GetComponentInChildren<CardSwipe>();
            swipe.OnBeginDrag(new PointerEventData(null) { position = new Vector2(150, 100) });
            swipe.OnEndDrag(new PointerEventData(null) { position = new Vector2(20, 100) });
            Assert.That(detail.CurrentIndex, Is.Zero);
            Object.DestroyImmediate(detail.gameObject);
            for (var suit = 0; suit < 4; suit++) { gallery.Filter(suit); Assert.That(gallery.CardCount, Is.EqualTo(13)); yield return null; }
            gallery.GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>()?.text == "Menu principal").onClick.Invoke();
            Assert.That(returned, Is.True);
            Assert.That(JsonUtility.ToJson(session.State), Is.EqualTo(stateBefore));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            foreach (var file in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file);
        }

        [UnityTest]
        public IEnumerator LongColumn_FitsBoardAndInspectorUsesSameOriginal()
        {
            Object.DestroyImmediate(view.gameObject);
            var path = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid() + ".json");
            var saves = new SaveService(path);
            var session = new GameSession(saves);
            session.NewGame(GameMode.Random, 123);
            var cards = session.State.AllCards().ToList();
            session.State.Stock.Clear();
            session.State.Waste.Clear();
            foreach (var pile in session.State.Tableau) pile.Cards.Clear();
            session.State.Tableau[0].Cards.AddRange(cards.Take(19));
            foreach (var card in session.State.Tableau[0].Cards) card.FaceUp = true;
            session.State.Stock.AddRange(cards.Skip(19));
            var screenObject = new GameObject("Screen", typeof(RectTransform));
            screenObject.transform.SetParent(root.transform, false);
            var screenRect = screenObject.GetComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = screenRect.offsetMax = Vector2.zero;
            var screen = screenObject.AddComponent<GameScreen>();
            saves.Data.Settings.ReduceMotion = true;
            screen.Initialize(session, saves, new TextCatalog(), root.GetComponent<Canvas>(), null, () => { });
            yield return null;
            Canvas.ForceUpdateCanvases();
            var views = screenObject.GetComponentsInChildren<CardView>();
            var board = views[0].transform.parent.GetComponent<RectTransform>();
            foreach (var card in views)
            {
                var rect = card.GetComponent<RectTransform>();
                Assert.That(rect.anchoredPosition.y - rect.rect.height / 2, Is.GreaterThanOrEqualTo(-board.rect.height / 2));
            }
            var face = views.First(v => v.State.FaceUp);
            CardDetails.Open(screen.transform, face.OriginalSprite, false);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var detail = screen.GetComponentInChildren<CardDetails>();
            var original = detail.GetComponentsInChildren<Image>().First(i => i.name == "Original");
            Assert.That(original.sprite, Is.SameAs(face.OriginalSprite));
            Assert.That(original.preserveAspect, Is.True);
            var viewport = original.rectTransform.parent.GetComponent<RectTransform>();
            Assert.That(original.rectTransform.rect.width, Is.LessThanOrEqualTo(viewport.rect.width + 1));
            Assert.That(original.rectTransform.rect.height, Is.LessThanOrEqualTo(viewport.rect.height + 1));
            foreach (var file in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file);
        }
    }
}
