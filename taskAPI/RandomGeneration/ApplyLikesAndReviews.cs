using Bogus;
using Entities;
using taskAPI.DataAccess;
using taskAPI.Random;

namespace taskAPI.RandomGeneration;

public sealed class MovieLikesReviewsGenerator
{
    private readonly IRegionDataProvider regionDataProvider;

    public MovieLikesReviewsGenerator(
        IRegionDataProvider regionDataProvider)
    {
        this.regionDataProvider = regionDataProvider;
    }

    public void Apply(
        Movie movie,
        ulong masterSeed,
        double averageLikes,
        double averageReviews,
        string locale)
    {

        var likesSeed = SeedDeriver.DeriveSeed(
    masterSeed,
    movie.Index,
    RandomStream.Likes
);

        var reviewsSeed = SeedDeriver.DeriveSeed(
            masterSeed,
            movie.Index,
            RandomStream.Reviews
        );

        var likesRng = new Xoshiro256StarStar(likesSeed);
        var reviewsRng = new Xoshiro256StarStar(reviewsSeed);

        movie.Likes = RollCount(averageLikes, likesRng);

        var reviewCount = RollCount(averageReviews, reviewsRng);

        if (reviewCount == 0)
        {
            movie.Reviews = [];
            return;
        }

        var region = regionDataProvider.Get(locale);

        var faker = new Faker(region.FakerLocale)
        {
            Random = new Randomizer(ToBogusSeed(reviewsSeed))
        };

        movie.Reviews = Enumerable
            .Range(0, reviewCount)
            .Select(_ => Normalize(faker.Rant.Review()))
            .ToArray();
    }

    private static int RollCount(
        double average,
        Xoshiro256StarStar random)
    {
        var whole = (int)Math.Floor(average);
        var fractional = average - whole;

        return whole + (random.NextDouble() < fractional ? 1 : 0);
    }

    private static int ToBogusSeed(ulong value)
    {
        var folded = value ^ (value >> 32);
        return unchecked((int)folded);
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }
}