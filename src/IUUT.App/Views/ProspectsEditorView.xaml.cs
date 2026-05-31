using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Prospects editor (master §8.8). Loads on display; hosts the unstick confirm.</summary>
public partial class ProspectsEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public ProspectsEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProspectsEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnUnstick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProspectsEditorViewModel vm || vm.SelectedAssociation is null)
        {
            return;
        }

        var message =
            $"Remove the prospect association “{vm.SelectedAssociation}”?\n\n" +
            "Use this to free a character stuck on a phantom Continue-menu entry. A timestamped backup " +
            "of the slot file is taken first, and it is re-validated after writing. The prospect's world " +
            "save is not touched — only this character's claim on it.";

        if (MessageBox.Show(message, "Unstick prospect", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await vm.UnstickSelectedAsync();
        }
    }
}
