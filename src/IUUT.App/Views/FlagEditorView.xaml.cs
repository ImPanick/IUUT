using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Engine Flags editor (master §8.11). Loads on display; hosts the apply confirm.</summary>
public partial class FlagEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public FlagEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FlagEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FlagEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Write the edited engine flags to the binary flags file?\n\n" +
            "A timestamped backup is taken first, and the written bytes are re-decoded to validate " +
            "them before replacing the original. These are low-level engine unlock IDs — only apply " +
            "changes you understand.";

        if (MessageBox.Show(message, "Apply engine flags", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
