using Avalonia.Media.Immutable;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using AvaloniaEdit;

using SyntaxSystem.Validation;
using System.IO;
using ColorTextBlock.Avalonia;

namespace AmxxTutorial.Shared
{
    public static class TextEditorSyntaxSystem
    {
        public static string AutoFillEquivalent(string Former)
        {
            var Latter = Former switch
            {
                "{" => "}",
                "[" => "]",
                "(" => ")",
                _ => string.Empty
            };

            return Latter;
        }
        public static async Task FormatThenEnter(TextEditor Target)
        {
            if(Target.IsFocused)
            {
                var doc = Target.Document;
                if (doc is null) return;

                // 1. 定位当前行
                int caretOffset = Target.TextArea.Caret.Offset;
                DocumentLine line = doc.GetLineByOffset(caretOffset);
                int lineOffset = line.Offset;
                int lineLength = line.Length;

                // 2. 提取这一行原文
                string originalLine = doc.GetText(lineOffset, lineLength);

                // 3. 写入临时文件
                string ext = ".c";  // 根据实际语言选择扩展名
                string tmpIn = Path.ChangeExtension(Path.GetTempFileName(), ext);
                string tmpOut = tmpIn + ".out";
                await File.WriteAllTextAsync(tmpIn, originalLine);
            }
        }
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
