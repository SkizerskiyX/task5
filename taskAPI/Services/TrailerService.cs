using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using taskAPI.Configuration;
using taskAPI.Contracts;
using taskAPI.DataAccess;
using taskAPI.Random;
using taskAPI.RandomGeneration;

namespace taskAPI.Services;

public sealed class TrailerService : ITrailerService
{
    private const string DefaultProfileKey = "default";
    private const string VideoAssetType = "video";
    private const string ImageAssetType = "image";

    private readonly MovieGenerator movieGenerator;
    private readonly ITrailerAssetProvider assetProvider;
    private readonly TrailerSeedDeriver seedDeriver;
    private readonly TrailerGenerationOptions options;

    public TrailerService(
        MovieGenerator movieGenerator,
        ITrailerAssetProvider assetProvider,
        TrailerSeedDeriver seedDeriver,
        IOptions<TrailerGenerationOptions> options)
    {
        this.movieGenerator = movieGenerator;
        this.assetProvider = assetProvider;
        this.seedDeriver = seedDeriver;
        this.options = options.Value;
    }

    public TrailerDescriptor Generate(
        ulong seed,
        long movieIndex,
        string locale)
    {
        var movie = movieGenerator.GenerateMovie(seed, movieIndex, locale);
        var catalog = assetProvider.Get();
        var profile = ResolveProfile(movie.Genre, catalog.Profiles);
        var signature = CreateSignature(seed, movieIndex, movie.Title, movie.Year, movie.Genre);
        var durations = GenerateSceneDurations(seed, movieIndex);
        var scenes = GenerateScenes(seed, movieIndex, profile, catalog, durations);
        var audio = GenerateAudio(seed, movieIndex, profile, catalog.Audio);

        return new TrailerDescriptor(
            signature,
            seed,
            movieIndex,
            movie.Title,
            movie.Year,
            movie.Genre,
            options.CanvasWidth,
            options.CanvasHeight,
            options.DurationSeconds,
            audio,
            scenes
        );
    }

    private IReadOnlyList<TrailerSceneDescriptor> GenerateScenes(
        ulong seed,
        long movieIndex,
        TrailerGenreProfile profile,
        TrailerAssetCatalog catalog,
        IReadOnlyList<double> durations)
    {
        var candidates = FilterMedia(catalog.Media, profile.MediaTags);
        var usedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scenes = new List<TrailerSceneDescriptor>(options.SceneCount);
        var currentStart = 0d;

        for (var sceneIndex = 0; sceneIndex < options.SceneCount; sceneIndex++)
        {
            var random = CreateRandom(seed, movieIndex, "scene", sceneIndex);
            var asset = PickUnusedMedia(candidates, usedPaths, random);
            usedPaths.Add(asset.Path);

            var zoomA = Scale(random.NextDouble(), options.Zoom);
            var zoomB = Scale(random.NextDouble(), options.Zoom);
            var zoomFrom = sceneIndex % 2 == 0
                ? Math.Min(zoomA, zoomB)
                : Math.Max(zoomA, zoomB);
            var zoomTo = sceneIndex % 2 == 0
                ? Math.Max(zoomA, zoomB)
                : Math.Min(zoomA, zoomB);

            scenes.Add(
                new TrailerSceneDescriptor(
                    sceneIndex,
                    currentStart,
                    durations[sceneIndex],
                    asset.Path,
                    NormalizeAssetType(asset.Type),
                    Pick(profile.Transitions, random),
                    Pick(profile.Motions, random),
                    Pick(profile.Filters, random),
                    Pick(profile.TitleStyles, random),
                    zoomFrom,
                    zoomTo,
                    Scale(random.NextDouble(), options.Pan),
                    Scale(random.NextDouble(), options.Pan),
                    Scale(random.NextDouble(), options.Rotation),
                    Scale(random.NextDouble(), options.Brightness),
                    Scale(random.NextDouble(), options.Contrast),
                    Scale(random.NextDouble(), options.Saturation),
                    Scale(random.NextDouble(), options.PlaybackRate),
                    Scale(random.NextDouble(), options.AssetStartRatio),
                    Scale(random.NextDouble(), options.OverlayHue),
                    Scale(random.NextDouble(), options.OverlayOpacity),
                    Scale(random.NextDouble(), options.GrainOpacity),
                    Scale(random.NextDouble(), options.VignetteOpacity),
                    Scale(random.NextDouble(), options.Shake),
                    Scale(random.NextDouble(), options.ChromaticAberration),
                    Scale(random.NextDouble(), options.TransitionDurationRatio),
                    sceneIndex == options.SceneCount - 1,
                    sceneIndex == 0 || sceneIndex == options.SceneCount - 1
                )
            );

            currentStart += durations[sceneIndex];
        }

        return scenes;
    }

    private IReadOnlyList<double> GenerateSceneDurations(
        ulong seed,
        long movieIndex)
    {
        var random = CreateRandom(seed, movieIndex, "duration");
        var weights = new double[options.SceneCount];
        var totalWeight = 0d;

        for (var index = 0; index < weights.Length; index++)
        {
            weights[index] = Scale(random.NextDouble(), options.SceneDurationWeight);
            totalWeight += weights[index];
        }

        var result = new double[weights.Length];
        var accumulated = 0d;

        for (var index = 0; index < weights.Length - 1; index++)
        {
            result[index] = weights[index] / totalWeight * options.DurationSeconds;
            accumulated += result[index];
        }

        result[^1] = options.DurationSeconds - accumulated;

        return result;
    }

    private TrailerAudioDescriptor GenerateAudio(
        ulong seed,
        long movieIndex,
        TrailerGenreProfile profile,
        IReadOnlyList<TrailerAudioAsset> audioAssets)
    {
        var random = CreateRandom(seed, movieIndex, "audio");
        var candidates = audioAssets
            .Where(asset => asset.Tags.Any(tag => ContainsIgnoreCase(profile.AudioTags, tag)))
            .ToArray();

        if (candidates.Length == 0)
        {
            candidates = audioAssets.ToArray();
        }

        var asset = candidates[random.NextInt(0, candidates.Length)];

        return new TrailerAudioDescriptor(
            asset.Path,
            Scale(random.NextDouble(), options.AudioVolume),
            Scale(random.NextDouble(), options.AudioPlaybackRate),
            Scale(random.NextDouble(), options.AudioStartOffset)
        );
    }

    private static IReadOnlyList<TrailerMediaAsset> FilterMedia(
        IReadOnlyList<TrailerMediaAsset> media,
        IReadOnlyList<string> tags)
    {
        var matches = media
            .Where(asset => asset.Tags.Any(tag => ContainsIgnoreCase(tags, tag)))
            .ToArray();

        return matches.Length > 0 ? matches : media;
    }

    private static TrailerMediaAsset PickUnusedMedia(
        IReadOnlyList<TrailerMediaAsset> source,
        IReadOnlySet<string> usedPaths,
        Xoshiro256StarStar random)
    {
        var unused = source
            .Where(asset => !usedPaths.Contains(asset.Path))
            .ToArray();

        var candidates = unused.Length > 0
            ? unused
            : source.ToArray();

        return candidates[random.NextInt(0, candidates.Length)];
    }

    private static TrailerGenreProfile ResolveProfile(
        string genre,
        IReadOnlyDictionary<string, TrailerGenreProfile> profiles)
    {
        var exact = profiles.FirstOrDefault(pair =>
            string.Equals(pair.Key, genre, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(exact.Key))
        {
            return exact.Value;
        }

        var partial = profiles.FirstOrDefault(pair =>
            !string.Equals(pair.Key, DefaultProfileKey, StringComparison.OrdinalIgnoreCase) &&
            genre.Contains(pair.Key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(partial.Key))
        {
            return partial.Value;
        }

        var fallback = profiles.FirstOrDefault(pair =>
            string.Equals(pair.Key, DefaultProfileKey, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(fallback.Key))
        {
            throw new InvalidOperationException("Default trailer profile is missing.");
        }

        return fallback.Value;
    }

    private Xoshiro256StarStar CreateRandom(
        ulong seed,
        long movieIndex,
        string component,
        int ordinal = 0)
    {
        return new Xoshiro256StarStar(
            seedDeriver.Derive(seed, movieIndex, component, ordinal)
        );
    }

    private static string NormalizeAssetType(string value)
    {
        if (string.Equals(value, ImageAssetType, StringComparison.OrdinalIgnoreCase))
        {
            return ImageAssetType;
        }

        return VideoAssetType;
    }

    private static bool ContainsIgnoreCase(
        IEnumerable<string> source,
        string value)
    {
        return source.Contains(value, StringComparer.OrdinalIgnoreCase);
    }

    private static T Pick<T>(
        IReadOnlyList<T> source,
        Xoshiro256StarStar random)
    {
        if (source.Count == 0)
        {
            throw new InvalidOperationException("Trailer profile collection must not be empty.");
        }

        return source[random.NextInt(0, source.Count)];
    }

    private static double Scale(
        double value,
        NumericRange range)
    {
        return range.Minimum + value * (range.Maximum - range.Minimum);
    }

    private static string CreateSignature(
        ulong seed,
        long movieIndex,
        string title,
        int year,
        string genre)
    {
        var source = $"{seed}:{movieIndex}:{title}:{year}:{genre}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
