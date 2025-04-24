using System;
using Avalonia.Markup.Xaml;
using AmxxTutorial.Shared;

namespace AmxxTutorial.MarkupExtensions
{
    public class IconExtension : MarkupExtension
    {
        // XAML 写法：{icon:Icon SemiIconSearch}
        public string Key { get; set; }

        public IconExtension() { }

        public IconExtension(string key)
        {
            Key = key;
        }

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            return IconFactory.GetIcon(Key);
        }
    }
}
