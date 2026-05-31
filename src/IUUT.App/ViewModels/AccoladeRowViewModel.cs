using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>
/// One accolade in the Accolades &amp; Bestiary editor: a stable <see cref="RowName"/>, its catalog
/// <see cref="Label"/>, and a <see cref="IsGranted"/> toggle. On apply a granted-but-absent accolade
/// is added (timestamped now) and an ungranted-but-present one is removed (master §11.7).
/// </summary>
public sealed class AccoladeRowViewModel : ObservableObject
{
    private bool _isGranted;

    /// <summary>Creates an accolade row.</summary>
    public AccoladeRowViewModel(string rowName, string label, bool isGranted)
    {
        ArgumentException.ThrowIfNullOrEmpty(rowName);
        RowName = rowName;
        Label = string.IsNullOrEmpty(label) ? rowName : label;
        _isGranted = isGranted;
    }

    /// <summary>The <c>D_Accolades</c> row key — never edited.</summary>
    public string RowName { get; }

    /// <summary>The display name (catalog label, falling back to the key).</summary>
    public string Label { get; }

    /// <summary>Whether the account holds this accolade.</summary>
    public bool IsGranted
    {
        get => _isGranted;
        set => SetProperty(ref _isGranted, value);
    }
}
