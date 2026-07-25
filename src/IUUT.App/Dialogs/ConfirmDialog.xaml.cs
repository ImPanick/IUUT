using System.Windows;

namespace IUUT.App.Dialogs;

/// <summary>One planned change shown in the confirm dialog's diff list.</summary>
public sealed record ConfirmChange(string File, string Delta);

/// <summary>
/// DE-2 primitive: the app's one confirmation dialog — heading, explanation, an optional
/// per-file change list, a note line, and Cancel / confirm. See the XAML for rationale.
/// </summary>
public partial class ConfirmDialog : Window
{
    private ConfirmDialog(string title, string message, string confirmLabel, IReadOnlyList<ConfirmChange>? changes, string? note)
    {
        InitializeComponent();
        Title = title;
        HeadingText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmLabel;

        if (changes is { Count: > 0 })
        {
            ChangesList.ItemsSource = changes;
            ChangesPanel.Visibility = Visibility.Visible;
        }

        if (string.IsNullOrEmpty(note))
        {
            NoteText.Visibility = Visibility.Collapsed;
        }
        else
        {
            NoteText.Text = note;
        }
    }

    /// <summary>
    /// Shows the dialog modally over <paramref name="host"/>'s window. Returns true when the
    /// user confirmed. Drop-in replacement for the old Yes/No MessageBox confirms.
    /// </summary>
    public static bool Show(
        DependencyObject host,
        string title,
        string message,
        string confirmLabel = "APPLY",
        IReadOnlyList<ConfirmChange>? changes = null,
        string? note = null)
    {
        ArgumentNullException.ThrowIfNull(host);
        var dialog = new ConfirmDialog(title, message, confirmLabel, changes, note)
        {
            Owner = host as Window ?? Window.GetWindow(host),
        };
        return dialog.ShowDialog() == true;
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
