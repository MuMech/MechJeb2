/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System.Collections.Generic;
using System.Text;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    public class MarkdownTableTests
    {
        [Fact]
        public void TheHeaderIsTheHeaderRowThenTheAlignmentRow()
        {
            var sb = new StringBuilder();
            MarkdownTable.Header(sb, new[] { "Name", "Value" }, new[] { false, true });
            Assert.Equal("| Name | Value |\n| --- | ---: |\n", sb.ToString());
        }

        [Fact]
        public void APipeInACellIsEscapedAndALineBreakIsASpace()
        {
            var sb = new StringBuilder();
            MarkdownTable.Row(sb, new[] { "a|b", "one\r\ntwo\nthree" });
            Assert.Equal("| a\\|b | one two three |\n", sb.ToString());
        }

        [Fact]
        public void CellsAreTrimmedAndNullIsEmpty()
        {
            var sb = new StringBuilder();
            MarkdownTable.Row(sb, new[] { "  12 m/s ", null });
            Assert.Equal("| 12 m/s |  |\n", sb.ToString());
        }

        [Fact]
        public void ARowHasAsManyCellsAsTheHeaderHasColumns()
        {
            var sb = new StringBuilder();
            MarkdownTable.Row(sb, new[] { "a" }, 3);
            MarkdownTable.Row(sb, new[] { "a", "b", "c" }, 2);
            Assert.Equal("| a |  |  |\n| a | b |\n", sb.ToString());
        }

        [Fact]
        public void AHeaderPipeIsEscapedToo()
        {
            var sb = new StringBuilder();
            MarkdownTable.Header(sb, new List<string> { "in|out" }, null);
            Assert.Equal("| in\\|out |\n| --- |\n", sb.ToString());
        }

        // ---- a table model's copies (Delta-V Stats, PSG Stages) ----

        // column 0 the stage, 1 a mass in t, 2 a Δv in m/s, 3 a column of buttons
        private sealed class Stages
        {
            public readonly double[] Mass = { 10.5, 4.25, 1 };
            public readonly bool[] Visible = { true, true, true, true };

            public TableModel Make()
            {
                var t = new TableModel(() => Mass.Length, Format) { UnitMarkup = "<a>" }
                    .WithColumnState(k => Visible[k], null);
                t.AddColumn(0, "Stage");
                t.AddColumn(1, "Mass", TableWidthRule.Content, 0, " t");
                t.AddColumn(2, "Δv|vac");
                t.AddColumn(3, "").Copied = false;
                return t;
            }

            private void Format(int row, int key, CharBuffer into, ref string unit)
            {
                if (key == 0) into.AppendInt(Mass.Length - 1 - row);
                else if (key == 1) into.Append(Mass[row], 1);
                else if (key == 2)
                {
                    into.Append(Mass[row] * 100, 0);
                    unit = " m/s";
                }
                else into.Append("End here");
            }
        }

        private static string Markdown(TableModel t, out int rows)
        {
            var sb = new StringBuilder();
            rows = t.CopyMarkdown(sb);
            return sb.ToString();
        }

        [Fact]
        public void WithNothingSelectedTheMarkdownHoldsEveryRowAndEveryShownColumnRightAligned()
        {
            var s = new Stages();
            TableModel t = s.Make();
            t.Sync();
            string md = Markdown(t, out int rows);
            Assert.Equal(3, rows);
            Assert.Equal("| Stage | Mass | Δv\\|vac |\n| ---: | ---: | ---: |\n| 2 | 10.5 t | 1050 m/s |\n| 1 | 4.3 t | 425 m/s |\n| 0 | 1.0 t | 100 m/s |\n", md);
        }

        [Fact]
        public void TheMarkdownHoldsOnlyTheSelectedRowsUnitsKept()
        {
            var s = new Stages();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.ToggleRow(2, ChangeSource.Pointer);
            t.Selection.ToggleRow(0, ChangeSource.Pointer);
            string md = Markdown(t, out int rows);
            Assert.Equal(2, rows);
            Assert.Equal("| Stage | Mass | Δv\\|vac |\n| ---: | ---: | ---: |\n| 2 | 10.5 t | 1050 m/s |\n| 0 | 1.0 t | 100 m/s |\n", md);
        }

        [Fact]
        public void AHiddenColumnIsLeftOutOfTheMarkdown()
        {
            var s = new Stages();
            TableModel t = s.Make();
            s.Visible[1] = false;
            t.Sync();
            t.Selection.ToggleRow(1, ChangeSource.Pointer);
            Assert.Equal("| Stage | Δv\\|vac |\n| ---: | ---: |\n| 1 | 425 m/s |\n", Markdown(t, out _));
        }

        [Fact]
        public void ACellRangeGivesItsColumnsAndRows()
        {
            var s = new Stages();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.SelectCells(1, 1, 2, 2, ChangeSource.Pointer);
            Assert.Equal("| Mass | Δv\\|vac |\n| ---: | ---: |\n| 4.3 t | 425 m/s |\n| 1.0 t | 100 m/s |\n", Markdown(t, out _));
        }

        [Fact]
        public void PlainCopyWithNothingSelectedIsEveryRowWithTheHeaders()
        {
            var s = new Stages();
            TableModel t = s.Make();
            t.Sync();
            var sb = new StringBuilder();
            Assert.Equal(3, t.CopyRows(sb));
            Assert.Equal("Stage\tMass\tΔv|vac\n2\t10.5 t\t1050 m/s\n1\t4.3 t\t425 m/s\n0\t1.0 t\t100 m/s\n", sb.ToString());
        }

        [Fact]
        public void PlainCopyOfSelectedRowsCountsThem()
        {
            var s = new Stages();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.ToggleRow(1, ChangeSource.Pointer);
            var sb = new StringBuilder();
            Assert.Equal(1, t.CopyRows(sb));
            Assert.Equal("Stage\tMass\tΔv|vac\n1\t4.3 t\t425 m/s\n", sb.ToString());
        }

        [Fact]
        public void AHeaderPaddedForItsWidthIsCopiedWithoutItsSpaces()
        {
            var t = new TableModel(() => 1, (int row, int key, CharBuffer into, ref string unit) => into.AppendInt(key));
            t.AddColumn(0, "Stage   ");
            t.AddColumn(1, "TWR   ");
            t.Sync();
            var sb = new StringBuilder();
            t.CopyRows(sb);
            Assert.Equal("Stage\tTWR\n0\t1\n", sb.ToString());
            sb.Length = 0;
            t.CopyMarkdown(sb);
            Assert.StartsWith("| Stage | TWR |\n", sb.ToString());
        }

        [Fact]
        public void ATableWithNoShownColumnCopiesNothing()
        {
            var s = new Stages();
            TableModel t = s.Make();
            for (int i = 0; i < 4; i++) s.Visible[i] = false;
            t.Sync();
            Assert.Equal("", Markdown(t, out int rows));
            Assert.Equal(0, rows);
        }
    }
}
