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


namespace AmxxTutorial.Shared
{
    public static partial class TextEditorSyntaxSystem
    {
        private readonly static string UncrustifyBin = "Assets/Utils/uncrustify";
        private readonly static string UncrustifyConfig = "Assets/Utils/uncrustify.cfg";

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

            if (Offset > 0 && TextEditorSyntaxSystem.IsInsideString(TextEditor.Document, Offset))
                return;

            TextEditor.Document.Insert(Offset, Couple);
            TextEditor.CaretOffset = Offset;
        }

        /// <summary>
        /// 修复c#缩进策略中 { } 和 switch => case 缩进不正确的问题
        /// </summary>
        public static void OverrideIndentation(TextEditor TextEditor)
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
            }
        }

        public static bool AutoFillEquivalentIndentation(TextEditor TextEditor)
        {
            if (!TextEditor.TextArea.IsFocused) return false;

            var offset = TextEditor.CaretOffset;
            if (offset < 1 || offset >= TextEditor.Document.TextLength)
                return false;

            char prev = TextEditor.Document.GetCharAt(offset - 1);
            char next = TextEditor.Document.GetCharAt(offset);

            if (TextEditorSyntaxSystem.AutoFillEquivalentRules(prev.ToString()) == next.ToString())
            {
                // 插入两次换行
                TextEditor.Document.Replace(offset, 0, "\n\n");

                // 找到新插入的“空行”和“闭合行”
                var blankLine = TextEditor.Document.GetLineByOffset(offset + 1);
                var closeLine = TextEditor.Document.GetLineByOffset(blankLine.EndOffset + 1);

                // 用内置策略缩进
                TextEditor.TextArea.IndentationStrategy?.IndentLine(TextEditor.Document, blankLine);
                TextEditor.TextArea.IndentationStrategy?.IndentLine(TextEditor.Document, closeLine);

                var prevLine = TextEditor.Document.GetLineByNumber(blankLine.LineNumber - 1);
                var baseIndent = TextEditorSyntaxSystem.GetIndentation(TextEditor.Document, prevLine);
                // 在上一行基础上，加一个缩进单位
                var oneLevel = baseIndent + new string(' ', TextEditor.Options.IndentationSize);
                TextEditorSyntaxSystem.ReplaceIndent(TextEditor.Document, blankLine, oneLevel);

                // 5) 定位光标到空行缩进末尾
                TextEditor.CaretOffset = blankLine.Offset + oneLevel.Length;

                return true;
            }
            return false;
        }

        /// <summary>
        /// 内容是否位于字符串内部
        /// </summary>
        public static bool IsInsideString(TextDocument Doc, int Offset)
        {
            // 找到当前行和行内相对偏移
            var Line = Doc.GetLineByOffset(Offset);
            int LineStart = Line.Offset;
            int PositionInLine = Offset - LineStart + 1;
            if (PositionInLine < 0) PositionInLine = 0;

            // 取出从行首到光标前的文本
            string TextUpToCursor = Doc.GetText(LineStart, Math.Min(PositionInLine, Line.Length));

            // 匹配：非转义的双引号开始后，一直到行尾都没有成对结束
            var _DoubleQuotePattern = DoubleQuotePattern();
            // 匹配：非转义的单引号开始后，一直到行尾都没有成对结束
            var _SingleQotePattern = SingleQotePattern();

            return _DoubleQuotePattern.IsMatch(TextUpToCursor) || _SingleQotePattern.IsMatch(TextUpToCursor);
        }

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

        public static async Task RegulateFormat(TextEditor Target)
        {
            if(Target.TextArea.IsFocused)
            {
                var doc = Target.Document;
                if (doc is null) return;

                // 1. 定位当前行
                int globalCaretOffset = Target.TextArea.Caret.Offset;
                DocumentLine line = doc.GetLineByOffset(globalCaretOffset);
                int lineStartOffset = line.Offset;

                // 3. 提取原始行和它的缩进
                string originalLine = doc.GetText(lineStartOffset, line.Length);
                int indentLen = 0;
                while (indentLen < originalLine.Length && char.IsWhiteSpace(originalLine[indentLen]))
                    indentLen++;
                string indent = originalLine.Substring(0, indentLen);


                // 3. 写入临时文件
                string ext = ".sma";  // 根据实际语言选择扩展名
                string tmpIn = Path.ChangeExtension(Path.GetTempFileName(), ext);
                string tmpOut = tmpIn + ".temp";
                await File.WriteAllTextAsync(tmpIn, originalLine);

                try
                {
                    // 4. 调用 uncrustify
                    var psi = new ProcessStartInfo
                    {
                        FileName = UncrustifyBin,
                        Arguments = $"-c \"{UncrustifyConfig}\" -l C -f \"{tmpIn}\" -o \"{tmpOut}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    await Task.Run(() =>
                    {
                        using var proc = Process.Start(psi);
                        proc!.WaitForExit();
                    });

                    // 5. 读取并替换这一行
                    if (File.Exists(tmpOut))
                    {
                        string formattedCode = (await File.ReadAllTextAsync(tmpOut))
                                   .TrimEnd('\r', '\n');
                        int fIndent = 0;
                        while (fIndent < formattedCode.Length && char.IsWhiteSpace(formattedCode[fIndent]))
                            fIndent++;
                        formattedCode = formattedCode.Substring(fIndent);

                        // 拼回原始缩进
                        string finalLine = indent + formattedCode;


                        // 7. 在 UI 线程局部替换，并恢复光标
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            doc.Replace(lineStartOffset, line.Length, finalLine);

                            // 恢复光标：新偏移 = 行起始 + min(原相对偏移, 新行长度)
                            //int newRelative = Math.Min(relativeCaretPos, finalLine.Length);
                            //Target.TextArea.Caret.Offset = lineStartOffset + newRelative;
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"格式化当前行失败：{ex.Message}");
                }
                finally
                {
                    // 6. 清理临时文件
                    try { File.Delete(tmpIn); } catch { }
                    try { File.Delete(tmpOut); } catch { }
                }
            }
        }

        [GeneratedRegex(@"(?<!\\)""([^""\\]|\\.)*$")]
        public static partial Regex DoubleQuotePattern();
        [GeneratedRegex(@"(?<!\\)'([^'\\]|\\.)*$")]
        public static partial Regex SingleQotePattern();
    }

}

namespace SyntaxSystem.Validation
{
    /// <summary>
    /// Renders zigzag (squiggly) markers under text segments.
    /// </summary>
    public class ZigzagMarkerRenderer : IBackgroundRenderer
    {
        // Holds all markers to draw
        public TextSegmentCollection<ZigzagMarker> Markers { get; } = new TextSegmentCollection<ZigzagMarker>(null);

        public KnownLayer Layer => KnownLayer.Background;

        /// <summary>
        /// Draws all markers overlapping the visible text.
        /// </summary>
        void IBackgroundRenderer.Draw(TextView textView, DrawingContext drawingContext)
        {
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

        /// <summary>
        /// Adds a new zigzag marker.
        /// </summary>
        public ZigzagMarker Add(int startOffset, int length, IPen? pen = null)
        {
            var m = new ZigzagMarker(startOffset, length, pen);
            Markers.Add(m);
            return m;
        }

        /// <summary>
        /// Represents a squiggly marker segment.
        /// </summary>
        public class ZigzagMarker : TextSegment
        {
            private static readonly ImmutablePen s_defaultPen = new ImmutablePen(Brushes.Red, 0.75);
            public IPen Pen { get; }

            public ZigzagMarker(int startOffset, int length, IPen? pen)
            {
                StartOffset = startOffset;
                Length = length;
                Pen = pen ?? s_defaultPen;
            }

            internal void Draw(TextView textView, DrawingContext drawingContext)
            {
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, this))
                {
                    if (rect.Width <= 1 || rect.Height <= 1)
                        continue; // inside a fold

                    var start = rect.BottomLeft;
                    var end = rect.BottomRight;
                    var geometry = new StreamGeometry();
                    using (var ctx = geometry.Open())
                    {
                        ctx.BeginFigure(start, false);
                        const double zigLength = 3;
                        const double zigHeight = 3;
                        int count = (int)Math.Round((end.X - start.X) / zigLength);
                        if (count < 2) count = 2;
                        for (int i = 0; i < count; i++)
                        {
                            var p = new Point(
                                start.X + (i + 1) * zigLength,
                                start.Y - (i % 2) * zigHeight + 1);
                            ctx.LineTo(p);
                        }
                    }

                    drawingContext.DrawGeometry(null, Pen, geometry);
                }
            }
        }
    }
}
