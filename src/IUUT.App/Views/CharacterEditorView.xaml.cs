using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>
/// The Characters &amp; Talents editor (master §11.5). Loads the save on display and hosts the
/// apply confirm dialog (kept out of the view-model, which stays WPF-free).
/// </summary>
public partial class CharacterEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public CharacterEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CharacterEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not CharacterEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Write the edited characters to Characters.json?\n\n" +
            "A timestamped backup of Characters.json is taken first, and the file is re-validated " +
            "after writing. A talent set to rank 0 is removed. The game clamps each talent to its " +
            "true max on load. Only changed files are written.";

        if (MessageBox.Show(message, "Apply character changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
