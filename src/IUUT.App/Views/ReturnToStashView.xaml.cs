using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>Return to Stash (Tier 2 RESCUE). Loads on display; hosts the return confirm.</summary>
public partial class ReturnToStashView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public ReturnToStashView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReturnToStashViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnReturnAll(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReturnToStashViewModel vm || vm.SelectedProspect is null)
        {
            return;
        }

        var staying = vm.StayingQuantity > 0
            ? $"\n\n{vm.StayingQuantity:N0} other item(s) — resources, building pieces, food — can't go to the orbital " +
              "stash (the game only keeps workshop gear there), so they stay exactly where they are in the world, untouched."
            : "";

        var message =
            $"Return the workshop items in “{vm.SelectedProspect.Name}” to your orbital stash?\n\n" +
            "Each comes back as it was in the world — its own stack and its own wear. They are removed from the " +
            "prospect's world save and added to MetaInventory.json. The stash is written FIRST, so a mid-operation " +
            "failure can only duplicate items (recoverable from backup), never lose them. Both files are backed up " +
            "and re-validated." + staying;

        var changes = new List<Dialogs.ConfirmChange>
        {
            new("MetaInventory.json", $"+{vm.ReturnableQuantity:N0} workshop item(s)"),
            new(vm.SelectedProspect.Name, $"-{vm.ReturnableSlots:N0} slot(s)"),
        };

        if (Dialogs.ConfirmDialog.Show(this, "Return workshop items to stash", message, confirmLabel: "RETURN", changes: changes))
        {
            await vm.ReturnAllAsync();
        }
    }
}
