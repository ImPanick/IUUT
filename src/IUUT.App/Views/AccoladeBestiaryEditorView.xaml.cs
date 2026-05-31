using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>
/// The Accolades &amp; Bestiary editor (master §11.7). Loads the save on display and hosts the
/// apply confirm dialog (kept out of the view-model, which stays WPF-free).
/// </summary>
public partial class AccoladeBestiaryEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public AccoladeBestiaryEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccoladeBestiaryEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AccoladeBestiaryEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Write the edited accolades and bestiary to Accolades.json / BestiaryData.json?\n\n" +
            "Timestamped backups are taken first, and each file is re-validated after writing. A " +
            "creature group set to 0 points is removed from tracking. Only changed files are written.";

        if (MessageBox.Show(message, "Apply accolades & bestiary", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
