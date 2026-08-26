namespace taskAPI.DataAccess;

public sealed record RegionData(
    string FakerLocale,
    IReadOnlyList<string> Titles,
    IReadOnlyList<string> Genres
);