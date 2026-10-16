using System;
using System.Threading;

namespace BatsSolitaire.Core
{
    public static class SeedService
    {
        private static long randomState = DateTime.UtcNow.Ticks ^ ((long)Environment.TickCount << 32);

        public static long RandomSeed()
        {
            var value = unchecked((ulong)Interlocked.Add(ref randomState, (long)0x9e3779b97f4a7c15UL));
            value = (value ^ (value >> 30)) * 0xbf58476d1ce4e5b9UL;
            value = (value ^ (value >> 27)) * 0x94d049bb133111ebUL;
            return unchecked((long)(value ^ (value >> 31)));
        }
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
