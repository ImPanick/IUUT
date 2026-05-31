using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The read-only Loadouts viewer (master §8.7). Loads the save on first display.</summary>
public partial class LoadoutsViewerView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public LoadoutsViewerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoadoutsViewerViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }
}
