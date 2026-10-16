namespace BatsSolitaire.Core
{
    public sealed class Pcg32
    {
        private ulong state;
        private readonly ulong increment;

        public Pcg32(ulong seed, ulong sequence = 1442695040888963407UL)
        {
            state = 0;
            increment = (sequence << 1) | 1;
            NextUInt();
            state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            var oldState = state;
            state = oldState * 6364136223846793005UL + increment;
            var xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            var rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        public int Range(int exclusiveMax)
        {
            if (exclusiveMax <= 0) return 0;
            var bound = (uint)exclusiveMax;
            var threshold = unchecked((uint)(-bound)) % bound;
            uint value;
            do value = NextUInt(); while (value < threshold);
            return (int)(value % bound);
        }
    }
}
