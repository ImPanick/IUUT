namespace IUUT.Core.GameTuning;

/// <summary>How a tunable Engine.ini setting is presented/written (docs/GAME-TUNING.md).</summary>
public enum GameTuningKind
{
    /// <summary>On/off: ON writes <see cref="GameTuningSetting.OnValue"/>; OFF removes the cvar (game default).</summary>
    Toggle,

    /// <summary>A number written as the cvar value, bounded to a stable maximum.</summary>
    Number,
}
