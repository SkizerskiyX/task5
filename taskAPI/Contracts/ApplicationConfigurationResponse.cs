namespace taskAPI.Contracts;

public sealed record ApplicationConfigurationResponse(
    string DefaultLocale,
    IReadOnlyCollection<string> Locales,
    int DefaultPageSize,
    int MaximumPageSize,
    double MinimumAverage,
    double MaximumAverage,
    double DefaultLikesAverage,
    double DefaultReviewsAverage,
    string MaximumSeed
);