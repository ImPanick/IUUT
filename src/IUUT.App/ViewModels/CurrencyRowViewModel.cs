using CommunityToolkit.Mvvm.ComponentModel;

namespace IUUT.App.ViewModels;

/// <summary>
/// One editable account currency row in the Account &amp; Currencies editor: a stable
/// <see cref="MetaRow"/> key, its display <see cref="Label"/>, and the user-editable
/// <see cref="Count"/>. The amount is written back through <c>AccountEditService</c> on apply;
/// the game clamps to its own cap on load (master §11.6).
/// </summary>
public sealed class CurrencyRowViewModel : ObservableObject
{
    private long _count;

    /// <summary>Creates a currency row.</summary>
    public CurrencyRowViewModel(string metaRow, string label, long count)
    {
        ArgumentException.ThrowIfNullOrEmpty(metaRow);
        MetaRow = metaRow;
        Label = string.IsNullOrEmpty(label) ? metaRow : label;
        _count = count;
    }

    /// <summary>The currency key (e.g. <c>Credits</c>, <c>Exotic_Red</c>) — never edited.</summary>
    public string MetaRow { get; }

    /// <summary>The display name (catalog label, falling back to the key).</summary>
    public string Label { get; }

    /// <summary>The editable amount.</summary>
    public long Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }
}
