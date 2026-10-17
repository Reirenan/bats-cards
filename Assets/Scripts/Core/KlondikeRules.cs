using System;
using System.Collections.Generic;
using System.Linq;

namespace BatsSolitaire.Core
{
    public sealed class KlondikeRules
    {
        public GameState CreateGame(long seed, GameMode mode)
        {
            var deck = CreateDeck();
            Shuffle(deck, unchecked((ulong)seed));
            var state = new GameState { Seed = seed, Mode = mode };

            for (var column = 0; column < 7; column++)
            {
                for (var row = 0; row <= column; row++)
                {
                    var card = deck[^1];
                    deck.RemoveAt(deck.Count - 1);
                    state.Tableau[column].Cards.Add(new CardState(card, row == column));
                }
            }

            foreach (var card in deck) state.Stock.Add(new CardState(card, false));
            return state;
        }

        public GameState CreateGuaranteedGame(long seed, GameMode mode)
        {
            var state = new GameState { Seed = seed, Mode = mode };
            var random = new Pcg32(unchecked((ulong)seed));
            var suits = new List<CardSuit>
            {
                CardSuit.Clubs, CardSuit.Diamonds, CardSuit.Hearts, CardSuit.Spades
            };
            for (var i = suits.Count - 1; i > 0; i--)
            {
                var j = random.Range(i + 1);
                (suits[i], suits[j]) = (suits[j], suits[i]);
            }

            var columnSegments = new[]
            {
                (suit: suits[1], first: 13, last: 13),
                (suit: suits[2], first: 12, last: 13),
                (suit: suits[3], first: 11, last: 13),
                (suit: suits[3], first: 7, last: 10),
                (suit: suits[2], first: 7, last: 11),
                (suit: suits[1], first: 7, last: 12),
                (suit: suits[0], first: 7, last: 13)
            };

            for (var column = 0; column < columnSegments.Length; column++)
            {
                var segment = columnSegments[column];
                for (var rank = segment.last; rank >= segment.first; rank--)
                    state.Tableau[column].Cards.Add(new CardState(new CardId(segment.suit, (CardRank)rank), rank == segment.first));
            }

            var drawOrder = new List<CardId>(24);
            for (var rank = 1; rank <= 6; rank++)
            {
                var rankSuits = new List<CardSuit>(suits);
                for (var i = rankSuits.Count - 1; i > 0; i--)
                {
                    var j = random.Range(i + 1);
                    (rankSuits[i], rankSuits[j]) = (rankSuits[j], rankSuits[i]);
                }
                foreach (var suit in rankSuits) drawOrder.Add(new CardId(suit, (CardRank)rank));
            }
            for (var i = drawOrder.Count - 1; i >= 0; i--)
                state.Stock.Add(new CardState(drawOrder[i], false));
            return state;
        }

        public bool DrawOrRecycle(GameState state)
        {
            if (state.Stock.Count > 0)
            {
                var card = state.Stock[^1];
                state.Stock.RemoveAt(state.Stock.Count - 1);
                card.FaceUp = true;
                state.Waste.Add(card);
                state.Moves++;
                return true;
            }

            if (state.Waste.Count == 0) return false;
            for (var i = state.Waste.Count - 1; i >= 0; i--)
            {
                var card = state.Waste[i];
                card.FaceUp = false;
                state.Stock.Add(card);
            }
            state.Waste.Clear();
            state.StockPasses++;
            state.Score = Math.Max(0, state.Score - 100);
            state.Moves++;
            return true;
        }

        public bool CanMove(GameState state, Move move)
        {
            if (state == null || move == null || state.Won) return false;
            if (move.From.Kind == move.To.Kind && move.From.Index == move.To.Index) return false;
            if (move.From.Kind == PileKind.Tableau && (move.From.Index < 0 || move.From.Index >= state.Tableau.Count)) return false;
            if (move.From.Kind == PileKind.Foundation && (move.From.Index < 0 || move.From.Index >= state.Foundations.Count)) return false;
            var moving = GetMovingCards(state, move);
            if (moving.Count == 0 || moving.Any(card => !card.FaceUp)) return false;

            return move.To.Kind switch
            {
                PileKind.Tableau => CanPlaceOnTableau(state, move.To.Index, moving),
                PileKind.Foundation => moving.Count == 1 && CanPlaceOnFoundation(state, move.To.Index, moving[0]),
                _ => false
            };
        }

        public bool ApplyMove(GameState state, Move move)
        {
            if (!CanMove(state, move)) return false;
            var moving = GetMovingCards(state, move);
            RemoveMovingCards(state, move, moving.Count);

            if (move.To.Kind == PileKind.Tableau)
                state.Tableau[move.To.Index].Cards.AddRange(moving);
            else
                state.Foundations[move.To.Index].Cards.AddRange(moving);

            ApplyMoveScore(state, move);
            state.Moves++;
            RevealExposedCard(state, move.From);
            state.Won = state.Foundations.Sum(pile => pile.Cards.Count) == 52;
            return true;
        }

        public Move FindAutoFoundationMove(GameState state, PileRef source, int cardIndex = -1)
        {
            var probe = new Move(source, default, cardIndex);
            var cards = GetMovingCards(state, probe);
            if (cards.Count != 1) return null;
            var foundation = (int)cards[0].Card.Suit;
            var move = new Move(source, new PileRef(PileKind.Foundation, foundation), cardIndex);
            return CanMove(state, move) ? move : null;
        }

        public Move FindHint(GameState state)
        {
            return FindBestMove(state);
        }

        public Move FindBestMove(GameState state)
        {
            if (state == null || state.Won) return null;
            Move best = null;
            var bestScore = int.MinValue;

            void Consider(Move move, int score)
            {
                if (score <= bestScore || !CanMove(state, move)) return;
                best = move;
                bestScore = score;
            }

            for (var column = 0; column < 7; column++)
            {
                var pile = state.Tableau[column].Cards;
                for (var index = 0; index < pile.Count; index++)
                {
                    if (!pile[index].FaceUp) continue;
                    var source = new PileRef(PileKind.Tableau, column);
                    var foundationMove = FindAutoFoundationMove(state, source, index);
                    if (foundationMove != null)
                        Consider(foundationMove, IsSafeFoundationMove(state, pile[index].Card) ? 800 : 300);
                    for (var target = 0; target < 7; target++)
                    {
                        if (target == column) continue;
                        var move = new Move(source, new PileRef(PileKind.Tableau, target), index);
                        var revealsCard = index > 0 && !pile[index - 1].FaceUp;
                        var emptiesColumn = index == 0;
                        // Moving a whole king pile to another empty column cannot advance the game.
                        if (emptiesColumn && state.Tableau[target].Cards.Count == 0) continue;
                        // Do not shuffle the same exposed suffix back and forth between equivalent parents.
                        if (!revealsCard && !emptiesColumn &&
                            FindAutoFoundationMoveAfterRemovingSuffix(state, column, index) == false) continue;
                        Consider(move, revealsCard ? 1000 : emptiesColumn ? 420 : 180);
                    }
                }
            }

            if (state.Waste.Count > 0)
            {
                var source = new PileRef(PileKind.Waste);
                var foundationMove = FindAutoFoundationMove(state, source);
                if (foundationMove != null)
                    Consider(foundationMove, IsSafeFoundationMove(state, state.Waste[^1].Card) ? 850 : 320);
                for (var target = 0; target < 7; target++)
                {
                    var move = new Move(source, new PileRef(PileKind.Tableau, target));
                    Consider(move, 700);
                }
            }

            if (best != null) return best;
            return state.Stock.Count > 0 || state.Waste.Count > 0
                ? new Move(new PileRef(PileKind.Stock), new PileRef(PileKind.Waste)) : null;
        }

        private static bool IsSafeFoundationMove(GameState state, CardId card)
        {
            if (card.Value <= 2) return true;
            var first = card.IsRed ? CardSuit.Clubs : CardSuit.Diamonds;
            var second = card.IsRed ? CardSuit.Spades : CardSuit.Hearts;
            var firstValue = state.Foundations[(int)first].Cards.LastOrDefault()?.Card.Value ?? 0;
            var secondValue = state.Foundations[(int)second].Cards.LastOrDefault()?.Card.Value ?? 0;
            return card.Value <= Math.Min(firstValue, secondValue) + 1;
        }

        private static bool FindAutoFoundationMoveAfterRemovingSuffix(GameState state, int column, int index)
        {
            if (index <= 0) return false;
            var exposed = state.Tableau[column].Cards[index - 1];
            return exposed.FaceUp && IsSafeFoundationMove(state, exposed.Card) &&
                CanPlaceOnFoundation(state, (int)exposed.Card.Suit, exposed);
        }

        public bool IsSafeForAutoComplete(GameState state)
        {
            if (state.Tableau.Any(pile => pile.Cards.Any(card => !card.FaceUp))) return false;
            return state.Stock.Count == 0;
        }

        private static List<CardState> GetMovingCards(GameState state, Move move)
        {
            switch (move.From.Kind)
            {
                case PileKind.Waste:
                    return state.Waste.Count == 0 ? new List<CardState>() : new List<CardState> { state.Waste[^1] };
                case PileKind.Foundation:
                    var foundation = state.Foundations[move.From.Index].Cards;
                    return foundation.Count == 0 ? new List<CardState>() : new List<CardState> { foundation[^1] };
                case PileKind.Tableau:
                    var tableau = state.Tableau[move.From.Index].Cards;
                    var index = move.CardIndex < 0 ? tableau.Count - 1 : move.CardIndex;
                    if (index < 0 || index >= tableau.Count) return new List<CardState>();
                    var moving = tableau.GetRange(index, tableau.Count - index);
                    return IsDescendingAlternating(moving) ? moving : new List<CardState>();
                default:
                    return new List<CardState>();
            }
        }

        private static void RemoveMovingCards(GameState state, Move move, int count)
        {
            List<CardState> source = move.From.Kind switch
            {
                PileKind.Waste => state.Waste,
                PileKind.Foundation => state.Foundations[move.From.Index].Cards,
                _ => state.Tableau[move.From.Index].Cards
            };
            source.RemoveRange(source.Count - count, count);
        }

        private static bool CanPlaceOnTableau(GameState state, int index, IReadOnlyList<CardState> moving)
        {
            if (index < 0 || index >= 7) return false;
            var target = state.Tableau[index].Cards;
            var first = moving[0].Card;
            if (target.Count == 0) return first.Rank == CardRank.King;
            var top = target[^1];
            return top.FaceUp && top.Card.IsRed != first.IsRed && top.Card.Value == first.Value + 1;
        }

        private static bool CanPlaceOnFoundation(GameState state, int index, CardState moving)
        {
            if (index < 0 || index >= 4 || index != (int)moving.Card.Suit) return false;
            var target = state.Foundations[index].Cards;
            return target.Count == 0
                ? moving.Card.Rank == CardRank.Ace
                : target[^1].Card.Suit == moving.Card.Suit && target[^1].Card.Value + 1 == moving.Card.Value;
        }

        private static bool IsDescendingAlternating(IReadOnlyList<CardState> cards)
        {
            for (var i = 0; i < cards.Count - 1; i++)
            {
                if (cards[i].Card.Value != cards[i + 1].Card.Value + 1) return false;
                if (cards[i].Card.IsRed == cards[i + 1].Card.IsRed) return false;
            }
            return true;
        }

        private static void RevealExposedCard(GameState state, PileRef source)
        {
            if (source.Kind != PileKind.Tableau) return;
            var pile = state.Tableau[source.Index].Cards;
            if (pile.Count == 0 || pile[^1].FaceUp) return;
            pile[^1].FaceUp = true;
            state.Score += 5;
        }

        private static void ApplyMoveScore(GameState state, Move move)
        {
            if (move.To.Kind == PileKind.Foundation) state.Score += 10;
            else if (move.From.Kind == PileKind.Waste && move.To.Kind == PileKind.Tableau) state.Score += 5;
            else if (move.From.Kind == PileKind.Foundation && move.To.Kind == PileKind.Tableau)
                state.Score = Math.Max(0, state.Score - 15);
        }

        private static List<CardId> CreateDeck()
        {
            var cards = new List<CardId>(52);
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
                for (var rank = 1; rank <= 13; rank++) cards.Add(new CardId(suit, (CardRank)rank));
            return cards;
        }

        private static void Shuffle(IList<CardId> cards, ulong seed)
        {
            var random = new Pcg32(seed);
            for (var i = cards.Count - 1; i > 0; i--)
            {
                var j = random.Range(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }
    }
}
