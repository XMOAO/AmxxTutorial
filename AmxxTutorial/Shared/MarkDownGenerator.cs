using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using AmxxTutorial.ViewModels;
using CoreVideo;

namespace AmxxTutorial.Shared
{
    public static partial class MarkDownGenerator
    {
        public static string ConstructColumnOverview(IncTreeItem data)
        {
            var Builder = new StringBuilder();

            Builder.AppendLine($"#{Localization.GetString("FuncHelper_Overview") + data.Header}");
            Builder.AppendLine();
            Builder.AppendLine(" --- ");

            Builder.AppendLine($"| {Localization.GetString("FuncHelper_Usage_Table_Function")} | {Localization.GetString("FuncHelper_Usage_Table_Desc")} |");
            Builder.AppendLine("| --------------- | --------------- |");

            foreach(var Category in data.Children)
            {
                foreach(var Item in Category.Children)
                {
                    var Name = Item.FuncEntry?.FunctionName;
                    var Description = Item.FuncEntry?.Description;
                    string FormattedDescription = Description.ToString().Replace("\n", "\\n");

                    Builder.AppendLine($"| [{Name}](action://JumpToSub->{Name}) | {FormattedDescription} |");
                }
            }
            return Builder.ToString();
        }
        public static string ConstructFuncHelper(IncTreeItem data)
        {
            var Builder = new StringBuilder();

            Builder.AppendLine($"#{data.Header}");
            Builder.AppendLine();
            Builder.AppendLine(" --- ");

            // First Check If it is a function?
            if (data.FuncEntry != null)
            {
                Builder.AppendLine($"###{Localization.GetString("FuncHelper_Syntax")}");

                Builder.AppendLine("```Pawn");
                Builder.AppendLine($"{data.FuncEntry.Function}");
                Builder.AppendLine("```");
                Builder.AppendLine();

                Builder.AppendLine($"###{Localization.GetString("FuncHelper_Usage")}");
                Builder.AppendLine();

                // Check CommentTags for params.
                var bHasParams = false;
                if (data.FuncEntry.ParameterTags.Count() != 0)
                {
                    foreach (var Tag in data.FuncEntry.ParameterTags)
                    {
                        if (Tag.Tag != "param")
                            continue;

                        var ParamVar = Tag.Variable;
                        var ParamDesc = Tag.Description;

                        if (string.IsNullOrEmpty(ParamVar))
                            continue;

                        if (!bHasParams)
                        {
                            bHasParams = true;

                            Builder.AppendLine($"| {Localization.GetString("FuncHelper_Usage_Table_Param")} | {Localization.GetString("FuncHelper_Usage_Table_Desc")} |");
                            Builder.AppendLine("| --- | --- |");
                        }

                        Builder.AppendLine($"| {ParamVar} | {(string.IsNullOrEmpty(ParamDesc) ? Localization.GetString("FuncHelper_Usage_Table_NullDesc") : ParamDesc)} |");
                    }
                }
                else
                {
                    Builder.AppendLine($"{Localization.GetString("FuncHelper_Usage_Null")}");
                    Builder.AppendLine();
                }

                // Check Description.
                Builder.AppendLine($"###{Localization.GetString("FuncHelper_Desc/Comment")}");
                Builder.AppendLine();
                Builder.AppendLine($"{(string.IsNullOrEmpty(data.FuncEntry.Description) ? Localization.GetString("FuncHelper_Usage_Table_NullDesc") : data.FuncEntry.Description)}");
                Builder.AppendLine();

                // Check CommentTags for notes.
                /*if (data.FuncEntry.CommentTags.Count() != 0)
                {
                    foreach(var Tag in data.FuncEntry.CommentTags)
                    {
                        if (Tag.Tag != "note") continue;

                        var NoteDesc = Tag.Description;

                        if (string.IsNullOrEmpty(NoteDesc))
                            continue;

                        Builder.AppendLine($"###{Localization.GetString("FuncHelper_Note")}");
                        Builder.AppendLine();
                        Builder.AppendLine("```Pawn");
                        Builder.AppendLine($"{NoteDesc}");
                        Builder.AppendLine("```");
                        Builder.AppendLine();
                    }
                }*/

                // Check CommentTags for return.
                Builder.AppendLine($"###{Localization.GetString("FuncHelper_Return")}");
                Builder.AppendLine();

                var bHasReturn = false;
                /*if (data.FuncEntry.CommentTags.Count() != 0)
                {
                    foreach (var Tag in data.FuncEntry.CommentTags)
                    {
                        if (Tag.Tag != "return") 
                            continue;

                        var ReturnDesc = Tag.Description;

                        if (string.IsNullOrEmpty(ReturnDesc))
                            continue;

                        if (!bHasReturn)
                            bHasReturn = true;

                        Builder.AppendLine($"{ReturnDesc}");
                    }
                }
                else
                {
                    var Return = data.FuncEntry.Return;
                    if (!string.IsNullOrEmpty(Return))
                    {
                        if (!bHasReturn)
                            bHasReturn = true;

                        Builder.AppendLine($"{Return}");
                    }
                }*/

                if(!bHasReturn)
                    Builder.AppendLine($"{(string.IsNullOrEmpty(data.FuncEntry.Return) ? Localization.GetString("FuncHelper_Return_Null") : data.FuncEntry.Return)}");

                var Error = data.FuncEntry.Error;
                if (!string.IsNullOrEmpty(Error))
                {
                    Builder.AppendLine($"###{Localization.GetString("FuncHelper_Error")}");
                    Builder.AppendLine();
                    Builder.AppendLine($"{Error}");
                }
            }

            string FormattedStr = CodeBlockSupport().Replace(Builder.ToString(), match =>
            {
                if (match.Value.StartsWith("```"))
                    return match.Value;
                else
                    return "  \n";
            });

            return FormattedStr;
        }

        [GeneratedRegex(@"(```[\s\S]*?```)|\r?\n")]
        private static partial Regex CodeBlockSupport();
    }

    public class RedirectHyperlink : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public Action<string>? NavigateAction { get; set; }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        // parameter is only string.
        // The url described in markdown is passed to parameter as it is.
        // relative path remains relative path, absolute path remain absolute path.
        public void Execute(object parameter)
        {
            var urlText = (string)parameter;
            if (urlText != null && urlText.StartsWith("action://"))
            {
                var Action = urlText.Substring("action://".Length);

                if(Action.StartsWith("JumpToSub"))
                {
                    var SubHeader = Action.Substring("JumpToSub->".Length);
                    NavigateAction?.Invoke(SubHeader);
                }
            }
        }
    }
}
