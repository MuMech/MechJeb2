/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.PSG;
using Xunit;
using static MechJebLibTest.PSGTests.AscentFlight;

namespace MechJebLibTest.PSGTests
{
    /// <summary>
    ///     The AscentStateMachine flying stand-in rockets (AscentFlight) through synthetic Solutions.  These pin down the machine
    ///     as it was moved out of MechJebModulePSGAutopilot.  The Solutions' phases start at MET 4096 s and are given in whole
    ///     seconds, so a phase's start and end times are exact.
    /// </summary>
    public class AscentStateMachineTests
    {
        private const PlanEditor.CoastKind BEFORE    = PlanEditor.CoastKind.BEFORE;
        private const PlanEditor.CoastKind DURING    = PlanEditor.CoastKind.DURING;
        private const PlanEditor.CoastKind AFTER     = PlanEditor.CoastKind.AFTER;
        private const PlanEditor.CoastKind DEDICATED = PlanEditor.CoastKind.DEDICATED;

        // KSP stage 2 burns the booster (engine 3), stage 1 drops it and lights engine 2, and stage 0 drops that and lights the
        // last stage's engine 1.  The stage stats rows (and the flight plan's stages) are KSP stages 0, 1 and 2.
        private static AscentFlight ThreeStages(double booster = 60, double middle = 40, double last = 30)
        {
            var flight = new AscentFlight(2);
            flight.AddEngine(1, 0, -1, last);
            flight.AddEngine(2, 1, 0, middle);
            flight.AddEngine(3, 2, 1, booster);
            return flight;
        }

        // KSP stage 1 burns engine 2, and stage 0 drops it and lights the last stage's engine 1
        private static AscentFlight TwoStages(double first = 40, double last = 30)
        {
            var flight = new AscentFlight(1);
            flight.AddEngine(1, 0, -1, last);
            flight.AddEngine(2, 1, 0, first);
            return flight;
        }

        // a single stage with 100 s of propellant
        private static AscentFlight OneStage()
        {
            var flight = new AscentFlight(0);
            flight.AddEngine(1, 0, -1, 100);
            return flight;
        }

        // the MET of something that should happen at expected, up to early before it and late after it
        private static void AtAbout(double expected, double actual, double early = 0, double late = DT) =>
            Assert.InRange(actual, expected - early - 1e-9, expected + late + 1e-9);

        /*
         * Before guidance
         */

        [Fact]
        public void GuidanceTakesOverOnTheSecondTickInAVacuum()
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(60), Fixed(40), Fixed(30)));

            flight.Step();

            // the pitch program starts at 90° with the guidance throttle, and doesn't hand over in the tick it starts
            Assert.Equal(PSGState.PITCH_PROGRAM, flight.Machine.State);
            AscentCommand cmd = flight.Log.Last().Command;
            Assert.Equal(AscentAttitude.PITCH_HEADING, cmd.Attitude);
            Assert.Equal(90, cmd.Pitch);
            Assert.Equal(AscentThrottle.GUIDED, cmd.Throttle);
            Assert.Equal(1, cmd.ThrottleValue);

            flight.Step();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING", flight.Sequence());
            Assert.Equal(AscentAttitude.INERTIAL, flight.Log.Last().Command.Attitude);
        }

        [Fact]
        public void PadThroughThePitchProgram()
        {
            AscentFlight flight = ThreeStages();
            flight.HasAtmosphere = true;
            flight.Grounded = true;
            flight.LiftedOff = false;
            flight.AltitudeBottom = 0;
            flight.PrelaunchDone = false;
            flight.PitchRate = 10;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(60), Fixed(40), Fixed(30)));

            // the countdown's prelaunch, which nothing holds for
            flight.Step();
            Assert.Equal(PSGState.PRELAUNCH, flight.Machine.State);
            AscentCommand cmd = flight.Log.Last().Command;
            Assert.True(cmd.DrivePrelaunch);
            Assert.False(cmd.CheckLiftoffHold);
            Assert.Equal(AscentThrottle.NONE, cmd.Throttle);

            // straight up under the guidance throttle, behind the liftoff hold
            flight.PrelaunchDone = true;
            flight.Step();
            Assert.Equal(PSGState.VERTICAL_ASCENT, flight.Machine.State);
            cmd = flight.Log.Last().Command;
            Assert.Equal(AscentAttitude.VERTICAL, cmd.Attitude);
            Assert.Equal(AscentThrottle.GUIDED, cmd.Throttle);
            Assert.True(cmd.CheckLiftoffHold);
            Assert.True(cmd.DriveAscentParts);

            // off the pad, below the pitch start height
            flight.Grounded = false;
            flight.LiftedOff = true;
            flight.AltitudeBottom = 50;
            flight.Run(1);
            Assert.Equal(PSGState.VERTICAL_ASCENT, flight.Machine.State);

            flight.AltitudeBottom = 150;
            flight.Step();
            Assert.Equal(PSGState.PITCH_PROGRAM, flight.Machine.State);
            double start = flight.Log.Last().Met;
            Assert.Equal(90, flight.Log.Last().Command.Pitch);

            // the pitch program pitches over at 10°/s until it meets the guidance pitch, which is near the horizon
            Assert.True(flight.RunUntil(PSGState.BURNING, 20));
            Assert.InRange(flight.EnteredAt(PSGState.BURNING) - start, 8.5, 9.1);
            Assert.Equal("PRELAUNCH VERTICAL_ASCENT PITCH_PROGRAM BURNING", flight.Sequence());
        }

        [Fact]
        public void PitchProgramMeetsTheGuidanceOfANewSolutionAtOnce()
        {
            // in an atmosphere at 1°/s with no Solution, the pitch program pitches down to 3° in 87 s
            AscentFlight flight = OneStage();
            flight.HasAtmosphere = true;
            flight.Machine.Start();
            flight.Run(87);
            Assert.Equal(PSGState.PITCH_PROGRAM, flight.Machine.State);

            // where the synthetic trajectory's guidance pitch is about 5°
            flight.TrajectoryT = MET0 + 87;
            Assert.True(flight.Machine.SetSolution(Solve(flight.Plan(), MET0, Fixed(100))));

            // below it on the Solution's first tick: guidance takes over then, not a tick later
            flight.Step();
            Assert.Equal(PSGState.BURNING, flight.Machine.State);
            Assert.InRange(flight.Machine.Pitch, 4, 6);
        }

        /*
         * The last stage
         */

        [Fact]
        public void PreciseLastStageFliesTerminalGuidance()
        {
            AscentFlight flight = OneStage();
            Solution solution = Solve(flight.Plan(), MET0, Cut(60));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING TERMINAL FINISHED", flight.Sequence());
            AtAbout(solution.Tf - 10, flight.EnteredAt(PSGState.TERMINAL), DT);
            // cut on the target's angular momentum, a tick ahead
            AtAbout(solution.Tf, flight.EnteredAt(PSGState.FINISHED), 2 * DT);
            Assert.Equal(1, flight.Count(AscentEvents.TERMINAL_STARTED));
            AtAbout(solution.Tf - 10, flight.At(AscentEvents.TERMINAL_STARTED), DT);
            Assert.Equal(1, flight.Count(AscentEvents.FINISHED));
            Assert.All(flight.In(PSGState.TERMINAL), t =>
            {
                Assert.True(t.Command.MinimumWarp);
                Assert.Equal(AscentThrottle.GUIDED, t.Command.Throttle);
            });
            Assert.Equal(0, flight.Throttle);
            Assert.InRange(flight.EngineOf(1).Fuel, 39.9, 40.1);
        }

        [Fact]
        public void RcsTrimsAfterTheEnginesCut()
        {
            AscentFlight flight = OneStage();
            flight.RcsThrust = 1;
            flight.RCSGroup = true;
            Solution solution = Solve(flight.Plan(), MET0, Cut(60));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING TERMINAL TERMINAL_RCS FINISHED", flight.Sequence());
            // the engines are cut two ticks ahead of the target, and the RCS trims the last one
            AtAbout(solution.Tf - 2 * DT, flight.EnteredAt(PSGState.TERMINAL_RCS), 2 * DT);
            Assert.Single(flight.In(PSGState.TERMINAL_RCS));
            Assert.All(flight.In(PSGState.TERMINAL_RCS), t =>
            {
                Assert.Equal(AscentThrottle.RCS_FORWARD, t.Command.Throttle);
                Assert.True(t.Command.MinimumWarp);
            });
            // terminal guidance started once, as TERMINAL was entered
            Assert.Equal(1, flight.Count(AscentEvents.TERMINAL_STARTED));
            AtAbout(flight.EnteredAt(PSGState.TERMINAL), flight.At(AscentEvents.TERMINAL_STARTED));
            Assert.Equal(1, flight.Count(AscentMessages.RCS_TRIM));
            Assert.DoesNotContain(flight.Log, t => t.Command.RCSGroupOn);
        }

        [Fact]
        public void TgoAndVgoStayFrozenAsTheTrimEnds()
        {
            AscentFlight flight = OneStage();
            flight.RcsThrust = 1;
            flight.RCSGroup = true;
            // a slow trim, which ends well past the end of the Solution
            flight.RcsTrajectoryRate = 0.1;
            Solution solution = Solve(flight.Plan(), MET0, Cut(60));
            flight.Engage(solution);

            // frozen at the engine cut
            Assert.True(flight.RunUntil(PSGState.TERMINAL_RCS));
            double tgo = flight.Machine.Tgo;
            double vgo = flight.Machine.Vgo;
            double phaseTgo = flight.Machine.CurrentPhaseTgo;

            flight.Fly();

            Assert.True(flight.In(PSGState.TERMINAL_RCS).Count() > 1);
            Assert.True(flight.EnteredAt(PSGState.FINISHED) > solution.Tf);
            // the tick the trim ends doesn't read the Solution past its end
            Assert.Equal(tgo, flight.Machine.Tgo);
            Assert.Equal(vgo, flight.Machine.Vgo);
            Assert.Equal(phaseTgo, flight.Machine.CurrentPhaseTgo);
            Assert.True(flight.Machine.Tgo >= 0);
        }

        [Fact]
        public void RcsGroupOffSkipsTheTrim()
        {
            AscentFlight flight = OneStage();
            flight.RcsThrust = 1;
            Solution solution = Solve(flight.Plan(), MET0, Cut(60));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING TERMINAL FINISHED", flight.Sequence());
            AtAbout(solution.Tf, flight.EnteredAt(PSGState.FINISHED), 2 * DT);
            // it says the RCS is off, and leaves it off
            Assert.Equal(1, flight.Count(AscentMessages.RCS_OFF));
            AtAbout(solution.Tf - 10, flight.Log.First(t => (t.Command.Messages & AscentMessages.RCS_OFF) != 0).Met, DT);
            Assert.DoesNotContain(flight.Log, t => t.Command.RCSGroupOn);
            Assert.False(flight.RCSGroup);
        }

        [Fact]
        public void RcsGroupSwitchedOffEndsTheTrim()
        {
            AscentFlight flight = OneStage();
            flight.RcsThrust = 1;
            flight.RCSGroup = true;
            // the trim never reaches the target
            flight.RcsTrajectoryRate = 0;
            flight.Engage(Solve(flight.Plan(), MET0, Cut(60)));

            Assert.True(flight.RunUntil(PSGState.TERMINAL_RCS));
            flight.Run(0.5);
            Assert.Equal(PSGState.TERMINAL_RCS, flight.Machine.State);

            flight.RCSGroup = false;
            flight.Step();

            Assert.Equal(PSGState.FINISHED, flight.Machine.State);
        }

        [Fact]
        public void RcsRunningDryEndsTheTrim()
        {
            AscentFlight flight = OneStage();
            flight.RcsThrust = 1;
            flight.RCSGroup = true;
            flight.RcsTrajectoryRate = 0;
            flight.RcsFuel = 0.5;
            flight.Engage(Solve(flight.Plan(), MET0, Cut(60)));

            Assert.True(flight.RunUntil(PSGState.TERMINAL_RCS));
            double trim = flight.Log.Last().Met;

            flight.Fly();

            // out of RCS propellant after 0.5 s, and spent a second later
            AtAbout(trim + 1.5, flight.EnteredAt(PSGState.FINISHED), DT, 2 * DT);
        }

        [Fact]
        public void FixedLastStageFlameoutPastItsEndFinishes()
        {
            AscentFlight flight = OneStage();
            AscentFlight.Engine engine = flight.EngineOf(1);
            engine.Fuel = 50;
            engine.FailAt = MET0 + 45;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(40)));

            flight.Fly();

            // it burns on into its residuals past its end, and a second without thrust after that is the end, with the
            // propellant the stage stats still show
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 46, flight.EnteredAt(PSGState.FINISHED), DT, 2 * DT);
            Assert.InRange(engine.Fuel, 4.9, 5.1);
        }

        [Fact]
        public void FixedLastStageRunningDryEarlyFinishes()
        {
            AscentFlight flight = OneStage();
            flight.EngineOf(1).Fuel = 30;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(40)));

            flight.Fly();

            // the stage stats show it empty at their next run
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 30, flight.EnteredAt(PSGState.FINISHED), 0, flight.StatsPeriod + 2 * DT);
        }

        [Fact]
        public void FixedLastStageWaitingToLightDoesNotFinish()
        {
            AscentFlight flight = OneStage();
            AscentFlight.Engine engine = flight.EngineOf(1);
            engine.Fuel = 40;
            engine.LightAt = MET0 + 5;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(40)));

            // no thrust with its propellant, before its end
            flight.Run(5);
            Assert.Equal(PSGState.BURNING, flight.Machine.State);
            Assert.Equal(0, engine.Burns);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            Assert.Equal(1, engine.Burns);
            AtAbout(MET0 + 45, flight.EnteredAt(PSGState.FINISHED), 0, flight.StatsPeriod + 2 * DT);
        }

        [Fact]
        public void LastStageWaitingToLightPastItsEndDoesNotFinish()
        {
            // a 2 s kick stage, staged on time, whose engine waits 5 s for its propellant to settle
            AscentFlight flight = TwoStages(40, 2);
            AscentFlight.Engine engine = flight.EngineOf(1);
            engine.LightAt = MET0 + 45;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(40), Fixed(2)));

            flight.Fly();

            // past its end, it has not lit since the vessel staged into it
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            Assert.Equal(1, engine.Burns);
            Assert.Equal(0, engine.Fuel);
            AtAbout(MET0 + 47, flight.EnteredAt(PSGState.FINISHED), 0, flight.StatsPeriod + 2 * DT);
        }

        [Fact]
        public void LastStageFailingToLightFinishes()
        {
            // a 2 s kick stage whose engine never lights
            AscentFlight flight = TwoStages(40, 2);
            AscentFlight.Engine engine = flight.EngineOf(1);
            engine.FailAt = MET0;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(40), Fixed(2)));

            flight.Fly();

            // unlit for IGNITION_TIMEOUT (10 s) after the vessel staged into it, past its end, and a second without thrust
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            Assert.Equal(0, engine.Burns);
            AtAbout(MET0 + 51, flight.EnteredAt(PSGState.FINISHED), 0, 2 * DT);
        }

        /*
         * How burns end
         */

        // the BurnEnd of each phase of a Solution, "-" for its coast, and "skip" for a phase guidance never flies
        private static string BurnEnds(AscentFlight flight, Solution solution)
        {
            flight.Engage(solution);
            return string.Join(" ", solution.Phases.Select((phase, p) =>
                phase.Skip ? "skip" : phase.Coast ? "-" : flight.Machine.BurnEndOf(p).ToString()));
        }

        [Fact]
        public void EachBurnEndsOneWay()
        {
            // the rows of the table in the AscentStateMachine doc
            AscentFlight flight = ThreeStages();
            Assert.Equal("JETTISON DEPLETION BURNOUT", BurnEnds(flight, Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30))));

            flight = ThreeStages();
            Assert.Equal("DEPLETION DEPLETION TERMINAL", BurnEnds(flight, Solve(flight.Plan(), MET0, Fixed(60), Fixed(40), Cut(30))));

            flight = ThreeStages();
            Assert.Equal("COAST - DEPLETION BURNOUT",
                BurnEnds(flight, Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(30), Fixed(40), Fixed(30))));

            flight = TwoStages(80);
            Assert.Equal("COAST - DEPLETION BURNOUT",
                BurnEnds(flight, Solve(flight.Plan(DURING, 1), MET0, Cut(30), Coast(20), Fixed(50), Fixed(30))));

            flight = TwoStages();
            Assert.Equal("COAST - BURNOUT", BurnEnds(flight, Solve(flight.Plan(AFTER, 1), MET0, Cut(30), Coast(20), Fixed(30))));

            // boosters around a sustainer
            flight = new AscentFlight(1);
            flight.AddEngine(2, 1, -1, 100);
            flight.AddEngine(31, 1, 0, 40);
            flight.AddEngine(32, 1, 0, 40);
            Assert.Equal("JETTISON BURNOUT", BurnEnds(flight, Solve(flight.Plan(), MET0, Cut(30), Fixed(70))));

            // an optimized burn the optimizer burns out
            flight = TwoStages();
            Assert.Equal("DEPLETION TERMINAL", BurnEnds(flight, Solve(flight.Plan(), MET0, Optimized(40), Cut(30))));

            // Phases the optimizer shrank to nothing are skipped.  A coast-during shrunk to nothing takes the burn before it
            // with it (the zero coast constraint), and its stage is one burn.
            flight = TwoStages(80);
            Assert.Equal("skip skip DEPLETION BURNOUT",
                BurnEnds(flight, Solve(flight.Plan(DURING, 1), MET0, Cut(0), Coast(0), Fixed(80), Fixed(30))));

            // without its coast the burn cut before a coast-before is cut before a fixed upper stack
            flight = ThreeStages();
            Assert.Equal("JETTISON skip DEPLETION BURNOUT",
                BurnEnds(flight, Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(0), Fixed(40), Fixed(30))));

            // without the burn before a coast-during, the booster is the last burn before the coast
            flight = ThreeStages();
            Assert.Equal("COAST skip - DEPLETION BURNOUT",
                BurnEnds(flight, Solve(flight.Plan(DURING, 1), MET0, Fixed(60), Cut(0), Coast(20), Fixed(40), Fixed(30))));
        }

        /*
         * JETTISON
         */

        [Fact]
        public void BoosterBeforeAFixedStackIsJettisoned()
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30)));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 50, flight.EnteredAt(PSGState.JETTISON));

            // a tick with the throttle off: the stage drops the booster's engine at once
            Tick jettison = Assert.Single(flight.In(PSGState.JETTISON));
            Assert.Equal(AscentThrottle.OFF, jettison.Command.Throttle);
            Assert.Equal(AscentStaging.IMMEDIATE, jettison.Command.Staging);

            Assert.Equal(2, flight.Stagings.Count);
            Assert.Equal(StagedBy.IMMEDIATE, flight.Stagings[0].By);
            Assert.Equal(2, flight.Stagings[0].From);
            Assert.Equal(StagedBy.AUTOSTAGING, flight.Stagings[1].By);
            Assert.Equal(1, flight.Stagings[1].From);

            // dropped with its propellant, and no engine lit twice
            Assert.InRange(flight.EngineOf(3).Fuel, 9.9, 10.1);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));

            // out of time warp for the last 10 s before the cut
            double cut = MET0 + 50;
            Assert.All(flight.In(PSGState.BURNING), t => Assert.Equal(t.Met < cut && cut - t.Met <= 10, t.Command.MinimumWarp));
            Assert.Contains(flight.Log, t => t.Command.MinimumWarp);
        }

        // a cut planned 0.005 s past a tick is made on that tick, and one 0.015 s past it on the next
        [Theory]
        [InlineData(0.005, 0)]
        [InlineData(0.015, DT)]
        public void JettisonCutsOnTheNearestTick(double offset, double late)
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50 + offset), Fixed(40), Fixed(30)));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 50 + late, flight.EnteredAt(PSGState.JETTISON), 1e-6, 1e-6);
        }

        [Fact]
        public void JettisonStagesThroughAFairing()
        {
            // KSP stage 2 drops a fairing (held by its conditions) between the booster and its decoupler: the stats rows are KSP
            // stages 0-3, the booster's in stage 2, and stage 3 has its engine with no delta-v
            var flight = new AscentFlight(3) { HoldFairings = true };
            flight.AddEngine(1, 0, -1, 30);
            flight.AddEngine(2, 1, 0, 40);
            flight.AddEngine(3, 3, 1, 60);
            flight.AddPart(100, 2, true);
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30)));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 50, flight.EnteredAt(PSGState.JETTISON));

            // two stages, the second when KSP's staging cooldown allows it, with the throttle off throughout
            var guidance = flight.Stagings.Where(s => s.By == StagedBy.IMMEDIATE).ToList();
            Assert.Equal(new[] { 3, 2 }, guidance.Select(s => s.From));
            AtAbout(MET0 + 50, guidance[0].Met);
            AtAbout(guidance[0].Met + flight.StagingCooldown, guidance[1].Met, 0, 2 * DT);
            Assert.True(flight.StagingRefusals > 0);
            Assert.All(flight.In(PSGState.JETTISON), t => Assert.Equal(AscentThrottle.OFF, t.Command.Throttle));
            Assert.Equal(1, flight.EngineOf(3).Burns);
        }

        [Fact]
        public void JettisonWaitsForKSPToStage()
        {
            AscentFlight flight = ThreeStages();
            flight.RefuseStagingUntil = MET0 + 52;
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30)));

            flight.Fly();

            // it asks every tick, with the throttle off, until KSP stages
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            Assert.InRange(flight.In(PSGState.JETTISON).Count(), 100, 102);
            Assert.All(flight.In(PSGState.JETTISON), t =>
            {
                Assert.Equal(AscentThrottle.OFF, t.Command.Throttle);
                Assert.Equal(AscentStaging.IMMEDIATE, t.Command.Staging);
            });
            Assert.InRange(flight.StagingRefusals, 99, 101);
            Assert.Equal(StagedBy.IMMEDIATE, flight.Stagings[0].By);
            AtAbout(MET0 + 52, flight.Stagings[0].Met);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        [Fact]
        public void JettisonDropsTheBoostersOfASustainer()
        {
            // two boosters (engines 31, 32) burn with the sustainer (engine 2) in KSP stage 1, and stage 0 drops them: the plan's
            // stages are the sustainer alone (KSP stage 0) and the three together (KSP stage 1)
            var flight = new AscentFlight(1);
            flight.AddEngine(2, 1, -1, 100);
            flight.AddEngine(31, 1, 0, 40);
            flight.AddEngine(32, 1, 0, 40);
            flight.Engage(Solve(flight.Plan(), MET0, Cut(30), Fixed(70)));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 30, flight.EnteredAt(PSGState.JETTISON));
            // the boosters leave and the sustainer burns on (FlightPlan.PartsJettisonedWith)
            Assert.Single(flight.In(PSGState.JETTISON));
            Assert.Equal(1, flight.EngineOf(31).Burns);
            Assert.Equal(1, flight.EngineOf(32).Burns);
            Assert.InRange(flight.EngineOf(31).Fuel, 9.9, 10.1);
            // the sustainer burns on through the jettison at the next burn's throttle: a cut would shut it down
            Assert.All(flight.In(PSGState.JETTISON), t => Assert.Equal(AscentThrottle.GUIDED, t.Command.Throttle));
            Assert.Equal(1, flight.EngineOf(2).Burns);
        }

        [Fact]
        public void JettisonAtTheAutostageLimitGivesUp()
        {
            // the first burn's engine (3) is never dropped, and KSP stage 1 only lights the last stage's (2): hot staging keeps
            // autostaging from lighting it early, and the stats rows are KSP stage 0 (empty), 1 and 2
            var flight = new AscentFlight(2) { HotStaging = true };
            flight.AddEngine(2, 1, -1, 40);
            flight.AddEngine(3, 2, -1, 60);
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40)));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            // one stage takes it to the last stage's KSP stage, where autostaging stops, with the engine still on the vessel:
            // guidance gives up on it (with a warning) and the next burn's throttle relights it
            Staging staging = Assert.Single(flight.Stagings);
            Assert.Equal(StagedBy.IMMEDIATE, staging.By);
            Assert.Equal(2, staging.From);
            Assert.Single(flight.In(PSGState.JETTISON));
            Assert.Equal(2, flight.EngineOf(3).Burns);
            Assert.Equal(1, flight.EngineOf(2).Burns);
        }

        [Fact]
        public void BurnedOutBoosterIsNotJettisoned()
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(), MET0, Optimized(60), Fixed(40), Fixed(30)));

            flight.Fly();

            // autostaging drops the empty stages, and guidance never stages
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            Assert.All(flight.Log, t => Assert.Equal(AscentStaging.NONE, t.Command.Staging));
            Assert.Equal(2, flight.Stagings.Count);
            Assert.All(flight.Stagings, s => Assert.Equal(StagedBy.AUTOSTAGING, s.By));
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        /*
         * Coasts
         */

        [Fact]
        public void CoastBefore()
        {
            AscentFlight flight = ThreeStages();
            flight.RcsThrust = 1;
            // BURN:0 BURN:1 COAST_BEFORE:1 BURN:2
            Solution solution = Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(30), Fixed(40), Fixed(30));
            double coastStart = solution.StartTime(1);
            double coastEnd = solution.EndTime(1);
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            AtAbout(coastStart, flight.EnteredAt(PSGState.COASTING));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_STARTED));
            AtAbout(coastStart, flight.At(AscentEvents.COAST_STARTED));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_ENDED));
            AtAbout(coastEnd, flight.At(AscentEvents.COAST_ENDED));

            // the RCS group is turned on for the coast
            Assert.Equal(1, flight.Count(AscentMessages.RCS_COAST));
            Assert.Equal(1, flight.Log.Count(t => t.Command.RCSGroupOn));
            AtAbout(coastStart, flight.Log.First(t => t.Command.RCSGroupOn).Met);

            // COASTING stages the cut booster off, into the coast's KSP stage, whose engine waits for the burn after the coast
            Assert.Equal(StagedBy.STAGE, flight.Stagings[0].By);
            Assert.Equal(2, flight.Stagings[0].From);
            AtAbout(coastStart, flight.Stagings[0].Met);
            Assert.InRange(flight.EngineOf(3).Fuel, 9.9, 10.1);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));

            // the ullage RCS for the last UllageLeadTime of the coast
            Assert.All(flight.In(PSGState.COASTING), t =>
                Assert.Equal(coastEnd - t.Met < flight.UllageLeadTime ? AscentThrottle.RCS_FORWARD : AscentThrottle.OFF, t.Command.Throttle));
            Assert.Contains(flight.Log, t => t.Command.Throttle == AscentThrottle.RCS_FORWARD);

            // autostaging stops at the coast's KSP stage until the coast is over, then at the last stage's
            Assert.All(flight.Log.Where(t => t.State != PSGState.FINISHED), t =>
                Assert.Equal(t.Met < coastEnd ? 1 : 0, t.Command.AutostageLimit));

            // out of time warp for the last 10 s before the coast starts, and before it ends
            Assert.All(flight.In(PSGState.BURNING), t =>
                Assert.Equal(t.Met < coastStart && coastStart - t.Met <= 10, t.Command.MinimumWarp));
            Assert.All(flight.In(PSGState.COASTING), t => Assert.Equal(coastEnd - t.Met <= 10, t.Command.MinimumWarp));
            Assert.Contains(flight.In(PSGState.BURNING), t => t.Command.MinimumWarp);
            Assert.Contains(flight.In(PSGState.COASTING), t => t.Command.MinimumWarp);
        }

        // a coast planned to start and end 0.005 s past a tick does so on that tick, and 0.015 s past it on the next
        [Theory]
        [InlineData(0.005, 0)]
        [InlineData(0.015, DT)]
        public void CoastCutsOnTheNearestTick(double offset, double late)
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(BEFORE, 1), MET0, Cut(50 + offset), Coast(30), Fixed(40), Fixed(30)));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 50 + late, flight.EnteredAt(PSGState.COASTING), 1e-6, 1e-6);
            AtAbout(MET0 + 80 + late, flight.At(AscentEvents.COAST_ENDED), 1e-6, 1e-6);
        }

        [Fact]
        public void CoastDuring()
        {
            AscentFlight flight = TwoStages(80);
            // BURN:0 BURN:1 COAST_DURING:1 BURN:1
            Solution solution = Solve(flight.Plan(DURING, 1), MET0, Cut(30), Coast(20), Fixed(50), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            AtAbout(solution.StartTime(1), flight.EnteredAt(PSGState.COASTING));
            AtAbout(solution.EndTime(1), flight.At(AscentEvents.COAST_ENDED));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_STARTED));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_ENDED));

            // the coast is in the KSP stage the vessel is in: guidance stages nothing, and the stage's engine lights again after it
            Assert.All(flight.Log, t => Assert.Equal(AscentStaging.NONE, t.Command.Staging));
            Staging staging = Assert.Single(flight.Stagings);
            Assert.Equal(StagedBy.AUTOSTAGING, staging.By);
            Assert.Equal(2, flight.EngineOf(2).Burns);
        }

        [Fact]
        public void CoastAfterJettisonsTheCarriedBurn()
        {
            AscentFlight flight = TwoStages();
            // BURN:0 COAST_AFTER:1 BURN:1
            Solution solution = Solve(flight.Plan(AFTER, 1), MET0, Cut(30), Coast(20), Fixed(30));
            double coastEnd = solution.EndTime(1);
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING JETTISON BURNING FINISHED", flight.Sequence());
            AtAbout(solution.StartTime(1), flight.EnteredAt(PSGState.COASTING));

            // the coast is in the KSP stage of the burn it carries, which JETTISON drops as the coast ends
            AtAbout(coastEnd, flight.EnteredAt(PSGState.JETTISON));
            Assert.Single(flight.In(PSGState.JETTISON));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_ENDED));
            AtAbout(coastEnd, flight.At(AscentEvents.COAST_ENDED));

            Staging staging = Assert.Single(flight.Stagings);
            Assert.Equal(StagedBy.IMMEDIATE, staging.By);
            Assert.Equal(1, staging.From);
            AtAbout(coastEnd, staging.Met);

            // dropped with its propellant, never relit
            Assert.InRange(flight.EngineOf(2).Fuel, 9.9, 10.1);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        [Fact]
        public void DedicatedCoast()
        {
            // KSP stage 1 only drops the booster
            var flight = new AscentFlight(2);
            flight.AddEngine(1, 0, -1, 30);
            flight.AddEngine(3, 2, 1, 60);
            // BURN:0 COAST_DEDICATED:1 BURN:2
            Solution solution = Solve(flight.Plan(DEDICATED, 1), MET0, Cut(50), Coast(20), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());

            // COASTING stages the cut booster off into the coast's stage, and autostaging lights the last stage as it ends
            Assert.Equal(2, flight.Stagings.Count);
            Assert.Equal(StagedBy.STAGE, flight.Stagings[0].By);
            Assert.Equal(2, flight.Stagings[0].From);
            AtAbout(solution.StartTime(1), flight.Stagings[0].Met);
            Assert.Equal(StagedBy.AUTOSTAGING, flight.Stagings[1].By);
            Assert.Equal(1, flight.Stagings[1].From);
            AtAbout(solution.EndTime(1), flight.Stagings[1].Met);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        /*
         * Phases the optimizer shrank to nothing, which guidance skips (Phase.Skip)
         */

        // a skipped coast is no coast: no events, no RCS, and the autostaging stops at the last stage
        private static void NoCoast(AscentFlight flight)
        {
            Assert.Equal(0, flight.Count(AscentEvents.COAST_STARTED));
            Assert.Equal(0, flight.Count(AscentEvents.COAST_ENDED));
            Assert.Equal(0, flight.Count(AscentMessages.RCS_COAST));
            Assert.DoesNotContain(flight.Log, t => t.Command.RCSGroupOn);
            Assert.All(flight.Log.Where(t => t.State != PSGState.FINISHED), t => Assert.Equal(0, t.Command.AutostageLimit));
            Assert.False(flight.Machine.HasCoasted);
        }

        // A coast-during in the last stage the optimizer shrank to nothing, with the burn before it (the zero coast constraint):
        // the stage is one burn, never cut and relit, and terminal guidance ends it at the end of the burn after the coast.
        [Fact]
        public void ZeroCoastDuringIsOneBurn()
        {
            AscentFlight flight = OneStage();
            // BURN:0 COAST_DURING:0 BURN:0
            Solution solution = Solve(flight.Plan(DURING, 0), MET0, Cut(0), Coast(0), Cut(60));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING TERMINAL FINISHED", flight.Sequence());
            AtAbout(solution.Tf - 10, flight.EnteredAt(PSGState.TERMINAL), DT);
            AtAbout(solution.Tf, flight.EnteredAt(PSGState.FINISHED), 2 * DT);
            NoCoast(flight);
            Assert.Equal(1, flight.EngineOf(1).Burns);
            Assert.InRange(flight.EngineOf(1).Fuel, 39.9, 40.1);
        }

        // the same before the last stage: the stage burns out into the last stage, which autostaging lights
        [Fact]
        public void ZeroCoastDuringBeforeTheLastStage()
        {
            AscentFlight flight = TwoStages(80);
            // BURN:0 BURN:1 COAST_DURING:1 BURN:1
            Solution solution = Solve(flight.Plan(DURING, 1), MET0, Cut(0), Coast(0), Fixed(80), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING FINISHED", flight.Sequence());
            NoCoast(flight);
            Assert.All(flight.Log, t => Assert.Equal(AscentStaging.NONE, t.Command.Staging));
            Staging staging = Assert.Single(flight.Stagings);
            Assert.Equal(StagedBy.AUTOSTAGING, staging.By);
            AtAbout(MET0 + 80, staging.Met, 0, flight.StatsPeriod + 2 * DT);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        // A coast-before the optimizer shrank to nothing: the burn cut before it is cut before a fixed upper stack, which JETTISON
        // drops at its planned end.
        [Fact]
        public void ZeroCoastBeforeJettisons()
        {
            AscentFlight flight = ThreeStages();
            // BURN:0 BURN:1 COAST_BEFORE:1 BURN:2
            Solution solution = Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(0), Fixed(40), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 50, flight.EnteredAt(PSGState.JETTISON));
            NoCoast(flight);
            Assert.Equal(StagedBy.IMMEDIATE, flight.Stagings[0].By);
            Assert.Equal(2, flight.Stagings[0].From);
            Assert.InRange(flight.EngineOf(3).Fuel, 9.9, 10.1);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        // The burn before a coast-during shrunk to nothing, with a coast that isn't: the coast starts as the booster before it
        // ends, and the coast's stage lights once, after it.
        [Fact]
        public void SkippedBurnBeforeACoast()
        {
            AscentFlight flight = ThreeStages();
            // BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2
            Solution solution = Solve(flight.Plan(DURING, 1), MET0, Fixed(60), Cut(0), Coast(20), Fixed(40), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 60, flight.EnteredAt(PSGState.COASTING));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_STARTED));
            AtAbout(solution.EndTime(2), flight.At(AscentEvents.COAST_ENDED));
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        [Fact]
        public void BoosterRunningDryStartsTheCoastEarly()
        {
            AscentFlight flight = ThreeStages(45);
            // the stage stats see it empty at once
            flight.StatsPeriod = DT;
            Solution solution = Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(30), Fixed(40), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            // the coast starts when the burn before it runs dry, 5 s before its planned start, and still ends on time
            AtAbout(MET0 + 45, flight.EnteredAt(PSGState.COASTING), 0, 2 * DT);
            AtAbout(solution.EndTime(1), flight.At(AscentEvents.COAST_ENDED));
            Assert.Equal(StagedBy.STAGE, flight.Stagings[0].By);
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        [Fact]
        public void BoosterOverburnIsNotStagedOffByTheCoast()
        {
            // the booster burns 40 s past its planned end, 10 s past the planned start of the coast before the last stage
            AscentFlight flight = ThreeStages(100);
            // BURN:0 COAST_BEFORE:0 BURN:1 BURN:2
            Solution solution = Solve(flight.Plan(BEFORE, 0), MET0, Fixed(60), Cut(30), Coast(20), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            // the coast waits for the booster to burn out (to under a m/s), and the burn before the coast, past its time, is cut
            // before it lights: the coast stages both off into its KSP stage
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            AtAbout(MET0 + 100, flight.EnteredAt(PSGState.COASTING), 0, flight.StatsPeriod + 2 * DT);
            AtAbout(solution.EndTime(2), flight.At(AscentEvents.COAST_ENDED));
            Assert.InRange(flight.EngineOf(3).Fuel, 0, 1e-9);
            Assert.Equal(2, flight.Stagings.Count);
            Assert.All(flight.Stagings, s => Assert.Equal(StagedBy.STAGE, s.By));
            Assert.Equal(2, flight.Stagings[0].From);
            AtAbout(flight.EnteredAt(PSGState.COASTING), flight.Stagings[0].Met);
            Assert.Equal(1, flight.EngineOf(3).Burns);
            Assert.Equal(0, flight.EngineOf(2).Burns);
            Assert.Equal(1, flight.EngineOf(1).Burns);
        }

        /*
         * The burnout checks look at the stage the vessel is in.  After a booster below burns out, the vessel waits in the
         * booster's empty stage until autostaging drops it (its pre-delay, or the RCS settling the next stage's propellant), and
         * stage stats run in that wait show the current stage empty: that is not the end of the burn after it.
         */

        [Fact]
        public void BoosterBurnoutDoesNotStartACoastDuring()
        {
            // the booster burns out at its planned end, 30 s before the coast during the middle stage
            AscentFlight flight = ThreeStages(middle: 80);
            flight.AutostagePreDelay = 0.5;
            // BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2
            Solution solution = Solve(flight.Plan(DURING, 1), MET0, Fixed(60), Cut(30), Coast(20), Fixed(50), Fixed(30));
            flight.Engage(solution);

            flight.Fly();

            // the coast starts on time, after the burn before it
            AtAbout(solution.StartTime(2), flight.EnteredAt(PSGState.COASTING));
            Assert.All(flight.Engines, e => Assert.Equal(e.Id == 2 ? 2 : 1, e.Burns));
        }

        [Fact]
        public void BoosterBurnoutDoesNotSpendTheLastStage()
        {
            // the booster burns out at its planned end, and autostaging waits 1.5 s to light the last stage
            AscentFlight flight = TwoStages();
            flight.AutostagePreDelay = 1.5;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(40), Fixed(30)));

            flight.Fly();

            // the last stage lights, and burns out
            Assert.Equal(1, flight.EngineOf(1).Burns);
            AtAbout(MET0 + 71.5, flight.EnteredAt(PSGState.FINISHED), 0, flight.StatsPeriod + 3 * DT);
        }

        /*
         * Solutions and stage stats
         */

        [Fact]
        public void SolutionsSolvedInFlight()
        {
            AscentFlight flight = ThreeStages();
            Solution pad = Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(30), Fixed(40), Fixed(30));
            flight.Engage(pad);

            Assert.True(flight.RunUntil(PSGState.COASTING));
            flight.Run(5);

            // solved in the coast: the rest of it, from the coast's flight plan stage (2), then the burns after it
            double now = flight.Met;
            Assert.True(flight.Machine.SetSolution(Solve(flight.FlownPlan, now, 2, Coast(pad.EndTime(1) - now), Fixed(40), Fixed(30))));

            Assert.True(flight.RunUntil(PSGState.BURNING));
            AtAbout(pad.EndTime(1), flight.Log.Last().Met);
            flight.Run(10);

            // solved after it: the burns from flight plan stage 1
            now = flight.Met;
            Assert.True(flight.Machine.SetSolution(Solve(flight.FlownPlan, now, 1, Fixed(pad.EndTime(2) - now), Fixed(30))));

            flight.Fly();

            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING COASTING BURNING FINISHED", flight.Sequence());
            Assert.Equal(1, flight.Count(AscentEvents.COAST_STARTED));
            Assert.Equal(1, flight.Count(AscentEvents.COAST_ENDED));
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        [Fact]
        public void StaleStatsAfterTheJettison()
        {
            AscentFlight flight = ThreeStages();
            // the stage stats show the booster for up to 5 s after it is gone
            flight.StatsPeriod = 5;
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30)));

            flight.Fly();

            // JETTISON sees the booster's engine leave the vessel, and guidance moves on to the next stage without the stats
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING JETTISON BURNING FINISHED", flight.Sequence());
            Assert.Single(flight.In(PSGState.JETTISON));
            Assert.Equal(StagedBy.IMMEDIATE, flight.Stagings[0].By);
            Assert.All(flight.Stagings.Skip(1), s => Assert.Equal(StagedBy.AUTOSTAGING, s.By));
            Assert.All(flight.Engines, e => Assert.Equal(1, e.Burns));
        }

        [Fact]
        public void StagedPastTheLastStageFinishes()
        {
            AscentFlight flight = ThreeStages();
            // the plan ends with the middle stage: BURN:1 BURN:2
            flight.Engage(Solve(flight.Plan(planned: 2), MET0, Fixed(60), Fixed(40)));

            Assert.True(flight.RunUntil(f => f.Met >= MET0 + 80));
            Assert.Equal(PSGState.BURNING, flight.Machine.State);

            // staged by hand: the last stage is gone from the stage stats at their next run
            double staged = flight.Met;
            flight.StageByHand();
            Assert.Equal(StagedBy.HAND, flight.Stagings.Last().By);

            Assert.True(flight.RunUntil(PSGState.FINISHED, 1));
            AtAbout(staged, flight.EnteredAt(PSGState.FINISHED), 0, flight.StatsPeriod + 2 * DT);
        }

        [Fact]
        public void BoosterOverburnCascadesIntoTheRcsTrim()
        {
            // a fixed booster (engine 2) that burns 15 s past its planned end, past the end of the last stage's planned 10 s burn
            var flight = new AscentFlight(1) { RcsThrust = 1, RCSGroup = true };
            flight.AddEngine(1, 0, -1, 100);
            flight.AddEngine(2, 1, 0, 75);
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(60), Cut(10)));

            flight.Fly();

            // the last stage is past the target as it starts: one tick from BURNING through TERMINAL to TERMINAL_RCS, which
            // still says terminal guidance started
            Assert.Equal("PRELAUNCH PITCH_PROGRAM BURNING TERMINAL_RCS FINISHED", flight.Sequence());
            Assert.Equal(1, flight.Count(AscentEvents.TERMINAL_STARTED));
            Assert.Equal(1, flight.Count(AscentMessages.RCS_TRIM));
            AtAbout(MET0 + 75, flight.EnteredAt(PSGState.TERMINAL_RCS), 0, flight.StatsPeriod + 2 * DT);
        }
    }
}
