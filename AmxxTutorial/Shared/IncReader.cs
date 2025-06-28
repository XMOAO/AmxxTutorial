using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using HtmlAgilityPack;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AmxxTutorial.Shared
{
    public static class IncReader
    {
        private static readonly Dictionary<string, List<IncFile>> IncFilesByVersion = new Dictionary<string, List<IncFile>>();
        private static readonly List<IncFuncEntry> FuncEntriesCaches = new List<IncFuncEntry>();
        private static readonly List<IncTreeItem> FuncTreeItemCaches = new List<IncTreeItem>();

        public static List<string> ExportedFuncName = new();

        public static Task InitializeIncFilesAsync()
        {
            return Task.Run(() =>
            {
                string IncludesPath = Path.Combine(AppContext.BaseDirectory, "Assets/Configs", "Includes"); ;

                if (!Directory.Exists(IncludesPath))
                {
                    Directory.CreateDirectory(IncludesPath);
                    return;
                }

                foreach (var VersionFolder in Directory.GetDirectories(IncludesPath))
                {
                    var VersionName = new DirectoryInfo(VersionFolder).Name;
                    var NewFiles = new List<IncFile>();

                    foreach (var IncPath in Directory.GetFiles(VersionFolder, "*.inc"))
                    {
                        // Add basic info
                        var IncFile = new IncFile
                        {
                            FileName = Path.GetFileName(IncPath),
                            Content = File.ReadAllText(IncPath)
                        };

                        // Add .Json info by .inc
                        var InlineFileName = Path.GetFileNameWithoutExtension(IncPath);
                        var InlinePath = Path.Combine(VersionFolder, InlineFileName);

                        if (!Directory.Exists(InlinePath))
                            continue;

                        foreach (var JsonPath in Directory.GetFiles(InlinePath, "*.json"))
                        {
                            if (File.Exists(JsonPath))
                            {
                                try
                                {
                                    var JsonText = File.ReadAllText(JsonPath);
                                    var RootNode = JsonNode.Parse(JsonText);

                                    if (RootNode == null)
                                        continue;

                                    var FunctionNode = RootNode["Function"];
                                    List<JsonObject> FunctionsArray = new List<JsonObject>();

                                    if (FunctionNode is JsonObject Single)
                                    {
                                        // 单对象，放到列表里
                                        FunctionsArray.Add(Single);
                                    }
                                    else if (FunctionNode is JsonArray Array)
                                    {
                                        // 数组，就把所有子项加到列表里
                                        FunctionsArray.AddRange(Array.OfType<JsonObject>());
                                    }

                                    if (FunctionsArray != null)
                                    {
                                        foreach (var FnNode in FunctionsArray)
                                        {
                                            var DescNode = FnNode["Description"];

                                            string MainDescription;
                                            if (DescNode is JsonArray DescArray)
                                            {
                                                MainDescription = string.Join(Environment.NewLine, DescArray
                                                    .OfType<JsonNode>()
                                                    .Select(n => n?.GetValue<string>() ?? String.Empty));
                                            }
                                            else
                                            {
                                                MainDescription = DescNode?.GetValue<string>() ?? String.Empty;
                                            }

                                            DescNode = FnNode["Return"];

                                            string Return;
                                            if (DescNode is JsonArray DescArray2)
                                            {
                                                Return = string.Join(Environment.NewLine, DescArray2
                                                    .OfType<JsonNode>()
                                                    .Select(n => n?.GetValue<string>() ?? String.Empty));
                                            }
                                            else
                                            {
                                                Return = DescNode?.GetValue<string>() ?? String.Empty;
                                            }

                                            DescNode = FnNode["Error"];

                                            string Error;
                                            if (DescNode is JsonArray DescArray3)
                                            {
                                                Error = string.Join(Environment.NewLine, DescArray3
                                                    .OfType<JsonNode>()
                                                    .Select(n => n?.GetValue<string>() ?? String.Empty));
                                            }
                                            else
                                            {
                                                Error = DescNode?.GetValue<string>() ?? String.Empty;
                                            }

                                            var ParameterTags = new List<IncFuncEntry.ParameterTag>();
                                            if (FnNode["Parameters"] is JsonArray ParameterTagsArray)
                                            {
                                                foreach (var TagItem in ParameterTagsArray.OfType<JsonObject>())
                                                {
                                                    var Tag = TagItem["Tag"]?.GetValue<string>() ?? string.Empty;
                                                    var Variable = TagItem["Variable"]?.GetValue<string>();
                                                    var Desc = TagItem["Description"]?.GetValue<string>() ?? string.Empty;
                                                    ParameterTags.Add(new IncFuncEntry.ParameterTag
                                                    {
                                                        Tag = Tag,
                                                        Variable = Variable,
                                                        Description = Desc
                                                    });
                                                }
                                            }

                                            var Entry = new IncFuncEntry
                                            {
                                                Function = FnNode["Syntax"]?.GetValue<string>() ?? string.Empty,
                                                FunctionName = FnNode["FunctionName"]?.GetValue<string>() ?? string.Empty,
                                                Error = Error,
                                                Return = Return,
                                                Description = MainDescription,
                                                ParameterTags = ParameterTags, 
                                            };

                                            var Trimmed = Entry.Function.TrimStart();
                                            if (Trimmed.StartsWith("native ", StringComparison.OrdinalIgnoreCase))
                                                IncFile.NativeEntries.Add(Entry);
                                            else if (Trimmed.StartsWith("forward ", StringComparison.OrdinalIgnoreCase))
                                                IncFile.ForwardEntries.Add(Entry);
                                            else
                                                IncFile.StockEntries.Add(Entry);
                                        }
                                    }

                                    // 处理 constants（新增部分）
                                    /*var ConstantsArray = RootNode?["constants"]?.AsArray();
                                    if (ConstantsArray != null)
                                    {
                                        foreach (var ConstNode in ConstantsArray)
                                        {
                                            var comment = ConstNode["Comment"]?.GetValue<string>() ?? string.Empty;

                                            var commentTags = new List<IncFuncEntry.CommentTag>();
                                            if (ConstNode["CommentTags"] is JsonArray tagsArray)
                                            {
                                                foreach (var tagItem in tagsArray.OfType<JsonObject>())
                                                {
                                                    var tag = tagItem["Tag"]?.GetValue<string>() ?? string.Empty;
                                                    var variable = tagItem["Variable"]?.GetValue<string>();
                                                    var desc = tagItem["Description"]?.GetValue<string>() ?? string.Empty;
                                                    commentTags.Add(new IncFuncEntry.CommentTag
                                                    {
                                                        Tag = tag,
                                                        Variable = variable,
                                                        Description = desc
                                                    });
                                                }
                                            }

                                            var constant = ConstNode["Constant"]?.GetValue<string>() ?? string.Empty;

                                            IncFile.ConstantEntries.Add(new IncConstantEntry
                                            {
                                                Comment = comment,
                                                CommentTags = commentTags,
                                                Constant = constant
                                            });
                                        }
                                    }*/
                                }
                                catch (JsonException ex)
                                {
                                    throw new InvalidDataException($"解析 JSON 文件失败: {JsonPath}", ex);
                                }
                            }
                        }

                        var Item = new IncTreeItem();
                        Item.Header = IncFile.FileName;

                        bool NoFunction = true;
                        // 遍历每个inc文件的所有类别（Forward、Enum、Native...）
                        foreach (var Category in IncFile.FuncEntryCategories)
                        {
                            if (Category.Entries.Count() == 0)
                                continue;

                            var SubItem = new IncTreeItem();
                            SubItem.Header = Category.Name;
                            SubItem.Parent = Item;
                            SubItem.FontSize = 13;

                            // 遍历每个类别中存在的函数
                            foreach (var Entry in Category.Entries)
                            {
                                SubItem.Children!.Add(new IncTreeItem()
                                {
                                    FuncEntry = Entry,
                                    Header = Entry.FunctionName,
                                    Parent = SubItem,
                                    FontSize = 12
                                });

                                FuncEntriesCaches.Add(Entry);
                                AddExportFuncName(Item.Header, Entry.FunctionName);
                            }
                            NoFunction = false;
                            Item.Children!.Add(SubItem);
                        }

                        if (NoFunction)
                        {
                            var SubItem = new IncTreeItem()
                            {
                                Header = " No Functions In This Include.",
                                FontSize = 12,
                                IsSeparator = true,
                            };
                            Item.Children!.Add(SubItem);
                        }
                        else
                        {
                            if (Item.Children!.Count() > 0)
                            {
                                Item.Children!.Insert(0, new IncTreeItem()
                                {
                                    Header = Localization.GetString("IncNavMenu_Overview"),
                                    Parent = Item,
                                    FontSize = 13,
                                    IsOverview = true
                                });
                            }
                        }
                        FuncTreeItemCaches.Add(Item);
                        NewFiles.Add(IncFile);
                    }
                    IncFilesByVersion[VersionName] = NewFiles;
                }

                //await ExportFuncNameAsync();
                //await FetchAndParseAmxAPIAsync();
            });
        }

        public static void AddExportFuncName(string IncName, string FuncName)
        {
            string Name = IncName.EndsWith(".inc", StringComparison.OrdinalIgnoreCase)
                ? IncName.Substring(0, IncName.Length - 4) : IncName;

            ExportedFuncName.Add(Name + "/" + FuncName);
        }
        public static async Task ExportFuncNameAsync()
        {
            string ExportPath = Path.Combine(AppContext.BaseDirectory, "Assets/Configs/", "ExportedFunctions.txt");
            await File.WriteAllLinesAsync(ExportPath, ExportedFuncName);

            ExportedFuncName.Clear();
        }
        public static async Task FetchAndParseAmxAPIAsync()
        {
            foreach (var PathName in ExportedFuncName)
            {
                string Url = "https://www.amxmodx.org/api/" + PathName;
                string? JsonResult = null;

                try
                {
                    JsonResult = await ParseAmxAPIFromHTMLAsync(Url);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"解析失败: {Url} -> {ex.Message}");
                    continue;
                }

                if (JsonResult != null)
                {
                    string RelativeDir = Path.GetDirectoryName(PathName) ?? "";
                    string FileName = Path.GetFileName(PathName);

                    string OutPath = Path.Combine(AppContext.BaseDirectory, "Assets/Configs/API/", $"{RelativeDir}");

                    if (!Directory.Exists(OutPath))
                        Directory.CreateDirectory(OutPath);

                    OutPath += $"/{FileName}.json";

                    try
                    {
                        await File.WriteAllTextAsync(OutPath, JsonResult);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"写入文件失败: {OutPath} -> {ex.Message}");
                    }

                    Debug.WriteLine($"Read API: {PathName} from amxmodx.org");
                }
            }
        }

        static async Task<string> ParseAmxAPIFromHTMLAsync(string url)
        {
            using var HC = new HttpClient();
            var HtmlStr = await HC.GetStringAsync(url);
            var HtmlDoc = new HtmlDocument();
            HtmlDoc.LoadHtml(HtmlStr);

            var Func = new Dictionary<string, object>();
            var SyntaxPre = HtmlDoc.DocumentNode.SelectSingleNode(
                "//h4[@class='sub-header2' and normalize-space(text())='Syntax']" +
                "/following-sibling::*[self::pre and contains(@class,'syntax')][1]"
            );
            string Syntax = SyntaxPre?.InnerText.Trim() ?? "";
            Func["Syntax"] = Syntax;

            if (!string.IsNullOrEmpty(Syntax))
            {
                var Parts = Syntax.Split(new[] { ' ', '(' }, StringSplitOptions.RemoveEmptyEntries);
                Func["FunctionName"] = Parts.Length >= 2 ? Parts[1] : "";
            }
            else
            {
                Func["FunctionName"] = "";
            }

            // —— Parameters (Usage) ——  
            var Parameters = new List<Dictionary<string, string>>();
            var UsageHdr = HtmlDoc.DocumentNode.SelectSingleNode("//h4[@class='sub-header2' and normalize-space(text())='Usage']");
            if (UsageHdr != null)
            {
                var DL = UsageHdr.SelectSingleNode("following-sibling::dl[1]");
                if (DL != null)
                {
                    var DtNodes = DL.SelectNodes("dt") ?? new HtmlNodeCollection(null);
                    foreach (var DT in DtNodes)
                    {
                        var TagNode = DT.SelectSingleNode("./code");
                        var VarNode = DT.SelectSingleNode("./var");
                        var DdNode = DT.SelectSingleNode("following-sibling::dd[1]");
                        if (TagNode != null && DdNode != null)
                        {
                            Parameters.Add(new Dictionary<string, string>
                            {
                                ["Tag"] = TagNode.InnerText.Trim(),
                                ["Variable"] = VarNode?.InnerText.Trim() ?? "",
                                ["Description"] = DdNode.InnerText.Trim()
                            });
                        }
                    }
                }
                else
                {
                    // 回退到 <table>
                    var TableDiv = UsageHdr.SelectSingleNode("following-sibling::div[contains(@class,'table-responsive')][1]");
                    if (TableDiv != null)
                    {
                        var Rows = TableDiv.SelectNodes(".//tr") ?? new HtmlNodeCollection(null);
                        foreach (var TR in Rows)
                        {
                            var Cols = TR.SelectNodes("td");
                            if (Cols == null || Cols.Count < 2) continue;
                            string Name = Cols[0].InnerText.Trim();
                            var PreDesc = Cols[1].SelectSingleNode(".//pre[contains(@class,'description')]");
                            string Desc = PreDesc?.InnerText.Trim() ?? Cols[1].InnerText.Trim();
                            Parameters.Add(new Dictionary<string, string>
                            {
                                ["Tag"] = "param",
                                ["Variable"] = Name,
                                ["Description"] = Desc
                            });
                        }
                    }
                }
            }
            Func["Parameters"] = Parameters;

            // —— 通用：根据标题抓取所有对应的 <pre class="description"> ——  
            List<string> ExtractAll(string title)
            {
                var List = new List<string>();
                var Headers = HtmlDoc.DocumentNode.SelectNodes($"//h4[@class='sub-header2' and normalize-space(text())='{title}']");
                if (Headers == null) return List;

                foreach (var Hdr in Headers)
                {
                    // 从 hdr 后面往下找直到第一个 <pre class="description">
                    var Node = Hdr.NextSibling;
                    while (Node != null && !(Node.Name == "pre" && Node.GetAttributeValue("class", "").Contains("description")))
                        Node = Node.NextSibling;

                    if (Node != null && Node.Name == "pre")
                        List.Add(Node.InnerText.Trim());
                }
                return List;
            }

            Func["Description"] = ExtractAll("Description");
            Func["Note"] = ExtractAll("Note");
            Func["Return"] = ExtractAll("Return");
            Func["Error"] = ExtractAll("Error");

            // 3. 序列化输出
            var Root = new Dictionary<string, object> { ["Function"] = Func };
            var Opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(Root, Opts);
        }

        public static List<string> GetVersions()
            => IncFilesByVersion.Keys.OrderBy(v => v).ToList();

        public static List<IncFile> GetIncFilesByVersion(string version)
            => IncFilesByVersion.TryGetValue(version, out var list) ? list : new List<IncFile>();

        public static List<IncFuncEntry> GetFuncEntriesCaches() => FuncEntriesCaches.ToList();
        public static List<IncTreeItem> GetFuncTreeItemCaches() => FuncTreeItemCaches.ToList();

        public static List<IncFuncEntry> GetNativeEntries(string version, string incFileName)
            => GetEntries(version, incFileName, f => f.NativeEntries);

        public static List<IncFuncEntry> GetForwardEntries(string version, string incFileName)
            => GetEntries(version, incFileName, f => f.ForwardEntries);

        public static List<IncFuncEntry> GetStockEntries(string version, string incFileName)
            => GetEntries(version, incFileName, f => f.StockEntries);

        private static List<IncFuncEntry> GetEntries(string version, string fileName, Func<IncFile, List<IncFuncEntry>> selector)
        {
            var file = GetIncFilesByVersion(version)
                .FirstOrDefault(f => f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            return file != null ? selector(file) : new List<IncFuncEntry>();
        }
    }

    public partial class IncTreeItem : ObservableObject
    {
        [ObservableProperty]
        private string _Header = string.Empty;
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
        private ObservableCollection<IncTreeItem> _Children = [];

        public IEnumerable<IncTreeItem> GetLeaves(bool root = false)
        {
            if (this.Children == null || this.Children.Count == 0)
            {
                yield return this;
                yield break;
            }

            if (!root)
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

    public class IncFuncEntry
    {
        public class ParameterTag
        {
            public string Tag { get; set; } = string.Empty;
            public string Variable { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public string Function { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ParameterTag> ParameterTags { get; set; } = new List<ParameterTag>();
        public List<string> Notes { get; set; } = new List<string>();
        public string Return { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    public class IncConstantEntry
    {
        public string Comment { get; set; } = string.Empty;
        //public List<IncFuncEntry.CommentTag> CommentTags { get; set; } = new();
        public string Constant { get; set; } = string.Empty;
    }

    public class IncFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public List<IncFuncEntry> NativeEntries { get; } = new List<IncFuncEntry>();
        public List<IncFuncEntry> ForwardEntries { get; } = new List<IncFuncEntry>();
        public List<IncFuncEntry> StockEntries { get; } = new List<IncFuncEntry>();
        public List<IncConstantEntry> ConstantEntries { get; } = new List<IncConstantEntry>();

        public IEnumerable<(string Name, List<IncFuncEntry> Entries)> FuncEntryCategories => new[]
        {
            ("Natives", NativeEntries),
            ("Forwards", ForwardEntries),
            ("Stocks", StockEntries)
        };
    }
}
