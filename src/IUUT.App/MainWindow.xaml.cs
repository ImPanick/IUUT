using System.IO;
using System.Windows;
using IUUT.App.ViewModels;
using Microsoft.Win32;

namespace IUUT.App;

/// <summary>
/// The "Glass Console" Home window (master doc §10.2; docs/UI-DESIGN-CONCEPT.md). A WPF-UI
/// <c>FluentWindow</c> for frameless custom chrome; binds to <see cref="HomeViewModel"/>,
/// triggers the initial load, and hosts the folder-Browse + Lazy Max confirm dialogs. The
/// base type is fully qualified so <c>MessageBox</c> still resolves to <c>System.Windows</c>.
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly HomeViewModel _viewModel;

    /// <summary>Creates the window with its injected view-model and kicks off the first load.</summary>
    public MainWindow(HomeViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e) => await _viewModel.LoadAsync();

    private async void OnLazyMax(object sender, RoutedEventArgs e)
    {
        var plan = await _viewModel.PreviewSelectedAsync();
        if (plan is null || !plan.CanApply)
        {
            return; // the view-model already set an explanatory status message.
        }

        var result = plan.Result;
        var warnings = plan.Validation.Warnings.ToList();
        var warningLine = warnings.Count > 0
            ? "\n\n⚠ " + string.Join("\n⚠ ", warnings.Select(w => w.Message))
            : string.Empty;

        var message =
            $"Apply Lazy Max to “{_viewModel.SelectedSlot?.DisplayLabel}”?\n\n" +
            $"This updates {plan.Files.Count} files (a timestamped backup of each is created first):\n" +
            $"  • Characters maxed: {result?.CharactersMaxed}\n" +
            $"  • Account currencies: {result?.MetaResourcesMaxed};  workshop/prospect unlocks: {result?.WorkshopUnlocksTotal}\n" +
            $"  • Accolades added: {result?.AccoladesAdded}\n" +
            $"  • Bestiary groups: {result?.BestiaryGroupsTotal}\n\n" +
            "Make sure Icarus is closed, or sitting on the Main Menu, before applying." +
            warningLine;

        var confirm = MessageBox.Show(this, message, "Confirm Lazy Max", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm == MessageBoxResult.Yes)
        {
            await _viewModel.ApplyAsync(plan);
        }
        else
        {
            _viewModel.NotifyCancelled();
        }
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select your Icarus “Saved” folder",
        };

        if (Directory.Exists(_viewModel.SaveRoot))
        {
            dialog.InitialDirectory = _viewModel.SaveRoot;
        }

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.SaveRoot = dialog.FolderName;
            _viewModel.LoadCommand.Execute(null);
        }
    }
}
