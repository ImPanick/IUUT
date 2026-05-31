namespace IUUT.Core.Services;

/// <summary>
/// One row in the Home screen's profile dropdown (master doc §10.2): a discovered
/// <see cref="SaveProfile"/> joined with its resolved Steam display
/// (<see cref="SteamProfileDisplay"/>). View-ready — the ViewModel binds this directly.
/// </summary>
public sealed record HomeSaveSlot
{
    /// <summary>The on-disk folder name (SteamID64).</summary>
    public required string SteamId64 { get; init; }

    /// <summary>Absolute path to the profile folder.</summary>
    public required string FolderPath { get; init; }

    /// <summary>The label to show: the resolved PersonaName if known, else the SteamID64.</summary>
    public required string DisplayLabel { get; init; }

    /// <summary>The resolved PersonaName, or <c>null</c> when unresolved.</summary>
    public string? PersonaName { get; init; }

    /// <summary>Where the name came from (cache / local VDF / Steam API / fallback).</summary>
    public required SteamNameSource NameSource { get; init; }

    /// <summary>Character count parsed from <c>Characters.json</c>, or <c>null</c> if missing/unparseable.</summary>
    public int? CharacterCount { get; init; }

    /// <summary>Newest file modification time in the folder (UTC), or <c>null</c> if unreadable.</summary>
    public DateTimeOffset? LastModifiedUtc { get; init; }

    /// <summary>Whether a <c>Profile.json</c> exists in the folder.</summary>
    public bool HasProfileJson { get; init; }

    /// <summary>Whether the folder name is a 17-digit SteamID64.</summary>
    public bool LooksLikeSteamId64 { get; init; }

    /// <summary>Joins a discovered profile with its resolved display into a view-ready slot.</summary>
    public static HomeSaveSlot From(SaveProfile profile, SteamProfileDisplay display)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(display);

        return new HomeSaveSlot
        {
            SteamId64 = profile.SteamId64,
            FolderPath = profile.FolderPath,
            DisplayLabel = display.DisplayLabel,
            PersonaName = display.PersonaName,
            NameSource = display.Source,
            CharacterCount = profile.CharacterCount,
            LastModifiedUtc = profile.LastModifiedUtc,
            HasProfileJson = profile.HasProfileJson,
            LooksLikeSteamId64 = profile.LooksLikeSteamId64,
        };
    }
}
