using System.Windows;

namespace IUUT.App.Dialogs;

/// <summary>DE primitive: a one-line text prompt (see the XAML). Returns null on cancel.</summary>
public partial class PromptDialog : Window
{
    private PromptDialog(string title, string message, string initialValue, string confirmLabel)
    {
        InitializeComponent();
        Title = title;
        HeadingText.Text = title;
        MessageText.Text = message;
        ValueBox.Text = initialValue;
        ConfirmButton.Content = confirmLabel;
    }

    /// <summary>Shows the prompt modally; returns the trimmed non-empty value, or null on cancel.</summary>
    public static string? Show(
        DependencyObject host, string title, string message, string initialValue = "", string confirmLabel = "OK")
    {
        ArgumentNullException.ThrowIfNull(host);
        var dialog = new PromptDialog(title, message, initialValue, confirmLabel)
        {
            Owner = host as Window ?? Window.GetWindow(host),
        };
        dialog.ValueBox.SelectAll();
        dialog.ValueBox.Focus();

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var value = dialog.ValueBox.Text.Trim();
        return value.Length == 0 ? null : value;
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
