using Avalonia.Controls;
using Avalonia.Threading;

using System;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

using AmxxTutorial.Shared;
using AmxxTutorial.ViewModels;

namespace AmxxTutorial.Pages
{
    public partial class FunctionFinderPage : UserControl
    {
        FunctionFinderViewModel ViewModel;
        bool bInitiated = false;

        public FunctionFinderPage()
        {
            InitializeComponent();

            ViewModel = new FunctionFinderViewModel();
            DataContext = ViewModel;

            this.Loaded += async (_, _) => await InitializeExtraAsync();
        }

        public async Task InitializeExtraAsync()
        {
            if (bInitiated)
                return;

            LoadingArea.IsLoading = true;

            ViewModel.NavigateCommandRequested += (sender, args) =>
            {
                if (args == "CloseDropDown")
                {
                    FuncSearchBar.IsDropDownOpen = false;
                }
                else if (args == "UpdateFocus")
                {
                    IncListNavMenu.BringIntoView();
                }
            };

            await ViewModel.InitializeIncFilesAsync();
            await Task.Delay(new Random().Next(300, 500));

            FuncSearchBar.FilterMode = AutoCompleteFilterMode.None;
            FuncSearchBar.AsyncPopulator = ViewModel.PopulateAsync;

            LoadingArea.IsLoading = false;

            bInitiated = true;
        }
    }
}

namespace AmxxTutorial.ViewModels
{
    public partial class FunctionFinderViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<string> _IncVersions = [];
        [ObservableProperty]
        private ObservableCollection<IncTreeItem> _IncFiles = [];
        [ObservableProperty]
        private ObservableCollection<NavBreadCrumbItem> _NavColumns = [];

        public List<IncFuncEntry> EntriesCaches = new List<IncFuncEntry>();

        [ObservableProperty]
        private string? _SelectedVersion;
        [ObservableProperty]
        private IncTreeItem? _SelectedFuncItem;
        [ObservableProperty]
        private string _SearchCurText = string.Empty;
        [ObservableProperty]
        private IncFuncEntry? _SelectedSearchEntry;

        [ObservableProperty]
        private string _MarkDownText = string.Empty;
        public ICommand MarkDownRedirectHyperlink { get; }

        [GeneratedRegex(@"[\W_]+", RegexOptions.Compiled)]
        private static partial Regex SplitOnNonWord();

        public event EventHandler<string>? NavigateCommandRequested;

        public FunctionFinderViewModel()
        {
            MarkDownRedirectHyperlink = new RedirectHyperlink()
            {
                NavigateAction = subHeader =>
                {
                    var GetEntryByName = EntriesCaches.Find(e => e.FunctionName == subHeader);
                    if(GetEntryByName != null)
                    {
                        OnSelectedSearchEntryChanged(GetEntryByName);   
                    }
                }
            };
        }

        public Task InitializeIncFilesAsync()
        {
            return Task.Run(() =>
            {
                var TempVersions = IncReader.GetVersions();
                var TempEntriesCache = IncReader.GetFuncEntriesCaches();
                var TempIncFiles = IncReader.GetFuncTreeItemCaches();

                IncVersions = new ObservableCollection<string>(TempVersions);
                EntriesCaches = TempEntriesCache;
                IncFiles = new ObservableCollection<IncTreeItem>(TempIncFiles);

                Dispatcher.UIThread.Post(() =>
                {
                   
                });
            });
        }

        // Called When select functions
        partial void OnSelectedFuncItemChanged(IncTreeItem? value)
        {
            if (value == null)
                return;

            ClearNavColumns();

            // It's a function ? Show func pattern
            if (value.FuncEntry != null)
            {
                MarkDownText = MarkDownGenerator.ConstructFuncHelper(value);

                var Ancestors = value.GetAncestors().Reverse();
                if (Ancestors.Count() != 0)
                {
                    foreach(var Ancestor in Ancestors)
                    {
                        Ancestor.IsExpanded = true;
                        PushNavColumns(Ancestor);
                    }
                    PushNavColumns(value);

                    NavigateCommandRequested?.Invoke(this, "UpdateFocus");
                }
            }
            else
            {
                // It's an Overview?
                if(value.Parent != null && value.IsOverview)
                {
                    MarkDownText = MarkDownGenerator.ConstructColumnOverview(value.Parent);

                    PushNavColumns(value.Parent);
                    PushNavColumns(value);
                }
            }
        }

        partial void OnSelectedSearchEntryChanged(IncFuncEntry? value)
        {
            if (value != null)
            {
                var GetRootTree = IncFiles.SelectMany(t1 => t1.Children.SelectMany(t2 => t2.Children.Where(t3 => t3.Header == value.FunctionName)));
                var GetTreeItem = GetRootTree.FirstOrDefault();

                if (GetTreeItem != null)
                {
                    if (SelectedFuncItem != GetTreeItem)
                        SelectedFuncItem = GetTreeItem;

                    NavigateCommandRequested?.Invoke(this, "CloseDropDown");
                }
            }
        }

        partial void OnSearchCurTextChanged(string? value)
        {
        }

        private void ClearNavColumns() => NavColumns.Clear();

        private void PushNavColumns(IncTreeItem item)
        {
            NavColumns.Add(new NavBreadCrumbItem(this)
            {
                Section = item.Header,
                ItemInstance = item,
                IsReadOnly = ((item.Children == null) || item.Children.Count == 0) ? true : false,
            });
        }

        public async Task<IEnumerable<object>> PopulateAsync(string? searchText, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return Array.Empty<IncFuncEntry>();

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

            var PrimaryMatch = EntriesCaches.Where(e => e.FunctionName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if(PrimaryMatch.Any())
            {
                return PrimaryMatch.OrderByDescending(e =>String.Equals(e.FunctionName, searchText, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ThenBy(e =>e.FunctionName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase))
                    .Cast<object>();
            }

            // 备用多词匹配
            var Keys = searchText.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var Fallback = new List<(IncFuncEntry Entry, int FirstPos)>();

            foreach (var Entry in EntriesCaches)
            {
                // 将函数名按非字母数字分词
                var Tokens = SplitOnNonWord()
                    .Split(Entry.FunctionName)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.ToLowerInvariant())
                    .ToArray();

                int minPos = int.MaxValue;
                bool allHit = true;

                foreach (var Key in Keys)
                {
                    var hitPos = Tokens.Select((tok, idx) => tok.Contains(Key) ? idx : int.MaxValue).Min();

                    if (hitPos == int.MaxValue)
                    {
                        allHit = false;
                        break;
                    }
                    minPos = Math.Min(minPos, hitPos);
                }

                if (allHit)
                    Fallback.Add((Entry, minPos));
            }

            return Fallback.OrderBy(t => t.FirstPos).Select(t => t.Entry).Cast<object>(); ;
        }
    }

    public partial class NavBreadCrumbItem : ObservableObject
    {
        [ObservableProperty]
        private string? _Section;
        [ObservableProperty] 
        private bool _IsReadOnly;
        public ICommand Command { get; set; }
        public IncTreeItem? ItemInstance { get; set; }
        
        public NavBreadCrumbItem(FunctionFinderViewModel parent)
        {
            Command = new RelayCommand(() =>
            {
                /*if (ItemInstance != null)
                {
                    parent.JumpToNavColumn(ItemInstance);
                }*/
            });
        }
    };
}