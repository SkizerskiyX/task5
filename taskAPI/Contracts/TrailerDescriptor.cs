namespace taskAPI.Contracts;

public sealed record TrailerDescriptor(
    string Signature,
    ulong Seed,
    long MovieIndex,
    string Title,
    int Year,
    string Genre,
    int Width,
    int Height,
    double DurationSeconds,
    TrailerAudioDescriptor Audio,
    IReadOnlyList<TrailerSceneDescriptor> Scenes
);

public sealed record TrailerAudioDescriptor(
    string Asset,
    double Volume,
    double PlaybackRate,
    double StartOffset
);

public sealed record TrailerSceneDescriptor(
    int Index,
    double Start,
    double Duration,
    string Asset,
    string AssetType,
    string Transition,
    string Motion,
    string Filter,
    string TitleStyle,
    double ZoomFrom,
    double ZoomTo,
    double PanX,
    double PanY,
    double Rotation,
    double Brightness,
    double Contrast,
    double Saturation,
    double PlaybackRate,
    double AssetStartRatio,
    double OverlayHue,
    double OverlayOpacity,
    double GrainOpacity,
    double VignetteOpacity,
    double Shake,
    double ChromaticAberration,
    double TransitionDurationRatio,
    bool ShowTitle,
    bool ShowMetadata
);
