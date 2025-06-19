using AmxxTutorial.Shared;
using AmxxTutorial.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AmxxTutorial.Pages
{
    public partial class CodeEditorPage : UserControl
    {
        CodeEditorViewModel ViewModel;
        bool bInitiated = false;
        private bool IsSyncingScroll;
        
        public CodeEditorPage()
        {
            InitializeComponent();

            ViewModel = new CodeEditorViewModel();
            DataContext = ViewModel;

            this.Loaded += async (_, _) => await InitializeExtraAsync();
        }

        public async Task InitializeExtraAsync()
        {
            if (bInitiated)
                return;

            TextEditorInitializer.InitializeTextEditor(MainEditor, this.Background);
            TextEditorInitializer.TextEditorRegulateTheme(MainEditor);
            TextEditorInitializer.InitializeTextEditor(PreviewEditor, this.Background);
            TextEditorInitializer.TextEditorRegulateTheme(PreviewEditor);

            bInitiated = true;
        }

        private void CodeEditorOnTextChanged(object? sender, EventArgs e)
        {
            if (sender is not TextEditor)
                return;

            PreviewEditor.Text = MainEditor.Text;
        }

        private void CodeEditorOnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (IsSyncingScroll || sender is not TextEditor source)
                return;

            try
            {
                IsSyncingScroll = true;
                var target = source == MainEditor ? PreviewEditor : MainEditor;

                // 获取源编辑器的垂直偏移
                var offset = source.TextArea.TextView.VerticalOffset;
                // 获取每行高度
                var lineHeight = source.TextArea.TextView.DefaultLineHeight;
                // 计算要滚动到的行号
                var targetLine = (int)(offset / lineHeight);

                // 调用 AvaloniaEdit.ScrollTo 方法同步滚动
                target.ScrollTo(targetLine, 0);
            }
            finally
            {
                IsSyncingScroll = false;
            }
        }
    }
}

namespace AmxxTutorial.ViewModels
{
    public partial class CodeEditorViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<FileTabItem> _OpenedFileTabs = new();
        [ObservableProperty]
        private FileTabItem? _SelectedTab;

        public Func<object> NewFileTabItemFactory => AddNewFileTabItem;

        public CodeEditorViewModel()
        {

        }

        private object AddNewFileTabItem()
        {
            return new FileTabItem
            {
            };
        }
    }
    public class FileTabItem
    {
        public string FileName { get; set; } = "untitled";
        public ICommand CloseCommand { get; set; }
    }
}