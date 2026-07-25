using CommunityToolkit.Mvvm.ComponentModel;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// One editable character talent row: a stable <see cref="RowName"/>, its catalog
/// <see cref="Label"/>, and the user-editable <see cref="Rank"/> (0–4, clamped). On apply a rank of
/// 0 removes the row; the game clamps over-ranked rows to each row's true max on load (master §8.3).
/// </summary>
public sealed class TalentRowViewModel : ObservableObject
{
    private int _rank;

    /// <summary>Creates a talent row. <paramref name="maxRank"/> is the row's true max from the
    /// catalog mine (null → the permissive <see cref="CharacterEditService.MaxTalentRank"/> fallback).
    /// The LOADED rank is never coerced — catalog data must not gatekeep save data (CONSTITUTION VI;
    /// a stale catalog would silently downgrade earned ranks on the next Apply). The slider ceiling
    /// stretches to accommodate an above-catalog loaded value instead.</summary>
    public TalentRowViewModel(string rowName, string label, int rank, bool isLive = true, int? maxRank = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(rowName);
        RowName = rowName;
        Label = string.IsNullOrEmpty(label) ? rowName : label;
        IsLive = isLive;
        Maximum = Math.Max(maxRank ?? CharacterEditService.MaxTalentRank, rank);
        _rank = Math.Max(0, rank); // loaded value preserved verbatim (floor 0 only)
    }

    /// <summary>The row's max rank (slider ceiling): the mined true max — stretched to the loaded
    /// rank when the save is already above it, so the loaded value round-trips untouched.</summary>
    public int Maximum { get; }

    /// <summary>The <c>D_Talents</c> row key — never edited.</summary>
    public string RowName { get; }

    /// <summary>The display name (catalog label, falling back to the key).</summary>
    public string Label { get; }

    /// <summary>Whether this talent exists in the current live game data. <c>false</c> = staged/removed
    /// content the editor badges as "not live" (still editable; the game ignores unknown talents on load).</summary>
    public bool IsLive { get; }

    /// <summary>A short suffix the UI appends to <see cref="Label"/> for not-live rows.</summary>
    public string LiveBadge => IsLive ? "" : "  · not live";

    /// <summary>The editable rank, clamped to 0..<see cref="Maximum"/>.</summary>
    public int Rank
    {
        get => _rank;
        set => SetProperty(ref _rank, Clamp(value));
    }

    private int Clamp(int rank) => Math.Clamp(rank, 0, Maximum);
}
