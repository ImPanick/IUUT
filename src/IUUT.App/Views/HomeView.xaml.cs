using System.IO;
using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;
using Microsoft.Win32;

namespace IUUT.App.Views;

/// <summary>
/// The Home page (master §10.2). Thin code-behind: triggers the first load and hosts the
/// folder-Browse + Lazy Max confirm dialogs. The view-model arrives via <see cref="FrameworkElement.DataContext"/>
/// (set by the shell's DataTemplate); the base type is implicit so <c>MessageBox</c> stays <c>System.Windows</c>.
/// </summary>
public partial class HomeView : UserControl
{
    /// <summary>Creates the view and triggers the initial load on display.</summary>
    public HomeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is HomeViewModel vm)
        {
            await vm.LoadAsync();
        }
    }

    private async void OnLazyMax(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HomeViewModel vm)
        {
            return;
        }

        var plan = await vm.PreviewSelectedAsync();
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
            $"Apply Lazy Max to “{vm.SelectedSlot?.DisplayLabel}”?\n\n" +
            $"This updates {plan.Files.Count} files (a timestamped backup of each is created first):\n" +
            $"  • Characters maxed: {result?.CharactersMaxed}\n" +
            $"  • Account currencies: {result?.MetaResourcesMaxed};  workshop/prospect unlocks: {result?.WorkshopUnlocksTotal}\n" +
            $"  • Accolades added: {result?.AccoladesAdded}\n" +
            $"  • Bestiary groups: {result?.BestiaryGroupsTotal}\n\n" +
            "Make sure Icarus is closed, or sitting on the Main Menu, before applying." +
            warningLine;

        var confirm = MessageBox.Show(message, "Confirm Lazy Max", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync(plan);
        }
        else
        {
            vm.NotifyCancelled();
        }
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HomeViewModel vm)
        {
            return;
        }

        var dialog = new OpenFolderDialog { Title = "Select your Icarus “Saved” folder" };
        if (Directory.Exists(vm.SaveRoot))
        {
            dialog.InitialDirectory = vm.SaveRoot;
        }

        if (dialog.ShowDialog() == true)
        {
            vm.SaveRoot = dialog.FolderName;
            vm.LoadCommand.Execute(null);
        }
    }
}
