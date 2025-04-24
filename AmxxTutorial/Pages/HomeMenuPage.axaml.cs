using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

using AmxxTutorial.Shared;
using AmxxTutorial.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AmxxTutorial.Pages
{
    public partial class HomeMenuPage : UserControl
    {
        public HomeMenuPage()
        {
            InitializeComponent();
            this.DataContext = new HomeMenuPageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnClicked_MainMenuItem(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: IconItem s }) return;

            if (sender is Control control)
            {
                var tabControl = control.GetVisualAncestors().OfType<TabControl>().FirstOrDefault();
                if (tabControl == null)
                    return;

                var curPage = 0;
                switch (s.ResourceKey)
                {
                    case "SemiIconArticle":
                        curPage = 1; break;
                    case "SemiIconSearch":
                        curPage = 2; break;
                    case "SemiIconSetting":
                        curPage = 3; break;
                }
                tabControl.SelectedIndex = curPage;
            }
        }
    }
}

namespace AmxxTutorial.ViewModels
{
    public partial class HomeMenuPageViewModel : ViewModelBase
    {
        private readonly HashSet<string> MainMenuButtonItems = new()
        { "SemiIconArticle", "SemiIconSearch", "SemiIconSetting" };

        [ObservableProperty]
        private ObservableCollection<IconItem> _MainMenuIcons;

        public HomeMenuPageViewModel()
        {
            _MainMenuIcons = IconFactory.GetIconsByKeys(MainMenuButtonItems);
        }
    }
}