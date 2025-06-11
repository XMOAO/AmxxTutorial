using Avalonia;
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
            await Task.Delay(new Random().Next(1000, 2001));

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
            var TempVersions = IncReader.GetVersions().OrderBy(x => x).ToList();
            var TempEntriesCache = new List<IncFuncEntry>();
            var TempIncFiles = new List<IncTreeItem>();

            return Task.Run(() =>
            {
                var DefaultVersion = TempVersions.FirstOrDefault();

                if (!string.IsNullOrEmpty(DefaultVersion))
                {
                    var Rawfiles = IncReader.GetIncFilesByVersion(DefaultVersion);

                    // 遍历所有的*.inc文件
                    foreach (var File in Rawfiles)
                    {
                        var Item = new IncTreeItem();
                        Item.Header = File.FileName;
                        Item.Icon = "SemiIconFile";

                        bool NoFunction = true;
                        // 遍历每个inc文件的所有类别（Forward、Enum、Native...）
                        foreach (var Category in File.FuncEntryCategories)
                        {
                            if (Category.Entries.Count() == 0)
                                continue;

                            var SubItem = new IncTreeItem();
                            SubItem.Header = Category.Name;
                            SubItem.Parent = Item;
                            SubItem.Icon = Category.Name == "Natives" ? "SemiIconPuzzle" : "SemiIconInherit";
                            SubItem.FontSize = 13;

                            // 遍历每个类别中存在的函数
                            foreach (var Entry in Category.Entries)
                            {
                                SubItem.Children.Add(new IncTreeItem()
                                {
                                    FuncEntry = Entry,
                                    Header = Entry.FunctionName,
                                    Parent = SubItem,
                                    FontSize = 12
                                });

                                TempEntriesCache.Add(Entry);
                            }
                            NoFunction = false;
                            Item.Children.Add(SubItem);
                        }

                        if (NoFunction)
                        {
                            var SubItem = new IncTreeItem()
                            {
                                Header = " No Functions In This Include.",
                                FontSize = 12,
                                IsSeparator = true,
                            };
                            Item.Children.Add(SubItem);
                        }
                        else
                        {
                            if (Item.Children.Count() > 0)
                            {
                                Item.Children.Insert(0, new IncTreeItem()
                                {
                                    Header = Localization.GetString("IncNavMenu_Overview"),
                                    Parent = Item,
                                    FontSize = 13,
                                    IsOverview = true
                                });
                            }
                        }
                        TempIncFiles.Add(Item);
                    }
                }
                Dispatcher.UIThread.Post(() =>
                {
                    IncVersions = new ObservableCollection<string>(TempVersions);
                    EntriesCaches = TempEntriesCache;
                    IncFiles = new ObservableCollection<IncTreeItem>(TempIncFiles);
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

        public void JumpToNavColumn(IncTreeItem value)
        {
            OnSelectedFuncItemChanged(value);
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

    public partial class IncTreeItem : ObservableObject
    {
        [ObservableProperty]
        private string? _Header;
        [ObservableProperty]
        private string? _Icon;
        [ObservableProperty]
        private bool _IsSeparator = false;
        [ObservableProperty]
        private bool _IsExpanded = false;

        [ObservableProperty]
        private int _FontSize = 14;

        [ObservableProperty]
        private IncFuncEntry? _FuncEntry;
        [ObservableProperty]
        private IncConstantEntry? _ConstEntry;

        public bool IsOverview { get; set; } = false;
        public IncTreeItem? Parent { get; set; }

        [ObservableProperty]
        private ObservableCollection<IncTreeItem>? _Children = [];

        public IEnumerable<IncTreeItem> GetLeaves(bool root = false)
        {
            if (this.Children == null || this.Children.Count == 0)
            {
                yield return this;
                yield break;
            }

            if(!root)
            {
                foreach (var child in Children)
                {
                    var items = child.GetLeaves();
                    foreach (var item in items)
                    {
                        yield return item;
                    }
                }
            }
        }
        public IEnumerable<IncTreeItem> GetAncestors()
        {
            var current = this.Parent;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }
    };

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