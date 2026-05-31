namespace IUUT.Core.GameTuning;

/// <summary>
/// One tunable Engine.ini setting (a UE console variable / engine key) — docs/GAME-TUNING.md.
/// Candidate values from public UE knowledge; whether Icarus honours a given cvar is per-cvar and
/// must be verified against the live client (the cvar may be ignored/locked).
/// </summary>
public sealed record GameTuningSetting
{
    /// <summary>Stable identifier.</summary>
    public required string Id { get; init; }

    /// <summary>UI label.</summary>
    public required string Label { get; init; }

    /// <summary>What it does.</summary>
    public required string Description { get; init; }

    /// <summary>The INI section, e.g. <c>ConsoleVariables</c> or <c>/Script/Engine.Engine</c>.</summary>
    public required string Section { get; init; }

    /// <summary>The cvar / key, e.g. <c>r.VolumetricFog</c> or <c>bSmoothFrameRate</c>.</summary>
    public required string Key { get; init; }

    /// <summary>Toggle or number.</summary>
    public required GameTuningKind Kind { get; init; }

    /// <summary>For a toggle: the value written when ON (OFF removes the key). Default <c>0</c>.</summary>
    public string OnValue { get; init; } = "0";

    /// <summary>For a number: the minimum.</summary>
    public double Min { get; init; }

    /// <summary>For a number: the stable maximum (the cap — values are clamped here).</summary>
    public double StableMax { get; init; }

    /// <summary>For a number: the game default (used when the cvar is absent).</summary>
    public double Default { get; init; }

    /// <summary>For a number: the slider step.</summary>
    public double Step { get; init; } = 1;

    /// <summary>Optional unit suffix for the UI (e.g. <c>%</c>, <c>MB</c>, <c>FPS</c>).</summary>
    public string? Unit { get; init; }
}
