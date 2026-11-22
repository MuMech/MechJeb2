#if !CSHARP_7_3
#nullable disable
#endif
using System.Collections.Generic;
using System.Text;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>
    /// Writes a GitHub-flavoured Markdown table: the "Copy as Markdown" of a table (<see cref="TableModel.CopyMarkdown"/>: Delta-V
    /// Stats, PSG Stages) and of a custom info window's read-outs (name and value).
    /// <list type="bullet">
    /// <item><see cref="Header"/> writes the header row and the alignment row: <c>---:</c> for a right-aligned column (numbers),
    /// <c>---</c> otherwise.</item>
    /// <item><see cref="Row"/> writes one line, a cell per column: a missing cell is empty, an extra one is dropped.</item>
    /// <item>In headers and cells, <c>|</c> is written <c>\|</c> and a line break becomes a space (a cell holds one line); spaces
    /// at either end are trimmed; null is empty.</item>
    /// <item>Lines end with <c>\n</c>.</item>
    /// </list>
    /// </summary>
    public static class MarkdownTable
    {
        /// <summary>The header row of <paramref name="headers"/>, then the alignment row (<paramref name="right"/>[i]: right-aligned).</summary>
        public static void Header(StringBuilder into, IReadOnlyList<string> headers, IReadOnlyList<bool> right)
        {
            Row(into, headers, headers.Count);
            into.Append('|');
            for (int i = 0; i < headers.Count; i++)
                into.Append(right != null && i < right.Count && right[i] ? " ---: |" : " --- |");
            into.Append('\n');
        }

        /// <summary>One row of <paramref name="cells"/>, as many cells as the header has columns (<paramref name="columns"/>; -1: the cells').</summary>
        public static void Row(StringBuilder into, IReadOnlyList<string> cells, int columns = -1)
        {
            if (columns < 0) columns = cells.Count;
            into.Append('|');
            for (int i = 0; i < columns; i++)
            {
                into.Append(' ');
                Cell(into, i < cells.Count ? cells[i] : null);
                into.Append(" |");
            }
            into.Append('\n');
        }

        /// <summary>A cell's text: trimmed, <c>|</c> escaped, line breaks as spaces.</summary>
        public static void Cell(StringBuilder into, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            int a = 0, b = text.Length;
            while (a < b && char.IsWhiteSpace(text[a])) a++;
            while (b > a && char.IsWhiteSpace(text[b - 1])) b--;
            for (int i = a; i < b; i++)
            {
                char c = text[i];
                if (c == '|') into.Append('\\').Append('|');
                else if (c == '\r')
                {
                    if (i + 1 < b && text[i + 1] == '\n') continue;
                    into.Append(' ');
                }
                else if (c == '\n') into.Append(' ');
                else into.Append(c);
            }
        }
    }
}
