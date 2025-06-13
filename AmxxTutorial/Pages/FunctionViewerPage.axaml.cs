using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using AvaloniaEdit.Indentation.CSharp;

using Ursa.Common;
using Ursa.Controls.Options;
using Ursa.Controls;

using System;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

using AmxxTutorial.Views;
using AmxxTutorial.Pages.Dialogs;
using AmxxTutorial.ViewModels;
using AmxxTutorial.Shared;

using Avalonia.Controls.Notifications;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;
using Avalonia.Input;
using AvaloniaEdit.Document;

namespace AmxxTutorial.Pages
{
    public partial class FunctionViewerPage : UserControl
    {
        private TextMate.Installation TextMateInstallation;
        private RegistryOptions RegistryOptions;

        FunctionViewerViewModel ViewModel;
        bool bInitiated = false;

        public FunctionViewerPage()
        {
            InitializeComponent();

            DataContext = ViewModel = new FunctionViewerViewModel();

            RegistryOptions = new RegistryOptions(ThemeName.DarkPlus);
            TextMateInstallation = ContentTextEditor.InstallTextMate(RegistryOptions);
            TextMateInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;

            Language cppLanguageRules = RegistryOptions.GetLanguageByExtension(".cpp");
            TextMateInstallation.SetGrammar(RegistryOptions.GetScopeByLanguageId(cppLanguageRules.Id));

            this.Loaded += async (_, _) => await InitializeExtraAsync();
        }

        private void textEditor_TextArea_TextEntered(object sender, PointerEventArgs e)
        {
            var pos = ContentTextEditor.GetPositionFromPoint(e.GetPosition(ContentTextEditor));
            if (pos == null) return;

            var doc = ContentTextEditor.Document;
            string word = GetWordAt(doc, pos.Value.Line, pos.Value.Column);
            if (string.IsNullOrEmpty(word)) return;

        }
        private string GetWordAt(TextDocument doc, int line, int col)
        {
            var offset = doc.GetOffset(line, col);
            var text = doc.Text;
            if (offset < 0 || offset >= text.Length) return null;

            int start = offset, end = offset;
            while (start > 0 && IsIdentifierChar(text[start - 1])) start--;
            while (end < text.Length && IsIdentifierChar(text[end])) end++;
            return doc.GetText(start, end - start);
        }

        private bool IsIdentifierChar(char c) =>
            char.IsLetterOrDigit(c) || c == '_' || c == '@';

        public async Task InitializeExtraAsync()
        {
            if (bInitiated)
                return;

            LoadingArea.IsLoading = true;

            ContentTextEditor.Background = Brushes.Transparent;
            ContentTextEditor.TextArea.Background = this.Background;
            ContentTextEditor.TextArea.TextView.PointerHover += textEditor_TextArea_TextEntered;
            //ContentTextEditor.TextArea.TextView

            ContentTextEditor.Options.AllowToggleOverstrikeMode = true;
            ContentTextEditor.Options.EnableTextDragDrop = true;
            ContentTextEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy(ContentTextEditor.Options);
            ContentTextEditor.TextArea.RightClickMovesCaret = true;

            await ViewModel.InitializeIncFilesAsync();
            await Task.Delay(new Random().Next(200, 300));

            ViewModel.TextAreaCommandRequested += (sender, args) =>
            {
                if (ContentTextEditor.Text != null)
                {
                    if (args.StartsWith("ResetToDefault"))
                    {
                        ContentTextEditor.ScrollToHome();

                        if(args.Substring("ResetToDefault".Length) == "2")
                            ViewModel.ShowNotification();

                        ContentTextEditor.SelectionStart = 0;
                        ContentTextEditor.SelectionLength = 0;
                    }
                    else if (args == "SelectAll")
                    {
                        ContentTextEditor.SelectionStart = 0;
                        ContentTextEditor.SelectionLength = ContentTextEditor.Document.TextLength;
                    }
                }
            };

            LoadingArea.IsLoading = false;

            bInitiated = true;
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var GetTopLevel = TopLevel.GetTopLevel(this);

            MainViewModel? GetMainViewModel = (MainViewModel)((GetTopLevel as MainWindow)?.Content as MainView)?.DataContext;
            if(GetMainViewModel is not null)
            {
                if (!GetMainViewModel.GetCurTheme())
                    TextMateInstallation.SetTheme(RegistryOptions.LoadTheme(ThemeName.LightPlus));
                else
                    TextMateInstallation.SetTheme(RegistryOptions.LoadTheme(ThemeName.DarkPlus));

                GetMainViewModel.OnThemeChanged += (sender, isLightTheme) =>
                {
                    if (isLightTheme)
                        TextMateInstallation.SetTheme(RegistryOptions.LoadTheme(ThemeName.LightPlus));
                    else
                        TextMateInstallation.SetTheme(RegistryOptions.LoadTheme(ThemeName.DarkPlus));
                };
            }

            WindowNotificationManager.TryGetNotificationManager(GetTopLevel, out var manager);

            if (manager is not null)
                ViewModel.NotificationManager = manager;
        }
        private void TextMateInstallationOnAppliedTheme(object sender, TextMate.Installation e)
        {
            ApplyBrushAction(e, "editor.background", brush => ContentTextEditor.Background = brush);
            ApplyBrushAction(e, "editor.foreground", brush => ContentTextEditor.Foreground = brush);

            if (!ApplyBrushAction(e, "editor.selectionBackground",
                    brush => ContentTextEditor.TextArea.SelectionBrush = brush))
            {
                if (Application.Current!.TryGetResource("TextAreaSelectionBrush", out var resourceObject))
                {
                    if (resourceObject is IBrush brush)
                    {
                        ContentTextEditor.TextArea.SelectionBrush = brush;
                    }
                }
            }

            if (!ApplyBrushAction(e, "editor.lineHighlightBackground",
                    brush =>
                    {
                        ContentTextEditor.TextArea.TextView.CurrentLineBackground = brush;
                        ContentTextEditor.TextArea.TextView.CurrentLineBorder = new Pen(brush); // Todo: VS Code didn't seem to have a border but it might be nice to have that option. For now just make it the same..
                    }))
            {
                ContentTextEditor.TextArea.TextView.SetDefaultHighlightLineColors();
            }

            //Todo: looks like the margin doesn't have a active line highlight, would be a nice addition
            if (!ApplyBrushAction(e, "editorLineNumber.foreground",
                    brush => ContentTextEditor.LineNumbersForeground = brush))
            {
                ContentTextEditor.LineNumbersForeground = ContentTextEditor.Foreground;
            }
        }
        bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
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
}

namespace AmxxTutorial.ViewModels
{
    public enum FontWeightFields
    {
        [DescriptionLocalization("TextEditorSetting_Light")]
        Light = FontWeight.Light,
        [DescriptionLocalization("TextEditorSetting_Normal")]
        Normal = (FontWeight.Normal),
        [DescriptionLocalization("TextEditorSetting_Bold")]
        Bold = (FontWeight.Bold),
        [DescriptionLocalization("TextEditorSetting_Heavy")]
        Heavy = (FontWeight.Heavy)
    }

    public partial class TextEditorDialogProxy : ViewModelBase
    {
        private FunctionViewerViewModel ParentViewModel;

        [ObservableProperty] private bool _ShowLineNum;
        [ObservableProperty] private int _TextFontSize;
        [ObservableProperty] private int _TextFontWeight;
        [ObservableProperty] private bool _ShowTabs;
        [ObservableProperty] private bool _ShowEndOfLine;
        [ObservableProperty] private bool _HighlightCurrentLine;
        [ObservableProperty] private bool _ShowSpaces;

        public TextEditorDialogProxy(FunctionViewerViewModel parentViewModel)
        {
            ParentViewModel = parentViewModel;

            ShowLineNum = ParentViewModel.ShowLineNum;
            TextFontWeight = ParentViewModel.TextFontWeight;
            TextFontSize = ParentViewModel.TextFontSize;
            ShowTabs = ParentViewModel.ShowTabs;
            ShowSpaces = ParentViewModel.ShowSpaces;
            ShowEndOfLine = ParentViewModel.ShowEndOfLine;
            HighlightCurrentLine = ParentViewModel.HighlightCurrentLine;
        }

        partial void OnShowLineNumChanged(bool value) => ParentViewModel.ShowLineNum = value;
        partial void OnTextFontWeightChanged(int value) => ParentViewModel.TextFontWeight = value;
        partial void OnTextFontSizeChanged(int value) => ParentViewModel.TextFontSize = value;
        partial void OnShowTabsChanged(bool value) => ParentViewModel.ShowTabs = value;
        partial void OnShowEndOfLineChanged(bool value) => ParentViewModel.ShowEndOfLine = value;
        partial void OnHighlightCurrentLineChanged(bool value) => ParentViewModel.HighlightCurrentLine = value;
    }

    public partial class FunctionViewerViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<string> _IncVersions = [];
        public ObservableCollection<IncFile> IncFiles { get; set; } = [];

        [ObservableProperty]
        private ObservableCollection<IncFile> _FilteredIncFiles = [];

        [ObservableProperty]
        private string? _SelectedVersion;

        [ObservableProperty]
        private IncFile? _SelectedIncFile;

        [ObservableProperty] 
        private string _SearchCurText = string.Empty;

        [ObservableProperty]
        private bool _SearchedResultValidity = false;

        // TextEditor Setting Dialog
        [ObservableProperty] private int _TextFontSize = 12;
        [ObservableProperty] private bool _ShowLineNum = true;
        [ObservableProperty] private int _TextFontWeight = (int)FontWeightFields.Normal;
        [ObservableProperty] private bool _ShowTabs = true;
        [ObservableProperty] private bool _ShowEndOfLine = false;
        [ObservableProperty] private bool _HighlightCurrentLine = true;
        [ObservableProperty] private bool _ShowSpaces = true;

        public ICommand ShowDialogCommand { get; set; }
        public event EventHandler<string>? TextAreaCommandRequested;

        public WindowNotificationManager? NotificationManager { get; set; }

        public FunctionViewerViewModel()
        {
            ShowDialogCommand = new AsyncRelayCommand(ShowDefaultDialog);
        }

        public Task InitializeIncFilesAsync()
        {
            var TempVersions = IncReader.GetVersions();
            var DefaultVersion = TempVersions.FirstOrDefault();

            return Task.Run(() =>
            {
                if (!string.IsNullOrEmpty(DefaultVersion))
                {
                    var TempIncFiles = IncReader.GetIncFilesByVersion(DefaultVersion);

                    IncVersions = new ObservableCollection<string>(TempVersions);
                    IncFiles = new ObservableCollection<IncFile>(TempIncFiles);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    OnSearchCurTextChanged(string.Empty);
                    SelectedVersion = DefaultVersion;
                    SelectedIncFile = IncFiles.FirstOrDefault();
                });
            });
        }

        [RelayCommand]
        private void ResetArea() => TextAreaCommandRequested?.Invoke(this, "ResetToDefault2");
        [RelayCommand]
        private void MouseSelectAll() => TextAreaCommandRequested?.Invoke(this, "SelectAll");
        [RelayCommand]
        private void MouseCut(TextArea textArea) => ApplicationCommands.Cut.Execute(null, textArea);
        [RelayCommand]
        private void MouseCopy(TextArea textArea) => ApplicationCommands.Copy.Execute(null, textArea);
        [RelayCommand]
        private void MousePaste(TextArea textArea) => ApplicationCommands.Paste.Execute(null, textArea);

        private async Task ShowDefaultDialog()
        {
            var options = new DrawerOptions()
            {
                Position = Position.Right,
                Buttons = DialogButton.None,
                CanLightDismiss = true,
                IsCloseButtonVisible = true,
                Title = Localization.GetString("TextEditorSetting_Title"),
                CanResize = false,
            };

            var vm = new TextEditorDialogProxy(this);
            await Drawer.ShowModal<TextEditorDialog, TextEditorDialogProxy>(vm, "FuncViewerHost", options);
        }

        public void ShowNotification()
        {
            NotificationManager?.Show(
                new Notification("Welcome", "»¶Ó­»Ø¼Ò£¡"),
                showIcon: true,
                showClose: true,
                type: NotificationType.Success);
        }
        partial void OnSearchCurTextChanged(string? value)
        {
            var search = value?.ToLowerInvariant() ?? string.Empty;

            FilteredIncFiles.Clear();
            foreach (var pair in IncFiles.Where(i => i.FileName.Contains(search)))
            {
                FilteredIncFiles.Add(pair);
            }

            SearchedResultValidity = FilteredIncFiles.Any();
        }

        partial void OnSelectedVersionChanged(string? value)
        {
            var CurVersion = value;
            if (!string.IsNullOrEmpty(CurVersion))
            {
                IncFiles.Clear();

                var files = IncReader.GetIncFilesByVersion(CurVersion);
                foreach (var file in files)
                    IncFiles.Add(file);

                OnSearchCurTextChanged(string.Empty);
                SelectedVersion = CurVersion;
                SelectedIncFile = IncFiles.FirstOrDefault();
            }
        }

        partial void OnSelectedIncFileChanged(IncFile? value) 
            => TextAreaCommandRequested?.Invoke(this, "ResetToDefault");
    }
}