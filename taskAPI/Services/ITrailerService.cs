using taskAPI.Contracts;

namespace taskAPI.Services;

public interface ITrailerService
{
    TrailerDescriptor Generate(
        ulong seed,
        long movieIndex,
        string locale
    );
}