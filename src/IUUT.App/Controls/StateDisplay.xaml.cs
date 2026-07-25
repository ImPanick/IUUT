using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace IUUT.App.Controls;

/// <summary>Which of the three states <see cref="StateDisplay"/> shows.</summary>
public enum StateKind
{
    /// <summary>Nothing to show yet (no selection, no data).</summary>
    Empty,

    /// <summary>Work in progress (pulsing accent dot).</summary>
    Loading,

    /// <summary>Something broke; <see cref="StateDisplay.Detail"/> says where to go.</summary>
    Error,
}

/// <summary>
/// DE-2 primitive: the empty / loading / error state trio. See the XAML for rationale.
/// </summary>
public partial class StateDisplay : UserControl
{
    /// <summary>The state to render.</summary>
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(StateKind), typeof(StateDisplay),
        new PropertyMetadata(StateKind.Empty, (d, _) => ((StateDisplay)d).ApplyKind()));

    /// <summary>Headline (e.g. "No character selected").</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(StateDisplay));

    /// <summary>Supporting line — for errors, say where to go next (e.g. "Open Recovery").</summary>
    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail), typeof(string), typeof(StateDisplay));

    private static readonly DoubleAnimation _pulse = new(1.0, 0.35, TimeSpan.FromSeconds(0.6))
    {
        AutoReverse = true,
        RepeatBehavior = RepeatBehavior.Forever,
    };

    /// <summary>Creates the control.</summary>
    public StateDisplay()
    {
        InitializeComponent();
        ApplyKind();
    }

    /// <summary>The state to render.</summary>
    public StateKind Kind
    {
        get => (StateKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Headline.</summary>
    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Supporting line.</summary>
    public string? Detail
    {
        get => (string?)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    private void ApplyKind()
    {
        BusyDot.BeginAnimation(OpacityProperty, null);
        switch (Kind)
        {
            case StateKind.Loading:
                Glyph.Visibility = Visibility.Collapsed;
                BusyDot.Visibility = Visibility.Visible;
                Frame.BorderBrush = (Brush)FindResource("AccentBrush");
                BusyDot.BeginAnimation(OpacityProperty, _pulse);
                break;
            case StateKind.Error:
                Glyph.Visibility = Visibility.Visible;
                BusyDot.Visibility = Visibility.Collapsed;
                Glyph.Text = "✕";
                Glyph.Foreground = (Brush)FindResource("StateDangerBrush");
                Frame.BorderBrush = (Brush)FindResource("StateDangerBrush");
                break;
            default:
                Glyph.Visibility = Visibility.Visible;
                BusyDot.Visibility = Visibility.Collapsed;
                Glyph.Text = "◌";
                Glyph.Foreground = (Brush)FindResource("TextLowBrush");
                Frame.BorderBrush = (Brush)FindResource("GridLineStrongBrush");
                break;
        }
    }
}
