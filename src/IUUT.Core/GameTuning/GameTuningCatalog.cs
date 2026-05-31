namespace IUUT.Core.GameTuning;

/// <summary>
/// The curated set of tunable Engine.ini settings (docs/GAME-TUNING.md §4, §5). Toggles disable
/// effects; numbers set quality/perf cvars bounded to a conservative <c>StableMax</c> cap. Every
/// value is a candidate pending live-client verification (Icarus may ignore/lock some).
/// </summary>
public sealed class GameTuningCatalog
{
    /// <summary>The startup-cvar section for <c>r.*</c> / <c>sg.*</c> / <c>t.*</c>.</summary>
    public const string ConsoleVariablesSection = "ConsoleVariables";

    /// <summary>The engine section for frame-rate smoothing.</summary>
    public const string EngineSection = "/Script/Engine.Engine";

    /// <summary>The tunable settings, in display order.</summary>
    public IReadOnlyList<GameTuningSetting> Settings { get; } = Build();

    private static IReadOnlyList<GameTuningSetting> Build() =>
    [
        // ---- Toggles (ON disables the effect; OFF removes the cvar → game default) ----
        Toggle("disable-fog", "Disable fog", "Turn off scene fog.", "r.Fog"),
        Toggle("disable-volfog", "Disable volumetric fog", "Turn off god-ray volumetric fog.", "r.VolumetricFog"),
        Toggle("disable-volclouds", "Disable volumetric clouds", "Turn off volumetric clouds.", "r.VolumetricCloud"),
        Toggle("disable-motionblur", "Disable motion blur", "Turn off motion blur.", "r.MotionBlurQuality"),
        Toggle("disable-dof", "Disable depth of field", "Turn off depth-of-field blur.", "r.DepthOfFieldQuality"),
        Toggle("disable-vsync", "Disable VSync", "Turn off vertical sync (may increase FPS / tearing).", "r.VSync"),
        new()
        {
            Id = "disable-smoothing",
            Label = "Disable frame-rate smoothing",
            Description = "Stop the engine from smoothing/limiting the frame rate.",
            Section = EngineSection,
            Key = "bSmoothFrameRate",
            Kind = GameTuningKind.Toggle,
            OnValue = "False",
        },

        // ---- Numbers (bounded to a stable max) ----
        Number("res-scale", "Resolution scale", "Internal render resolution; below 100 upscales (faster).",
            "r.ScreenPercentage", min: 50, stableMax: 100, def: 100, step: 5, unit: "%"),
        Number("max-fps", "Max FPS", "Frame-rate cap.",
            "t.MaxFPS", min: 30, stableMax: 360, def: 60, step: 10, unit: "FPS"),
        Number("view-dist-scale", "View distance scale", "Multiplier on draw distance.",
            "r.ViewDistanceScale", min: 0.4, stableMax: 1.5, def: 1.0, step: 0.1, unit: null),
        Number("pool-size", "Texture pool", "Texture-streaming pool (raise only within your VRAM).",
            "r.Streaming.PoolSize", min: 1000, stableMax: 8192, def: 3000, step: 256, unit: "MB"),
        Quality("shadow-quality", "Shadow quality", "sg.ShadowQuality"),
        Quality("effects-quality", "Effects quality", "sg.EffectsQuality"),
        Quality("viewdist-quality", "View distance quality", "sg.ViewDistanceQuality"),
        Quality("foliage-quality", "Foliage quality", "sg.FoliageQuality"),
        Quality("postprocess-quality", "Post-processing quality", "sg.PostProcessQuality"),
    ];

    private static GameTuningSetting Toggle(string id, string label, string description, string key) => new()
    {
        Id = id,
        Label = label,
        Description = description,
        Section = ConsoleVariablesSection,
        Key = key,
        Kind = GameTuningKind.Toggle,
        OnValue = "0",
    };

    private static GameTuningSetting Number(
        string id, string label, string description, string key,
        double min, double stableMax, double def, double step, string? unit) => new()
        {
            Id = id,
            Label = label,
            Description = description,
            Section = ConsoleVariablesSection,
            Key = key,
            Kind = GameTuningKind.Number,
            Min = min,
            StableMax = stableMax,
            Default = def,
            Step = step,
            Unit = unit,
        };

    // sg.* scalability scalars: 0 Low · 1 Med · 2 High · 3 Epic · 4 Cinematic (the stable max).
    private static GameTuningSetting Quality(string id, string label, string key) =>
        Number(id, label, "Scalability scalar (0 Low … 4 Cinematic).", key, min: 0, stableMax: 4, def: 3, step: 1, unit: null);
}
