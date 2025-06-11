using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AmxxTutorial.Shared
{
    public static class IncReader
    {
        private static readonly Dictionary<string, List<IncFile>> IncFilesByVersion = new Dictionary<string, List<IncFile>>();

        public static void LoadIncFiles()
        {
            IncFilesByVersion.Clear();

            string IncludesPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Includes"); ;

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
                    var IncFile = new IncFile
                    {
                        FileName = Path.GetFileName(IncPath),
                        Content = File.ReadAllText(IncPath)
                    };

                    var BaseName = Path.GetFileNameWithoutExtension(IncPath);
                    var JsonPath = Path.Combine(VersionFolder, BaseName + ".json");

                    if (File.Exists(JsonPath))
                    {
                        try
                        {
                            var JsonText = File.ReadAllText(JsonPath);
                            var RootNode = JsonNode.Parse(JsonText);

                            var FunctionsArray = RootNode?["functions"]?.AsArray();
                            if (FunctionsArray != null)
                            {
                                foreach (var FnNode in FunctionsArray)
                                {
                                    var comment = FnNode["Comment"]?.GetValue<string>() ?? string.Empty;

                                    var commentTags = new List<IncFuncEntry.CommentTag>();
                                    if (FnNode["CommentTags"] is JsonArray commentTagsArray)
                                    {
                                        foreach (var tagItem in commentTagsArray.OfType<JsonObject>())
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

                                    var parameters = FnNode["Parameters"]?.AsArray()
                                        ?.Select(p => p?.GetValue<string>() ?? string.Empty)
                                        .ToList() ?? new List<string>();

                                    var parametersDesc = FnNode["ParametersDesc"]?.AsArray()
                                        ?.Select(d => d?.GetValue<string>() ?? string.Empty)
                                        .ToList() ?? new List<string>();

                                    var Entry = new IncFuncEntry
                                    {
                                        Comment = comment,
                                        CommentTags = commentTags,
                                        FunctionName = FnNode["FunctionName"]?.GetValue<string>() ?? string.Empty,
                                        Function = FnNode["Function"]?.GetValue<string>() ?? string.Empty,
                                        Parameters = parameters,
                                        ParametersDesc = parametersDesc
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
                            var ConstantsArray = RootNode?["constants"]?.AsArray();
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
                            }
                        }
                        catch (JsonException ex)
                        {
                            throw new InvalidDataException($"解析 JSON 文件失败: {JsonPath}", ex);
                        }
                    }
                    NewFiles.Add(IncFile);
                }
                IncFilesByVersion[VersionName] = NewFiles;
            }
        }

        public static List<string> GetVersions()
            => IncFilesByVersion.Keys.OrderBy(v => v).ToList();

        public static List<IncFile> GetIncFilesByVersion(string version)
            => IncFilesByVersion.TryGetValue(version, out var list) ? list : new List<IncFile>();

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

    public class IncFuncEntry
    {
        public class CommentTag
        {
            public string Tag { get; set; } = string.Empty;
            public string Variable { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public string Comment { get; set; } = string.Empty;
        public List<CommentTag> CommentTags { get; set; } = new List<CommentTag>();
        public string FunctionName { get; set; } = string.Empty;
        public string Function { get; set; } = string.Empty;

        public List<string> Parameters { get; set; } = new List<string>();
        public List<string> ParametersDesc { get; set; } = new List<string>();
        public string Return { get; set; } = string.Empty;
    }

    public class IncConstantEntry
    {
        public string Comment { get; set; } = string.Empty;
        public List<IncFuncEntry.CommentTag> CommentTags { get; set; } = new();
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
