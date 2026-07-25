using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Custom editor shell page (master §10.3). Lists saves on first display.</summary>
public partial class CustomView : UserControl
{
    /// <summary>Creates the view and lists saves on first display.</summary>
    public CustomView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CustomViewModel vm)
        {
            return;
        }

        // Dirty guard dialog (Tier 1): asked before a category/slot switch discards staged edits.
        vm.ConfirmDiscard ??= message => Dialogs.ConfirmDialog.Show(
            this,
            "Discard staged changes?",
            message,
            confirmLabel: "DISCARD");

        // Reload on first display OR when the shared save root changed since our last load
        // (singleton VM — an empty-only guard would ignore a root browsed on Home; review finding).
        if (vm.Slots.Count == 0 || vm.IsSaveRootStale)
        {
            vm.LoadSavesCommand.Execute(null);
        }
    }
}
