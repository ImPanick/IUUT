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

        var message =
            $"Return every trapped item from “{vm.SelectedProspect.Name}” to the orbital stash?\n\n" +
            "The items are removed from the prospect's world save and added to MetaInventory.json. " +
            "The stash is written FIRST, so a mid-operation failure can only duplicate items " +
            "(recoverable from backup), never lose them. Both files are backed up and re-validated.";

        var changes = new List<Dialogs.ConfirmChange>
        {
            new("MetaInventory.json", $"+{vm.TrappedItems.Sum(t => t.TotalQuantity):N0} item(s)"),
            new(vm.SelectedProspect.Name, $"-{vm.TrappedItems.Sum(t => t.SlotCount):N0} slot(s)"),
        };

        if (Dialogs.ConfirmDialog.Show(this, "Return items to stash", message, confirmLabel: "RETURN ALL", changes: changes))
        {
            await vm.ReturnAllAsync();
        }
    }
}
