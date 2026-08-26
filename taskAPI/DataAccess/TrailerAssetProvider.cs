using System.Text.Json;
using Microsoft.Extensions.Options;
using taskAPI.Configuration;

namespace taskAPI.DataAccess;

public sealed class TrailerAssetProvider : ITrailerAssetProvider
{
    private readonly TrailerGenerationOptions options;
    private readonly IWebHostEnvironment environment;
    private readonly Lazy<TrailerAssetCatalog> catalog;

    public TrailerAssetProvider(
        IOptions<TrailerGenerationOptions> options,
        IWebHostEnvironment environment)
    {
        this.options = options.Value;
        this.environment = environment;
        catalog = new Lazy<TrailerAssetCatalog>(Load);
    }

    public TrailerAssetCatalog Get()
    {
        return catalog.Value;
    }

    private TrailerAssetCatalog Load()
    {
        var path = Path.Combine(environment.ContentRootPath, options.AssetsFile);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Trailer asset catalog was not found at '{path}'.", path);
        }

        var value = JsonSerializer.Deserialize<TrailerAssetCatalog>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }
        );

        if (value is null)
        {
            throw new InvalidOperationException("Trailer asset catalog could not be deserialized.");
        }

        Validate(value);
        return value;
    }

    private static void Validate(TrailerAssetCatalog value)
    {
        if (value.Media.Count == 0)
        {
            throw new InvalidOperationException("Trailer media catalog must not be empty.");
        }

        if (value.Audio.Count == 0)
        {
            throw new InvalidOperationException("Trailer audio catalog must not be empty.");
        }

        if (value.Profiles.Count == 0 ||
            !value.Profiles.Keys.Any(key => string.Equals(key, "default", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Trailer profiles must contain a default profile.");
        }

        if (value.Media.Any(asset =>
                string.IsNullOrWhiteSpace(asset.Path) ||
                string.IsNullOrWhiteSpace(asset.Type) ||
                asset.Tags.Count == 0))
        {
            throw new InvalidOperationException("Every trailer media asset must have a path, type and tags.");
        }

        if (value.Audio.Any(asset =>
                string.IsNullOrWhiteSpace(asset.Path) ||
                asset.Tags.Count == 0))
        {
            throw new InvalidOperationException("Every trailer audio asset must have a path and tags.");
        }

        if (value.Profiles.Values.Any(profile =>
                profile.MediaTags.Count == 0 ||
                profile.AudioTags.Count == 0 ||
                profile.Transitions.Count == 0 ||
                profile.Motions.Count == 0 ||
                profile.Filters.Count == 0 ||
                profile.TitleStyles.Count == 0))
        {
            throw new InvalidOperationException("Every trailer profile must define media tags, audio tags, transitions, motions, filters and title styles.");
        }
    }
}
