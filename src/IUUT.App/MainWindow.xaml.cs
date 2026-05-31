using IUUT.App.ViewModels;

namespace IUUT.App;

/// <summary>
/// The Glass Console shell window (UI-DESIGN-CONCEPT §8): a WPF-UI <c>FluentWindow</c> hosting the
/// <see cref="ShellViewModel"/>, which swaps page view-models into the content region. Navigates
/// Home on first show.
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly ShellViewModel _shell;

    /// <summary>Creates the shell window with its injected navigation view-model.</summary>
    public MainWindow(ShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        _shell = shell;
        InitializeComponent();
        DataContext = shell;
        Loaded += (_, _) => _shell.GoHome();
    }
}
