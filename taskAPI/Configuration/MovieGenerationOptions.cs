namespace taskAPI.Configuration;

public sealed class MovieGenerationOptions
{
    public const string SectionName = "MovieGeneration";

    public required int DefaultPageSize { get; init; }

    public required int MaximumPageSize { get; init; }

    public required double MinimumAverage { get; init; }

    public required double MaximumAverage { get; init; }

    public required double DefaultLikesAverage { get; init; }

    public required double DefaultReviewsAverage { get; init; }

    public required int MinimumYear { get; init; }

    public required int MaximumYear { get; init; }

    public required int MinimumActors { get; init; }

    public required int MaximumActors { get; init; }

    public required string DefaultLocale { get; init; }

    public required Dictionary<string, LocaleConfiguration> Locales { get; init; }
}

public sealed class LocaleConfiguration
{
    public required string FakerLocale { get; init; }

    public required string TitlesFile { get; init; }

    public required string GenresFile { get; init; }
}