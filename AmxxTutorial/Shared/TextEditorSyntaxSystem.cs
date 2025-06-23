using Avalonia.Media.Immutable;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

using System;
using System.Threading.Tasks;
using Avalonia;
using AvaloniaEdit;

using SyntaxSystem.Validation;
using System.IO;
using ColorTextBlock.Avalonia;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using System.Diagnostics;
using System.Linq;


namespace AmxxTutorial.Shared
{
    public static partial class TextEditorSyntaxSystem
    {
        private readonly static string UncrustifyBin = "Assets/Utils/uncrustify";
        private readonly static string UncrustifyConfig = "Assets/Utils/uncrustify.cfg";

        /// <summary>
        /// 自动补全
        /// </summary>
        public static string AutoFillEquivalentRules(string Former)
        {
            var Latter = Former switch
            {
                "{" => "}",
                "[" => "]",
                "(" => ")",
                "'" => "'",
                "\"" => "\"",
                _ => string.Empty
            };

            return Latter;
        }
        public static void AutoFillEquivalent(TextEditor TextEditor, string Couple)
        {
            var Offset = TextEditor.CaretOffset;

            if (Offset > 0 && TextEditorSyntaxSystem.IsInsideString(TextEditor.Document, Offset - 1))
                return;

            TextEditor.Document.Insert(Offset, Couple);
            TextEditor.CaretOffset = Offset;
        }
        public static bool AutoFillEquivalentIndentation(TextEditor TextEditor)
        {
            if (!TextEditor.TextArea.IsFocused) return false;

            var Offset = TextEditor.CaretOffset;
            if (Offset < 1 || Offset >= TextEditor.Document.TextLength)
                return false;

            char Prev = TextEditor.Document.GetCharAt(Offset - 1);
            char Next = TextEditor.Document.GetCharAt(Offset);

            if (TextEditorSyntaxSystem.AutoFillEquivalentRules(Prev.ToString()) == Next.ToString())
            {
                // 插入两次换行
                TextEditor.Document.Replace(Offset, 0, "\n\n");

                // 找到新插入的“空行”和“闭合行”
                var BlankLine = TextEditor.Document.GetLineByOffset(Offset + 1);
                var CloseLine = TextEditor.Document.GetLineByOffset(BlankLine.EndOffset + 1);

                // 用内置策略缩进
                TextEditor.TextArea.IndentationStrategy?.IndentLine(TextEditor.Document, BlankLine);
                TextEditor.TextArea.IndentationStrategy?.IndentLine(TextEditor.Document, CloseLine);

                var PrevLine = TextEditor.Document.GetLineByNumber(BlankLine.LineNumber - 1);
                var BaseIndent = TextEditorSyntaxSystem.GetIndentation(TextEditor.Document, PrevLine);
                // 在上一行基础上，加一个缩进单位
                var OneLevel = BaseIndent + new string(' ', TextEditor.Options.IndentationSize);
                TextEditorSyntaxSystem.ReplaceIndent(TextEditor.Document, BlankLine, OneLevel);

                // 5) 定位光标到空行缩进末尾
                TextEditor.CaretOffset = BlankLine.Offset + OneLevel.Length;

                return true;
            }
            return false;
        }

        public static bool AutoFillEquivalentIndentation2(TextEditor TextEditor)
        {
            if (!TextEditor.TextArea.IsFocused) 
                return false;

            var Offset = TextEditor.CaretOffset;
            if (Offset < 1 || Offset >= TextEditor.Document.TextLength)
                return false;

            char Prev = TextEditor.Document.GetCharAt(Offset - 1);
            char Next = TextEditor.Document.GetCharAt(Offset);

            // 只有当 prev/next 构成我们的配对规则时才触发
            if (TextEditorSyntaxSystem.AutoFillEquivalentRules(Prev.ToString()) == Next.ToString())
            {
                using (TextEditor.Document.RunUpdate())
                {
                    // 插入两次换行
                    TextEditor.Document.Replace(Offset, 0, "\n\n");

                    // 找到新插入的空行和闭合行
                    var BlankLine = TextEditor.Document.GetLineByOffset(Offset + 1);
                    var CloseLine = TextEditor.Document.GetLineByOffset(BlankLine.EndOffset + 1);

                    // 1) 先给空行缩进：上一行缩进 + 一格
                    var PrevLine = TextEditor.Document.GetLineByNumber(BlankLine.LineNumber - 1);
                    var BaseIndent = TextEditorSyntaxSystem.GetIndentation(TextEditor.Document, PrevLine);
                    var OneLevel = BaseIndent + new string(' ', TextEditor.Options.IndentationSize);
                    TextEditorSyntaxSystem.ReplaceIndent(TextEditor.Document, BlankLine, OneLevel);

                    // 2) 给闭合行也手动替换成和上一行相同的 baseIndent
                    TextEditorSyntaxSystem.ReplaceIndent(TextEditor.Document, CloseLine, BaseIndent);

                    // 3) 光标移到空行末尾
                    TextEditor.CaretOffset = BlankLine.Offset + OneLevel.Length;
                }

                return true;
            }
            return false;
        }


        /// <summary>
        /// 修复c#缩进策略中 { } 和 switch => case 缩进不正确的问题
        /// </summary>
        public static void OverrideIndentation(TextEditor TextEditor, bool bNext = false)
        {
            var Doc = TextEditor.Document;
            var CaretOffset = TextEditor.CaretOffset;
            var CurLine = Doc.GetLineByOffset(CaretOffset);
            // 拿上一行
            if (CurLine.LineNumber > 1)
            {
                var PrevLine = Doc.GetLineByNumber(CurLine.LineNumber - 1);
                var BaseIndent = TextEditorSyntaxSystem.GetIndentation(Doc, PrevLine);
                // 直接把这一行前导空白替换成上一行相同缩进
                TextEditorSyntaxSystem.ReplaceIndent(Doc, CurLine, BaseIndent);

                if(bNext)
                {
                    var NextLine = Doc.GetLineByNumber(CurLine.LineNumber + 1);
                    TextEditorSyntaxSystem.ReplaceIndent(Doc, NextLine, BaseIndent);
                }
            }
        }
        /// <summary>
        /// 获取指定行缩进
        /// </summary>
        public static string GetIndentation(TextDocument Doc, DocumentLine Line)
        {
            var Text = Doc.GetText(Line.Offset, Line.Length);
            int i = 0;
            while (i < Text.Length && char.IsWhiteSpace(Text[i]))
                i++;
            return Text.Substring(0, i);
        }

        /// <summary>
        /// 把指定行的前导空白替换成 newIndent
        /// </summary>
        public static void ReplaceIndent(TextDocument Doc, DocumentLine Line, string NewIndent)
        {
            var FullText = Doc.GetText(Line.Offset, Line.Length);
            int OldIndentLen = FullText.Length - FullText.TrimStart().Length;
            Doc.Replace(Line.Offset, OldIndentLen, NewIndent);
        }

        /// <summary>
        /// 内容是否位于字符串内部
        /// </summary>
        public static bool IsInsideString(TextDocument Doc, int Offset)
        {
            string Prefix = Doc.GetText(0, Offset);
            // 统计双引号出现次数
            int QuoteCount = Prefix.Count(c => c == '"');
            // 出现奇数次就说明在字符串内部
            return (QuoteCount % 2) == 1;
        }

        /// <summary>
        /// 格式化
        /// </summary>
        public static async Task RegulateFormat(TextEditor Target)
        {
            if(Target.TextArea.IsFocused)
            {
                var Doc = Target.Document;
                if (Doc is null) return;

                // 1. 定位当前行
                int GlobalCaretOffset = Target.TextArea.Caret.Offset;
                DocumentLine Line = Doc.GetLineByOffset(GlobalCaretOffset);
                int LineStartOffset = Line.Offset;

                // 3. 提取原始行和它的缩进
                string OriginalLine = Doc.GetText(LineStartOffset, Line.Length);
                int IndentLen = 0;
                while (IndentLen < OriginalLine.Length && char.IsWhiteSpace(OriginalLine[IndentLen]))
                    IndentLen++;
                string Indent = OriginalLine.Substring(0, IndentLen);

                // 3. 写入临时文件
                string tmpIn = Path.ChangeExtension(Path.GetTempFileName(), ".sma");
                string tmpOut = tmpIn + ".temp";
                await File.WriteAllTextAsync(tmpIn, OriginalLine);

                try
                {
                    // 4. 调用 uncrustify
                    var _ProcessStartInfo = new ProcessStartInfo
                    {
                        FileName = UncrustifyBin,
                        Arguments = $"-c \"{UncrustifyConfig}\" -l C -f \"{tmpIn}\" -o \"{tmpOut}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    await Task.Run(() =>
                   {
                       using var _Process = Process.Start(_ProcessStartInfo);
                       _Process!.WaitForExit();
                   });

                    // 5. 读取并替换这一行
                    if (File.Exists(tmpOut))
                    {
                        string FormattedCode = (await File.ReadAllTextAsync(tmpOut)).TrimEnd('\r', '\n');
                        int fIndent = 0;
                        while (fIndent < FormattedCode.Length && char.IsWhiteSpace(FormattedCode[fIndent]))
                            fIndent++;
                        FormattedCode = FormattedCode.Substring(fIndent);

                        // 拼回原始缩进
                        string ResultLine = Indent + FormattedCode;

                        // 7. 在 UI 线程局部替换，并恢复光标
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Doc.Replace(LineStartOffset, Line.Length, ResultLine);
                        });
                    }
                }
                catch (Exception Ex)
                {
                    Debug.WriteLine($"格式化当前行失败：{Ex.Message}");
                }
                finally
                {
                    // 6. 清理临时文件
                    try { File.Delete(tmpIn); } catch { }
                    try { File.Delete(tmpOut); } catch { }
                }
            }
        }
    }
}

namespace SyntaxSystem.Validation
{
    /// <summary>
    /// Renders zigzag (squiggly) markers under text segments.
    /// </summary>
    public class MarkerRenderer : IBackgroundRenderer
    {
        public TextSegmentCollection<Marker> Markers { get; } = [];

        public KnownLayer Layer => KnownLayer.Background;

        void IBackgroundRenderer.Draw(TextView textView, DrawingContext drawingContext)
        {
            ArgumentNullException.ThrowIfNull(textView);
            ArgumentNullException.ThrowIfNull(drawingContext);

            if (Markers.Count == 0)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            int viewStart = visualLines[0].FirstDocumentLine.Offset;
            int viewEnd = visualLines[^1].LastDocumentLine.EndOffset;

            foreach (var marker in Markers.FindOverlappingSegments(viewStart, viewEnd - viewStart))
                marker.Draw(textView, drawingContext);
        }
    }

    public abstract class Marker : TextSegment
    {
        public abstract void Draw(TextView textView, DrawingContext drawingContext);
    }

    public class ZigzagMarker : Marker
    {
        private static readonly ImmutablePen s_defaultPen = new(Brushes.Red, 0.1, null, PenLineCap.Square, PenLineJoin.Round);

        public IPen? Pen { get; set; }

        public override void Draw(TextView textView, DrawingContext drawingContext)
        {
            foreach (Rect rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, this))
            {
                if (rect.Width <= 1 || rect.Height <= 1)
                {
                    // Current segment is inside a fold.
                    continue;
                }

                var pen = Pen ?? s_defaultPen;
                var start = rect.BottomLeft;
                var end = rect.BottomRight;
                var geometry = new StreamGeometry();
                using (var context = geometry.Open())
                {
                    context.BeginFigure(start, false);

                    const double zigLength = 3;
                    const double zigHeight = 3;
                    int numberOfZigs = (int)double.Round((end.X - start.X) / zigLength);
                    if (numberOfZigs < 2)
                        numberOfZigs = 2;

                    for (int i = 0; i < numberOfZigs; i++)
                    {
                        var p = new Point(
                            start.X + (i + 1) * zigLength,
                            start.Y - (i % 2) * zigHeight + 1);

                        context.LineTo(p);
                    }
                }

                drawingContext.DrawGeometry(null, pen, geometry);
            }
        }
    }
}
