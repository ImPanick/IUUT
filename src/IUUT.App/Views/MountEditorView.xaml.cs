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

        if (MessageBox.Show(message, "Apply mount changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
