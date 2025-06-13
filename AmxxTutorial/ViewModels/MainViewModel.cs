using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Semi.Avalonia;
using AmxxTutorial.Shared;
using AmxxTutorial.Pages;
using System.Collections.ObjectModel;
using System.ComponentModel;
using AmxxTutorial.Controls;
using System.Security.AccessControl;
using System.Linq;
using Avalonia.Threading;
using Markdown.Avalonia.Html.Core;

namespace AmxxTutorial.ViewModels;


public partial class MainViewModel : ViewModelBase
{
    // Top MainMenu
    [ObservableProperty]
    private string _DocumentationUrl = "https://docs.irihi.tech/semi";
    [ObservableProperty]
    private string _RepoUrl = "https://github.com/irihitech/Semi.Avalonia";

    // For switching scheme by binding.
    [ObservableProperty]
    private bool _CheckToggleScheme;
    public event EventHandler<bool>? OnThemeChanged;
    public bool GetCurTheme() => !CheckToggleScheme;
    partial void OnCheckToggleSchemeChanged(bool value) => OnThemeChanged?.Invoke(this, value);

    [ObservableProperty]
    private List<RightMenuItem> _RightMenuItems;

    // Left Navigation Menu
    // Tab Icons' key and path
    private readonly HashSet<string> NavigationTabItemIconsKey = new()
        { "SemiIconHome", "SemiIconArticle", "SemiIconFile", "SemiIconSearch", "SemiIconSetting" };

    [ObservableProperty]
    private ObservableCollection<IconItem> _NavigationTabItemIcons;

    // Tab Info
    [ObservableProperty]
    private ObservableCollection<NavigationTabItem> _NavigationTabItemInfo;

    // Tab switch control
    [ObservableProperty]
    private NavigationTabItem _SelectedNavigationTab;

    partial void OnSelectedNavigationTabChanged(NavigationTabItem value)
    {
        if (value != null)
        {
            foreach (var tab in NavigationTabItemInfo)
                tab.IsSelected = tab == value;
        }
    }

    public async Task InitializePageAndMenu()
    {
        FunctionViewerPage FunctionViewerPage = null;
        FunctionFinderPage FunctionFinderPage = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            FunctionViewerPage = new FunctionViewerPage();
            FunctionFinderPage = new FunctionFinderPage();
        });

        await Task.Run(() =>
        {
            var TempNavigationTabItemIcons = IconFactory.GetIconsByKeys(NavigationTabItemIconsKey);

            var TempNavigationTab = new ObservableCollection<NavigationTabItem>
            {
                new NavigationTabItem { Header="Home", Detail="Home_Desc",
                                        Icon = TempNavigationTabItemIcons[0], Page = null  },
                new NavigationTabItem { Header="Article", Detail="Article_Desc",
                                        Icon = TempNavigationTabItemIcons[1], Page = null },
                new NavigationTabItem { Header="Reader",  Detail="Reader_Desc",
                                        Icon = TempNavigationTabItemIcons[2], Page = FunctionViewerPage  },
                new NavigationTabItem { Header="Search",  Detail="Search_Desc",
                                        Icon = TempNavigationTabItemIcons[3], Page = FunctionFinderPage  },
                new NavigationTabItem { Header="Setting", Detail="Setting_Desc",
                                        Icon = TempNavigationTabItemIcons[4], Page = null }
            };

            var TempRightMenuItems = new List<RightMenuItem>
            {
                new RightMenuItem
                {
                    Header = "主题",
                    Items =
                    [
                        new RightMenuItem
                        {
                            Header = "跟随系统",
                            Command = FollowSystemThemeCommand
                        },
                        new RightMenuItem
                        {
                            Header = "水生",
                            Command = SelectThemeCommand,
                            CommandParameter = SemiTheme.Aquatic
                        },
                        new RightMenuItem
                        {
                            Header = "沙漠",
                            Command = SelectThemeCommand,
                            CommandParameter = SemiTheme.Desert
                        },
                        new RightMenuItem
                        {
                            Header = "黄昏",
                            Command = SelectThemeCommand,
                            CommandParameter = SemiTheme.Dusk
                        },
                        new RightMenuItem
                        {
                            Header = "夜空",
                            Command = SelectThemeCommand,
                            CommandParameter = SemiTheme.NightSky
                        },
                    ]
                },
                new RightMenuItem
                {
                    Header = "语言",
                    Items =
                    [
                        new RightMenuItem
                        {
                            Header = "简体中文",
                            Command = SelectLocaleCommand,
                            CommandParameter = new CultureInfo("zh-cn")
                        },
                        new RightMenuItem
                        {
                            Header = "繁體中文",
                            Command = SelectLocaleCommand,
                            CommandParameter = new CultureInfo("zh-tw")
                        },
                        new RightMenuItem
                        {
                            Header = "English",
                            Command = SelectLocaleCommand,
                            CommandParameter = new CultureInfo("en-us")
                        },
                    ]
                }
            };

            NavigationTabItemIcons = TempNavigationTabItemIcons;
            NavigationTabItemInfo = new ObservableCollection<NavigationTabItem>(TempNavigationTab);
            RightMenuItems = new List<RightMenuItem>(TempRightMenuItems);

            Dispatcher.UIThread.Post(() =>
            {
                
                SelectedNavigationTab = NavigationTabItemInfo.FirstOrDefault();

                
            });
        });
    }

    public MainViewModel()
    {

    }

    [RelayCommand]
    private void BackToHome()
    {
        
    }

    [RelayCommand]
    private void FollowSystemTheme()
    {
        var app = Application.Current;
        if (app is null) return;

        app.RegisterFollowSystemTheme();

        bool isDarkScheme = (app.ActualThemeVariant != ThemeVariant.Light &&
            app.ActualThemeVariant != SemiTheme.Desert);

        CheckToggleScheme = isDarkScheme ? false : true;
    }

    // 点击switch触发事件
    [RelayCommand]
    private void ToggleTheme()
    {
        var app = Application.Current;
        if (app is null) return;

        bool isDarkScheme = CheckToggleScheme;
        app.RequestedThemeVariant = isDarkScheme ? ThemeVariant.Light : ThemeVariant.Dark;
        app.UnregisterFollowSystemTheme();
    }

    [RelayCommand]
    private void SelectTheme(object? obj)
    {
        var app = Application.Current;
        if (app is null) return;

        var scheme = obj as ThemeVariant;
        bool isDarkScheme = (scheme != ThemeVariant.Light && scheme != SemiTheme.Desert);

        CheckToggleScheme = isDarkScheme ? false : true;
        app.RequestedThemeVariant = scheme;

        app.UnregisterFollowSystemTheme();
    }

    [RelayCommand]
    private void SelectLocale(object? obj)
    {
        var app = Application.Current;
        if (app is null) return;
        SemiTheme.OverrideLocaleResources(app, obj as CultureInfo);
        ResLocalization.OverrideLocaleResources(app, obj as CultureInfo);
    }

    [RelayCommand]
    private static async Task OpenUrl(string url)
    {
        var launcher = ResolveDefaultTopLevel()?.Launcher;
        if (launcher is not null)
        {
            await launcher.LaunchUriAsync(new Uri(url));
        }
    }

    private static TopLevel? ResolveDefaultTopLevel()
    {
        return Application.Current?.ApplicationLifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktopLifetime => desktopLifetime.MainWindow,
            ISingleViewApplicationLifetime singleView => TopLevel.GetTopLevel(singleView.MainView),
            _ => null
        };
    }
}

public class RightMenuItem
{
    public string? Header { get; set; }
    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    public IList<RightMenuItem>? Items { get; set; }
}

public partial class NavigationTabItem : ViewModelBase
{
    [ObservableProperty]
    private string? _Header;
    [ObservableProperty]
    private string? _Detail;
    [ObservableProperty]
    private IconItem? _Icon;

    [ObservableProperty]
    private Control? _Page;

    [ObservableProperty]
    private bool? _IsSelected;
}