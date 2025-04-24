using Avalonia.Data.Converters;

namespace AmxxTutorial.Converters;

public static class StringConverter
{
    public static readonly IValueConverter StringEquals =
        new FuncValueConverter<object, string, bool>(
            (value, param) =>
                value?.ToString() == param
        );

    public static readonly IValueConverter StringNotEquals =
       new FuncValueConverter<object, string, bool>(
           (value, param) =>
               value?.ToString() != param
       );

    // 可以继续添加其他字符串比较转换器
    public static readonly IValueConverter StringContains =
        new FuncValueConverter<object, string, bool>(
            (value, param) =>
                value?.ToString()?.Contains(param) ?? false
        );
}

