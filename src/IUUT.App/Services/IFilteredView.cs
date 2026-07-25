using System.ComponentModel;

namespace IUUT.App.Services;

/// <summary>
/// The non-generic surface of <see cref="FilteredView{T}"/>, so the shared
/// <c>FilteredListBox</c> control can bind without knowing the row type (DE-2 primitive).
/// </summary>
public interface IFilteredView
{
    /// <summary>Live search text; every change re-filters.</summary>
    string SearchText { get; set; }

    /// <summary>The filtered projection to bind ItemsSource to.</summary>
    ICollectionView View { get; }
}
