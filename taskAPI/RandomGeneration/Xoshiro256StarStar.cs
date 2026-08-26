namespace taskAPI.Random
{
    public class Xoshiro256StarStar
    {
        private readonly ulong[] s = new ulong[4];

        public Xoshiro256StarStar(ulong seed)
        {
            var sm = new SplitMix64(seed);

            s[0] = sm.NextUInt64();
            s[1] = sm.NextUInt64();
            s[2] = sm.NextUInt64();
            s[3] = sm.NextUInt64();


        }

        public ulong NextUInt64()
        {
            ulong result = ulong.RotateLeft(s[1] * 5, 7) * 9;
            ulong t = s[1] << 17;
            s[2] ^= s[0];
            s[3] ^= s[1];
            s[1] ^= s[2];
            s[0] ^= s[3];
            s[2] ^= t;
            s[3] = ulong.RotateLeft(s[3], 45);
            return result;
        }

        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / (1UL << 53));
        }
        public int NextInt(int minInclusive, int maxExclusive)
        {

            return minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));

        }
    }
}
