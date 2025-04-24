using AmxxTutorial.Controls;
using AmxxTutorial.Shared;
using AmxxTutorial.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace AmxxTutorial.Pages;

public partial class FunctionFinder : BaseUserControl
{
    public FunctionFinder()
    {
        InitializeComponent();
        DataContext = new FunctionFinderViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public class FunctionFinderViewModel : ViewModelBase
{
    public ObservableCollection<string> Versions { get; } = new();
    public ObservableCollection<IncFile> CurrentFiles { get; } = new();

    private string _selectedVersion;
    public string SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            if (_selectedVersion != value)
            {
                _selectedVersion = value;
                OnPropertyChanged(nameof(SelectedVersion));
                LoadFilesForVersion(_selectedVersion);
            }
        }
    }

    public FunctionFinderViewModel()
    {
        foreach (var version in IncReader.GetVersions())
        {
            Versions.Add(version);
        }

        if (Versions.Count > 0)
            SelectedVersion = Versions[0]; // 默认选中第一个版本
    }
    private void LoadFilesForVersion(string version)
    {
        CurrentFiles.Clear();
        foreach (var file in IncReader.GetIncFilesByVersion(version))
        {
            CurrentFiles.Add(file);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}