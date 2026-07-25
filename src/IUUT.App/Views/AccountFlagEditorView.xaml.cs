using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Account Flags checklist (#81, master §8.5). Loads on display; hosts the apply confirm.</summary>
public partial class AccountFlagEditorView : UserControl
{
    /// <summary>Creates the view and loads the save on first display.</summary>
    public AccountFlagEditorView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountFlagEditorViewModel vm && !vm.IsLoaded)
        {
            vm.LoadCommand.Execute(null);
        }
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AccountFlagEditorViewModel vm)
        {
            return;
        }

        const string message =
            "Write the account flag checklist to Profile.json?\n\n" +
            "A timestamped backup is taken first, and the file is re-parsed to validate it before " +
            "replacing the original. Unchecking a flag re-locks whatever it gates.";

        if (MessageBox.Show(message, "Apply account flags", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await vm.ApplyAsync();
        }
    }
}
