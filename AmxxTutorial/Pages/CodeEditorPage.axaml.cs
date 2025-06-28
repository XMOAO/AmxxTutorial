using AmxxTutorial.Shared;
using AmxxTutorial.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaWebView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Semi.Avalonia.Tokens.Palette;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;


namespace AmxxTutorial.Pages
{
    public partial class CodeEditorPage : UserControl
    {
        CodeEditorViewModel ViewModel;
        bool bInitiated = false;
        
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

            var Html = Path.Combine(AppContext.BaseDirectory, "Assets/Monaco", "monaco-editor.html");
            WebView.Url = new Uri($"file:///{Html.Replace('\\', '/')}");

            WebView.NavigationCompleted += WebView_NavigationCompleted;
            WebView.WebMessageReceived += WebView_WebMessageReceived;

            bInitiated = true;
        }

        private void WebView_NavigationCompleted(object? sender, WebViewCore.Events.WebViewUrlLoadedEventArg e)
        {
            try
            {
                var Theme = Globals.GetCurTheme();
                WebView.PlatformWebView?.ExecuteScriptAsync($"window.setMonacoTheme('{(!Theme ? "vs-light-plus" : "vs-dark-plus")}')");

                Globals.OnThemeChanged += (sender, isLightTheme) =>
                {
                    WebView.PlatformWebView?.ExecuteScriptAsync($"window.setMonacoTheme('{(isLightTheme ? "vs-light-plus" : "vs-dark-plus")}')");
                };
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void WebView_WebMessageReceived(object? sender, WebViewCore.Events.WebViewMessageReceivedEventArgs e)
        {
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