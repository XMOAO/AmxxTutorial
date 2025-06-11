using System;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Utils;

namespace AmxxTutorial.Shared
{
    public class TextEditorBindingBehavior : Behavior<TextEditor>
    {
        private TextEditor _textEditor = null;

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<TextEditorBindingBehavior, string>(nameof(Text));

        public static readonly StyledProperty<bool> HighlightCurrentLineProperty =
            AvaloniaProperty.Register<TextEditorBindingBehavior, bool>(nameof(HighlightCurrentLine));

        public static readonly StyledProperty<bool> ShowTabsProperty =
            AvaloniaProperty.Register<TextEditorBindingBehavior, bool>(nameof(ShowTabs));

        public static readonly StyledProperty<bool> ShowEndOfLineProperty =
            AvaloniaProperty.Register<TextEditorBindingBehavior, bool>(nameof(ShowEndOfLine));

        public bool HighlightCurrentLine
        {
            get => GetValue(HighlightCurrentLineProperty);
            set => SetValue(HighlightCurrentLineProperty, value);
        }

        public bool ShowTabs
        {
            get => GetValue(ShowTabsProperty);
            set => SetValue(ShowTabsProperty, value);
        }

        public bool ShowEndOfLine
        {
            get => GetValue(ShowEndOfLineProperty);
            set => SetValue(ShowEndOfLineProperty, value);
        }

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject is TextEditor textEditor)
            {
                _textEditor = textEditor;
                _textEditor.TextChanged += TextChanged;
                this.GetObservable(TextProperty).Subscribe(TextPropertyChanged);

                this.GetObservable(HighlightCurrentLineProperty).Subscribe(highlight =>
                {
                    _textEditor.Options.HighlightCurrentLine = highlight;
                });
                this.GetObservable(ShowTabsProperty).Subscribe(tabs =>
                {
                    _textEditor.Options.ShowTabs = tabs;
                });
                this.GetObservable(ShowEndOfLineProperty).Subscribe(endofline =>
                {
                    _textEditor.Options.ShowEndOfLine = endofline;
                });
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (_textEditor != null)
            {
                _textEditor.TextChanged -= TextChanged;
            }
        }

        private void TextChanged(object sender, EventArgs eventArgs)
        {
            if (_textEditor != null && _textEditor.Document != null)
            {
                Text = _textEditor.Document.Text;
            }
        }

        private void TextPropertyChanged(string text)
        {
            if (_textEditor != null && _textEditor.Document != null && text != null)
            {
                var caretOffset = _textEditor.CaretOffset;
                _textEditor.Document.Text = text;
                _textEditor.CaretOffset = caretOffset;
            }
        }
    }
}
