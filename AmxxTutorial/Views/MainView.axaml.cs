using System.Linq;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

using AmxxTutorial.ViewModels;
using AmxxTutorial.Shared;
using Avalonia.VisualTree;

namespace AmxxTutorial.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel();
    }

    protected override async void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
    }

    private void OnClicked_NavigationTabItem(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: IconItem s }) return;

        var tabControl = this.GetControl<TabControl>("PageSwitchControl");
        if (tabControl == null)
            return;

        
    }
}