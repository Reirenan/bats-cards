using System;
using System.Collections.Generic;
using System.Linq;
using BatsSolitaire.Core;
using BatsSolitaire.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BatsSolitaire.Presentation
{
    public sealed class GameScreen : MonoBehaviour
    {
        private GameSession session;
        private SaveService saves;
        private TextCatalog text;
        private Canvas canvas;
        private AudioService audio;
        private Action showMenu;
        private RectTransform board;
        private Text status;
        private Text hint;
        private DeckManifest manifest;
        private readonly List<GameObject> renderedCards = new();
        private Vector2[] tableauTargets = new Vector2[7];
        private Vector2[] tableauDropTargets = new Vector2[7];
        private Vector2[] foundationTargets = new Vector2[4];
        private Vector2 stockTarget;
        private Vector2 wasteTarget;
        private Vector2 cardSize;
        private Vector2 lastResolution;
        private PileRef? selectedSource;
        private int selectedCardIndex = -1;
        private GameObject pauseOverlay;
        private GameObject victoryOverlay;
        private GameObject detailsButton;
        private CardView selectedView;
        private readonly Dictionary<string, Vector2> previousPositions = new();
        private bool layoutOnly;
        private bool autoRunning;
        private GameObject hintTarget;

        public void Initialize(GameSession session, SaveService saves, TextCatalog text, Canvas canvas, AudioService audio, Action showMenu)
        {
            this.session = session;
            this.saves = saves;
            this.text = text;
            this.canvas = canvas;
            this.audio = audio;
            this.showMenu = showMenu;
            manifest = Resources.Load<DeckManifest>("DeckManifest");
            lastResolution = new Vector2(Screen.width, Screen.height);
            BuildChrome();
            Canvas.ForceUpdateCanvases();
            session.Changed += Render;
            Render();
        }

        private void OnDestroy()
        {
            if (session != null) session.Changed -= Render;
        }

        private void Update()
        {
            if (pauseOverlay == null && GetComponentInChildren<CardDetails>() == null) session.Tick(Time.unscaledDeltaTime);
            UpdateStatus();
            var resolution = new Vector2(Screen.width, Screen.height);
            if (resolution != lastResolution)
            {
                lastResolution = resolution;
                layoutOnly = true;
                foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                renderedCards.Clear();
                pauseOverlay = null;
                victoryOverlay = null;
                previousPositions.Clear();
                BuildChrome();
                Canvas.ForceUpdateCanvases();
                Render();
                layoutOnly = false;
            }
        }

        private void BuildChrome()
        {
            var background = UiFactory.Panel(transform, Color.white, Vector2.zero, Vector2.one);
            board = background.GetComponent<RectTransform>();
            UiFactory.Background(background.transform, Resources.Load<Sprite>("TableBackground"));
            UiFactory.Panel(background.transform,
                saves.Data.Settings.HighContrast ? new Color(0.01f, 0.025f, 0.03f, 0.82f) : new Color(0.01f, 0.045f, 0.05f, 0.62f),
                Vector2.zero, Vector2.one).GetComponent<Image>().raycastTarget = false;
            var bar = UiFactory.Panel(background.transform, new Color(0.018f, 0.035f, 0.038f, 0.94f), new Vector2(0, 0.91f), Vector2.one);
            var brand = UiFactory.Label(bar.transform, "BATS\nCARDS", 27, TextAnchor.MiddleCenter,
                new Vector2(0.008f, 0.06f), new Vector2(0.105f, 0.94f), new Color(0.96f, 0.92f, 0.84f));
            brand.fontStyle = FontStyle.Bold;
            status = UiFactory.Label(bar.transform, string.Empty, 23, TextAnchor.MiddleCenter,
                new Vector2(0.11f, 0), new Vector2(0.50f, 1), new Color(0.96f, 0.92f, 0.84f));
            var auto = AddTopButton(bar.transform, text.Get("auto_move"), 0.57f, 0.68f, StartAutomaticMove);
            UiFactory.SetAccent(auto);
            AddTopButton(bar.transform, text.Get("undo"), 0.69f, 0.80f, () => session.Undo());
            AddTopButton(bar.transform, text.Get("hint"), 0.81f, 0.91f, ShowHint);
            AddTopButton(bar.transform, "Menu", 0.93f, 0.985f, ShowPauseMenu);
            hint = UiFactory.Label(background.transform, string.Empty, 26, TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.012f), new Vector2(0.8f, 0.064f), new Color(1f, 0.84f, 0.62f));
            var inspect = UiFactory.Button(transform, text.Language.StartsWith("en") ? "View original card" : "Ver carta original",
                new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.07f));
            detailsButton = inspect.gameObject;
            inspect.onClick.AddListener(() => { if (selectedView != null) CardDetails.Open(transform, selectedView.OriginalSprite, text.Language.StartsWith("en"), reduced: saves.Data.Settings.ReduceMotion); });
            detailsButton.SetActive(false);
            if (UiFactory.ScreenWidth < 600)
            {
                bar.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1 - Mathf.Max(0.16f, 104f / UiFactory.ScreenHeight));
                brand.rectTransform.anchorMin = new Vector2(0.02f, 0.53f);
                brand.rectTransform.anchorMax = new Vector2(0.22f, 0.98f);
                status.rectTransform.anchorMin = new Vector2(0.24f, 0.53f);
                status.rectTransform.anchorMax = new Vector2(0.98f, 0.98f);
                var buttons = bar.GetComponentsInChildren<Button>();
                for (var i = 0; i < buttons.Length; i++)
                {
                    var r = buttons[i].GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.02f + i * 0.245f, 0.02f);
                    r.anchorMax = new Vector2(0.245f + i * 0.245f, 0.48f);
                }
            }
            else
            {
                var header = Mathf.Max(0.09f, 64f / UiFactory.ScreenHeight);
                bar.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1f - header);
                brand.rectTransform.anchorMax = new Vector2(0.12f, 0.94f);
                status.rectTransform.anchorMin = new Vector2(0.13f, 0);
                status.rectTransform.anchorMax = new Vector2(0.43f, 1);
                var buttons = bar.GetComponentsInChildren<Button>();
                for (var i = 0; i < buttons.Length; i++)
                {
                    var r = (RectTransform)buttons[i].transform;
                    r.anchorMin = new Vector2(0.45f + i * 0.137f, 0.08f);
                    r.anchorMax = new Vector2(0.58f + i * 0.137f, 0.92f);
                }
            }
            // Compact controls use clear text; larger controls have the same vector icon set as the menu.
            if (UiFactory.ScreenWidth >= 1000)
            {
                var buttons = bar.GetComponentsInChildren<Button>();
                var kinds = new[] { "play", "undo", "hint", "menu" };
                for (var i = 0; i < buttons.Length; i++) UiFactory.Icon(buttons[i], kinds[i]);
            }
        }

        private Button AddTopButton(Transform parent, string label, float min, float max, UnityEngine.Events.UnityAction action)
        {
            var button = UiFactory.Button(parent, label, new Vector2(min, 0.16f), new Vector2(max, 0.84f));
            button.onClick.AddListener(action);
            return button;
        }

        private void Render()
        {
            if (session.State == null || board == null) return;
            previousPositions.Clear();
            foreach (var card in renderedCards)
            {
                if (card == null) continue;
                var view = card.GetComponent<CardView>();
                if (view != null) previousPositions[view.State.Card.Key] = card.GetComponent<RectTransform>().anchoredPosition;
                card.SetActive(false);
                Destroy(card);
            }
            renderedCards.Clear();
            if (hintTarget != null) Destroy(hintTarget);
            hintTarget = null;
            selectedSource = null;
            selectedView = null;
            if (detailsButton != null) detailsButton.SetActive(false);
            CalculateLayout();
            RenderFoundations();
            RenderTableau();
            RenderStockAndWaste();
            UpdateStatus();
            if (session.State.Won) ShowVictory();
        }

        private void CalculateLayout()
        {
            var width = board.rect.width > 0 ? board.rect.width : 1920;
            var height = board.rect.height > 0 ? board.rect.height : 1080;
            var availableWidth = width * 0.98f;
            var cardWidth = Mathf.Min(availableWidth / 7.2f, height * 0.16f);
            var aspect = manifest != null && manifest.Back != null
                ? manifest.Back.rect.height / manifest.Back.rect.width : 1.416f;
            cardSize = new Vector2(cardWidth, cardWidth * aspect);
            var left = -availableWidth * 0.5f + cardWidth * 0.5f;
            var gap = (availableWidth - cardWidth) / 6f;
            var headerFraction = UiFactory.ScreenWidth < 600 ? Mathf.Max(0.16f, 104f / UiFactory.ScreenHeight) : Mathf.Max(0.09f, 64f / UiFactory.ScreenHeight);
            var top = height * (0.48f - headerFraction) - UiFactory.Pixels(transform, 18);
            var upperY = top - cardSize.y * 0.5f;
            stockTarget = new Vector2(left, upperY);
            wasteTarget = new Vector2(left + gap, upperY);
            for (var i = 0; i < 4; i++) foundationTargets[i] = new Vector2(left + gap * (i + 3), upperY);
            if (saves.Data.Settings.LeftHanded)
            {
                stockTarget = new Vector2(left + gap * 6, upperY);
                wasteTarget = new Vector2(left + gap * 5, upperY);
                for (var i = 0; i < 4; i++) foundationTargets[i] = new Vector2(left + gap * i, upperY);
            }
            for (var i = 0; i < 7; i++) tableauTargets[i] = new Vector2(left + gap * i, upperY - cardSize.y - Mathf.Max(height * 0.025f, UiFactory.Pixels(transform, 26)));
        }

        private void RenderStockAndWaste()
        {
            CreatePilePlaceholder(stockTarget, session.State.Stock.Count > 0 ? string.Empty : "↻", TryDraw);
            if (session.State.Stock.Count > 0)
                CreateCard(session.State.Stock[^1], new PileRef(PileKind.Stock), session.State.Stock.Count - 1, stockTarget, true);
            if (session.State.Waste.Count > 0)
                CreateCard(session.State.Waste[^1], new PileRef(PileKind.Waste), session.State.Waste.Count - 1, wasteTarget, true);
            else
                CreatePilePlaceholder(wasteTarget, string.Empty, null);
        }

        private void RenderFoundations()
        {
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var pile = session.State.Foundations[i].Cards;
                if (pile.Count == 0)
                    CreatePilePlaceholder(foundationTargets[i], new CardId((CardSuit)i, CardRank.Ace).SuitSymbol,
                        () => TrySelectedDestination(new PileRef(PileKind.Foundation, index)));
                else
                    CreateCard(pile[^1], new PileRef(PileKind.Foundation, i), pile.Count - 1, foundationTargets[i], true);
            }
        }

        private void RenderTableau()
        {
            var faceDownOffset = cardSize.y * 0.16f;
            var faceUpOffset = Mathf.Max(cardSize.y * 0.29f, UiFactory.Pixels(transform, 20));
            for (var column = 0; column < 7; column++)
            {
                var pile = session.State.Tableau[column].Cards;
                var idealSpread = 0f;
                for (var j = 0; j < pile.Count - 1; j++) idealSpread += pile[j].FaceUp ? faceUpOffset : faceDownOffset;
                var availableSpread = tableauTargets[column].y + board.rect.height * 0.40f - cardSize.y * 0.5f;
                var compression = idealSpread > 0 ? Mathf.Clamp01(availableSpread / idealSpread) : 1f;
                var upCount = 0;
                for (var j = 0; j < pile.Count - 1; j++) if (pile[j].FaceUp) upCount++;
                var downCount = Mathf.Max(0, pile.Count - 1 - upCount);
                var upStep = faceUpOffset * compression;
                var downStep = faceDownOffset * compression;
                var readableStep = UiFactory.Pixels(transform, 20);
                var hiddenStep = UiFactory.Pixels(transform, 2);
                if (availableSpread >= upCount * readableStep + downCount * hiddenStep)
                {
                    upStep = upCount > 0 ? Mathf.Clamp((availableSpread - downCount * hiddenStep) / upCount, readableStep, faceUpOffset) : faceUpOffset;
                    downStep = downCount > 0 ? Mathf.Min(faceDownOffset, (availableSpread - upCount * upStep) / downCount) : faceDownOffset;
                }
                if (pile.Count == 0)
                {
                    var targetColumn = column;
                    CreatePilePlaceholder(tableauTargets[column], "K",
                        () => TrySelectedDestination(new PileRef(PileKind.Tableau, targetColumn)));
                    tableauDropTargets[column] = tableauTargets[column];
                }
                var y = tableauTargets[column].y;
                for (var index = 0; index < pile.Count; index++)
                {
                    var position = new Vector2(tableauTargets[column].x, y);
                    CreateCard(pile[index], new PileRef(PileKind.Tableau, column), index, position, pile[index].FaceUp);
                    tableauDropTargets[column] = position;
                    y -= pile[index].FaceUp ? upStep : downStep;
                }
            }
        }

        private void CreateCard(CardState state, PileRef source, int index, Vector2 position, bool interactive)
        {
            var cardObject = new GameObject($"Card_{state.Card.Key}");
            cardObject.transform.SetParent(board, false);
            var view = cardObject.AddComponent<CardView>();
            view.Initialize(state, source, index, cardSize, manifest, canvas);
            view.ReduceMotion = saves.Data.Settings.ReduceMotion;
            view.SetAnchoredPosition(position);
            if (!layoutOnly && previousPositions.TryGetValue(state.Card.Key, out var from)) view.AnimateFrom(from, position, 0);
            else if (!layoutOnly && previousPositions.Count == 0 && source.Kind == PileKind.Tableau)
                view.AnimateFrom(stockTarget, position, source.Index * 0.025f + index * 0.015f);
            if (interactive)
            {
                view.Clicked += OnCardClicked;
                view.DragEnded += OnCardDragEnded;
            }
            renderedCards.Add(cardObject);
        }

        private void CreatePilePlaceholder(Vector2 position, string label, UnityEngine.Events.UnityAction action)
        {
            var gameObject = new GameObject("Pile", typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(board, false);
            var rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = cardSize;
            rect.anchoredPosition = position;
            var image = gameObject.GetComponent<Image>();
            image.color = new Color(0.027f, 0.11f, 0.125f, 0.96f);
            UiFactory.Border(gameObject.transform, new Color(1f, 0.84f, 0.82f, 0.38f));
            if (!string.IsNullOrEmpty(label))
                UiFactory.Label(gameObject.transform, label, 36, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Color(1f, 0.9f, 0.86f, 0.65f));
            if (action != null)
            {
                var button = gameObject.AddComponent<Button>();
                button.onClick.AddListener(action);
            }
            gameObject.name = "Pile_" + label;
            renderedCards.Add(gameObject);
        }

        private void OnCardClicked(CardView card, bool doubleClick)
        {
            if (autoRunning) return;
            CancelInvoke(nameof(ClearHint));
            ClearHint();
            if (card.Source.Kind == PileKind.Stock)
            {
                TryDraw();
                return;
            }
            if (doubleClick)
            {
                ClearSelection();
                var move = session.Rules.FindAutoFoundationMove(session.State, card.Source, card.CardIndex);
                if (move != null) TryMove(move);
                return;
            }

            if (selectedSource.HasValue &&
                (card.Source.Kind == PileKind.Tableau || card.Source.Kind == PileKind.Foundation))
            {
                var move = new Move(selectedSource.Value, new PileRef(card.Source.Kind, card.Source.Index), selectedCardIndex);
                if (session.Rules.CanMove(session.State, move)) { ClearSelection(); TryMove(move); return; }
            }

            selectedSource = card.Source;
            selectedCardIndex = card.CardIndex;
            selectedView = card;
            detailsButton.SetActive(card.OriginalSprite != null);
            HighlightValidDestinations(card);
        }

        private void OnCardDragEnded(CardView card, Vector2 screenPosition)
        {
            if (autoRunning) return;
            CancelInvoke(nameof(ClearHint));
            ClearHint();
            ClearSelection();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPosition, null, out var local)) return;
            var destination = ClosestDestination(local, card);
            if (destination.HasValue)
                TryMove(new Move(card.Source, destination.Value, card.CardIndex));
        }

        private PileRef? ClosestDestination(Vector2 point, CardView card)
        {
            PileRef? closest = null;
            var closestDistance = float.MaxValue;
            for (var i = 0; i < 4; i++)
            {
                var target = new PileRef(PileKind.Foundation, i);
                var distance = Vector2.Distance(point, foundationTargets[i]);
                if (Mathf.Abs(point.x - foundationTargets[i].x) <= cardSize.x * 0.72f &&
                    Mathf.Abs(point.y - foundationTargets[i].y) <= cardSize.y * 0.65f &&
                    distance < closestDistance && session.Rules.CanMove(session.State, new Move(card.Source, target, card.CardIndex)))
                {
                    closest = target;
                    closestDistance = distance;
                }
            }
            for (var i = 0; i < 7; i++)
            {
                var target = new PileRef(PileKind.Tableau, i);
                var top = tableauTargets[i].y + cardSize.y * 0.55f;
                var bottom = tableauDropTargets[i].y - cardSize.y * 0.65f;
                var nearestY = Mathf.Clamp(point.y, bottom, top);
                var distance = Vector2.Distance(point, new Vector2(tableauTargets[i].x, nearestY));
                if (Mathf.Abs(point.x - tableauTargets[i].x) <= cardSize.x * 0.72f &&
                    point.y <= top && point.y >= bottom && distance < closestDistance &&
                    session.Rules.CanMove(session.State, new Move(card.Source, target, card.CardIndex)))
                {
                    closest = target;
                    closestDistance = distance;
                }
            }
            return closest;
        }

        private void ShowHint()
        {
            CancelInvoke(nameof(ClearHint));
            ClearHint();
            var move = session.Rules.FindHint(session.State);
            ClearSelection();
            if (move != null) HighlightMove(move);
            hint.text = move == null ? text.Get("no_hint") : DescribeMove(move);
            CancelInvoke(nameof(ClearHint));
            Invoke(nameof(ClearHint), 3f);
        }

        private string DescribeMove(Move move)
        {
            if (move.From.Kind == PileKind.Stock) return text.Language.StartsWith("en") ? "Draw a card" : "Compre uma carta";
            return text.Language.StartsWith("en") ? "A valid move is highlighted by its source pile" : "Há uma jogada válida na pilha de origem";
        }

        private void HighlightMove(Move move)
        {
            foreach (var view in renderedCards.Select(o => o.GetComponent<CardView>()).Where(v => v != null))
            {
                var samePile = view.Source.Kind == move.From.Kind && view.Source.Index == move.From.Index;
                var inSequence = move.From.Kind == PileKind.Tableau && move.CardIndex >= 0
                    ? view.CardIndex >= move.CardIndex : move.CardIndex < 0 || view.CardIndex == move.CardIndex;
                if (samePile && inSequence) view.SetHighlighted(true);
            }

            var position = move.From.Kind == PileKind.Stock ? wasteTarget :
                move.To.Kind == PileKind.Foundation ? foundationTargets[move.To.Index] : tableauDropTargets[move.To.Index];
            hintTarget = UiFactory.Panel(board, Color.clear, Vector2.zero, Vector2.zero);
            hintTarget.name = "HintDestination";
            var rect = hintTarget.GetComponent<RectTransform>();
            rect.sizeDelta = cardSize * 1.06f;
            rect.anchoredPosition = position;
            hintTarget.GetComponent<Image>().raycastTarget = false;
            UiFactory.Border(hintTarget.transform, new Color(0.35f, 1f, 0.62f, 0.95f), UiFactory.Pixels(transform, 3));
            hintTarget.transform.SetAsLastSibling();
        }

        private void ClearHint()
        {
            if (hint != null) hint.text = string.Empty;
            if (hintTarget != null) Destroy(hintTarget);
            hintTarget = null;
            foreach (var rendered in renderedCards)
            {
                var view = rendered != null ? rendered.GetComponent<CardView>() : null;
                if (view != null) view.SetHighlighted(false);
            }
        }

        private void TrySelectedDestination(PileRef destination)
        {
            if (!selectedSource.HasValue) return;
            var move = new Move(selectedSource.Value, destination, selectedCardIndex);
            ClearSelection();
            TryMove(move);
        }

        private void HighlightValidDestinations(CardView selected)
        {
            foreach (var rendered in renderedCards)
            {
                var view = rendered != null ? rendered.GetComponent<CardView>() : null;
                if (view == null) continue;
                var isSelected = view == selected;
                var isTop = view.Source.Kind switch
                {
                    PileKind.Tableau => view.CardIndex == session.State.Tableau[view.Source.Index].Cards.Count - 1,
                    PileKind.Foundation => view.CardIndex == session.State.Foundations[view.Source.Index].Cards.Count - 1,
                    _ => false
                };
                var validTarget = isTop &&
                    (view.Source.Kind == PileKind.Tableau || view.Source.Kind == PileKind.Foundation) &&
                    session.Rules.CanMove(session.State,
                        new Move(selectedSource.Value, new PileRef(view.Source.Kind, view.Source.Index), selectedCardIndex));
                view.SetHighlighted(isSelected || validTarget);
            }
        }

        private void ClearSelection()
        {
            selectedView = null;
            if (detailsButton != null) detailsButton.SetActive(false);
            selectedSource = null;
            selectedCardIndex = -1;
            foreach (var rendered in renderedCards)
            {
                var view = rendered != null ? rendered.GetComponent<CardView>() : null;
                if (view != null) view.SetHighlighted(false);
            }
        }

        private void StartAutomaticMove()
        {
            if (autoRunning) return;
            var move = session.Rules.FindBestMove(session.State);
            ClearSelection();
            ClearHint();
            if (move == null)
            {
                hint.text = text.Get("no_auto_move");
                audio.PlayInvalid();
                Invoke(nameof(ClearHint), 3f);
                return;
            }
            StartCoroutine(AutomaticMove(move));
        }

        private System.Collections.IEnumerator AutomaticMove(Move move)
        {
            autoRunning = true;
            HighlightMove(move);
            hint.text = DescribeMove(move);
            if (!saves.Data.Settings.ReduceMotion) yield return new WaitForSecondsRealtime(0.24f);
            ClearHint();
            var moved = move.From.Kind == PileKind.Stock ? session.Draw() : session.Move(move);
            if (moved) audio.PlayMove(); else audio.PlayInvalid();
            autoRunning = false;
        }

        private void StartAutoComplete()
        {
            if (autoRunning) return;
            if (!session.Rules.IsSafeForAutoComplete(session.State))
            {
                hint.text = text.Language.StartsWith("en") ? "Reveal all hidden cards first" : "Revele todas as cartas ocultas primeiro";
                Invoke(nameof(ClearHint), 3f);
                return;
            }
            StartCoroutine(AutoComplete());
        }

        private System.Collections.IEnumerator AutoComplete()
        {
            autoRunning = true;
            try
            {
            while (!session.State.Won)
            {
                Move move = null;
                if (session.State.Waste.Count > 0)
                    move = session.Rules.FindAutoFoundationMove(session.State, new PileRef(PileKind.Waste));
                if (move == null)
                {
                    for (var column = 0; column < 7 && move == null; column++)
                    {
                        var pile = session.State.Tableau[column].Cards;
                        if (pile.Count > 0)
                            move = session.Rules.FindAutoFoundationMove(session.State,
                                new PileRef(PileKind.Tableau, column), pile.Count - 1);
                    }
                }
                if (move == null || !session.Move(move)) yield break;
                audio.PlayMove();
                yield return new WaitForSecondsRealtime(saves.Data.Settings.ReduceMotion ? 0.02f : 0.12f);
            }
            }
            finally { autoRunning = false; }
        }

        private void UpdateStatus()
        {
            if (status == null || session.State == null) return;
            var time = TimeSpan.FromSeconds(session.State.ElapsedSeconds);
            status.text = $"{text.Get("score")}: {session.State.Score}   {text.Get("moves")}: {session.State.Moves}\n{text.Get("time")}: {time.Minutes:00}:{time.Seconds:00}";
        }

        private void ShowVictory()
        {
            if (victoryOverlay != null) return;
            audio.PlayWin();
            var overlay = UiFactory.Panel(transform, new Color(0.01f, 0.045f, 0.05f, 0.96f), new Vector2(0.2f, 0.28f), new Vector2(0.8f, 0.72f));
            victoryOverlay = overlay;
            UiFactory.Border(overlay.transform, new Color(0.85f, 0.70f, 0.48f, 0.9f), 3f);
            var victory = UiFactory.Label(overlay.transform, text.Get("win"), 64, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.9f), new Color(0.85f, 0.70f, 0.48f));
            victory.fontStyle = FontStyle.Bold;
            var menu = UiFactory.Button(overlay.transform, text.Get("menu"), new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.36f));
            UiFactory.SetAccent(menu);
            menu.onClick.AddListener(showMenu.Invoke);
        }

        private void ShowPauseMenu()
        {
            if (pauseOverlay != null) return;
            CancelInvoke(nameof(ClearHint));
            ClearHint();
            StopAllCoroutines();
            autoRunning = false;
            var blocker = UiFactory.Panel(transform, new Color(0, 0, 0, 0.6f), Vector2.zero, Vector2.one);
            blocker.name = "PauseBlocker";
            pauseOverlay = UiFactory.Panel(transform, new Color(0.005f, 0.02f, 0.025f, 0.96f),
                new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.76f));
            UiFactory.Border(pauseOverlay.transform, new Color(0.85f, 0.70f, 0.48f, 0.9f));
            UiFactory.Label(pauseOverlay.transform, "BATS CARDS", 34, TextAnchor.MiddleCenter,
                new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.94f), new Color(0.96f, 0.92f, 0.84f));
            var resume = UiFactory.Button(pauseOverlay.transform, text.Get("resume"), new Vector2(0.14f, 0.66f), new Vector2(0.86f, 0.78f));
            UiFactory.SetAccent(resume);
            resume.onClick.AddListener(() => { Destroy(blocker); Destroy(pauseOverlay); pauseOverlay = null; });
            var restart = UiFactory.Button(pauseOverlay.transform, text.Get("restart"), new Vector2(0.14f, 0.49f), new Vector2(0.86f, 0.61f));
            restart.onClick.AddListener(() => { session.Restart(); Destroy(blocker); Destroy(pauseOverlay); pauseOverlay = null; });
            var finish = UiFactory.Button(pauseOverlay.transform, text.Get("auto_finish"), new Vector2(0.14f, 0.32f), new Vector2(0.86f, 0.44f));
            finish.interactable = session.Rules.IsSafeForAutoComplete(session.State);
            finish.onClick.AddListener(() =>
            {
                Destroy(blocker); Destroy(pauseOverlay); pauseOverlay = null;
                StartAutoComplete();
            });
            var mainMenu = UiFactory.Button(pauseOverlay.transform, text.Get("menu"), new Vector2(0.14f, 0.15f), new Vector2(0.86f, 0.27f));
            mainMenu.onClick.AddListener(showMenu.Invoke);
        }

        private void TryDraw()
        {
            if (session.Draw()) audio.PlayMove();
            else audio.PlayInvalid();
        }

        private void TryMove(Move move)
        {
            if (session.Move(move)) audio.PlayMove();
            else audio.PlayInvalid();
        }
    }
}
