using Entities;

namespace taskAPI.Contracts;

public sealed record MoviePageResponse(
    ulong Seed,
    string Locale,
    int Page,
    int PageSize,
    IReadOnlyList<Movie> Items
);