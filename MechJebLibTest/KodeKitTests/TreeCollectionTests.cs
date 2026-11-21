/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.Text;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's tree collection (folding, search, checks) and its reorder among siblings, without KSP or Unity.</summary>
    public class TreeCollectionTests
    {
        // Orbit: Apoapsis, Periapsis, Time to apoapsis; Surface: Altitude, Speed; Target: Target apoapsis, Distance; Empty (a branch, no child)
        private static TreeCollection<int> Registry(TreeState state = null)
        {
            var t = new TreeCollection<int>(state);
            t.AddBranch("orbit", "Orbit");
            t.Add("apo", "Apoapsis", 1, "orbit", "Apoapsis");
            t.Add("pe", "Periapsis", 2, "orbit");
            t.Add("tta", "Time to apoapsis", 3, "orbit");
            t.AddBranch("surface", "Surface");
            t.Add("alt", "Altitude", 4, "surface");
            t.Add("spd", "Speed", 5, "surface");
            t.AddBranch("target", "Target");
            t.Add("tapo", "Target apoapsis", 6, "target");
            t.Add("dist", "Distance", 7, "target");
            t.AddBranch("empty", "Empty");
            return t;
        }

        private static string Rows<T>(TreeCollection<T> t)
        {
            var sb = new StringBuilder();
            foreach (TreeRow<T> r in t.VisibleRows)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(new string('-', r.Depth)).Append(r.Node.Key);
            }
            return sb.ToString();
        }

        [Fact]
        public void ClosedBranchesShowOnlyTheRoots()
        {
            TreeCollection<int> t = Registry();
            Assert.Equal("orbit surface target empty", Rows(t));
        }

        [Fact]
        public void OpeningABranchShowsItsChildrenUnderItInTheirOrder()
        {
            TreeCollection<int> t = Registry();
            Assert.True(t.Open("surface"));
            Assert.Equal("orbit surface -alt -spd target empty", Rows(t));
        }

        [Fact]
        public void ARowCarriesItsDepthAndWhetherItCanFoldAndIsOpen()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            IReadOnlyList<TreeRow<int>> rows = t.VisibleRows;
            Assert.True(rows[0].Expandable);
            Assert.True(rows[0].Open);
            Assert.Equal(0, rows[0].Depth);
            Assert.False(rows[1].Expandable);
            Assert.Equal(1, rows[1].Depth);
            Assert.False(rows[4].Open);
        }

        [Fact]
        public void ClosingABranchHidesItsChildren()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            Assert.True(t.Close("orbit"));
            Assert.Equal("orbit surface target empty", Rows(t));
        }

        [Fact]
        public void AClosedAncestorHidesItsDescendantsWhateverTheirOwnState()
        {
            var t = new TreeCollection<int>();
            t.AddBranch("a", "A");
            t.AddBranch("b", "B", 0, "a");
            t.Add("c", "C", 1, "b");
            t.Open("a");
            t.Open("b");
            Assert.Equal("a -b --c", Rows(t));
            t.Close("a");
            Assert.Equal("a", Rows(t));
            Assert.True(t.IsOpen("b"));
            t.Open("a");
            Assert.Equal("a -b --c", Rows(t));
        }

        [Fact]
        public void ToggleOpensAClosedBranchAndClosesAnOpenOne()
        {
            TreeCollection<int> t = Registry();
            Assert.True(t.Toggle("target"));
            Assert.True(t.IsOpen("target"));
            Assert.True(t.Toggle("target"));
            Assert.False(t.IsOpen("target"));
        }

        [Fact]
        public void ALeafOrABranchWithoutChildrenDoesNotOpen()
        {
            TreeCollection<int> t = Registry();
            Assert.False(t.Open("apo"));
            Assert.False(t.Toggle("empty"));
            Assert.False(t.IsOpen("empty"));
            Assert.False(t.VisibleRows[3].Expandable);
        }

        [Fact]
        public void AnEmptyBranchAddedAsOneStaysABranch()
        {
            TreeCollection<int> t = Registry();
            Assert.True(t.Get("empty").IsBranch);
            Assert.False(t.Get("apo").IsBranch);
        }

        [Fact]
        public void OpenAllOpensEveryBranchWithAChildAndCloseAllClosesThem()
        {
            TreeCollection<int> t = Registry();
            t.OpenAll();
            Assert.Equal("orbit -apo -pe -tta surface -alt -spd target -tapo -dist empty", Rows(t));
            Assert.False(t.IsOpen("empty"));
            t.CloseAll();
            Assert.Equal("orbit surface target empty", Rows(t));
        }

        [Fact]
        public void RevealOpensTheAncestorsOfANode()
        {
            var t = new TreeCollection<int>();
            t.AddBranch("a", "A");
            t.AddBranch("b", "B", 0, "a");
            t.Add("c", "C", 1, "b");
            t.Reveal("c");
            Assert.Equal(2, t.RowOf("c"));
        }

        [Fact]
        public void RowOfIsTheVisibleIndexOrMinusOneWhenHidden()
        {
            TreeCollection<int> t = Registry();
            t.Open("surface");
            Assert.Equal(3, t.RowOf("spd"));
            Assert.Equal(-1, t.RowOf("apo"));
            Assert.Equal(-1, t.RowOf("nope"));
        }

        [Fact]
        public void EveryChangeBumpsTheVersion()
        {
            TreeCollection<int> t = Registry();
            int v = t.Version;
            t.Open("orbit");
            Assert.True(t.Version > v);
            v = t.Version;
            t.Check("apo", true);
            Assert.True(t.Version > v);
            v = t.Version;
            t.Find("apo");
            Assert.True(t.Version > v);
        }

        [Fact]
        public void ANoOpDoesNotBumpTheVersion()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            int v = t.Version;
            Assert.False(t.Open("orbit"));
            Assert.False(t.Close("surface"));
            Assert.Equal(v, t.Version);
        }

        [Fact]
        public void ReadingTheRowsTwiceWithoutAChangeGivesTheSameList()
        {
            TreeCollection<int> t = Registry();
            IReadOnlyList<TreeRow<int>> a = t.VisibleRows, b = t.VisibleRows;
            Assert.Same(a, b);
        }

        [Fact]
        public void AKeyIsUniqueInItsTree()
        {
            TreeCollection<int> t = Registry();
            Assert.Throws<ArgumentException>(() => t.Add("apo", "Again", 9, "orbit"));
        }

        [Fact]
        public void AParentIsAddedBeforeItsChildren()
        {
            var t = new TreeCollection<int>();
            Assert.Throws<ArgumentException>(() => t.Add("c", "C", 1, "missing"));
        }

        [Fact]
        public void TheStateIsKeptByKeySoARebuiltTreeKeepsItsOpenBranches()
        {
            var state = new TreeState();
            TreeCollection<int> t = Registry(state);
            t.Open("target");
            TreeCollection<int> again = Registry(state);
            Assert.Equal("orbit surface target -tapo -dist empty", Rows(again));
        }

        [Fact]
        public void ClearKeepsTheStateForNodesAddedAgain()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            t.Clear();
            Assert.Empty(t.VisibleRows);
            t.AddBranch("orbit", "Orbit");
            t.Add("apo", "Apoapsis", 1, "orbit");
            Assert.Equal("orbit -apo", Rows(t));
        }

        // ---- search ----

        [Fact]
        public void ASearchOpensOnlyTheBranchesWithAMatch()
        {
            TreeCollection<int> t = Registry();
            t.Find("apo");
            Assert.True(t.IsOpen("orbit"));
            Assert.True(t.IsOpen("target"));
            Assert.False(t.IsOpen("surface"));
        }

        [Fact]
        public void ASearchClosesTheBranchesWithoutAMatchThatWereOpen()
        {
            TreeCollection<int> t = Registry();
            t.Open("surface");
            t.Find("apo");
            Assert.False(t.IsOpen("surface"));
        }

        [Fact]
        public void ASearchShowsOnlyTheMatchingLeavesAndEveryBranch()
        {
            TreeCollection<int> t = Registry();
            Assert.Equal(3, t.Find("apo"));
            Assert.Equal("orbit -apo -tta surface target -tapo empty", Rows(t));
        }

        [Fact]
        public void ABranchWithoutAMatchCannotOpenDuringASearch()
        {
            TreeCollection<int> t = Registry();
            t.Find("apo");
            Assert.False(t.Open("surface"));
            Assert.False(t.VisibleRows[3].Expandable);
        }

        [Fact]
        public void ASearchMatchesTheSearchKeyToo()
        {
            var t = new TreeCollection<int>();
            t.AddBranch("b", "Orbite");
            t.Add("x", "Apoastre", 1, "b", "Apoapsis");
            Assert.Equal(1, t.Find("apoapsis"));
            Assert.Equal("b -x", Rows(t));
        }

        [Fact]
        public void ASearchOpensTheAncestorsOfADeepMatch()
        {
            var t = new TreeCollection<int>();
            t.AddBranch("a", "A");
            t.AddBranch("b", "B", 0, "a");
            t.Add("c", "Apoapsis", 1, "b");
            t.AddBranch("d", "D");
            t.Add("e", "Speed", 2, "d");
            t.Find("apo");
            Assert.Equal("a -b --c d", Rows(t));
        }

        [Fact]
        public void TheMatchCountsAreKeptPerBranch()
        {
            TreeCollection<int> t = Registry();
            t.Find("apo");
            t.UpdateCounts();
            Assert.Equal(2, t.Get("orbit").MatchCount);
            Assert.Equal(3, t.Get("orbit").LeafCount);
            Assert.Equal(0, t.Get("surface").MatchCount);
            Assert.Equal(3, t.MatchCount);
        }

        [Fact]
        public void ASearchWithNoMatchClosesEveryBranchAndCountsNone()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            Assert.Equal(0, t.Find("zzzz"));
            Assert.Equal("orbit surface target empty", Rows(t));
        }

        [Fact]
        public void ClearingTheSearchShowsEveryLeafAndTheBranchesOpenBeforeIt()
        {
            TreeCollection<int> t = Registry();
            t.Open("surface");
            t.Find("apo");
            t.Find("apoa");
            Assert.Equal(7, t.Find(""));
            Assert.False(t.Searching);
            Assert.Equal("orbit surface -alt -spd target empty", Rows(t));
        }

        [Fact]
        public void FoldsDuringASearchAreDroppedWhenItEnds()
        {
            TreeCollection<int> t = Registry();
            t.Find("apo");
            t.Close("orbit");
            t.Find("");
            Assert.Equal("orbit surface target empty", Rows(t));
        }

        [Fact]
        public void ATreeRebuiltDuringASearchStillRestoresTheBranchesFromBeforeIt()
        {
            var state = new TreeState();
            TreeCollection<int> t = Registry(state);
            t.Open("surface");
            t.Find("apo");
            TreeCollection<int> again = Registry(state);
            again.Find("apo");
            again.Find("");
            Assert.Equal("orbit surface -alt -spd target empty", Rows(again));
        }

        [Fact]
        public void ANodeAddedDuringASearchIsMatchedToo()
        {
            TreeCollection<int> t = Registry();
            t.Find("apo");
            t.Add("ap2", "Apoapsis speed", 8, "surface");
            Assert.True(t.Get("ap2").Matched);
            Assert.False(t.Get("alt").Matched);
        }

        // ---- checks ----

        [Fact]
        public void CheckingAParentChecksEveryDescendant()
        {
            TreeCollection<int> t = Registry();
            t.Check("orbit", true);
            Assert.Equal(CheckState.All, t.StateOf("apo"));
            Assert.Equal(CheckState.All, t.StateOf("tta"));
            Assert.Equal(CheckState.All, t.StateOf("orbit"));
            Assert.Equal(CheckState.None, t.StateOf("surface"));
        }

        [Fact]
        public void APartlyCheckedBranchIsSome()
        {
            TreeCollection<int> t = Registry();
            t.Check("pe", true);
            Assert.Equal(CheckState.Some, t.StateOf("orbit"));
        }

        [Fact]
        public void ABranchIsAllWhenEveryLeafIsChecked()
        {
            TreeCollection<int> t = Registry();
            t.Check("alt", true);
            t.Check("spd", true);
            Assert.Equal(CheckState.All, t.StateOf("surface"));
        }

        [Fact]
        public void UncheckingAParentUnchecksEveryDescendant()
        {
            TreeCollection<int> t = Registry();
            t.Check("orbit", true);
            t.Check("orbit", false);
            Assert.Equal(CheckState.None, t.StateOf("apo"));
            Assert.Equal(CheckState.None, t.StateOf("orbit"));
        }

        [Fact]
        public void ToggleCheckOnASomeBranchChecksAll()
        {
            TreeCollection<int> t = Registry();
            t.Check("apo", true);
            t.ToggleCheck("orbit");
            Assert.Equal(CheckState.All, t.StateOf("orbit"));
            t.ToggleCheck("orbit");
            Assert.Equal(CheckState.None, t.StateOf("orbit"));
        }

        [Fact]
        public void ABranchWithoutLeavesHasItsOwnCheck()
        {
            TreeCollection<int> t = Registry();
            Assert.Equal(CheckState.None, t.StateOf("empty"));
            t.Check("empty", true);
            Assert.Equal(CheckState.All, t.StateOf("empty"));
        }

        [Fact]
        public void ANestedBranchCountsTheLeavesBelowIt()
        {
            var t = new TreeCollection<int>();
            t.AddBranch("a", "A");
            t.AddBranch("b", "B", 0, "a");
            t.Add("c", "C", 1, "b");
            t.Add("d", "D", 2, "b");
            t.Add("e", "E", 3, "a");
            t.Check("c", true);
            Assert.Equal(CheckState.Some, t.StateOf("b"));
            Assert.Equal(CheckState.Some, t.StateOf("a"));
            t.Check("b", true);
            t.Check("e", true);
            Assert.Equal(CheckState.All, t.StateOf("a"));
        }

        // ---- order and reorder ----

        [Fact]
        public void MoveChildMovesANodeWithItsSubtree()
        {
            var t = new TreeCollection<int>();
            t.AddBranch("a", "A");
            t.Add("a1", "A1", 1, "a");
            t.AddBranch("b", "B");
            t.Open("a");
            Assert.True(t.MoveChild(null, 0, 1));
            Assert.Equal("b a -a1", Rows(t));
        }

        [Fact]
        public void ATreeRowMovesAmongItsSiblings()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            var r = new TreeReorder<int>(t);
            // orbit -apo -pe -tta surface ...: apo (row 1) dragged to the gap above tta (row 3) lands at row 2
            Assert.True(r.Model.Begin(1));
            r.Model.Over(3);
            Assert.True(r.Model.Drop().Moved);
            Assert.Equal("orbit -pe -apo -tta surface target empty", Rows(t));
        }

        [Fact]
        public void ATreeRowCannotLeaveItsParent()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            var r = new TreeReorder<int>(t);
            Assert.True(r.Model.Begin(2));
            // past surface (row 4): clamped to the gap above it, the last of orbit's children
            r.Model.Over(6);
            Assert.Equal(4, r.Model.Gap);
            r.Model.Over(0);
            Assert.Equal(1, r.Model.Gap);
        }

        [Fact]
        public void AnOpenSiblingBranchsChildrenAreAWall()
        {
            TreeCollection<int> t = Registry();
            t.Open("orbit");
            var r = new TreeReorder<int>(t);
            // target (row 5) can't move above orbit's open children
            Assert.True(r.Model.Begin(5));
            r.Model.Over(0);
            Assert.Equal(4, r.Model.Gap);
        }

        [Fact]
        public void AClosedBranchMovesAmongTheRootsWithItsChildren()
        {
            TreeCollection<int> t = Registry();
            var r = new TreeReorder<int>(t);
            int calls = 0;
            r.Moved += (n, from, to) =>
            {
                calls++;
                Assert.Equal("target", n.Key);
                Assert.Equal(2, from);
                Assert.Equal(0, to);
            };
            Assert.True(r.Move(2, 0, ChangeSource.Agent));
            Assert.Equal(1, calls);
            t.Open("target");
            Assert.Equal("target -tapo -dist orbit surface empty", Rows(t));
        }

        [Fact]
        public void NoTreeReorderDuringASearch()
        {
            TreeCollection<int> t = Registry();
            t.Find("apo");
            var r = new TreeReorder<int>(t);
            Assert.False(r.Model.Begin(1));
            Assert.False(r.Move(1, 2, ChangeSource.Agent));
        }

        [Fact]
        public void TheRowCountFeedsTheVirtualList()
        {
            TreeCollection<int> t = Registry();
            var list = new VirtualListModel(t.VisibleRows.Count, 20, 100);
            Assert.Equal(4, list.Count);
            int before = t.VisibleRows.Count;
            t.Open("orbit");
            list.Inserted(1, t.VisibleRows.Count - before);
            Assert.Equal(7, list.Count);
        }
    }
}
