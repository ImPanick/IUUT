namespace IUUT.Core.Services;

/// <summary>Result of a game-process scan (master doc §14). Drives the warn-only banner; never blocks.</summary>
public sealed record GameDetectionResult
{
    /// <summary>Whether an Icarus shipping process is currently running.</summary>
    public required bool IsRunning { get; init; }

    /// <summary>The matched process name(s) (for display), empty when not running.</summary>
    public required IReadOnlyList<string> MatchedProcessNames { get; init; }

    /// <summary>A not-running result.</summary>
    public static GameDetectionResult NotRunning { get; } =
        new() { IsRunning = false, MatchedProcessNames = [] };
}
