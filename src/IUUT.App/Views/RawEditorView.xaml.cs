using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Advanced / Raw editor (master §10.4). Loads on display; hosts the save confirm.</summary>
public partial class RawEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public RawEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RawEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RawEditorViewModel vm || vm.SelectedFile is null)
        {
            return;
        }

        var message =
            $"Save your edits to {vm.SelectedFile.Name}?\n\n" +
            "The text is re-parsed as JSON before writing — if it is malformed, nothing is written and " +
            "the original is kept. A timestamped backup is taken first. This is a direct edit: there are " +
            "no content checks beyond valid JSON, so save only what you intend.";

        if (MessageBox.Show(message, "Save raw JSON", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await vm.SaveAsync();
        }
    }
}
