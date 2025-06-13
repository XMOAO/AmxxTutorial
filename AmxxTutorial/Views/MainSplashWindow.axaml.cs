using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Ursa.Controls;

using AmxxTutorial.ViewModels;

namespace AmxxTutorial.Views;

public partial class MainSplashWindow : SplashWindow
{
    public MainSplashWindow()
    {
        InitializeComponent();
        this.Loaded += MainSplashWindow_Loaded;
    }

    private void MainSplashWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if(DataContext is MainSplashViewModel vm)
        {
            vm.SignalReady();
        }
    }

    protected override async Task<Window?> CreateNextWindow()
    {
        if (this.DialogResult is true)
        {
            
            return new MainWindow()
            {
                DataContext = (this.DataContext as MainSplashViewModel)!.ViewModel
            };
        }
        return null;
    }
}