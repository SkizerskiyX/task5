using taskAPI.Contracts;

namespace taskAPI.Services
{
    public interface IMovieService
    {
        MoviePageResponse GeneratePage(MoviePageRequest request);

        ulong GenerateSeed();
    }
}
