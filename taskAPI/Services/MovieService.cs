using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using taskAPI.Configuration;
using taskAPI.Contracts;
using taskAPI.RandomGeneration;

namespace taskAPI.Services;

public sealed class MovieService : IMovieService
{
    private readonly MovieGenerator movieGenerator;
    private readonly MovieLikesReviewsGenerator likesReviewsGenerator;
    private readonly MovieGenerationOptions options;

    public MovieService(
        MovieGenerator movieGenerator,
        MovieLikesReviewsGenerator likesReviewsGenerator,
        IOptions<MovieGenerationOptions> options)
    {
        this.movieGenerator = movieGenerator;
        this.likesReviewsGenerator = likesReviewsGenerator;
        this.options = options.Value;
    }

    public MoviePageResponse GeneratePage(MoviePageRequest request)
    {
        Validate(request);

        var locale = string.IsNullOrWhiteSpace(request.Locale)
            ? options.DefaultLocale
            : request.Locale;

        var pageSize = request.PageSize ?? options.DefaultPageSize;

        var startIndex = checked(
            ((long)request.Page - 1) * pageSize + 1
        );

        var items = new List<Entities.Movie>(pageSize);

        for (var offset = 0; offset < pageSize; offset++)
        {
            var index = checked(startIndex + offset);

            var movie = movieGenerator.GenerateMovie(
                request.Seed,
                index,
                locale
            );

            likesReviewsGenerator.Apply(
                movie,
                request.Seed,
                request.Likes,
                request.Reviews,
                locale
            );

            items.Add(movie);
        }

        return new MoviePageResponse(
            request.Seed,
            locale,
            request.Page,
            pageSize,
            items
        );
    }

    public ulong GenerateSeed()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);

        return BitConverter.ToUInt64(bytes);
    }

    private void Validate(MoviePageRequest request)
    {
        if (request.Page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Page)
            );
        }

        var pageSize = request.PageSize ?? options.DefaultPageSize;

        if (pageSize < 1 || pageSize > options.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.PageSize)
            );
        }

        ValidateAverage(request.Likes, nameof(request.Likes));
        ValidateAverage(request.Reviews, nameof(request.Reviews));

        var locale = string.IsNullOrWhiteSpace(request.Locale)
            ? options.DefaultLocale
            : request.Locale;

        if (!options.Locales.ContainsKey(locale))
        {
            throw new ArgumentException(
                $"Unsupported locale '{locale}'.",
                nameof(request.Locale)
            );
        }
    }

    private void ValidateAverage(double value, string name)
    {
        if (double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < options.MinimumAverage ||
            value > options.MaximumAverage)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}