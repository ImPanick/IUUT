using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Field Guide (Tier 4). Loads on display; hosts the apply confirm.</summary>
public partial class FieldGuideView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public FieldGuideView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FieldGuideViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FieldGuideViewModel vm)
        {
            return;
        }

        const string message =
            "Write the edited field guide to Accolades.json / BestiaryData.json?\n\n" +
            "Statistics and checklists live in Accolades.json; fishing records live in " +
            "BestiaryData.json. Timestamped backups are taken first and each file is re-validated " +
            "after writing. Only changed files are written, and every part of these files IUUT " +
            "does not edit is preserved exactly as the game wrote it.";

        var changes = new List<Dialogs.ConfirmChange>
        {
            new("Accolades.json", $"{vm.Stats.Count(s => s.Value > 0):N0} stats · {vm.TaskLists.Sum(l => l.Tasks.Count(t => t.IsCompleted)):N0} tasks"),
            new("BestiaryData.json", $"{vm.Fish.Count(f => f.IsCaught):N0} fish records"),
        };

        if (Dialogs.ConfirmDialog.Show(this, "Apply field guide", message, changes: changes))
        {
            await vm.ApplyAsync();
        }
    }
}
