namespace IUUT.Core.Services;

/// <summary>
/// A discovered save profile under <c>PlayerData\</c> (master doc §7.1, §11.1 F-003/F-004).
/// On disk the folder is named by SteamID64; the UI shows the resolved PersonaName
/// (resolved later by the Steam name resolver — WP-6 — not here).
/// </summary>
public sealed record SaveProfile
{
    /// <summary>The on-disk folder name (expected to be a SteamID64).</summary>
    public required string SteamId64 { get; init; }

    /// <summary>Absolute path to the profile folder.</summary>
    public required string FolderPath { get; init; }

    /// <summary>Character count parsed from <c>Characters.json</c>, or <c>null</c> if missing/unparseable.</summary>
    public int? CharacterCount { get; init; }

    /// <summary>Newest file modification time in the folder (UTC), or <c>null</c> if unreadable.</summary>
    public DateTimeOffset? LastModifiedUtc { get; init; }

    /// <summary>Whether a <c>Profile.json</c> exists in the folder.</summary>
    public bool HasProfileJson { get; init; }

    /// <summary>Whether the folder name is a 17-digit string (the SteamID64 shape).</summary>
    public bool LooksLikeSteamId64 { get; init; }
}
