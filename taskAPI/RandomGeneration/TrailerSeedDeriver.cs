using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace taskAPI.RandomGeneration;

public sealed class TrailerSeedDeriver
{
    public ulong Derive(
        ulong masterSeed,
        long movieIndex,
        string component,
        int ordinal = 0)
    {
        var componentBytes = Encoding.UTF8.GetBytes(component);
        var input = new byte[
            sizeof(ulong) +
            sizeof(long) +
            sizeof(int) +
            componentBytes.Length
        ];

        BinaryPrimitives.WriteUInt64LittleEndian(
            input.AsSpan(0, sizeof(ulong)),
            masterSeed
        );

        BinaryPrimitives.WriteInt64LittleEndian(
            input.AsSpan(sizeof(ulong), sizeof(long)),
            movieIndex
        );

        BinaryPrimitives.WriteInt32LittleEndian(
            input.AsSpan(sizeof(ulong) + sizeof(long), sizeof(int)),
            ordinal
        );

        componentBytes.CopyTo(
            input.AsSpan(sizeof(ulong) + sizeof(long) + sizeof(int))
        );

        var hash = SHA256.HashData(input);

        return BinaryPrimitives.ReadUInt64LittleEndian(hash);
    }
}
