using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SlnDependencyStudio.Wpf.Converters.Xaml;

/// <summary>Converts a <see cref="bool"/> to <see cref="ScrollBarVisibility"/>.</summary>
public sealed class BoolToScrollBarVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
