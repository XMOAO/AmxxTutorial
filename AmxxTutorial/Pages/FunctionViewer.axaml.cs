
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using AmxxTutorial.ViewModels;
using AmxxTutorial.Shared;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AvaloniaEdit;
using AvaloniaEdit.Indentation.CSharp;

using Avalonia.Input;
using AmxxTutorial.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Threading.Tasks;
using Ursa.Common;
using Ursa.Controls.Options;
using Ursa.Controls;
using Ursa.Demo.Dialogs;

namespace AmxxTutorial.Pages
{

    public partial class FunctionViewer : BaseUserControl
    {
        private readonly TextEditor TextEditor;

        public FunctionViewer()
        {
            InitializeComponent();
            
            TextEditor = this.FindControl<TextEditor>("ContentTextEditor");
            TextEditor.ShowLineNumbers = true;
            TextEditor.TextArea.IndentationStrategy = new CSharpIndentationStrategy();

            DataContext = new FunctionViewerViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void InitializeTextEditor()
        {
            
        }
    }
}

namespace AmxxTutorial.ViewModels
{
    public partial class FunctionViewerViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<string> _IncVersions;

        [ObservableProperty]
        private ObservableCollection<IncFile> _IncFiles;

        [ObservableProperty]
        private string? _SelectedVersion;

        [ObservableProperty]
        private IncFile? _SelectedIncFile;


        public ICommand ShowDialogCommand { get; set; }

        public FunctionViewerViewModel()
        {
            InitializeIncFiles();

            ShowDialogCommand = new AsyncRelayCommand(ShowDefaultDialog);
        }

        private async Task ShowDefaultDialog()
        {
            var options = new DrawerOptions()
            {
                Position = Position.Right,
                Buttons = DialogButton.None,
                CanLightDismiss = true,
                IsCloseButtonVisible = true,
                Title = "Title",
                CanResize = false,
            };

            var vm = new CustomDemoDialogViewModel();
            await Drawer.ShowCustomModal<CustomDemoDialog, CustomDemoDialogViewModel, object?>(vm, "LocalHost", options);
        }

        public void InitializeIncFiles()
        {
            IncVersions = new ObservableCollection<string>();
            IncFiles = new ObservableCollection<IncFile>();

            foreach (var v in IncReader.GetVersions().OrderBy(x => x))
                IncVersions.Add(v);

            var defaultVersion = IncVersions.FirstOrDefault();
            if (!string.IsNullOrEmpty(defaultVersion))
            {
                var files = IncReader.GetIncFilesByVersion(defaultVersion);
                foreach (var file in files)
                    IncFiles.Add(file);

                SelectedVersion = defaultVersion;
                SelectedIncFile = IncFiles.FirstOrDefault();
            }
        }
    }
}