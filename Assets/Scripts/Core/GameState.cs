using System;
using System.Collections.Generic;
using System.Linq;

namespace BatsSolitaire.Core
{
    [Serializable]
    public sealed class CardStateList
    {
        public List<CardState> Cards = new();
        public CardStateList Clone()
        {
            var result = new CardStateList();
            result.Cards.AddRange(Cards.Select(card => card.Clone()));
            return result;
        }
    }

    [Serializable]
    public sealed class GameState
    {
        public int SchemaVersion = 1;
        public long Seed;
        public GameMode Mode;
        public List<CardState> Stock = new();
        public List<CardState> Waste = new();
        public List<CardStateList> Tableau = new();
        public List<CardStateList> Foundations = new();
        public int Score;
        public int Moves;
        public float ElapsedSeconds;
        public int StockPasses;
        public bool Won;

        public GameState()
        {
            EnsurePiles();
        }

        public void EnsurePiles()
        {
            while (Tableau.Count < 7) Tableau.Add(new CardStateList());
            while (Foundations.Count < 4) Foundations.Add(new CardStateList());
        }

        public GameState Clone()
        {
            var clone = new GameState
            {
                SchemaVersion = SchemaVersion,
                Seed = Seed,
                Mode = Mode,
                Score = Score,
                Moves = Moves,
                ElapsedSeconds = ElapsedSeconds,
                StockPasses = StockPasses,
                Won = Won,
                Stock = Stock.Select(card => card.Clone()).ToList(),
                Waste = Waste.Select(card => card.Clone()).ToList(),
                Tableau = Tableau.Select(pile => pile.Clone()).ToList(),
                Foundations = Foundations.Select(pile => pile.Clone()).ToList()
            };
            clone.EnsurePiles();
            return clone;
        }

        public IEnumerable<CardState> AllCards()
        {
            foreach (var card in Stock) yield return card;
            foreach (var card in Waste) yield return card;
            foreach (var pile in Tableau)
                foreach (var card in pile.Cards) yield return card;
            foreach (var pile in Foundations)
                foreach (var card in pile.Cards) yield return card;
        }
    }
}
