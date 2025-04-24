using AmxxTutorial.Shared;
using Avalonia;
using Avalonia.Controls;

namespace AmxxTutorial.Controls
{
    public class LocalizedTextBlock : TextBlock
    {
        // 1. 正确注册 ResourceKeyProperty（不带 notifying 参数）
        public static readonly StyledProperty<string> ResourceKeyProperty =
            AvaloniaProperty.Register<LocalizedTextBlock, string>(
                nameof(ResourceKey),
                defaultValue: string.Empty);

        // 2. 在静态构造函数里添加属性变化回调
        static LocalizedTextBlock()
        {
            ResourceKeyProperty.Changed
                .AddClassHandler<LocalizedTextBlock>((ctrl, e) => ctrl.UpdateText());
        }

        // 3. CLR 包装属性
        public string ResourceKey
        {
            get => GetValue(ResourceKeyProperty);
            set => SetValue(ResourceKeyProperty, value);
        }

        public LocalizedTextBlock()
        {
            // 4. 订阅全局语言切换事件，切换时刷新文本
            ResLocalization.LocaleChanged += (_, __) => UpdateText();
        }

        // 5. 核心刷新逻辑
        private void UpdateText()
        {
            var appRes = Application.Current?.Resources;
            if (appRes != null
                && appRes.TryGetResource(ResourceKey, theme: null, out var val))
            {
                Text = val?.ToString() ?? ResourceKey;
            }
            else
            {
                Text = ResourceKey;
            }
        }
    }
}
