using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Game Tuner page (master §20.1). Reads Engine.ini on display; hosts the apply confirm.</summary>
public partial class GameTunerView : UserControl
{
    /// <summary>Creates the view and reads Engine.ini on first display.</summary>
    public GameTunerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GameTunerViewModel vm && vm.Settings.Count == 0)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not GameTunerViewModel vm)
        {
            return;
        }

        const string message =
            "Write these settings to Engine.ini?\n\n" +
            "A timestamped backup of Engine.ini is taken first. These are standard Unreal console " +
            "variables — Icarus may ignore or clamp some of them. Restart the game for changes to take " +
            "effect. A bad Engine.ini is restorable from the backup.";

        if (MessageBox.Show(message, "Apply Game Tuning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
