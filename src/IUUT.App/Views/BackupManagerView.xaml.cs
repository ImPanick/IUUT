using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Backup Manager (Tier 2). Loads on display; hosts the restore/prune confirms.</summary>
public partial class BackupManagerView : UserControl
{
    /// <summary>Creates the view and lists backups on first display.</summary>
    public BackupManagerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BackupManagerViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private void OnRestore(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BackupManagerViewModel vm || vm.SelectedBackup is null)
        {
            return;
        }

        var message =
            $"Restore “{vm.SelectedBackup.OriginalName}” from the backup taken {vm.SelectedBackup.Meta}?\n\n" +
            "The current file is backed up FIRST and the copy is atomic — a restore never destroys " +
            "state and can itself be undone from the new backup.";

        if (Dialogs.ConfirmDialog.Show(this, "Restore backup", message, confirmLabel: "RESTORE"))
        {
            vm.RestoreSelected();
        }
    }

    private void OnPrune(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BackupManagerViewModel vm)
        {
            return;
        }

        var message =
            $"Delete old backups, keeping the newest {BackupManagerViewModel.PruneKeepPerFile} per file?\n\n" +
            "Only files with the .iuut-backup- marker are touched — save files themselves are never " +
            "deleted. This cannot be undone.";

        if (Dialogs.ConfirmDialog.Show(this, "Prune old backups", message, confirmLabel: "PRUNE"))
        {
            vm.Prune();
        }
    }
}
