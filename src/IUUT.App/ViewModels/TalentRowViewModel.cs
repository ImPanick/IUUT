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

    /// <summary>Creates a talent row.</summary>
    public TalentRowViewModel(string rowName, string label, int rank, bool isLive = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(rowName);
        RowName = rowName;
        Label = string.IsNullOrEmpty(label) ? rowName : label;
        IsLive = isLive;
        _rank = Clamp(rank);
    }

    /// <summary>The <c>D_Talents</c> row key — never edited.</summary>
    public string RowName { get; }

    /// <summary>The display name (catalog label, falling back to the key).</summary>
    public string Label { get; }

    /// <summary>Whether this talent exists in the current live game data. <c>false</c> = staged/removed
    /// content the editor badges as "not live" (still editable; the game ignores unknown talents on load).</summary>
    public bool IsLive { get; }

    /// <summary>A short suffix the UI appends to <see cref="Label"/> for not-live rows.</summary>
    public string LiveBadge => IsLive ? "" : "  · not live";

    /// <summary>The editable rank, clamped to 0..<see cref="CharacterEditService.MaxTalentRank"/>.</summary>
    public int Rank
    {
        get => _rank;
        set => SetProperty(ref _rank, Clamp(value));
    }

    private static int Clamp(int rank) => Math.Clamp(rank, 0, CharacterEditService.MaxTalentRank);
}
