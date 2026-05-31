namespace IUUT.Core.Services;

/// <summary>
/// Detects whether Icarus is running so the UI can show its warn-only game-state
/// banner (master doc §14). Detection is **never** used to hard-block a save —
/// it only informs the user (CONSTITUTION IX).
/// </summary>
/// <remarks>
/// The shipping executable name is <b>not fixed</b>: it encodes the game version and
/// expansion and changes with every patch/DLC — e.g. <c>Icarus-Win64-Shipping</c> on
/// older builds, <c>Icarus-3.0.12.152317-Shipping-DangerousHorizons</c> currently.
/// Detection therefore matches by pattern (starts with <c>Icarus</c> + contains
/// <c>Shipping</c>), never against a literal name.
/// </remarks>
public sealed class GameProcessDetector
{
    private readonly IRunningProcesses _processes;

    /// <summary>Creates a detector over the given process source.</summary>
    public GameProcessDetector(IRunningProcesses processes)
    {
        ArgumentNullException.ThrowIfNull(processes);
        _processes = processes;
    }

    /// <summary>
    /// Whether <paramref name="processName"/> (without <c>.exe</c>) is the Icarus shipping
    /// build: starts with <c>Icarus</c> and contains <c>Shipping</c>, case-insensitively.
    /// Pure and patch-proof — the version/expansion suffix is ignored.
    /// </summary>
    public static bool IsIcarusShippingProcessName(string processName) =>
        !string.IsNullOrEmpty(processName)
        && processName.StartsWith("Icarus", StringComparison.OrdinalIgnoreCase)
        && processName.Contains("Shipping", StringComparison.OrdinalIgnoreCase);

    /// <summary>Scans running processes for the Icarus shipping build.</summary>
    public GameDetectionResult Detect()
    {
        var matches = _processes.GetProcessNames()
            .Where(IsIcarusShippingProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return matches.Count == 0
            ? GameDetectionResult.NotRunning
            : new GameDetectionResult { IsRunning = true, MatchedProcessNames = matches };
    }
}
