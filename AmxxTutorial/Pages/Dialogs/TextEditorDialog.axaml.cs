using AmxxTutorial.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Ursa.Controls;

namespace AmxxTutorial.Pages.Dialogs;

public partial class TextEditorDialog : UserControl
{
    public TextEditorDialog() => InitializeComponent();

    private void FontWeightSelector_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is EnumSelector selector && selector.Value == null)
        {
            selector.Value = FontWeightFields.Normal; // …Ë÷√ƒ¨»œ÷µ
        }
    }
}