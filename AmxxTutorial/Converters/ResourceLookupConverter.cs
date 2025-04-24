using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AmxxTutorial.Converters
{
    public class ResourceLookupConverter : IValueConverter
    {
        // 全局可用的静态实例
        public static readonly ResourceLookupConverter Instance = new();

        private ResourceLookupConverter() { }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string key)
                return null;

            if (Application.Current?.Resources.TryGetResource(key, null, out var result) == true)
                return result;

            return key; // fallback：返回原始 key
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
