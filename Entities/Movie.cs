namespace Entities;

public sealed class Movie
{
    public required long Index { get; init; }

    public required string Title { get; init; }

    public required IReadOnlyList<string> Actors { get; init; }

    public required int Year { get; init; }

    public required string Genre { get; init; }

    public required int Likes { get; set; }

    public required IReadOnlyList<string> Reviews { get; set; }
}