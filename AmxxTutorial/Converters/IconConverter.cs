using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AmxxTutorial.Shared;

namespace AmxxTutorial.Converters;

public class IconConverter : IValueConverter
{
    public static readonly IconConverter Instance = new IconConverter();

    private IconConverter() { }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            return IconFactory.GetIcon(key);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
