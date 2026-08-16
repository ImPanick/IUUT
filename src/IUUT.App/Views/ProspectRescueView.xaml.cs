using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>
/// Prospect Rescue (RESCUE). Loads on display; hosts the three rescue confirms. Each one states
/// what will happen in the user's own terms before anything is written.
/// </summary>
public partial class ProspectRescueView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public ProspectRescueView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProspectRescueViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnBringGrave(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProspectRescueViewModel vm || !vm.CanBringGrave)
        {
            return;
        }

        if (Confirm(
                "Bring the body here",
                vm.BringGraveSummary,
                "BRING IT",
                vm,
                $"move the grave next to player {vm.SelectedCharacter!.Character.MaskedPlayerId}"))
        {
            await vm.BringGraveToCharacterAsync();
        }
    }

    private async void OnGoToGrave(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProspectRescueViewModel vm || !vm.CanGoToGrave)
        {
            return;
        }

        if (Confirm(
                "Send the character to their body",
                vm.GoToGraveSummary,
                "SEND",
                vm,
                $"move player {vm.SelectedCharacter!.Character.MaskedPlayerId} to the grave"))
        {
            await vm.SendCharacterToGraveAsync();
        }
    }

    private async void OnRevive(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProspectRescueViewModel vm || !vm.CanRevive)
        {
            return;
        }

        if (Confirm(
                "Revive the character",
                vm.ReviveSummary,
                "REVIVE",
                vm,
                $"revive player {vm.SelectedCharacter!.Character.MaskedPlayerId}"))
        {
            await vm.ReviveCharacterAsync();
        }
    }

    private bool Confirm(string title, string summary, string confirmLabel, ProspectRescueViewModel vm, string change)
    {
        var message = summary + "\n\n" +
            "The write is in-place and size-preserving, so nothing else in the world changes. A " +
            "timestamped backup is taken first, and the file is re-validated after writing.\n\n" +
            "Everyone must be OUT of this prospect — a running session will overwrite the file when it saves.";

        var changes = new List<Dialogs.ConfirmChange>
        {
            new(vm.SelectedProspect?.Name ?? "prospect", change),
        };

        return Dialogs.ConfirmDialog.Show(this, title, message, confirmLabel: confirmLabel, changes: changes);
    }
}
