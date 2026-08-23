using AllOverIt.Extensions;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SlnDependencyStudio.Wpf.Converters.Xaml;

/// <summary>Converts a hex color string (with or without leading #) to a <see cref="Color"/>.</summary>
public sealed class HexToColorConverter : IValueConverter
{
    /// <summary>The singleton instance.</summary>
    public static readonly HexToColorConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value as string;

        if (hex.IsNullOrEmpty())
        {
            return Colors.Transparent;
        }

        hex = hex.TrimStart('#');

        try
        {
            return (Color)ColorConverter.ConvertFromString($"#{hex}");
        }
        catch
        {
            return Colors.Transparent;
        }
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
