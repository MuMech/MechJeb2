/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System.Collections.Generic;
using System.Linq;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The command palette's ranking, its two-step run of flight commands and its cancel paths (PaletteModel), without KSP or Unity.</summary>
    public class PaletteModelTests
    {
        private sealed class World
        {
            public bool         AscentOpen, DeltaVOpen, Engaged, HasNode, PlannerLocked;
            public int          NodeRuns;
            public readonly List<string>       Runs    = new List<string>();
            public readonly List<ChangeSource> Sources = new List<ChangeSource>();
            public readonly List<ChangeSource> Commits = new List<ChangeSource>();

            public List<PaletteCommand> Commands()
            {
                return new List<PaletteCommand>
                {
                    new PaletteCommand("window:Ascent", () => "Ascent Guidance", "AscentMenu", s => Did("ascent", s, () => AscentOpen = !AscentOpen)),
                    new PaletteCommand("window:DeltaV", () => "Delta-V Stats", "Delta-V Stats", s => Did("deltav", s, () => DeltaVOpen = !DeltaVOpen)),
                    new PaletteCommand("window:Planner", () => "Maneuver Planner", "ManeuverPlanner", s => Did("planner", s, null)),
                    new PaletteCommand("ascent:engage", () => "Engage ascent autopilot", "Engage ascent autopilot", s => Did("engage", s, () => Engaged = true))
                        .Confirm(() => "Engages the ascent autopilot to 100 km")
                        .ListedWhen(() => !Engaged),
                    new PaletteCommand("ascent:disengage", () => "Disengage ascent autopilot", "Disengage ascent autopilot", s => Did("disengage", s, () => Engaged = false))
                        .Confirm(() => "Disengages the ascent autopilot")
                        .ListedWhen(() => Engaged),
                    new PaletteCommand("node:one", () => "Execute next node", "Execute next node", s => Did("node", s, () => NodeRuns++))
                        .Confirm(() => "Executes the next node")
                        .When(() => !PlannerLocked && HasNode, () => PlannerLocked ? "not unlocked yet" : "No maneuver node")
                };
            }

            private void Did(string what, ChangeSource s, System.Action act)
            {
                Runs.Add(what);
                Sources.Add(s);
                Commits.Add(CommitPath.Resolve(ChangeSource.Program));
                act?.Invoke();
            }
        }

        private static PaletteModel Opened(World w, out List<PaletteClose> closes, Clock clock = null)
        {
            var m = new PaletteModel { Label = "Command palette" };
            if (clock != null) m.Clock = () => clock.Now;
            var c = new List<PaletteClose>();
            m.Closed += r => c.Add(r);
            m.Open(w.Commands());
            closes = c;
            return m;
        }

        private sealed class Clock
        {
            public double Now;
        }

        private static string[] Ids(PaletteModel m) => Enumerable.Range(0, m.ResultCount).Select(r => m.ResultAt(r).Id).ToArray();

        // ---- the list and the ranking ----

        [Fact]
        public void OpeningListsTheListedCommandsInTheirOrderWithTheOnesThatCanRunFirst()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            Assert.True(m.IsOpen);
            Assert.Equal("", m.Query);
            // disengage is not listed while nothing is engaged; the node can't run without a node: last
            Assert.Equal(new[] { "window:Ascent", "window:DeltaV", "window:Planner", "ascent:engage", "node:one" }, Ids(m));
            Assert.Equal(0, m.Highlight);
            Assert.Equal(PaletteStatus.Hint, m.Status);
        }

        [Fact]
        public void AnIdGivenTwiceIsListedOnce()
        {
            var m = new PaletteModel();
            m.Open(new[] { new PaletteCommand("a", () => "A", null, s => { }), new PaletteCommand("a", () => "A again", null, s => { }) });
            Assert.Equal(1, m.ResultCount);
            Assert.Equal("A", m.ResultAt(0).Label);
        }

        [Fact]
        public void AQueryRanksTheBestMatchFirstAndPutsTheHighlightOnIt()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.Move(2);
            m.SetQuery("delta");
            Assert.Equal("window:DeltaV", m.ResultAt(0).Id);
            Assert.Equal(0, m.Highlight);
            m.SetQuery("asc");
            // a prefix of a label ranks above the same letters inside another
            Assert.Equal("window:Ascent", m.ResultAt(0).Id);
            Assert.Contains("ascent:engage", Ids(m));
        }

        [Fact]
        public void TheEnglishKeyFindsACommandWhoseLabelDoesNotMatch()
        {
            var m = new PaletteModel();
            m.Open(new[] { new PaletteCommand("window:Ascent", () => "上升制导", "AscentMenu", s => { }) });
            m.SetQuery("ascent");
            Assert.Equal(1, m.ResultCount);
            m.SetQuery("上升");
            Assert.Equal(1, m.ResultCount);
        }

        [Fact]
        public void ACommandThatCannotRunRanksAfterEveryCommandThatCanEvenWithABetterScore()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("e");
            string[] ids = Ids(m);
            Assert.Equal("node:one", ids[ids.Length - 1]);
            Assert.False(m.ResultAvailable(ids.Length - 1));
        }

        [Fact]
        public void AResultsLabelIsTheOneReadWhenTheListWasMade()
        {
            int asked = 0;
            var m = new PaletteModel();
            m.Open(new[] { new PaletteCommand("a", () => { asked++; return "Ascent"; }, null, s => { }) });
            int afterOpen = asked;
            Assert.Equal("Ascent", m.ResultLabel(0));
            Assert.Equal("Ascent", m.ResultLabel(0));
            Assert.Equal(afterOpen, asked);
            Assert.Null(m.ResultLabel(1));
        }

        [Fact]
        public void NoMatchSaysSoAndLeavesNoHighlight()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("zzzz");
            Assert.Equal(0, m.ResultCount);
            Assert.Equal(-1, m.Highlight);
            Assert.Equal(PaletteStatus.NoMatch, m.Status);
            Assert.Equal(PaletteEnter.Nothing, m.Enter(ChangeSource.Keyboard));
            Assert.True(m.IsOpen);
        }

        [Fact]
        public void UpAndDownMoveTheHighlightClampedAtTheEnds()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            Assert.False(m.Handle(ScopeKeys.Up, ChangeSource.Keyboard));
            Assert.Equal(0, m.Highlight);
            Assert.True(m.Handle(ScopeKeys.Down, ChangeSource.Keyboard));
            Assert.Equal(1, m.Highlight);
            Assert.True(m.Handle(ScopeKeys.PageDown, ChangeSource.Keyboard));
            Assert.Equal(m.ResultCount - 1, m.Highlight);
            Assert.False(m.Handle(ScopeKeys.Down, ChangeSource.Keyboard));
            Assert.True(m.Handle(ScopeKeys.PageUp, ChangeSource.Keyboard));
            Assert.Equal(0, m.Highlight);
        }

        // ---- running ----

        [Fact]
        public void EnterOnAWindowRunsItAtOnceAndClosesThePalette()
        {
            var w = new World();
            PaletteModel m = Opened(w, out List<PaletteClose> closes);
            m.SetQuery("delta");
            Assert.Equal(PaletteEnter.Ran, m.Enter(ChangeSource.Keyboard));
            Assert.True(w.DeltaVOpen);
            Assert.False(m.IsOpen);
            AssertionExtensions.EqualSequence(new[] { PaletteClose.Ran }, closes);
            Assert.Equal("window:DeltaV", m.LastRunId);
            Assert.Equal("Delta-V Stats", m.LastRunLabel);
        }

        [Fact]
        public void TheCommandRunsAfterThePaletteClosed()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            bool openWhenRun = true;
            m.Ran += (c, s) => openWhenRun = m.IsOpen;
            m.SetQuery("delta");
            m.Enter(ChangeSource.Keyboard);
            Assert.False(openWhenRun);
        }

        [Fact]
        public void TheRunCarriesTheEntersSourceThroughTheCommitPath()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("delta");
            m.Enter(ChangeSource.Agent);
            AssertionExtensions.EqualSequence(new[] { ChangeSource.Agent }, w.Sources);
            AssertionExtensions.EqualSequence(new[] { ChangeSource.Agent }, w.Commits);
            Assert.Equal(ChangeSource.Agent, m.LastSource);
            Assert.Equal(0, CommitPath.OpenScopes);
        }

        [Fact]
        public void TheFirstEnterOnAFlightCommandArmsItAndRunsNothing()
        {
            var w = new World();
            PaletteModel m = Opened(w, out List<PaletteClose> closes);
            m.SetQuery("engage");
            Assert.Equal("ascent:engage", m.Highlighted.Id);
            Assert.Equal(PaletteEnter.Armed, m.Enter(ChangeSource.Keyboard));
            Assert.False(w.Engaged);
            Assert.Empty(w.Runs);
            Assert.True(m.IsOpen);
            Assert.Empty(closes);
            Assert.Equal("ascent:engage", m.Armed.Id);
            Assert.Equal(PaletteStatus.Confirm, m.Status);
            Assert.Equal("Engages the ascent autopilot to 100 km", m.StatusCommand.Preview);
        }

        [Fact]
        public void TheSecondEnterOnTheArmedCommandRunsItAndCloses()
        {
            var w = new World();
            PaletteModel m = Opened(w, out List<PaletteClose> closes);
            m.SetQuery("engage");
            m.Enter(ChangeSource.Keyboard);
            Assert.Equal(PaletteEnter.Ran, m.Enter(ChangeSource.Keyboard));
            Assert.True(w.Engaged);
            Assert.Equal(new[] { "engage" }, w.Runs);
            AssertionExtensions.EqualSequence(new[] { PaletteClose.Ran }, closes);
            Assert.Null(m.Armed);
        }

        [Fact]
        public void ACommandThatDoesNotChangeTheFlightHasNoPreview()
        {
            var w = new World();
            List<PaletteCommand> c = w.Commands();
            Assert.False(c[0].ChangesFlight);
            Assert.Null(c[0].Preview);
            Assert.True(c[3].ChangesFlight);
        }

        // ---- cancel paths ----

        [Fact]
        public void EscapeAfterTheFirstEnterClosesAndRunsNothing()
        {
            var w = new World();
            PaletteModel m = Opened(w, out List<PaletteClose> closes);
            m.SetQuery("engage");
            m.Enter(ChangeSource.Keyboard);
            Assert.True(m.Handle(ScopeKeys.Escape, ChangeSource.Keyboard));
            Assert.False(m.IsOpen);
            AssertionExtensions.EqualSequence(new[] { PaletteClose.Escape }, closes);
            Assert.False(w.Engaged);
            Assert.Null(m.Armed);
            Assert.Null(m.LastRunId);
        }

        [Fact]
        public void TypingAfterTheFirstEnterDisarmsSoTheNextEnterOnlyArmsAgain()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("engage");
            m.Enter(ChangeSource.Keyboard);
            m.SetQuery("engag");
            Assert.Null(m.Armed);
            Assert.Equal(PaletteStatus.Hint, m.Status);
            Assert.Equal(PaletteEnter.Armed, m.Enter(ChangeSource.Keyboard));
            Assert.False(w.Engaged);
        }

        [Fact]
        public void MovingTheHighlightAfterTheFirstEnterDisarms()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("engage");
            m.Enter(ChangeSource.Keyboard);
            m.Move(1);
            m.Move(-1);
            Assert.Null(m.Armed);
            Assert.Equal(PaletteEnter.Armed, m.Enter(ChangeSource.Keyboard));
            Assert.False(w.Engaged);
        }

        [Fact]
        public void ClosingByAnyOtherReasonRunsNothingAndForgetsTheArmedCommand()
        {
            foreach (PaletteClose why in new[] { PaletteClose.OutsidePress, PaletteClose.OwnerClosed, PaletteClose.SceneChange, PaletteClose.Program })
            {
                var w = new World();
                PaletteModel m = Opened(w, out List<PaletteClose> closes);
                m.SetQuery("engage");
                m.Enter(ChangeSource.Keyboard);
                m.Close(why);
                m.Close(why);
                AssertionExtensions.EqualSequence(new[] { why }, closes);
                Assert.False(w.Engaged);
                m.Open(w.Commands());
                Assert.Null(m.Armed);
            }
        }

        [Fact]
        public void AClickOnARowIsAnEnterAndASecondClickConfirms()
        {
            var w = new World();
            var t = new Clock();
            PaletteModel m = Opened(w, out _, t);
            int engage = Ids(m).ToList().IndexOf("ascent:engage");
            Assert.Equal(PaletteEnter.Armed, m.Pick(engage, ChangeSource.Pointer));
            Assert.Equal(engage, m.Highlight);
            Assert.False(w.Engaged);
            t.Now = PaletteModel.ClickConfirmDelay;
            Assert.Equal(PaletteEnter.Ran, m.Pick(engage, ChangeSource.Pointer));
            Assert.True(w.Engaged);
            AssertionExtensions.EqualSequence(new[] { ChangeSource.Pointer }, w.Sources);
        }

        [Fact]
        public void ADoubleClickOnAFlightCommandArmsItAndRunsNothing()
        {
            var w = new World();
            var t = new Clock { Now = 10 };
            PaletteModel m = Opened(w, out List<PaletteClose> closes, t);
            int engage = Ids(m).ToList().IndexOf("ascent:engage");
            Assert.Equal(PaletteEnter.Armed, m.Pick(engage, ChangeSource.Pointer));
            t.Now = 10.15;
            Assert.Equal(PaletteEnter.Armed, m.Pick(engage, ChangeSource.Pointer));
            t.Now = 10.49;
            Assert.Equal(PaletteEnter.Armed, m.Pick(engage, ChangeSource.Pointer));
            Assert.False(w.Engaged);
            Assert.True(m.IsOpen);
            Assert.Empty(closes);
            Assert.Equal("ascent:engage", m.Armed.Id);
            Assert.Equal(PaletteStatus.Confirm, m.Status);
        }

        [Fact]
        public void EnterRunsACommandArmedByAClickAtOnce()
        {
            var w = new World();
            var t = new Clock();
            PaletteModel m = Opened(w, out _, t);
            m.Pick(Ids(m).ToList().IndexOf("ascent:engage"), ChangeSource.Pointer);
            t.Now = 0.05;
            Assert.Equal(PaletteEnter.Ran, m.Enter(ChangeSource.Keyboard));
            Assert.True(w.Engaged);
        }

        [Fact]
        public void WithoutAClockAClickOnTheArmedRowNeverRunsItButEnterDoes()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            int engage = Ids(m).ToList().IndexOf("ascent:engage");
            m.Pick(engage, ChangeSource.Pointer);
            Assert.Equal(PaletteEnter.Armed, m.Pick(engage, ChangeSource.Pointer));
            Assert.False(w.Engaged);
            Assert.Equal(PaletteEnter.Ran, m.Enter(ChangeSource.Keyboard));
            Assert.True(w.Engaged);
        }

        [Fact]
        public void TheAgentsSecondPickRunsWithoutTheClickDelay()
        {
            var w = new World();
            var t = new Clock();
            PaletteModel m = Opened(w, out _, t);
            int engage = Ids(m).ToList().IndexOf("ascent:engage");
            Assert.Equal(PaletteEnter.Armed, m.Pick(engage, ChangeSource.Agent));
            Assert.Equal(PaletteEnter.Ran, m.Pick(engage, ChangeSource.Agent));
            Assert.True(w.Engaged);
        }

        [Fact]
        public void AClickOnAnotherRowAfterArmingDisarmsTheFirst()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            int engage = Ids(m).ToList().IndexOf("ascent:engage");
            m.Pick(engage, ChangeSource.Pointer);
            Assert.Equal(PaletteEnter.Ran, m.Pick(0, ChangeSource.Pointer)); // Ascent Guidance, a window
            Assert.False(w.Engaged);
            Assert.True(w.AscentOpen);
        }

        // ---- unavailable commands ----

        [Fact]
        public void ATechLockedCommandIsListedGreyedWithItsReasonAndNeitherArmsNorRuns()
        {
            var w = new World { HasNode = true, PlannerLocked = true };
            PaletteModel m = Opened(w, out _);
            Assert.Contains("node:one", Ids(m));
            m.SetQuery("execute");
            Assert.False(m.ResultAvailable(m.Highlight));
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Null(m.Armed);
            Assert.Equal(0, w.NodeRuns);
            Assert.Equal("not unlocked yet", m.StatusCommand.Reason);
        }

        [Fact]
        public void AClosedPaletteHoldsNoCommandAndKeepsOnlyTheLastRunsIdAndLabel()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("delta");
            m.Enter(ChangeSource.Keyboard);
            Assert.Empty(m.Listed);
            Assert.Equal(0, m.ResultCount);
            Assert.Null(m.Highlighted);
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.DoesNotContain(parts, p => p.Role == PartRole.Option);
            Assert.Equal("window:DeltaV", m.LastRunId);
            m.Open(w.Commands());
            Assert.Equal(5, m.ResultCount);
        }

        [Fact]
        public void EnterOnACommandThatCannotRunShowsTheReasonAndKeepsThePaletteOpen()
        {
            var w = new World();
            PaletteModel m = Opened(w, out List<PaletteClose> closes);
            m.SetQuery("execute");
            Assert.Equal("node:one", m.Highlighted.Id);
            Assert.False(m.ResultAvailable(m.Highlight));
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(0, w.NodeRuns);
            Assert.True(m.IsOpen);
            Assert.Empty(closes);
            Assert.Equal(PaletteStatus.Unavailable, m.Status);
            Assert.Equal("No maneuver node", m.StatusCommand.Reason);
        }

        [Fact]
        public void ACommandThatBecomesAvailableRunsAfterARefresh()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("execute");
            w.HasNode = true;
            Assert.True(m.Refresh());
            Assert.True(m.ResultAvailable(m.Highlight));
            Assert.Equal(PaletteEnter.Armed, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(PaletteEnter.Ran, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(1, w.NodeRuns);
        }

        [Fact]
        public void AnArmedCommandThatCanNoLongerRunIsDisarmedWithItsReason()
        {
            var w = new World { HasNode = true };
            PaletteModel m = Opened(w, out _);
            m.SetQuery("execute");
            m.Enter(ChangeSource.Keyboard);
            Assert.NotNull(m.Armed);
            w.HasNode = false;
            m.Refresh();
            Assert.Null(m.Armed);
            Assert.Equal(PaletteStatus.Unavailable, m.Status);
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(0, w.NodeRuns);
        }

        [Fact]
        public void TheSecondEnterChecksAgainWhetherTheCommandCanRun()
        {
            var w = new World { HasNode = true };
            PaletteModel m = Opened(w, out _);
            m.SetQuery("execute");
            m.Enter(ChangeSource.Keyboard);
            w.HasNode = false; // the node went between the two Enters, with no refresh
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Equal(0, w.NodeRuns);
            Assert.True(m.IsOpen);
        }

        [Fact]
        public void ARefreshSwapsEngageForDisengageAndKeepsTheHighlightedCommand()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.Move(1); // Delta-V Stats
            w.Engaged = true;
            Assert.True(m.Refresh());
            Assert.Contains("ascent:disengage", Ids(m));
            Assert.DoesNotContain("ascent:engage", Ids(m));
            Assert.Equal("window:DeltaV", m.Highlighted.Id);
        }

        [Fact]
        public void ARefreshWithNothingChangedChangesNothing()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("a");
            int results = 0;
            m.ResultsChanged += () => results++;
            Assert.False(m.Refresh());
            Assert.Equal(0, results);
        }

        [Fact]
        public void AnArmedCommandThatIsNoLongerListedDoesNotRunOnTheSecondEnter()
        {
            var w = new World();
            PaletteModel m = Opened(w, out List<PaletteClose> closes);
            m.SetQuery("engage");
            m.Enter(ChangeSource.Keyboard);
            w.Engaged = true; // engaged elsewhere between the two Enters, before the palette's refresh: Engage has no meaning now
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.Empty(w.Runs);
            Assert.Empty(closes);
            Assert.Null(m.Armed);
        }

        [Fact]
        public void AnUnlistedCommandsConditionIsNotAsked()
        {
            bool listed = true;
            var c = new PaletteCommand("x", () => "X", null, s => { })
                .ListedWhen(() => listed)
                .When(() => throw new System.InvalidOperationException("asked while not listed"), () => throw new System.InvalidOperationException("reason asked"));
            listed = false;
            Assert.False(c.Available);
            Assert.Null(c.Reason);
        }

        [Fact]
        public void ACommandWithoutARunNeverRuns()
        {
            var m = new PaletteModel();
            m.Open(new[] { new PaletteCommand("x", () => "X", null, null) });
            Assert.Equal(PaletteEnter.Unavailable, m.Enter(ChangeSource.Keyboard));
            Assert.True(m.IsOpen);
        }

        // ---- the description ----

        [Fact]
        public void TheDescriptionNamesTheOptionsByIdAndMarksTheHighlightAndTheDisabled()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.Equal(PartRole.Dialog, parts[0].Role);
            Assert.True(parts[0].Has(PartFlags.Expanded));
            Assert.True(parts[1].Focusable);
            Assert.Contains(PartKey.Enter, parts[1].Keys);
            PartDescription node = parts.Single(p => p.Name == "node:one");
            Assert.True(node.Has(PartFlags.Disabled));
            Assert.Single(parts, p => p.Has(PartFlags.Focus));
            Assert.Empty(Patterns.Check(m));
        }

        [Fact]
        public void AnArmedCommandShowsAsTheStatusPart()
        {
            var w = new World();
            PaletteModel m = Opened(w, out _);
            m.SetQuery("engage");
            m.Enter(ChangeSource.Keyboard);
            var parts = new List<PartDescription>();
            m.Describe(parts);
            PartDescription status = parts.Single(p => p.Role == PartRole.Status);
            Assert.Equal("Engage ascent autopilot", status.Label);
            Assert.False(status.Has(PartFlags.Invalid));
            Assert.Empty(Patterns.Check(m));
        }

        [Fact]
        public void PopupDismissalsMapToThePalettesReasons()
        {
            Assert.Equal(PaletteClose.OutsidePress, PaletteModel.FromDismiss(PopupDismiss.OutsidePress));
            Assert.Equal(PaletteClose.Escape, PaletteModel.FromDismiss(PopupDismiss.Escape));
            Assert.Equal(PaletteClose.SceneChange, PaletteModel.FromDismiss(PopupDismiss.SceneChange));
            Assert.Equal(PaletteClose.OwnerClosed, PaletteModel.FromDismiss(PopupDismiss.OwnerClosed));
        }
    }
}
