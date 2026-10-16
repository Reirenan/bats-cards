using System;
using BatsSolitaire.Core;

namespace BatsSolitaire.Services
{
    public sealed class GameSession
    {
        private readonly KlondikeRules rules = new();
        private readonly UndoHistory undo = new();
        private readonly SaveService saves;

        public GameState State { get; private set; }
        public KlondikeRules Rules => rules;
        public bool CanUndo => undo.Count > 0;
        public event Action Changed;

        public GameSession(SaveService saves)
        {
            this.saves = saves;
        }

        public void NewGame(GameMode mode, long? seed = null)
        {
            var resolvedSeed = seed ?? mode switch
            {
                GameMode.Daily => SeedService.DailySeed(DateTime.Today),
                GameMode.Winnable => SeedService.WinnableSeed(saves.Data.Stats.GamesStarted),
                _ => SeedService.RandomSeed()
            };
            State = mode == GameMode.Random
                ? rules.CreateGame(resolvedSeed, mode)
                : rules.CreateGuaranteedGame(resolvedSeed, mode);
            undo.Clear();
            saves.Data.Stats.GamesStarted++;
            Commit();
        }

        public bool Resume()
        {
            if (saves.Data.CurrentGame == null || saves.Data.CurrentGame.Won) return false;
            State = saves.Data.CurrentGame.Clone();
            undo.Clear();
            Changed?.Invoke();
            return true;
        }

        public bool Draw()
        {
            if (State == null) return false;
            undo.Push(State);
            if (!rules.DrawOrRecycle(State))
            {
                undo.TryPop(out _);
                return false;
            }
            Commit();
            return true;
        }

        public bool Move(Move move)
        {
            if (State == null) return false;
            undo.Push(State);
            if (!rules.ApplyMove(State, move))
            {
                undo.TryPop(out _);
                return false;
            }
            if (State.Won) RegisterWin();
            Commit();
            return true;
        }

        public bool Undo()
        {
            if (!undo.TryPop(out var previous)) return false;
            State = previous;
            Commit();
            return true;
        }

        public void Restart()
        {
            if (State == null) return;
            State = State.Mode == GameMode.Random
                ? rules.CreateGame(State.Seed, State.Mode)
                : rules.CreateGuaranteedGame(State.Seed, State.Mode);
            undo.Clear();
            Commit();
        }

        public void Tick(float deltaTime)
        {
            if (State == null || State.Won) return;
            State.ElapsedSeconds += deltaTime;
        }

        public void Save() => saves.SaveGame(State);

        private void Commit()
        {
            saves.SaveGame(State);
            Changed?.Invoke();
        }

        private void RegisterWin()
        {
            var stats = saves.Data.Stats;
            stats.GamesWon++;
            stats.CurrentWinStreak++;
            stats.BestWinStreak = Math.Max(stats.BestWinStreak, stats.CurrentWinStreak);
            stats.BestScore = Math.Max(stats.BestScore, State.Score);
            if (stats.BestTimeSeconds <= 0 || State.ElapsedSeconds < stats.BestTimeSeconds)
                stats.BestTimeSeconds = State.ElapsedSeconds;
            if (State.Mode == GameMode.Daily) stats.LastDailyDate = DateTime.Today.ToString("yyyy-MM-dd");
        }
    }
}
