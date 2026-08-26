namespace taskAPI.Contracts;

public sealed record MoviePageRequest(
        ulong Seed,
        string? Locale,
        int Page,
        int? PageSize,
        double Likes,
        double Reviews
    );
