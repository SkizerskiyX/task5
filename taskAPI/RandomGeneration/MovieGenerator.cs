using Bogus;
using Entities;
using Microsoft.Extensions.Options;
using taskAPI.Configuration;
using taskAPI.DataAccess;
using taskAPI.Random;

namespace taskAPI.RandomGeneration;

public sealed class MovieGenerator
{
    private readonly IRegionDataProvider regionDataProvider;
    private readonly MovieGenerationOptions options;

    public MovieGenerator(
        IRegionDataProvider regionDataProvider,
        IOptions<MovieGenerationOptions> options)
    {
        this.regionDataProvider = regionDataProvider;
        this.options = options.Value;
    }

    public Movie GenerateMovie(
        ulong masterSeed,
        long movieIndex,
        string locale)
    {
        if (movieIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(movieIndex));
        }

        var region = regionDataProvider.Get(locale);

        var titleRng = CreateRandom(masterSeed, movieIndex, RandomStream.Title);
        var genreRng = CreateRandom(masterSeed, movieIndex, RandomStream.Genre);
        var yearRng = CreateRandom(masterSeed, movieIndex, RandomStream.Year);
        var actorsRng = CreateRandom(masterSeed, movieIndex, RandomStream.Actors);

        var title = region.Titles[
            titleRng.NextInt(0, region.Titles.Count)
        ];

        var genre = region.Genres[
            genreRng.NextInt(0, region.Genres.Count)
        ];

        var year = yearRng.NextInt(
            options.MinimumYear,
            checked(options.MaximumYear + 1)
        );

        var actorCount = actorsRng.NextInt(
            options.MinimumActors,
            checked(options.MaximumActors + 1)
        );

        var fakerSeed = ToBogusSeed(
    SeedDeriver.DeriveSeed(
    masterSeed,
    movieIndex,
    RandomStream.Actors
)
     
 );

        var faker = new Faker(region.FakerLocale)
        {
            Random = new Randomizer(fakerSeed)
        };

        var actors = Enumerable
            .Range(0, actorCount)
            .Select(_ => faker.Name.FullName())
            .ToArray();

        return new Movie
        {
            Index = movieIndex,
            Title = title,
            Actors = actors,
            Year = year,
            Genre = genre,
            Likes = 0,
            Reviews = []
        };
    }

    private static Xoshiro256StarStar CreateRandom(
    ulong masterSeed,
    long movieIndex,
    RandomStream stream)
    {
        var seed = SeedDeriver.DeriveSeed(
            masterSeed,
            movieIndex,
            stream
        );

        return new Xoshiro256StarStar(seed);
    }

    private static int ToBogusSeed(ulong value)
    {
        var folded = value ^ (value >> 32);
        return unchecked((int)folded);
    }
}