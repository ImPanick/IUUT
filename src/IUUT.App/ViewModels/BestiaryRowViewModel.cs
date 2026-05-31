using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>
/// One creature group in the Accolades &amp; Bestiary editor: a stable <see cref="RowName"/>, its
/// catalog <see cref="Label"/>, and the editable scan <see cref="Points"/>. On apply, points &gt; 0
/// set/add the group; points of 0 remove it from tracking (master §11.7).
/// </summary>
public sealed class BestiaryRowViewModel : ObservableObject
{
    private long _points;

    /// <summary>Creates a creature-group row.</summary>
    public BestiaryRowViewModel(string rowName, string label, long points)
    {
        ArgumentException.ThrowIfNullOrEmpty(rowName);
        RowName = rowName;
        Label = string.IsNullOrEmpty(label) ? rowName : label;
        _points = points;
    }

    /// <summary>The <c>D_BestiaryData</c> row key — never edited.</summary>
    public string RowName { get; }

    /// <summary>The display name (catalog label, falling back to the key).</summary>
    public string Label { get; }

    /// <summary>The editable scan points.</summary>
    public long Points
    {
        get => _points;
        set => SetProperty(ref _points, value);
    }
}
