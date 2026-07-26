using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IUUT.App.Converters;

/// <summary>False → Visible, true → Collapsed (the inverse of the built-in converter).</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
