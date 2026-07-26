using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Loadouts viewer + recovery (master §8.7, Tier 2). Loads the save on first display.</summary>
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

    private async void OnInsureAll(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoadoutsViewerViewModel vm)
        {
            return;
        }

        var message =
            $"Set bInsured on {vm.UninsuredCount} loadout(s)?\n\n" +
            "This is the safe version of the community hand-edit that recovers gear stuck with an " +
            "offline host: only the one boolean per loadout changes — every other field round-trips " +
            "untouched. A timestamped backup of Loadouts.json is taken first, and the file is " +
            "re-validated after writing.";

        if (Dialogs.ConfirmDialog.Show(this, "Insure all loadouts", message, confirmLabel: "INSURE ALL"))
        {
            await vm.InsureAllAsync();
        }
    }

    private async void OnRestoreMissing(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoadoutsViewerViewModel vm)
        {
            return;
        }

        var message =
            $"Recreate {vm.RestorableCount} missing stash item(s) these loadouts reference?\n\n" +
            "Each is added to MetaInventory.json with the EXACT GUID and item row the loadout " +
            "expects, making the loadout whole again. Additive only — nothing is removed. A " +
            "timestamped backup is taken first, and the file is re-validated after writing.";

        if (Dialogs.ConfirmDialog.Show(this, "Restore missing items", message, confirmLabel: "RESTORE"))
        {
            await vm.RestoreMissingAsync();
        }
    }
}
