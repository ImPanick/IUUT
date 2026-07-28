using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Mounts editor (master §8.10). Loads on display; hosts the apply confirm.</summary>
public partial class MountEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public MountEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MountEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnRenameDeployed(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MountEditorViewModel vm ||
            (sender as FrameworkElement)?.DataContext is not DeployedMountViewModel mount)
        {
            return;
        }

        var newName = Dialogs.PromptDialog.Show(
            this,
            "Rename deployed mount",
            $"New name for “{mount.Name}” (deployed in {mount.ProspectName}).\n\n" +
            "The prospect's world save is backed up first; only the name changes — stats, " +
            "inventory, and everything else in the world stay byte-identical.",
            initialValue: mount.Name,
            confirmLabel: "RENAME");

        if (newName is not null && !string.Equals(newName, mount.Name, StringComparison.Ordinal))
        {
            await vm.RenameDeployedAsync(mount, newName);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MountEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Write the edited mounts to Mounts.json?\n\n" +
            "A timestamped backup of Mounts.json is taken first, and the file is re-validated after " +
            "writing. Only the name and level (display fields) change — the mount's stats blob is preserved.";

        if (Dialogs.ConfirmDialog.Show(this, "Apply mount changes", message))
        {
            await vm.ApplyAsync();
        }
    }
}
