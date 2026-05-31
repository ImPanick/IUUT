namespace IUUT.Core.Services;

/// <summary>
/// The full data behind the Home screen (master doc §10.2): the resolved save root,
/// the discovered profiles (with PersonaName labels), and the warn-only game-state
/// banner. Produced by <see cref="HomeService.LoadAsync"/>; the ViewModel binds it.
/// </summary>
public sealed record HomeState
{
    /// <summary>The save root that was scanned.</summary>
    public required string SaveRoot { get; init; }

    /// <summary>
    /// Whether <see cref="SaveRoot"/> contains a <c>PlayerData\</c> folder. When false the UI
    /// prompts the user to Browse to a save root (master doc §7.1 manual fallback).
    /// </summary>
    public required bool SaveRootFound { get; init; }

    /// <summary>Discovered profiles, ordered by SteamID64 (empty when none / root not found).</summary>
    public required IReadOnlyList<HomeSaveSlot> Slots { get; init; }

    /// <summary>The game-process scan that drives the warn-only "Icarus is running" banner (§14).</summary>
    public required GameDetectionResult Game { get; init; }

    /// <summary>Whether any save profiles were discovered.</summary>
    public bool HasProfiles => Slots.Count > 0;
}
