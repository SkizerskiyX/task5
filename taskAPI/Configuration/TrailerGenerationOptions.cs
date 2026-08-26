namespace taskAPI.Configuration;

public sealed class TrailerGenerationOptions
{
    public const string SectionName = "TrailerGeneration";

    public required double DurationSeconds { get; init; }
    public required int SceneCount { get; init; }
    public required int CanvasWidth { get; init; }
    public required int CanvasHeight { get; init; }
    public required string AssetsFile { get; init; }
    public required NumericRange SceneDurationWeight { get; init; }
    public required NumericRange Zoom { get; init; }
    public required NumericRange Pan { get; init; }
    public required NumericRange Rotation { get; init; }
    public required NumericRange Brightness { get; init; }
    public required NumericRange Contrast { get; init; }
    public required NumericRange Saturation { get; init; }
    public required NumericRange PlaybackRate { get; init; }
    public required NumericRange AssetStartRatio { get; init; }
    public required NumericRange OverlayHue { get; init; }
    public required NumericRange OverlayOpacity { get; init; }
    public required NumericRange GrainOpacity { get; init; }
    public required NumericRange VignetteOpacity { get; init; }
    public required NumericRange Shake { get; init; }
    public required NumericRange ChromaticAberration { get; init; }
    public required NumericRange TransitionDurationRatio { get; init; }
    public required NumericRange AudioVolume { get; init; }
    public required NumericRange AudioPlaybackRate { get; init; }
    public required NumericRange AudioStartOffset { get; init; }
}

public sealed class NumericRange
{
    public required double Minimum { get; init; }
    public required double Maximum { get; init; }
}
