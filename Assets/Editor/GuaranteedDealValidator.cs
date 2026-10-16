using BatsSolitaire.Core;
using UnityEditor;
using UnityEngine;

namespace BatsSolitaire.Editor
{
    public static class GuaranteedDealValidator
    {
        [MenuItem("Bats Cards/Validate 1000 Guaranteed Deals")]
        public static void Validate()
        {
            var rules = new KlondikeRules();
            for (var seed = 1; seed <= 1000; seed++)
            {
                var state = rules.CreateGuaranteedGame(seed, GameMode.Winnable);
                if (!SolveConstructedDeal(rules, state))
                    throw new System.InvalidOperationException($"A distribuição garantida com seed {seed} não pôde ser resolvida.");
            }
            Debug.Log("1000 distribuições garantidas foram resolvidas com sucesso.");
        }

        private static bool SolveConstructedDeal(KlondikeRules rules, GameState state)
        {
            while (state.Stock.Count > 0)
            {
                if (!rules.DrawOrRecycle(state)) return false;
                var card = state.Waste[^1].Card;
                if (!rules.ApplyMove(state, new Move(new PileRef(PileKind.Waste),
                        new PileRef(PileKind.Foundation, (int)card.Suit)))) return false;
            }

            var safety = 100;
            while (!state.Won && safety-- > 0)
            {
                var moved = false;
                for (var column = 0; column < 7; column++)
                {
                    var pile = state.Tableau[column].Cards;
                    if (pile.Count == 0) continue;
                    var card = pile[^1].Card;
                    var move = new Move(new PileRef(PileKind.Tableau, column),
                        new PileRef(PileKind.Foundation, (int)card.Suit), pile.Count - 1);
                    if (!rules.ApplyMove(state, move)) continue;
                    moved = true;
                }
                if (!moved) return state.Won;
            }
            return state.Won;
        }
    }
}
