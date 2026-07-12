using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SlnDependencyStudio.Wpf.Converters.Xaml;

/// <summary>Converts a <see cref="bool"/> to <see cref="TextWrapping"/>.</summary>
public sealed class BoolToTextWrappingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
