using taskAPI.Random;

namespace taskAPI.RandomGeneration
{
    public class MovieSeedGenerator
    {
        public ulong CreateSeed(ulong masterSeed, int movieIndex)
        {
            ulong combinedSeed = masterSeed + (ulong)movieIndex;

            SplitMix64 mixer = new SplitMix64(combinedSeed);

            return mixer.NextUInt64();

        }
    }
}
