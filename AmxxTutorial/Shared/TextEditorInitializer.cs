using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using static AvaloniaEdit.Rendering.TextViewWeakEventManager;


namespace AmxxTutorial.Shared
{
    public static class TextEditorInitializer
    {
        private static readonly Dictionary<string, TextEditor> ContentTextEditor = new();
        private static readonly Dictionary<string, TextMate.Installation> TextMateInstallation = new();
        private static readonly Dictionary<string, FoldingManager> FoldingManagerInstallation = new();

        private static RegistryOptions? RegistryOptions;
        private static string LanguageScopeName = string.Empty;
        
        private static IRawTheme? DarkTheme;
        private static IRawTheme? LightTheme;

        private static BraceFoldingStrategy? BraceFoldingStrategy;

        public static Task InitializeRegistryAsync()
        {
            return Task.Run(() =>
            {
                RegistryOptions = new RegistryOptions(ThemeName.DarkPlus);
                LightTheme = RegistryOptions.LoadTheme(ThemeName.LightPlus);
                DarkTheme = RegistryOptions.LoadTheme(ThemeName.DarkPlus);

                Language cppLanguageRules = RegistryOptions.GetLanguageByExtension(".cpp");
                LanguageScopeName = RegistryOptions.GetScopeByLanguageId(cppLanguageRules.Id);

                BraceFoldingStrategy = new BraceFoldingStrategy();
            });
        }

        public static void InitializeTextEditor(TextEditor editor, IBrush background)
        {
            if (editor == null || string.IsNullOrEmpty(editor.Name))
                return;

            TextEditor _ContentTextEditor;
            if (ContentTextEditor.TryGetValue(editor.Name, out var value1))
            {
                _ContentTextEditor = value1;
            }
            else
            {
                _ContentTextEditor = editor;
                ContentTextEditor[_ContentTextEditor.Name] = _ContentTextEditor;
            }

            TextMate.Installation _TextMateInstallation;
            if (TextMateInstallation.TryGetValue(_ContentTextEditor.Name, out var value2))
            {
                _TextMateInstallation = value2;
            }
            else
            {
                _TextMateInstallation = _ContentTextEditor.InstallTextMate(RegistryOptions);
                _TextMateInstallation.AppliedTheme += (_, e) => TextMateInstallationOnAppliedTheme(_ContentTextEditor, e);
                _TextMateInstallation.SetGrammar(LanguageScopeName);

                TextMateInstallation[_ContentTextEditor.Name] = _TextMateInstallation;
            }

            FoldingManager _FoldingManager;
            if (FoldingManagerInstallation.TryGetValue(_ContentTextEditor.Name, out var value3))
            {
                _FoldingManager = value3;
            }
            else
            {
                _FoldingManager = FoldingManager.Install(_ContentTextEditor.TextArea);
                FoldingManagerInstallation[_ContentTextEditor.Name] = _FoldingManager;
            }

            _ContentTextEditor.Background = Brushes.Transparent;
            _ContentTextEditor.TextArea.Background = background;

            _ContentTextEditor.Options.AllowToggleOverstrikeMode = true;
            _ContentTextEditor.Options.EnableTextDragDrop = true;
            _ContentTextEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(_ContentTextEditor.Options);
            _ContentTextEditor.TextArea.RightClickMovesCaret = true;

            // Open a new file?
            _ContentTextEditor.DocumentChanged += (_, _) =>
            {
                if (_FoldingManager != null)
                {
                    _FoldingManager.Clear();
                    FoldingManager.Uninstall(_FoldingManager);

                    _ContentTextEditor.TextArea.Document.Changed -= (_, _) =>
                    {
                        BraceFoldingStrategy.UpdateFoldings(_FoldingManager, _ContentTextEditor.Document);
                    };
                }

                _FoldingManager = FoldingManager.Install(_ContentTextEditor.TextArea);
                FoldingManagerInstallation[_ContentTextEditor.Name] = _FoldingManager;
                BraceFoldingStrategy.UpdateFoldings(_FoldingManager, _ContentTextEditor.Document);

                _ContentTextEditor.TextArea.Document.Changed += (_, _) =>
                {
                    BraceFoldingStrategy.UpdateFoldings(_FoldingManager, _ContentTextEditor.Document);
                };
            };

            // Modify a file.
            _ContentTextEditor.TextArea.Document.Changed += (_, _) =>
            {
                BraceFoldingStrategy.UpdateFoldings(_FoldingManager, _ContentTextEditor.Document);
            };
        }

        public static void TextEditorTextAreaCommand(TextEditor? Editor, string e)
        {
            if (Editor == null)
                return;

            if (Editor.Text != null)
            {
                if (e.StartsWith("ResetToDefault"))
                {
                    Editor.ScrollToHome();

                    //if (e.Substring("ResetToDefault".Length) == "2")
                    //    ViewModel.ShowNotification();

                    Editor.SelectionStart = 0;
                    Editor.SelectionLength = 0;
                }
                else if (e == "SelectAll")
                {
                    Editor.SelectionStart = 0;
                    Editor.SelectionLength = Editor.Document.TextLength;
                }
            }
        }

        public static void TextEditorRegulateTheme(TextEditor? Editor)
        {
            if (Editor == null)
                return;

            if (TextMateInstallation.TryGetValue(Editor.Name, out var value))
            {
                if (!Globals.GetCurTheme())
                    value.SetTheme(LightTheme);
                else
                    value.SetTheme(DarkTheme);

                Globals.OnThemeChanged += (sender, isLightTheme) =>
                {
                    if (isLightTheme)
                        value.SetTheme(LightTheme);
                    else
                        value.SetTheme(DarkTheme);
                };
            }
        }

        private static void TextMateInstallationOnAppliedTheme(TextEditor? Editor, TextMate.Installation e)
        {
            if (Editor == null)
                return;

            ApplyBrushAction(e, "editor.background", brush => Editor.Background = brush);
            ApplyBrushAction(e, "editor.foreground", brush => Editor.Foreground = brush);

            if (!ApplyBrushAction(e, "editor.selectionBackground",
                    brush => Editor.TextArea.SelectionBrush = brush))
            {
                /*if (Application.Current!.TryGetResource("TextAreaSelectionBrush", out var resourceObject))
                {
                    if (resourceObject is IBrush brush)
                    {
                        Editor.TextArea.SelectionBrush = brush;
                    }
                }*/
            }

            if (!ApplyBrushAction(e, "editor.lineHighlightBackground",
                    brush =>
                    {
                        Editor.TextArea.TextView.CurrentLineBackground = brush;
                        Editor.TextArea.TextView.CurrentLineBorder = new Pen(brush); // Todo: VS Code didn't seem to have a border but it might be nice to have that option. For now just make it the same..
                    }))
            {
                Editor.TextArea.TextView.SetDefaultHighlightLineColors();
            }

            //Todo: looks like the margin doesn't have a active line highlight, would be a nice addition
            if (!ApplyBrushAction(e, "editorLineNumber.foreground",
                    brush => Editor.LineNumbersForeground = brush))
            {
                Editor.LineNumbersForeground = Editor.Foreground;
            }
        }
        private static bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
        {
            if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
                return false;

            if (!Color.TryParse(colorString, out Color color))
                return false;

            var colorBrush = new SolidColorBrush(color);
            applyColorAction(colorBrush);
            return true;
        }
    }

    public class BraceFoldingStrategy
    {
        /// <summary>
        /// Gets/Sets the opening brace. The default value is '{'.
        /// </summary>
        public char OpeningBrace { get; set; }

        /// <summary>
        /// Gets/Sets the closing brace. The default value is '}'.
        /// </summary>
        public char ClosingBrace { get; set; }

        /// <summary>
        /// Creates a new BraceFoldingStrategy.
        /// </summary>
        public BraceFoldingStrategy()
        {
            this.OpeningBrace = '{';
            this.ClosingBrace = '}';
        }

        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            int firstErrorOffset;
            IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }

        /// <summary>
        /// Create <see cref="NewFolding"/>s for the specified document.
        /// </summary>
        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            return CreateNewFoldings(document);
        }

        /// <summary>
        /// Create <see cref="NewFolding"/>s for the specified document.
        /// </summary>
        public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();

            Stack<int> startOffsets = new Stack<int>();
            int lastNewLineOffset = 0;
            char openingBrace = this.OpeningBrace;
            char closingBrace = this.ClosingBrace;
            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);
                if (c == openingBrace)
                {
                    startOffsets.Push(i);
                }
                else if (c == closingBrace && startOffsets.Count > 0)
                {
                    int startOffset = startOffsets.Pop();
                    // don't fold if opening and closing brace are on the same line
                    if (startOffset < lastNewLineOffset)
                    {
                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                    }
                }
                else if (c == '\n' || c == '\r')
                {
                    lastNewLineOffset = i + 1;
                }
            }
            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}
