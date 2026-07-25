using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace IUUT.App.Services;

/// <summary>
/// The one search-box pattern for every long list (Tier 1 "search everywhere"): bind a TextBox
/// to <see cref="SearchText"/> (UpdateSourceTrigger=PropertyChanged) and the list's ItemsSource
/// to <see cref="View"/>. Wraps its own <see cref="ListCollectionView"/> — never the shared
/// default view — so filtering here cannot leak into other bindings of the same collection.
/// Editing and Apply paths keep iterating the source collection, so a hidden row is still saved.
/// </summary>
public sealed class FilteredView<T> : INotifyPropertyChanged
    where T : class
{
    private readonly Func<T, string, bool> _matches;
    private string _searchText = "";

    /// <summary>
    /// Creates the view over <paramref name="source"/>. <paramref name="matches"/> receives the
    /// item and the trimmed, non-empty search text. <paramref name="groupBy"/> optionally adds
    /// property grouping (e.g. the Game Tuner's setting groups).
    /// </summary>
    public FilteredView(ObservableCollection<T> source, Func<T, string, bool> matches, string? groupBy = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(matches);
        _matches = matches;

        var view = new ListCollectionView(source)
        {
            Filter = o => string.IsNullOrWhiteSpace(_searchText) || _matches((T)o, _searchText.Trim()),
        };
        if (groupBy is not null)
        {
            view.GroupDescriptions.Add(new PropertyGroupDescription(groupBy));
        }

        View = view;
    }

    /// <summary>The filtered (and optionally grouped) projection to bind ItemsSource to.</summary>
    public ICollectionView View { get; }

    /// <summary>Live search text; every change re-filters.</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            var text = value ?? "";
            if (_searchText == text)
            {
                return;
            }

            _searchText = text;
            View.Refresh();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
        }
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;
}
