using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IUUT.App.Services;

namespace IUUT.App.Controls;

/// <summary>
/// DE-2 primitive: a search box over a virtualized item list, driven by a
/// <see cref="FilteredView{T}"/>. Replaces the per-view search wire-ups from Tier 1;
/// future checklists (missions, backup manager) get search for free.
/// </summary>
public partial class FilteredListBox : UserControl
{
    /// <summary>The filtered view to search and render.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(IFilteredView), typeof(FilteredListBox));

    /// <summary>Tooltip on the search box (e.g. "Search talents (name or RowName)").</summary>
    public static readonly DependencyProperty SearchHintProperty = DependencyProperty.Register(
        nameof(SearchHint), typeof(string), typeof(FilteredListBox));

    /// <summary>Row template.</summary>
    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(FilteredListBox));

    /// <summary>Creates the control.</summary>
    public FilteredListBox()
    {
        InitializeComponent();
    }

    /// <summary>The filtered view to search and render.</summary>
    public IFilteredView? Source
    {
        get => (IFilteredView?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Tooltip on the search box.</summary>
    public string? SearchHint
    {
        get => (string?)GetValue(SearchHintProperty);
        set => SetValue(SearchHintProperty, value);
    }

    /// <summary>Row template.</summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && Source is { } source)
        {
            source.SearchText = "";
            e.Handled = true;
        }
    }
}
