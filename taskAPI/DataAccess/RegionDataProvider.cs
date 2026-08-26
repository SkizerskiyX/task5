using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;
using taskAPI.Configuration;
using taskAPI.DataAccess;

namespace taskAPI.DataAccess;

public sealed class RegionDataProvider : IRegionDataProvider
{
    private readonly MovieGenerationOptions options;
    private readonly IWebHostEnvironment environment;
    private readonly ConcurrentDictionary<string, RegionData> cache =
        new(StringComparer.OrdinalIgnoreCase);

    public RegionDataProvider(
        IOptions<MovieGenerationOptions> options,
        IWebHostEnvironment environment)
    {
        this.options = options.Value;
        this.environment = environment;
    }

    public RegionData Get(string locale)
    {
        if (!options.Locales.ContainsKey(locale))
        {
            throw new ArgumentException($"Unsupported locale '{locale}'.", nameof(locale));
        }

        return cache.GetOrAdd(locale, Load);
    }

    private RegionData Load(string locale)
    {
        var configuration = options.Locales[locale];

        var titles = ReadValues(configuration.TitlesFile);
        var genres = ReadValues(configuration.GenresFile);

        if (titles.Count == 0)
        {
            throw new InvalidOperationException($"Title dataset for '{locale}' is empty.");
        }

        if (genres.Count == 0)
        {
            throw new InvalidOperationException($"Genre dataset for '{locale}' is empty.");
        }

        return new RegionData(
            configuration.FakerLocale,
            titles,
            genres
        );
    }

    private IReadOnlyList<string> ReadValues(string relativePath)
    {
        var path = Path.Combine(environment.ContentRootPath, relativePath);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Dataset was not found at '{path}'.", path);
        }

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<List<string>>(
                   json,
                   new JsonSerializerOptions
                   {
                       PropertyNameCaseInsensitive = true
                   })
               ?? [];
    }
}