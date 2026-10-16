using System.Collections.Generic;

namespace BatsSolitaire.Core
{
    public sealed class UndoHistory
    {
        private readonly Stack<GameState> states = new();
        public int Count => states.Count;

        public void Push(GameState state) => states.Push(state.Clone());
        public bool TryPop(out GameState state)
        {
            if (states.Count == 0)
            {
                state = null;
                return false;
            }
            state = states.Pop();
            return true;
        }
        public void Clear() => states.Clear();
    }
}
