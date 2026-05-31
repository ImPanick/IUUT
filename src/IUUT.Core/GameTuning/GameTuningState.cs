namespace IUUT.Core.GameTuning;

/// <summary>The current/edited state of a <see cref="GameTuningSetting"/> (mutable; the UI binds it).</summary>
public sealed class GameTuningState
{
    /// <summary>The setting this state is for.</summary>
    public required GameTuningSetting Setting { get; init; }

    /// <summary>Whether IUUT manages this cvar (true → written on Apply; false → removed, game default).</summary>
    public bool Enabled { get; set; }

    /// <summary>For a number setting: the current value (clamped to the setting's stable max on Apply).</summary>
    public double Value { get; set; }
}
