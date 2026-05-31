namespace IUUT.Core.Services;

/// <summary>
/// The display result for one save profile (master doc §7.5.1, §9.2): the SteamID64,
/// the resolved PersonaName (or <c>null</c> when unresolved), where it came from, and
/// when it was resolved.
/// </summary>
public sealed record SteamProfileDisplay(
    string SteamId64,
    string? PersonaName,
    SteamNameSource Source,
    DateTimeOffset? ResolvedAtUtc)
{
    /// <summary>The label to show in the UI: the PersonaName if resolved, else the raw SteamID64.</summary>
    public string DisplayLabel => PersonaName ?? SteamId64;
}
