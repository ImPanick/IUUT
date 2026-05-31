using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>
/// The Broken Save Recovery page (master §10.1, §11.3). Thin code-behind: lists saves on first
/// show and hosts the repair confirm dialog; the work is in <see cref="RecoveryViewModel"/>.
/// </summary>
public partial class RecoveryView : UserControl
{
    /// <summary>Creates the view and lists saves on first display.</summary>
    public RecoveryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RecoveryViewModel vm && vm.Slots.Count == 0)
        {
            vm.LoadSavesCommand.Execute(null);
        }
    }

    private async void OnRepair(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RecoveryViewModel vm || !vm.CanRepair)
        {
            return;
        }

        var message =
            $"Repair “{vm.SelectedSlot?.DisplayLabel}”?\n\n" +
            "A full-folder backup (zip) of this profile is taken first, then each broken file is " +
            "restored from its newest clean backup, or rebuilt from a template when no backup exists.\n\n" +
            "Close Icarus, or sit on the Main Menu, before applying.";

        var confirm = MessageBox.Show(message, "Confirm Recovery", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm == MessageBoxResult.Yes)
        {
            await vm.RepairAsync();
        }
    }
}
