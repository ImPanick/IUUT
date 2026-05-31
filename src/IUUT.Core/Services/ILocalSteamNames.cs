namespace IUUT.Core.Services;

/// <summary>
/// Offline source of Steam PersonaNames keyed by SteamID64 (typically parsed from the
/// local <c>loginusers.vdf</c>). Resolved before any network call (master doc §7.5.1).
/// </summary>
public interface ILocalSteamNames
{
    /// <summary>Returns the local PersonaName for <paramref name="steamId64"/>, or <c>null</c> if unknown.</summary>
    string? TryGetPersonaName(string steamId64);
}
