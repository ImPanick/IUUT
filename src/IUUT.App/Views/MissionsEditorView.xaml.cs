using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Missions checklist (Tier 2). Loads on display; hosts the apply confirm.</summary>
public partial class MissionsEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public MissionsEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MissionsEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MissionsEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Complete the staged missions?\n\n" +
            "Each staged mission's Prospect_* unlock is added to Profile.json, together with every " +
            "prerequisite mission it depends on. This is additive — nothing is ever removed. A " +
            "timestamped backup is taken first, and the file is re-validated after writing.";

        if (Dialogs.ConfirmDialog.Show(this, "Complete staged missions", message, confirmLabel: "COMPLETE"))
        {
            await vm.ApplyAsync();
        }
    }
}
