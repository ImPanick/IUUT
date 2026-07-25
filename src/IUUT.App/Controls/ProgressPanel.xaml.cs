using System.Windows;
using System.Windows.Controls;

namespace IUUT.App.Controls;

/// <summary>
/// DE-2/3 primitive: the long-operation surface. See the XAML for rationale.
/// </summary>
public partial class ProgressPanel : UserControl
{
    /// <summary>What is running (e.g. "Repairing this profile…").</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ProgressPanel));

    /// <summary>The current phase line (e.g. "Restoring Characters.json from backup").</summary>
    public static readonly DependencyProperty PhaseProperty = DependencyProperty.Register(
        nameof(Phase), typeof(string), typeof(ProgressPanel));

    /// <summary>True when no meaningful percentage exists (most save operations).</summary>
    public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
        nameof(IsIndeterminate), typeof(bool), typeof(ProgressPanel), new PropertyMetadata(true));

    /// <summary>Progress 0–100 when determinate.</summary>
    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent), typeof(double), typeof(ProgressPanel));

    /// <summary>Creates the control.</summary>
    public ProgressPanel()
    {
        InitializeComponent();
    }

    /// <summary>What is running.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The current phase line.</summary>
    public string? Phase
    {
        get => (string?)GetValue(PhaseProperty);
        set => SetValue(PhaseProperty, value);
    }

    /// <summary>True when no meaningful percentage exists.</summary>
    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    /// <summary>Progress 0–100 when determinate.</summary>
    public double Percent
    {
        get => (double)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }
}
