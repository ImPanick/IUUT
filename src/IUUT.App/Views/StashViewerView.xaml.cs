using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Orbital Stash builder (master §8.6). Loads on display; hosts the apply confirm.</summary>
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

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StashViewerViewModel vm)
        {
            return;
        }

        var referenced = vm.Items.Any(i => i.IsReferenced);
        var warning = referenced
            ? "\n\n⚠ Some items are referenced by loadouts — make sure you didn't remove one a loadout still needs."
            : "";
        var message =
            "Write the staged stash to MetaInventory.json?\n\n" +
            "A timestamped backup of MetaInventory.json is taken first, and the file is re-validated " +
            "after writing. Added items get fresh GUIDs; stacks are capped at 100." + warning;

        if (MessageBox.Show(message, "Apply stash changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
