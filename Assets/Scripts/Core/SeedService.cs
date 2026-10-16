using System;

namespace BatsSolitaire.Core
{
    public static class SeedService
    {
        public static long RandomSeed() => DateTime.UtcNow.Ticks ^ Environment.TickCount;
        public static long WinnableSeed(int index) => RandomSeed() ^ index;

        public static long DailySeed(DateTime localDate)
        {
            var key = localDate.Year * 10000L + localDate.Month * 100L + localDate.Day;
            unchecked
            {
                var hash = key ^ 0x42415453L;
                hash = (hash ^ (hash >> 30)) * unchecked((long)0xbf58476d1ce4e5b9UL);
                hash = (hash ^ (hash >> 27)) * unchecked((long)0x94d049bb133111ebUL);
                return hash ^ (hash >> 31);
            }
        }
    }
}
