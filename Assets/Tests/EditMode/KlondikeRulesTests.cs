using System.IO;
using System.Linq;
using BatsSolitaire.Core;
using BatsSolitaire.Services;
using NUnit.Framework;

namespace BatsSolitaire.Tests
{
    public sealed class KlondikeRulesTests
    {
        private KlondikeRules rules;

        [Test]
        public void LongMoveSequences_NeverLoseOrDuplicateCards()
        {
            for (var seed = 1; seed <= 20; seed++)
            {
                var state = rules.CreateGame(seed, GameMode.Random);
                for (var step = 0; step < 400 && !state.Won; step++)
                {
                    var move = rules.FindHint(state);
                    if (move == null) break;
                    if (move.From.Kind == PileKind.Stock) rules.DrawOrRecycle(state);
                    else Assert.That(rules.ApplyMove(state, move), Is.True);
                    Assert.That(state.AllCards().Count(), Is.EqualTo(52));
                    Assert.That(state.AllCards().Select(c => c.Card).Distinct().Count(), Is.EqualTo(52));
                }
            }
        }

        [Test]
        public void Save_NullSessionDoesNotEraseResumeAndBackupRecovers()
        {
            var path = Path.Combine(Path.GetTempPath(), System.Guid.NewGuid() + ".json");
            try
            {
                var saves = new SaveService(path);
                saves.SaveGame(rules.CreateGame(123, GameMode.Random));
                saves.SaveGame(null);
                Assert.That(new SaveService(path).Data.CurrentGame.Seed, Is.EqualTo(123));
                saves.Write();
                File.WriteAllText(path, "broken");
                Assert.That(new SaveService(path).Data.CurrentGame.Seed, Is.EqualTo(123));
            }
            finally
            {
                foreach (var file in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(file)) File.Delete(file);
            }
        }

        [SetUp]
        public void SetUp() => rules = new KlondikeRules();

        [Test]
        public void NewGame_DealsClassicLayoutAndAllCardsOnce()
        {
            var state = rules.CreateGame(12345, GameMode.Random);
            Assert.That(state.Stock.Count, Is.EqualTo(24));
            Assert.That(state.Tableau.Sum(pile => pile.Cards.Count), Is.EqualTo(28));
            Assert.That(state.AllCards().Count(), Is.EqualTo(52));
            Assert.That(state.AllCards().Select(card => card.Card).Distinct().Count(), Is.EqualTo(52));
            for (var column = 0; column < 7; column++)
            {
                Assert.That(state.Tableau[column].Cards.Count, Is.EqualTo(column + 1));
                Assert.That(state.Tableau[column].Cards.Count(card => card.FaceUp), Is.EqualTo(1));
            }
        }

        [Test]
        public void SameSeed_ProducesSameDeal()
        {
            var first = rules.CreateGame(999, GameMode.Random);
            var second = rules.CreateGame(999, GameMode.Random);
            CollectionAssert.AreEqual(first.AllCards().Select(card => card.Card), second.AllCards().Select(card => card.Card));
        }

        [Test]
        public void DrawOne_AndRecycle_PreservesOrderAndCount()
        {
            var state = rules.CreateGame(17, GameMode.Random);
            var first = state.Stock[^1].Card;
            while (state.Stock.Count > 0) Assert.That(rules.DrawOrRecycle(state), Is.True);
            Assert.That(state.Waste.Count, Is.EqualTo(24));
            Assert.That(rules.DrawOrRecycle(state), Is.True);
            Assert.That(state.Stock.Count, Is.EqualTo(24));
            Assert.That(rules.DrawOrRecycle(state), Is.True);
            Assert.That(state.Waste[^1].Card, Is.EqualTo(first));
        }

        [Test]
        public void Tableau_RequiresDescendingAlternatingCards()
        {
            var state = new GameState();
            state.Tableau[0].Cards.Add(new CardState(new CardId(CardSuit.Spades, CardRank.Eight), true));
            state.Tableau[1].Cards.Add(new CardState(new CardId(CardSuit.Hearts, CardRank.Seven), true));
            var valid = new Move(new PileRef(PileKind.Tableau, 1), new PileRef(PileKind.Tableau, 0), 0);
            Assert.That(rules.CanMove(state, valid), Is.True);

            state.Tableau[1].Cards[0] = new CardState(new CardId(CardSuit.Clubs, CardRank.Seven), true);
            Assert.That(rules.CanMove(state, valid), Is.False);
        }

        [Test]
        public void Foundation_BuildsBySuitFromAce()
        {
            var state = new GameState();
            state.Waste.Add(new CardState(new CardId(CardSuit.Hearts, CardRank.Ace), true));
            var ace = new Move(new PileRef(PileKind.Waste), new PileRef(PileKind.Foundation, (int)CardSuit.Hearts));
            Assert.That(rules.ApplyMove(state, ace), Is.True);
            Assert.That(state.Score, Is.EqualTo(10));
        }

        [Test]
        public void Undo_RestoresExactSnapshot()
        {
            var state = rules.CreateGame(22, GameMode.Random);
            var history = new UndoHistory();
            history.Push(state);
            rules.DrawOrRecycle(state);
            Assert.That(history.TryPop(out var previous), Is.True);
            Assert.That(previous.Stock.Count, Is.EqualTo(24));
            Assert.That(previous.Waste.Count, Is.Zero);
            Assert.That(previous.Moves, Is.Zero);
        }

        [Test]
        public void Save_RoundTripsGameState()
        {
            var path = Path.Combine(Path.GetTempPath(), "bats-solitaire-test-save.json");
            if (File.Exists(path)) File.Delete(path);
            var service = new SaveService(path);
            var state = rules.CreateGame(54321, GameMode.Daily);
            rules.DrawOrRecycle(state);
            service.SaveGame(state);
            var loaded = new SaveService(path).Data.CurrentGame;
            Assert.That(loaded.Seed, Is.EqualTo(54321));
            Assert.That(loaded.Waste.Count, Is.EqualTo(1));
            Assert.That(loaded.AllCards().Count(), Is.EqualTo(52));
            File.Delete(path);
        }

        [Test]
        public void GuaranteedDeal_IsActuallySolvable()
        {
            var state = rules.CreateGuaranteedGame(20261015, GameMode.Winnable);
            while (state.Stock.Count > 0)
            {
                Assert.That(rules.DrawOrRecycle(state), Is.True);
                var card = state.Waste[^1].Card;
                var move = new Move(new PileRef(PileKind.Waste), new PileRef(PileKind.Foundation, (int)card.Suit));
                Assert.That(rules.ApplyMove(state, move), Is.True, $"Stock card {card} should move to foundation");
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
                Assert.That(moved || state.Won, Is.True, "Guaranteed deal became stuck");
            }
            Assert.That(state.Won, Is.True);
        }
    }
}
