using Avalonia.Controls;
using Ursa.Controls;

namespace AmxxTutorial.Views;

public partial class MainWindow : Window
{
    public WindowNotificationManager? NotificationManager { get; set; }
    public MainWindow()
    {
        InitializeComponent();
        NotificationManager = new WindowNotificationManager(this) { MaxItems = 3 };
    }
}