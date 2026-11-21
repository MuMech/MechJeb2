/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    public class TableModelTests
    {
        // a stage-stats-like table: column 0 the stage (not hideable), 1 a mass in t, 2 a Δv in m/s
        private sealed class Stats
        {
            public readonly List<double> Mass = new List<double> { 10.5, 4.25, 1 };
            public readonly bool[] Visible = { true, true, true };
            public readonly string[] Headers = { "Stage", "Mass", "ΔV" };
            public int Current = -1;
            public long Version;
            public int Formats;
            public readonly List<string> Hidden = new List<string>();

            public TableModel Make(bool withVersion = false)
            {
                var t = new TableModel(() => Mass.Count, Format) { Label = "Stage stats", UnitMarkup = "<a>" }
                    .WithHighlight(() => Current)
                    .WithColumnState(k => Visible[k], k => Headers[k])
                    .WithHide((k, s) =>
                    {
                        Visible[k] = false;
                        Hidden.Add(k + ":" + s);
                    });
                if (withVersion) t.WithVersion(() => Version);
                t.AddColumn(0, "Stage").Hideable = false;
                t.AddColumn(1, "Mass", TableWidthRule.Content, 0, " t");
                t.AddColumn(2, "ΔV");
                return t;
            }

            private void Format(int row, int key, CharBuffer into, ref string unit)
            {
                Formats++;
                if (key == 0) into.AppendInt(Mass.Count - 1 - row);
                else if (key == 1) into.Append(Mass[row], 3);
                else
                {
                    into.Append(Mass[row] * 100, 0);
                    unit = " m/s";
                }
            }
        }

        private static string Text(TableModel t, int row, int col) => t.Cell(row, col).ToString();

        private static string Shown(TableModel t, int row, int col)
        {
            var sb = new StringBuilder();
            t.AppendShown(sb, row, col);
            return sb.ToString();
        }

        [Fact]
        public void ACellIsItsValueThenTheMarkupAndItsUnit()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            Assert.Equal("2", Text(t, 0, 0));
            Assert.Equal("10.500<a> t", Text(t, 0, 1));
            Assert.Equal("1050<a> m/s", Text(t, 0, 2));
            Assert.Equal("10.500 t", Shown(t, 0, 1));
        }

        [Fact]
        public void ACellWithoutAUnitHasNoMarkup()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            Assert.Equal("0", Text(t, 2, 0));
        }

        [Fact]
        public void TheFormatterCanChangeTheUnitPerCell()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            Assert.EndsWith(" m/s", Text(t, 1, 2)); // the column has none, the formatter gives one
        }

        [Fact]
        public void TheFirstSyncChangesTheShapeAndFormats()
        {
            var s = new Stats();
            TableModel t = s.Make();
            TableChange c = t.Sync();
            Assert.True(c.HasFlag(TableChange.Shape) && c.HasFlag(TableChange.Values) && c.HasFlag(TableChange.Widths));
            Assert.Equal(3, t.RowCount);
            Assert.Equal(1, t.ShapeVersion);
        }

        [Fact]
        public void WithoutASourceVersionEverySyncFormatsAgain()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            int n = s.Formats;
            s.Mass[0] = 11;
            Assert.Equal(TableChange.Values, t.Sync());
            Assert.Equal(2 * n, s.Formats);
            Assert.Equal("11.000<a> t", Text(t, 0, 1));
        }

        [Fact]
        public void WithASourceVersionOnlyANewVersionFormatsAgain()
        {
            var s = new Stats();
            TableModel t = s.Make(true);
            t.Sync();
            int n = s.Formats;
            s.Mass[0] = 11;
            Assert.Equal(TableChange.None, t.Sync());
            Assert.Equal(n, s.Formats);
            s.Version++;
            Assert.Equal(TableChange.Values, t.Sync());
            Assert.Equal("11.000<a> t", Text(t, 0, 1));
        }

        [Fact]
        public void InvalidateFormatsAtTheNextSyncWithTheSameVersion()
        {
            var s = new Stats();
            TableModel t = s.Make(true);
            t.Sync();
            s.Mass[0] = 12;
            t.Invalidate();
            t.Sync();
            Assert.Equal("12.000<a> t", Text(t, 0, 1));
        }

        [Fact]
        public void ANewRowCountChangesTheShape()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            s.Mass.Add(2);
            Assert.True(t.Sync().HasFlag(TableChange.Shape));
            Assert.Equal(4, t.RowCount);
            Assert.Equal(2, t.ShapeVersion);
        }

        [Fact]
        public void AHiddenColumnIsNotFormattedAndChangesTheShape()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            s.Visible[2] = false;
            int before = s.Formats;
            Assert.True(t.Sync().HasFlag(TableChange.Shape));
            Assert.False(t.Columns[2].Visible);
            Assert.Equal(before + 6, s.Formats); // 3 rows × 2 visible columns
            Assert.Equal(2, t.VisibleColumns);
        }

        [Fact]
        public void AContentColumnsWidthOnlyGrows()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            Assert.Equal(8, t.Columns[1].ContentChars); // "10.500" + " t"
            s.Mass[0] = 1;
            Assert.False(t.Sync().HasFlag(TableChange.Widths));
            Assert.Equal(8, t.Columns[1].ContentChars);
            s.Mass[0] = 100;
            Assert.True(t.Sync().HasFlag(TableChange.Widths));
            Assert.Equal(9, t.Columns[1].ContentChars);
        }

        [Fact]
        public void ANewHeaderFitsTheColumnAgain()
        {
            var s = new Stats();
            TableModel t = s.Make();
            s.Mass[0] = 100;
            t.Sync();
            Assert.Equal(9, t.Columns[1].ContentChars);
            s.Mass[0] = 1;
            s.Headers[1] = "Masse";
            TableChange c = t.Sync();
            Assert.True(c.HasFlag(TableChange.Shape) && c.HasFlag(TableChange.Widths));
            Assert.Equal("Masse", t.Columns[1].Header);
            Assert.Equal(7, t.Columns[1].ContentChars); // the widest now: "4.250 t"
        }

        [Fact]
        public void ResetWidthsMeasuresEveryColumnAfresh()
        {
            var s = new Stats();
            TableModel t = s.Make(true);
            s.Mass[0] = 100;
            t.Sync();
            s.Mass[0] = 1;
            int widths = t.WidthVersion;
            t.ResetWidths();
            Assert.Equal(widths + 1, t.WidthVersion); // a host measures every column again: one may narrow
            Assert.True(t.Sync().HasFlag(TableChange.Widths));
            Assert.Equal(7, t.Columns[1].ContentChars);
        }

        [Fact]
        public void TheHighlightedRowIsTheOwnersAndOneOutsideTheRowsIsNone()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            Assert.Equal(-1, t.Highlighted);
            s.Current = 1;
            Assert.True(t.Sync().HasFlag(TableChange.Highlight));
            Assert.Equal(1, t.Highlighted);
            s.Current = 7;
            t.Sync();
            Assert.Equal(-1, t.Highlighted);
        }

        [Fact]
        public void HideAsksTheOwnerWithTheSourceAndSyncs()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            var changes = new List<TableChange>();
            t.Changed += c => changes.Add(c);
            Assert.True(t.Hide(1, ChangeSource.Pointer));
            Assert.Equal(new[] { "1:Pointer" }, s.Hidden);
            Assert.False(t.Columns[1].Visible);
            Assert.Equal(ChangeSource.Pointer, t.LastSource);
            Assert.Contains(changes, c => c.HasFlag(TableChange.Shape));
        }

        [Fact]
        public void AColumnHiddenInAnyTableIsReportedWithItsSourceToTheWholeUisObservers()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            var heard = new List<string>();
            // other tests' tables may report meanwhile: only this one's are kept
            Action<TableModel, TableColumn, ChangeSource> observer = (m, c, src) =>
            {
                if (m == t) lock (heard) heard.Add(c.Key + ":" + src);
            };
            TableModel.AnyHidden += observer;
            try
            {
                Assert.False(t.Hide(0, ChangeSource.Agent)); // not hideable: no report
                Assert.True(t.Hide(1, ChangeSource.Agent));
            }
            finally
            {
                TableModel.AnyHidden -= observer;
            }
            Assert.Equal(new[] { "1:Agent" }, heard);
        }

        [Fact]
        public void AColumnThatIsNotHideableOrAlreadyHiddenRefusesHide()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            Assert.False(t.Hide(0, ChangeSource.Pointer));
            t.Hide(2, ChangeSource.Pointer);
            Assert.False(t.Hide(2, ChangeSource.Pointer));
            Assert.Equal(new[] { "2:Pointer" }, s.Hidden);
        }

        [Fact]
        public void ATableWithoutAHideCallbackRefusesHide()
        {
            var t = new TableModel(() => 1, (int r, int k, CharBuffer b, ref string u) => b.Append('x'));
            t.AddColumn(0, "A");
            t.Sync();
            Assert.False(t.Hide(0, ChangeSource.Agent));
        }

        [Fact]
        public void FormattingAllocatesNothingOnceTheBuffersExist()
        {
            var s = new Stats();
            TableModel t = s.Make();
            for (int i = 0; i < 10; i++) t.Sync();
            AssertionExtensions.ShouldNotAllocate(() =>
            {
                for (int i = 0; i < 1000; i++) t.Sync();
            });
        }

        [Fact]
        public void ARowSelectionIsKeptByKeyWhenRowsMove()
        {
            var keys = new List<long> { 30, 20, 10 };
            var t = new TableModel(() => keys.Count, (int r, int k, CharBuffer b, ref string u) => b.AppendInt(keys[r])).WithRowKeys(r => keys[r]);
            t.AddColumn(0, "Stage");
            t.Sync();
            t.Selection.ToggleRow(1, ChangeSource.Pointer);
            keys.Insert(0, 40);
            t.Sync();
            Assert.True(t.Selection.IsRowSelected(2));
            Assert.False(t.Selection.IsRowSelected(1));
        }

        [Fact]
        public void ARowThatVanishesLeavesTheSelection()
        {
            var keys = new List<long> { 30, 20, 10 };
            var t = new TableModel(() => keys.Count, (int r, int k, CharBuffer b, ref string u) => b.AppendInt(keys[r])).WithRowKeys(r => keys[r]);
            t.AddColumn(0, "Stage");
            t.Sync();
            t.Selection.ToggleRow(0, ChangeSource.Pointer);
            t.Selection.ToggleRow(2, ChangeSource.Pointer);
            keys.RemoveAt(0);
            Assert.True(t.Sync().HasFlag(TableChange.Selection));
            Assert.Equal(1, t.Selection.RowCount);
            Assert.True(t.Selection.IsRowSelected(1));
        }

        [Fact]
        public void ToggleRowSelectsThenUnselects()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.ToggleRow(1, ChangeSource.Pointer);
            Assert.True(t.Selection.IsRowSelected(1));
            t.Selection.ToggleRow(1, ChangeSource.Agent);
            Assert.False(t.Selection.IsRowSelected(1));
            Assert.Equal(ChangeSource.Agent, t.LastSource);
        }

        [Fact]
        public void ARangeOfRowsFollowsTheTablesOrderWhicheverEndComesFirst()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.SelectRows(2, 1, ChangeSource.Pointer);
            Assert.False(t.Selection.IsRowSelected(0));
            Assert.True(t.Selection.IsRowSelected(1) && t.Selection.IsRowSelected(2));
            t.Selection.SelectAll(ChangeSource.Pointer);
            Assert.Equal(3, t.Selection.RowCount);
            t.Selection.Clear(ChangeSource.Pointer);
            Assert.Equal(0, t.Selection.RowCount);
        }

        [Fact]
        public void ACellRangeContainsTheCellsBetweenItsAnchorAndFocusButNotHiddenOnes()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.SelectCells(2, 2, 1, 1, ChangeSource.Pointer);
            Assert.True(t.Selection.Contains(1, 1) && t.Selection.Contains(2, 2) && t.Selection.Contains(1, 2));
            Assert.False(t.Selection.Contains(0, 1) || t.Selection.Contains(1, 0));
            s.Visible[2] = false;
            t.Sync();
            Assert.False(t.Selection.Contains(1, 2));
        }

        [Fact]
        public void ExtendToMovesTheFocusCellOrStartsARange()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.ExtendTo(0, 0, ChangeSource.Pointer);
            Assert.True(t.Selection.HasRange && t.Selection.Contains(0, 0) && !t.Selection.Contains(1, 1));
            t.Selection.ExtendTo(1, 1, ChangeSource.Pointer);
            Assert.True(t.Selection.Contains(1, 1) && t.Selection.Contains(0, 1));
        }

        [Fact]
        public void ACellRangeIsClampedToTheRowsLeft()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.SelectCells(0, 0, 2, 2, ChangeSource.Pointer);
            s.Mass.RemoveAt(2);
            Assert.True(t.Sync().HasFlag(TableChange.Selection));
            Assert.Equal(1, t.Selection.Bottom);
        }

        [Fact]
        public void EverySelectionChangeFiresTheTablesChangedWithItsSource()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            var changes = new List<TableChange>();
            int own = 0;
            t.Changed += c => changes.Add(c);
            t.Selection.Changed += () => own++;
            t.Selection.ToggleRow(0, ChangeSource.Keyboard);
            t.Selection.SelectCells(0, 0, 1, 1, ChangeSource.Agent);
            Assert.Equal(new[] { TableChange.Selection, TableChange.Selection }, changes);
            Assert.Equal(2, own);
            Assert.Equal(ChangeSource.Agent, t.LastSource);
        }

        [Fact]
        public void CopyTextIsTheSelectedCellsAsShownTabAndNewlineSeparated()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.SelectCells(0, 1, 1, 2, ChangeSource.Pointer);
            var sb = new StringBuilder();
            t.CopyText(sb, true);
            Assert.Equal("Mass\tΔV\n10.500 t\t1050 m/s\n4.250 t\t425 m/s\n", sb.ToString());
        }

        [Fact]
        public void CopyTextOfSelectedRowsTakesEveryVisibleColumn()
        {
            var s = new Stats();
            TableModel t = s.Make();
            s.Visible[1] = false;
            t.Sync();
            t.Selection.ToggleRow(2, ChangeSource.Pointer);
            var sb = new StringBuilder();
            t.CopyText(sb);
            Assert.Equal("0\t100 m/s\n", sb.ToString());
        }

        [Fact]
        public void TheDescriptionNamesTheTableItsVisibleHeadersAndItsRows()
        {
            var s = new Stats { Current = 1 };
            TableModel t = s.Make();
            s.Visible[1] = false;
            t.Sync();
            t.Selection.ToggleRow(2, ChangeSource.Pointer);
            var parts = new List<PartDescription>();
            t.Describe(parts);
            Assert.Equal(new[] { "table", "column.0", "column.2", "row.0", "row.1", "row.2" }, parts.Select(p => p.Name).ToArray());
            Assert.Equal(PartRole.Table, parts[0].Role);
            Assert.Equal("ΔV", parts[2].Label);
            Assert.True(parts[4].Has(PartFlags.Current) && !parts[4].Has(PartFlags.On));
            Assert.True(parts[5].Has(PartFlags.On));
            Assert.Equal("1", parts[4].Label);
            Assert.Empty(Patterns.Check(t));
        }

        [Fact]
        public void ACellRangeIsDescribedCellByCell()
        {
            var s = new Stats();
            TableModel t = s.Make();
            t.Sync();
            t.Selection.SelectCells(0, 1, 0, 2, ChangeSource.Pointer);
            var parts = new List<PartDescription>();
            t.Describe(parts);
            PartDescription[] cells = parts.Where(p => p.Role == PartRole.Cell).ToArray();
            Assert.Equal(new[] { "cell.0.1", "cell.0.2" }, cells.Select(p => p.Name).ToArray());
            Assert.Equal("10.500 t", cells[0].Label);
        }

        [Fact]
        public void AColumnWithAnEmptyHeaderIsDescribedByItsName()
        {
            var t = new TableModel(() => 1, (int r, int k, CharBuffer b, ref string u) => b.Append('x')) { Label = "PSG Stages" };
            t.AddColumn(0, "Stage");
            t.AddColumn(1, "").Name = "Actions";
            t.Sync();
            var parts = new List<PartDescription>();
            t.Describe(parts);
            Assert.Equal("Actions", parts.Single(p => p.Name == "column.1").Label);
            Assert.Empty(Patterns.Check(t));
        }

        [Fact]
        public void AFixedColumnKeepsItsWidthRuleAndWidth()
        {
            var t = new TableModel(() => 0, (int r, int k, CharBuffer b, ref string u) => { });
            TableColumn c = t.AddColumn(3, "Guided", TableWidthRule.Fixed, 52);
            Assert.Equal(TableWidthRule.Fixed, c.WidthRule);
            Assert.Equal(52, c.Width);
            Assert.Same(c, t.ColumnOf(3));
            Assert.Null(t.ColumnOf(4));
        }
    }
}
