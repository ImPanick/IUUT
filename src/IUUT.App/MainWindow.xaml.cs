using System.IO;
using System.Windows;
using IUUT.App.ViewModels;
using Microsoft.Win32;

namespace IUUT.App;

/// <summary>
/// The provisional Home window (master doc §10.2). Thin shell: it binds to
/// <see cref="HomeViewModel"/>, triggers the initial load, and hosts the folder-Browse
/// dialog. Final presentation is deferred to Phase 6 (plan §0).
/// </summary>
public partial class MainWindow : Window
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
