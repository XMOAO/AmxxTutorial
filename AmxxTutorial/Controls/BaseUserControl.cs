using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmxxTutorial.Controls
{
    public partial class BaseUserControl : UserControl
    {
        public static readonly StyledProperty<Control?> PageExtraContentProperty =
        AvaloniaProperty.Register<BaseUserControl, Control?>(nameof(PageExtraContent));

        public Control? PageExtraContent
        {
            get => GetValue(PageExtraContentProperty);
            set => SetValue(PageExtraContentProperty, value);
        }

        public BaseUserControl()
        {

        }

        protected void InstanceExtraContent<T>(Action<T>? initializer = null) where T : Control, new()
        {
            var control = new T();
            initializer?.Invoke(control);
            PageExtraContent = control;
        }
    }
}
