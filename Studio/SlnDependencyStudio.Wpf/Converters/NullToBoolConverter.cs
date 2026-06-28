using System.Globalization;
using System.Windows.Data;

namespace SlnDependencyStudio.Wpf.Converters;

/// <summary>Returns <see langword="true"/> when the bound value is not <see langword="null"/>,
/// and <see langword="false"/> when it is.</summary>
public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is not null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
