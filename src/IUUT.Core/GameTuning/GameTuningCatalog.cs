namespace IUUT.Core.GameTuning;

/// <summary>
/// The curated set of tunable Engine.ini settings (docs/GAME-TUNING.md §4, §5). Toggles disable (or
/// enable) effects; numbers set quality/perf cvars bounded to a conservative <c>StableMax</c> cap.
/// <para>
/// CVars and their game defaults/ranges are <b>datamined from the game's own
/// <c>Icarus/Config/SettingsSchema.json</c></b> where present (e.g. <c>r.MotionBlur.Scale</c>,
/// <c>r.ContactShadows</c>, <c>r.Shadow.CSM.MaxCascades</c>, <c>r.Streaming.PoolSize</c>,
/// <c>r.VolumetricCloud</c>, the <c>r.RayTracing.*</c> family) and otherwise from public UE
/// knowledge. Whether Icarus honours a given cvar is per-cvar (it may be ignored/locked) — the
/// <see cref="GameTuningSetting.Experimental"/> ones especially.
/// </para>
/// </summary>
public sealed class GameTuningCatalog
{
    /// <summary>The startup-cvar section for <c>r.*</c> / <c>sg.*</c> / <c>t.*</c> / <c>grass.*</c> / <c>ShowFlag.*</c>.</summary>
    public const string ConsoleVariablesSection = "ConsoleVariables";

    /// <summary>The engine section for frame-rate smoothing.</summary>
    public const string EngineSection = "/Script/Engine.Engine";

    private const string GroupVisualFx = "VISUAL FX";
    private const string GroupFrameRate = "FRAME RATE";
    private const string GroupResolution = "RESOLUTION & QUALITY";
    private const string GroupShadows = "SHADOWS";
    private const string GroupTextures = "TEXTURES & STREAMING";
    private const string GroupAdvanced = "ADVANCED · MAY NOT APPLY";

    /// <summary>The tunable settings, in display order.</summary>
    public IReadOnlyList<GameTuningSetting> Settings { get; } = Build();

    private static IReadOnlyList<GameTuningSetting> Build() =>
    [
        // ---- Visual FX (toggle ON disables the effect; OFF removes the cvar → game default) ----
        Toggle("disable-fog", "Disable fog", "Turn off scene fog.", "r.Fog", GroupVisualFx),
        Toggle("disable-volfog", "Disable volumetric fog", "Turn off god-ray volumetric fog.", "r.VolumetricFog", GroupVisualFx),
        Toggle("disable-volclouds", "Disable volumetric clouds", "Turn off volumetric clouds.", "r.VolumetricCloud", GroupVisualFx),
        Toggle("disable-motionblur", "Disable motion blur", "Turn off motion blur.", "r.MotionBlurQuality", GroupVisualFx),
        Toggle("disable-dof", "Disable depth of field", "Turn off depth-of-field blur.", "r.DepthOfFieldQuality", GroupVisualFx),
        Number("motionblur-scale", "Motion blur amount", "Motion-blur strength (the game's own cvar; 0 = none).",
            "r.MotionBlur.Scale", GroupVisualFx, min: 0, stableMax: 2, def: 0, step: 0.1, unit: null),

        // ---- Frame rate ----
        Toggle("disable-vsync", "Disable VSync", "Turn off vertical sync (may increase FPS / tearing).", "r.VSync", GroupFrameRate),
        Number("max-fps", "Max FPS", "Frame-rate cap.",
            "t.MaxFPS", GroupFrameRate, min: 30, stableMax: 360, def: 60, step: 10, unit: "FPS"),
        new()
        {
            Id = "disable-smoothing",
            Label = "Disable frame-rate smoothing",
            Description = "Stop the engine from smoothing/limiting the frame rate.",
            Section = EngineSection,
            Key = "bSmoothFrameRate",
            Kind = GameTuningKind.Toggle,
            OnValue = "False",
            Group = GroupFrameRate,
        },

        // ---- Resolution & quality ----
        Number("res-scale", "Resolution scale", "Internal render resolution; below 100 upscales (faster).",
            "r.ScreenPercentage", GroupResolution, min: 50, stableMax: 100, def: 100, step: 5, unit: "%"),
        Number("view-dist-scale", "View distance scale", "Multiplier on draw distance.",
            "r.ViewDistanceScale", GroupResolution, min: 0.4, stableMax: 1.5, def: 1.0, step: 0.1, unit: null),
        Quality("effects-quality", "Effects quality", "sg.EffectsQuality", GroupResolution),
        Quality("viewdist-quality", "View distance quality", "sg.ViewDistanceQuality", GroupResolution),
        Quality("foliage-quality", "Foliage quality", "sg.FoliageQuality", GroupResolution),
        Quality("postprocess-quality", "Post-processing quality", "sg.PostProcessQuality", GroupResolution),

        // ---- Shadows ----
        Quality("shadow-quality", "Shadow quality", "sg.ShadowQuality", GroupShadows),
        Toggle("disable-contact-shadows", "Disable contact shadows", "Turn off screen-space contact shadows.", "r.ContactShadows", GroupShadows),
        Toggle("disable-grass-shadows", "Disable grass shadows", "Stop grass from casting dynamic shadows.", "grass.DisableDynamicShadows", GroupShadows, onValue: "1"),
        Number("shadow-cascades", "Shadow cascades", "Cascaded-shadow-map count; fewer = faster, less distant shadow detail.",
            "r.Shadow.CSM.MaxCascades", GroupShadows, min: 1, stableMax: 4, def: 4, step: 1, unit: null),

        // ---- Textures & streaming ----
        Number("pool-size", "Texture pool", "Texture-streaming pool (raise only within your VRAM).",
            "r.Streaming.PoolSize", GroupTextures, min: 1000, stableMax: 8192, def: 3000, step: 256, unit: "MB"),
        Toggle("limit-pool-vram", "Cap texture pool to VRAM", "Let the engine clamp the texture pool to available VRAM.", "r.Streaming.LimitPoolSizeToVRAM", GroupTextures, onValue: "1"),
        Quality("texture-quality", "Texture quality", "sg.TextureQuality", GroupTextures),

        // ---- Advanced / experimental (Icarus may ignore these entirely) ----
        Experimental("disable-tessellation", "Disable tessellation", "Turn off the tessellation show-flag.", "ShowFlag.Tessellation", onValue: "0"),
        Experimental("shadow-filter-pcss", "Soft shadows (PCSS)", "Use the PCSS shadow filter method (1) instead of uniform PCF (0).", "r.Shadow.FilterMethod", onValue: "1"),
        Experimental("rt-enable", "Enable ray tracing", "Master switch for in-game ray tracing.", "r.RayTracing.EnableInGame", onValue: "1"),
        Experimental("rt-shadows", "Ray-traced shadows", "Use ray tracing for shadows.", "r.RayTracing.Shadows", onValue: "1"),
        Experimental("rt-reflections", "Ray-traced reflections", "Use ray tracing for reflections.", "r.RayTracing.Reflections", onValue: "1"),
        Experimental("rt-ao", "Ray-traced ambient occlusion", "Use ray tracing for ambient occlusion.", "r.RayTracing.AmbientOcclusion", onValue: "1"),
        Experimental("rt-skylight", "Ray-traced sky light", "Use ray tracing for the sky light.", "r.RayTracing.SkyLight", onValue: "1"),
        Experimental("rt-rtxgi", "RTX global illumination", "NVIDIA RTXGI dynamic diffuse global illumination.", "r.RTXGI.DDGI", onValue: "1"),
    ];

    private static GameTuningSetting Toggle(
        string id, string label, string description, string key, string group, string onValue = "0") => new()
        {
            Id = id,
            Label = label,
            Description = description,
            Section = ConsoleVariablesSection,
            Key = key,
            Kind = GameTuningKind.Toggle,
            OnValue = onValue,
            Group = group,
        };

    private static GameTuningSetting Number(
        string id, string label, string description, string key, string group,
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
            Group = group,
        };

    // sg.* scalability scalars: 0 Low · 1 Med · 2 High · 3 Epic · 4 Cinematic (the stable max).
    private static GameTuningSetting Quality(string id, string label, string key, string group) =>
        Number(id, label, "Scalability scalar (0 Low … 4 Cinematic).", key, group, min: 0, stableMax: 4, def: 3, step: 1, unit: null);

    // Niche/experimental cvars Icarus may not honour at all — surfaced with a caveat.
    private static GameTuningSetting Experimental(string id, string label, string description, string key, string onValue) => new()
    {
        Id = id,
        Label = label,
        Description = description,
        Section = ConsoleVariablesSection,
        Key = key,
        Kind = GameTuningKind.Toggle,
        OnValue = onValue,
        Group = GroupAdvanced,
        Experimental = true,
    };
}
