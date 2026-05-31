using System.Windows;
using System.Windows.Controls;
using IUUT.App.ViewModels;

namespace IUUT.App.Views;

/// <summary>The Custom editor shell page (master §10.3). Lists saves on first display.</summary>
public partial class CustomView : UserControl
{
    /// <summary>Creates the view and lists saves on first display.</summary>
    public CustomView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CustomViewModel vm && vm.Slots.Count == 0)
        {
            vm.LoadSavesCommand.Execute(null);
        }
    }
}
