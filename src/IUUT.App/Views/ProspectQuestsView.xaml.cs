using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>Prospect Quests (Tier 3). Loads on display; hosts the reset confirm.</summary>
public partial class ProspectQuestsView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public ProspectQuestsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProspectQuestsViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnResetMission(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProspectQuestsViewModel vm || vm.SelectedProspect is null)
        {
            return;
        }

        var message =
            $"Reset the mission in “{vm.SelectedProspect.Name}” so it can be replayed?\n\n" +
            "Only quest progress is cleared — the write is in-place and size-preserving, so items, " +
            "mounts, bases, and everything else in the world stay byte-identical. A timestamped " +
            "backup is taken first, and the file is re-validated after writing.";

        var changes = new List<Dialogs.ConfirmChange>
        {
            new(vm.SelectedProspect.Name, $"reset {vm.CompleteSteps} completed step(s) of {vm.Steps.Count}"),
        };

        if (Dialogs.ConfirmDialog.Show(this, "Reset mission", message, confirmLabel: "RESET", changes: changes))
        {
            await vm.ResetMissionAsync();
        }
    }
}
