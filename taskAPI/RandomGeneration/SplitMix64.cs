
namespace taskAPI.Random
{
    public class SplitMix64
    {
        private ulong state;

        public SplitMix64(ulong seed)
        {
            state = seed;
        }

        public ulong NextUInt64()
        {
            state += 0x9E3779B97F4A7C15;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
            return z ^ (z >> 31);
           
        }
    }
}
