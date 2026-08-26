using taskAPI.Configuration;
using taskAPI.DataAccess;
using taskAPI.RandomGeneration;
using taskAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<MovieGenerationOptions>()
    .BindConfiguration(MovieGenerationOptions.SectionName)
    .Validate(
        options => options.DefaultPageSize > 0,
        "DefaultPageSize must be greater than zero."
    )
    .Validate(
        options => options.MaximumPageSize >= options.DefaultPageSize,
        "MaximumPageSize must be greater than or equal to DefaultPageSize."
    )
    .Validate(
        options => options.MaximumAverage >= options.MinimumAverage,
        "Average range is invalid."
    )
    .Validate(
        options => options.DefaultLikesAverage >= options.MinimumAverage &&
                   options.DefaultLikesAverage <= options.MaximumAverage,
        "DefaultLikesAverage is outside the configured average range."
    )
    .Validate(
        options => options.DefaultReviewsAverage >= options.MinimumAverage &&
                   options.DefaultReviewsAverage <= options.MaximumAverage,
        "DefaultReviewsAverage is outside the configured average range."
    )
    .Validate(
        options => options.MaximumYear >= options.MinimumYear,
        "Year range is invalid."
    )
    .Validate(
        options => options.MinimumActors > 0 &&
                   options.MaximumActors >= options.MinimumActors,
        "Actor range is invalid."
    )
    .Validate(
        options => options.Locales.Count > 0 &&
                   options.Locales.ContainsKey(options.DefaultLocale),
        "Default locale must exist in locale configuration."
    )
    .ValidateOnStart();

builder.Services
    .AddOptions<TrailerGenerationOptions>()
    .BindConfiguration(TrailerGenerationOptions.SectionName)
    .Validate(
        options => options.DurationSeconds > 0,
        "Trailer duration must be greater than zero."
    )
    .Validate(
        options => options.SceneCount > 0,
        "Trailer scene count must be greater than zero."
    )
    .Validate(
        options => options.CanvasWidth > 0 && options.CanvasHeight > 0,
        "Trailer canvas size is invalid."
    )
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.AssetsFile),
        "Trailer asset catalog path is required."
    )
    .Validate(
        options => IsValidRange(options.SceneDurationWeight, true),
        "Scene duration weight range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Zoom, true),
        "Zoom range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Pan),
        "Pan range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Rotation),
        "Rotation range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Brightness, true),
        "Brightness range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Contrast, true),
        "Contrast range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Saturation, true),
        "Saturation range is invalid."
    )
    .Validate(
        options => IsValidRange(options.PlaybackRate, true),
        "Playback rate range is invalid."
    )
    .Validate(
        options => IsUnitRange(options.AssetStartRatio),
        "Asset start ratio range is invalid."
    )
    .Validate(
        options => IsValidRange(options.OverlayHue),
        "Overlay hue range is invalid."
    )
    .Validate(
        options => IsUnitRange(options.OverlayOpacity),
        "Overlay opacity range is invalid."
    )
    .Validate(
        options => IsUnitRange(options.GrainOpacity),
        "Grain opacity range is invalid."
    )
    .Validate(
        options => IsUnitRange(options.VignetteOpacity),
        "Vignette opacity range is invalid."
    )
    .Validate(
        options => IsValidRange(options.Shake),
        "Shake range is invalid."
    )
    .Validate(
        options => IsValidRange(options.ChromaticAberration),
        "Chromatic aberration range is invalid."
    )
    .Validate(
        options => IsUnitRange(options.TransitionDurationRatio),
        "Transition duration ratio range is invalid."
    )
    .Validate(
        options => IsUnitRange(options.AudioVolume),
        "Audio volume range is invalid."
    )
    .Validate(
        options => IsValidRange(options.AudioPlaybackRate, true),
        "Audio playback rate range is invalid."
    )
    .Validate(
        options => IsValidRange(options.AudioStartOffset),
        "Audio start offset range is invalid."
    )
    .ValidateOnStart();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<IRegionDataProvider, RegionDataProvider>();
builder.Services.AddSingleton<ITrailerAssetProvider, TrailerAssetProvider>();

builder.Services.AddSingleton<MovieGenerator>();
builder.Services.AddSingleton<MovieLikesReviewsGenerator>();
builder.Services.AddSingleton<TrailerSeedDeriver>();

builder.Services.AddSingleton<IMovieService, MovieService>();
builder.Services.AddSingleton<ITrailerService, TrailerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

static bool IsValidRange(NumericRange range, bool requirePositiveMinimum = false)
{
    if (double.IsNaN(range.Minimum) ||
        double.IsNaN(range.Maximum) ||
        double.IsInfinity(range.Minimum) ||
        double.IsInfinity(range.Maximum) ||
        range.Maximum < range.Minimum)
    {
        return false;
    }

    return !requirePositiveMinimum || range.Minimum > 0;
}

static bool IsUnitRange(NumericRange range)
{
    return IsValidRange(range) &&
           range.Minimum >= 0 &&
           range.Maximum <= 1;
}
