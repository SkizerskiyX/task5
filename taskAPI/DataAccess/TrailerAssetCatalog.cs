namespace taskAPI.DataAccess;

public sealed record TrailerAssetCatalog(
    IReadOnlyList<TrailerMediaAsset> Media,
    IReadOnlyList<TrailerAudioAsset> Audio,
    IReadOnlyDictionary<string, TrailerGenreProfile> Profiles
);

public sealed record TrailerMediaAsset(
    string Path,
    string Type,
    IReadOnlyList<string> Tags
);

public sealed record TrailerAudioAsset(
    string Path,
    IReadOnlyList<string> Tags
);

public sealed record TrailerGenreProfile(
    IReadOnlyList<string> MediaTags,
    IReadOnlyList<string> AudioTags,
    IReadOnlyList<string> Transitions,
    IReadOnlyList<string> Motions,
    IReadOnlyList<string> Filters,
    IReadOnlyList<string> TitleStyles
);
