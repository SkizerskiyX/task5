using System.Buffers.Binary;
using System.Security.Cryptography;
using taskAPI.RandomGeneration;

namespace taskAPI.Random;

public static class SeedDeriver
{
    public static ulong DeriveSeed(
        ulong masterSeed,
        long movieIndex,
        RandomStream stream)
    {
        Span<byte> input = stackalloc byte[
            sizeof(ulong) +
            sizeof(long) +
            sizeof(int)
        ];

        BinaryPrimitives.WriteUInt64LittleEndian(
            input[..sizeof(ulong)],
            masterSeed
        );

        BinaryPrimitives.WriteInt64LittleEndian(
            input.Slice(sizeof(ulong), sizeof(long)),
            movieIndex
        );

        BinaryPrimitives.WriteInt32LittleEndian(
            input.Slice(sizeof(ulong) + sizeof(long), sizeof(int)),
            (int)stream
        );

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);

        return BinaryPrimitives.ReadUInt64LittleEndian(hash);
    }
}
