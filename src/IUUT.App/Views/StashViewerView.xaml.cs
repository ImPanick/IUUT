using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Orbital Stash viewer (master §8.6). Loads on display; hosts the remove confirm.</summary>
public partial class StashViewerView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public StashViewerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StashViewerViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnRemove(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StashViewerViewModel vm || vm.SelectedItem is null)
        {
            return;
        }

        var warning = vm.SelectedItem.IsReferenced
            ? "\n\n⚠ A loadout references this item — removing it will leave a dangling reference. " +
              "Consider restoring the loadout too."
            : "";
        var message =
            $"Remove “{vm.SelectedItem.Label}” from the orbital stash?\n\n" +
            "A timestamped backup of MetaInventory.json is taken first, and the file is re-validated " +
            "after writing." + warning;

        if (MessageBox.Show(message, "Remove stash item", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await vm.RemoveSelectedAsync();
        }
    }
}
