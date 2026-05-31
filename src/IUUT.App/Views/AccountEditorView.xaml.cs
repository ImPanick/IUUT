using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>
/// The Account &amp; Currencies editor (master §11.6). Loads the save on display and hosts the
/// apply / unlock confirm dialogs (kept out of the view-model, which stays WPF-free).
/// </summary>
public partial class AccountEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public AccountEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApplyCurrencies(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AccountEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Write the edited currency amounts to Profile.json?\n\n" +
            "A timestamped backup of Profile.json is taken first, and the file is re-validated after " +
            "writing. The game clamps each currency to its own maximum on load. Only changed files are written.";

        if (MessageBox.Show(message, "Apply currency changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await vm.ApplyCurrenciesAsync();
        }
    }

    private async void OnUnlockAllBlueprints(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AccountEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Unlock every workshop and prospect blueprint?\n\n" +
            "This adds all known blueprint unlocks (rank 1) to Profile.json. A timestamped backup is " +
            "taken first and the file is re-validated. This is additive — it never removes existing unlocks.";

        if (MessageBox.Show(message, "Unlock all blueprints", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await vm.UnlockAllBlueprintsAsync();
        }
    }
}
