namespace IUUT.Core.Services;

/// <summary>
/// Supplies the names of currently-running processes (without the <c>.exe</c> suffix).
/// Abstracted so <see cref="GameProcessDetector"/> can be tested without enumerating
/// real OS processes.
/// </summary>
public interface IRunningProcesses
{
    /// <summary>Returns the current process names (e.g. <c>Icarus-3.0.12.152317-Shipping-DangerousHorizons</c>).</summary>
    IReadOnlyList<string> GetProcessNames();
}
