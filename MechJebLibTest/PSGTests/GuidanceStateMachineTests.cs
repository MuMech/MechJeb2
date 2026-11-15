/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;
using Xunit.Abstractions;
using static MechJebLibTest.PSGTests.SolutionTests;

namespace MechJebLibTest.PSGTests
{
    /*
     * Each test flies a Solution through the guidance state machine with a stand-in vessel (Flight below): it sits on the
     * Solution's trajectory, moving along it only while it burns or the Solution's clock is in a coast, so a stage that runs
     * dry or a cut engine leaves it short of the target.  The synthetic Solutions are built like SolutionTests' (128 s per
     * normalized time unit, starting in flight at MET 4096 s), and their angular momentum grows strictly along the trajectory.
     */
    public class GuidanceStateMachineTests
    {
        private readonly ITestOutputHelper _out;

        public GuidanceStateMachineTests(ITestOutputHelper output) => _out = output;

        private static double Met(double tbar) => T0 + tbar * TIME_SCALE;

        // a burn from 1024 kg down to mf (the stage is empty at 512 kg)
        private static Seg Burn(Phase phase, double tbar0, double tbar1, double mf = 512) => new Seg(phase, tbar0, tbar1, 1024, mf, _xHat, _xHat, 1, 1);

        private static Seg Coast(Phase phase, double tbar0, double tbar1) => new Seg(phase, tbar0, tbar1, 512, 512, _xHat, _xHat, 0, 0);

        private static Phase OptBurn(int kspStage, bool precise = false, bool terminal = false)
        {
            Phase p = NewBurn(kspStage, kspStage, 1024, 512, 300, 64);
            p.PreciseShutdown = precise;
            p.TerminalStage = terminal;
            return p;
        }

        private static Phase FixedBurn(int kspStage, bool terminal = false)
        {
            Phase p = NewBurn(kspStage, kspStage, 1024, 512, 300, 64, false);
            p.TerminalStage = terminal;
            return p;
        }

        // an optimized booster (KSP stage 2, 4096-4160 s, cut with propellant left), a coast-before coast (stage 1, 4160-4224 s) and a
        // fixed upper stage (stage 1, 4224-4256 s): the RP-1 Juno I
        private static Solution BoosterBeforeCoastPlan() =>
            Build(Burn(OptBurn(2, true), 0, 0.5, 600), Coast(NewCoast(1, 1, 512), 0.5, 1), Burn(FixedBurn(1, true), 1, 1.25));

        // an optimized booster (KSP stage 2, 4096-4160 s, cut with propellant left unless boosterMf is its empty mass) under a fixed
        // upper stage (stage 1, 4160-4192 s)
        private static Solution BoosterBeforeStackPlan(double boosterMf = 600) =>
            Build(Burn(OptBurn(2, true), 0, 0.5, boosterMf), Burn(FixedBurn(1, true), 0.5, 0.75));

        // one KSP stage that burns (4096-4160 s), coasts (4160-4224 s) and burns again to the target (4224-4256 s)
        private static Solution CoastDuring() =>
            Build(Burn(OptBurn(1), 0, 0.5), Coast(NewCoast(1, 1, 512), 0.5, 1), Burn(OptBurn(1, true, true), 1, 1.25));

        // a booster burned out by the plan (KSP stage 2, 4096-4160 s), a coast-before coast (stage 1, 4160-4224 s) and the upper
        // stage to the target (stage 1, 4224-4256 s)
        private static Solution CoastBefore() =>
            Build(Burn(OptBurn(2), 0, 0.5), Coast(NewCoast(1, 1, 512), 0.5, 1), Burn(OptBurn(1, true, true), 1, 1.25));

        // a fixed booster (KSP stage 1, 4096-4160 s) and a fixed upper stage (stage 0, 4160-4224 s)
        private static Solution FixedRocket() => Build(Burn(FixedBurn(1), 0, 0.5), Burn(FixedBurn(0, true), 0.5, 1));

        // one stage to the target (KSP stage 0, 4096-4160 s)
        private static Solution SingleBurn() => Build(Burn(OptBurn(0, true, true), 0, 0.5));

        private struct Event
        {
            public double    Met;
            public PSGStatus Status;
            public int       Stage;

            public override string ToString() => $"{Met:F2} {Status} stage {Stage}";
        }

        /// <summary>
        ///     The vessel that guidance flies in these tests, and the KSP side: it applies the commands, and autostaging drops a stage
        ///     with no thrust left down to the autostage limit, as MechJeb's does for a stage that has burned out.
        /// </summary>
        private sealed class Flight
        {
            public readonly GuidanceStateMachine Machine = new GuidanceStateMachine();
            public readonly Solution             Solution;
            public readonly List<Event>          Events = new List<Event>();
            public readonly List<GuidanceCommand> Commands = new List<GuidanceCommand>();

            public const double DT = 0.02;

            public double Met = T0;
            public int    Stage;
            public bool   RCSGroup;
            public bool   ForwardRCS;
            public int    AutoStageLimit = -1;
            public int    Staged;

            // the MET each KSP stage runs dry; a stage burns until the end of its last burn phase in the plan plus a second,
            // so a planned cut leaves propellant in it
            public readonly Dictionary<int, double> DryAt = new Dictionary<int, double>();

            // MET ranges with the engines out though the stage still has propellant (a flameout)
            public readonly List<(double from, double to)> Flameouts = new List<(double, double)>();

            // the MET to stage at by hand, and how many times
            public readonly List<double> ManualStaging = new List<double>();

            private double              _tEff;
            private GuidanceThrottle    _throttle = GuidanceThrottle.SOLUTION;
            private PSGStatus           _lastStatus = (PSGStatus)(-1);

            public Flight(Solution solution)
            {
                Solution = solution;
                Stage = solution.KSPStage(0);
                _tEff = solution.T0;
                for (int i = 0; i < solution.Segments; i++)
                    if (!solution.CoastPhase(i))
                        DryAt[solution.KSPStage(i)] = solution.EndTime(i) + 1;
                Machine.SetSolution(solution);
            }

            private bool HasBurn(int stage) => DryAt.ContainsKey(stage);

            private bool Lit => HasBurn(Stage) && Met < DryAt[Stage] && !Flameouts.Any(f => Met >= f.from && Met < f.to);

            private bool Dry => !HasBurn(Stage) || Met >= DryAt[Stage];

            public void Tick()
            {
                (V3 r, V3 v) = Solution.StateVectors(_tEff);
                bool moving = Moving();
                V3 a = V3.zero;
                if (moving)
                {
                    (_, V3 v1) = Solution.StateVectors(_tEff + DT);
                    a = (v1 - v) / DT;
                }

                var inputs = new GuidanceInputs
                {
                    Met = Met,
                    Time = Met + 1e6,
                    Dt = DT,
                    KSPStage = Stage,
                    Grounded = false,
                    AllowExecution = true,
                    Mass = 1,
                    ThrustAvailable = Lit ? 1 : 0,
                    RCSThrustUp = ForwardRCS ? 0.1 : 0,
                    ForwardRCS = ForwardRCS,
                    RCSGroup = RCSGroup,
                    StageDeltaV = !HasBurn(Stage) ? double.NaN : Dry ? 0 : 100,
                    R = r,
                    V = v,
                    A = a,
                    UllageLeadTime = 20
                };

                GuidanceCommand cmd = Machine.Step(inputs);
                Commands.Add(cmd);
                Apply(cmd);

                if (moving)
                    _tEff += DT;

                if (ManualStaging.Count > 0 && Met >= ManualStaging[0])
                {
                    ManualStaging.RemoveAt(0);
                    Stage--;
                }

                Record();
                Met += DT;
            }

            // the vessel moves along the trajectory while it burns, or coasts on the Solution's clock
            private bool Moving()
            {
                if (Machine.Status == PSGStatus.FINISHED)
                    return false;
                if (_throttle == GuidanceThrottle.RCS_FORWARD)
                    return true;
                if (Solution.Coast(Met) && Met < Solution.Tf)
                    return true;
                return _throttle == GuidanceThrottle.SOLUTION && Lit;
            }

            private void Apply(in GuidanceCommand cmd)
            {
                if (cmd.Throttle != GuidanceThrottle.NONE)
                    _throttle = cmd.Throttle;
                if (cmd.RCSGroupOn)
                    RCSGroup = true;
                if (cmd.AutoStageLimit >= 0)
                    AutoStageLimit = cmd.AutoStageLimit;
                if (cmd.Staging != GuidanceStaging.NONE)
                    StageOnce();

                // autostaging: a stage with nothing left to burn is dropped, down to the limit
                while (Machine.Status != PSGStatus.FINISHED && Dry && Stage > 0 && Stage - 1 >= AutoStageLimit)
                    StageOnce();
            }

            private void StageOnce()
            {
                Stage--;
                Staged++;
            }

            private void Record()
            {
                if (Machine.Status == _lastStatus && (Events.Count == 0 || Events[Events.Count - 1].Stage == Stage))
                    return;
                _lastStatus = Machine.Status;
                Events.Add(new Event { Met = Met, Status = Machine.Status, Stage = Stage });
            }

            public void Fly(double until)
            {
                while (Met < until && Machine.Status != PSGStatus.FINISHED)
                    Tick();
            }

            public PSGStatus[] Statuses()
            {
                var list = new List<PSGStatus>();
                foreach (Event e in Events)
                    if (list.Count == 0 || list[list.Count - 1] != e.Status)
                        list.Add(e.Status);
                return list.ToArray();
            }

            public double When(PSGStatus status) => Events.First(e => e.Status == status).Met;

            public double WhenStage(int stage) => Events.First(e => e.Stage == stage).Met;

            public override string ToString() => string.Join("\n", Events);
        }

        private Flight Fly(Flight flight, double until)
        {
            flight.Fly(until);
            _out.WriteLine(flight.ToString());
            return flight;
        }

        [Fact]
        public void BoosterBeforeCoast()
        {
            using Solution solution = BoosterBeforeCoastPlan();
            Flight f = Fly(new Flight(solution), Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINISHED });
            // no terminal guidance on the booster: cut at its planned end, staged into the coast's stage on the same tick
            f.When(PSGStatus.COASTING).ShouldEqual(Met(0.5), Flight.DT);
            f.WhenStage(1).ShouldEqual(f.When(PSGStatus.COASTING), 1e-9);
            f.Commands.Count(c => c.Staging == GuidanceStaging.IMMEDIATE).ShouldEqual(1);
            f.Events.Last(e => e.Status == PSGStatus.BURNING).Met.ShouldEqual(Met(1), Flight.DT);
            // the fixed upper stage ends a second after its engine is out
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(1.25) + 2, Flight.DT);
        }

        [Fact]
        public void BoosterBeforeCoastRunsDry()
        {
            using Solution solution = BoosterBeforeCoastPlan();
            var flight = new Flight(solution);
            flight.DryAt[2] = Met(0.5) - 5;
            Flight f = Fly(flight, Met(2));

            // the Juno: the booster runs dry before its planned end, and a second later guidance stages it off and coasts
            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINISHED });
            f.When(PSGStatus.COASTING).ShouldEqual(Met(0.5) - 4, Flight.DT);
            f.WhenStage(1).ShouldEqual(Met(0.5) - 4, Flight.DT);
            f.Events.Last(e => e.Status == PSGStatus.BURNING).Met.ShouldEqual(Met(1), Flight.DT);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(1.25) + 2, Flight.DT);
        }

        [Fact]
        public void BoosterBeforeStack()
        {
            using Solution solution = BoosterBeforeStackPlan();
            Flight f = Fly(new Flight(solution), Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINISHED });
            // cut at its planned end and staged on the same tick
            f.WhenStage(1).ShouldEqual(Met(0.5), Flight.DT);
            f.Commands.Count(c => c.Staging == GuidanceStaging.IMMEDIATE).ShouldEqual(1);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.75) + 2, Flight.DT);
        }

        [Fact]
        public void BoosterBeforeStackRunsDry()
        {
            using Solution solution = BoosterBeforeStackPlan();
            var flight = new Flight(solution);
            flight.DryAt[2] = Met(0.5) - 5;
            Flight f = Fly(flight, Met(2));

            // the booster's autostage limit holds it, and a second after it runs dry guidance stages it off
            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINISHED });
            f.WhenStage(1).ShouldEqual(Met(0.5) - 4, Flight.DT);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.75) + 2, Flight.DT);
        }

        [Fact]
        public void OptimizedBoosterBurnedOutBeforeStack()
        {
            using Solution solution = BoosterBeforeStackPlan(512);
            Flight f = Fly(new Flight(solution), Met(2));

            // the plan burns it out: autostaging drops it when it is out, and guidance stages nothing
            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINISHED });
            f.WhenStage(1).ShouldEqual(Met(0.5) + 1, Flight.DT);
            f.Commands.Any(c => c.Staging != GuidanceStaging.NONE).ShouldBeFalse();
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.75) + 2, Flight.DT);
        }

        [Fact]
        public void CoastDuringTheBurn()
        {
            using Solution solution = CoastDuring();
            Flight f = Fly(new Flight(solution), Met(2));

            f.Statuses().ShouldEqual(new[]
            {
                PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED
            });
            f.When(PSGStatus.COASTING).ShouldEqual(Met(0.5), Flight.DT);
            f.Events.Last(e => e.Status == PSGStatus.BURNING).Met.ShouldEqual(Met(1), Flight.DT);
            f.When(PSGStatus.FINAL).ShouldEqual(Met(1.25) - 10, Flight.DT);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(1.25) - Flight.DT, 1e-9);
            f.Staged.ShouldEqual(0);
            // the ullage: RCS forward for the last 20 s of the coast
            ((double)f.Commands.Count(c => c.Throttle == GuidanceThrottle.RCS_FORWARD)).ShouldEqual(20 / Flight.DT, 2);
            f.Commands.Count(c => (c.Messages & GuidanceMessages.RCS_COAST) != 0).ShouldEqual(1);
            f.Commands.Any(c => c.MarkCoastDone).ShouldBeTrue();
        }

        [Fact]
        public void CoastBeforeTheUpperStage()
        {
            using Solution solution = CoastBefore();
            Flight f = Fly(new Flight(solution), Met(2));

            f.Statuses().ShouldEqual(new[]
            {
                PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED
            });
            f.When(PSGStatus.COASTING).ShouldEqual(Met(0.5), Flight.DT);
            f.WhenStage(1).ShouldEqual(f.When(PSGStatus.COASTING), 1e-9);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(1.25) - Flight.DT, 1e-9);
        }

        [Fact]
        public void FixedBurnRocket()
        {
            using Solution solution = FixedRocket();
            Flight f = Fly(new Flight(solution), Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINISHED });
            // autostaging drops the booster when it is out
            f.WhenStage(0).ShouldEqual(Met(0.5) + 1, Flight.DT);
            f.Commands.Any(c => c.Staging != GuidanceStaging.NONE).ShouldBeFalse();
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(1) + 2, Flight.DT);
        }

        [Fact]
        public void RCSTrim()
        {
            using Solution solution = SingleBurn();
            Flight f = Fly(new Flight(solution) { ForwardRCS = true, RCSGroup = true }, Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINAL_RCS, PSGStatus.FINISHED });
            // two ticks short on the engine, then one tick on the RCS
            f.When(PSGStatus.FINAL_RCS).ShouldEqual(Met(0.5) - 2 * Flight.DT, 1e-9);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.5) - Flight.DT, 1e-9);
            f.Commands.Count(c => (c.Messages & GuidanceMessages.RCS_TRIM) != 0).ShouldEqual(1);
        }

        [Fact]
        public void RCSTrimWithTheGroupOff()
        {
            using Solution solution = SingleBurn();
            Flight f = Fly(new Flight(solution) { ForwardRCS = true }, Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED });
            f.Commands.Count(c => (c.Messages & GuidanceMessages.RCS_OFF) != 0).ShouldEqual(1);
        }

        [Fact]
        public void LastStageRunsDry()
        {
            using Solution solution = SingleBurn();
            var flight = new Flight(solution);
            flight.DryAt[0] = Met(0.5) - 5;
            Flight f = Fly(flight, Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED });
            // a second after it runs dry
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.5) - 4, Flight.DT);
        }

        [Fact]
        public void LastStageRunsDryBeforeFinal()
        {
            using Solution solution = SingleBurn();
            var flight = new Flight(solution);
            flight.DryAt[0] = Met(0.5) - 30;
            Flight f = Fly(flight, Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINISHED });
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.5) - 29, Flight.DT);
        }

        [Fact]
        public void EarlyStaging()
        {
            using Solution solution = BoosterBeforeStackPlan();
            var flight = new Flight(solution);
            flight.ManualStaging.Add(Met(0.5) - 20);
            Flight f = Fly(flight, Met(2));

            // staged by hand off the booster before its end: guidance moves on without staging again
            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.FINISHED });
            f.WhenStage(1).ShouldEqual(Met(0.5) - 20, Flight.DT);
            f.Staged.ShouldEqual(0);
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(0.75) + 2, Flight.DT);
        }

        [Fact]
        public void EarlyStagingIntoTheCoast()
        {
            using Solution solution = BoosterBeforeCoastPlan();
            var flight = new Flight(solution);
            flight.ManualStaging.Add(Met(0.5) - 20);
            Flight f = Fly(flight, Met(2));

            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINISHED });
            f.When(PSGStatus.COASTING).ShouldEqual(Met(0.5) - 20, Flight.DT);
            f.Staged.ShouldEqual(0);
        }

        [Fact]
        public void FlameoutMidBurn()
        {
            using Solution solution = CoastBefore();
            var flight = new Flight(solution);
            flight.Flameouts.Add((Met(0.25), Met(0.25) + 3));
            Flight f = Fly(flight, Met(2));

            // an engine out with propellant left changes nothing: no staging, no end
            f.Statuses().ShouldEqual(new[]
            {
                PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED
            });
            f.WhenStage(1).ShouldEqual(Met(0.5), Flight.DT);
        }

        [Fact]
        public void FlameoutAtTheEndOfAFixedLastStage()
        {
            using Solution solution = FixedRocket();
            var flight = new Flight(solution);
            // the upper stage's engine is out from its planned end on, with propellant left (residuals)
            flight.DryAt[0] = Met(1) + 30;
            flight.Flameouts.Add((Met(1), Met(1) + 60));
            Flight f = Fly(flight, Met(2));

            // past its planned end no thrust is enough
            f.When(PSGStatus.FINISHED).ShouldEqual(Met(1) + 1, Flight.DT);
        }

        [Fact]
        public void StaleSolutionsDoNotGoBack()
        {
            using Solution solution = BoosterBeforeCoastPlan();
            var flight = new Flight(solution);
            flight.Fly(Met(0.75));
            flight.Machine.Status.ShouldEqual(PSGStatus.COASTING);

            // a Solution solved before the cut still has the booster
            flight.Machine.SetSolution(solution);
            flight.Machine.Status.ShouldEqual(PSGStatus.COASTING);
            flight.Tick();
            flight.Machine.Status.ShouldEqual(PSGStatus.COASTING);

            Flight f = Fly(flight, Met(2));
            f.Statuses().ShouldEqual(new[] { PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINISHED });
            f.Commands.Count(c => c.Staging == GuidanceStaging.IMMEDIATE).ShouldEqual(1);
        }

        [Fact]
        public void SolutionsSolvedInFlight()
        {
            using Solution solution = CoastDuring();
            var flight = new Flight(solution);
            flight.Fly(Met(0.75));
            flight.Machine.Status.ShouldEqual(PSGStatus.COASTING);

            // solved in the coast: the rest of the coast and the burn
            using Solution inCoast = Build(Coast(NewCoast(1, 1, 512), 0.75, 1), Burn(OptBurn(1, true, true), 1, 1.25));
            flight.Machine.SetSolution(inCoast);
            flight.Machine.Status.ShouldEqual(PSGStatus.COASTING);
            flight.Fly(Met(1.1));
            flight.Machine.Status.ShouldEqual(PSGStatus.BURNING);

            // solved after the coast: the burn alone, the second one in that KSP stage
            using Solution afterCoast = Build(Burn(OptBurn(1, true, true), 1.1, 1.25));
            flight.Machine.SetSolution(afterCoast);
            flight.Machine.Status.ShouldEqual(PSGStatus.BURNING);

            // and a stale one with the coast in it again
            flight.Machine.SetSolution(inCoast);
            flight.Machine.Status.ShouldEqual(PSGStatus.BURNING);

            Flight f = Fly(flight, Met(2));
            f.Statuses().ShouldEqual(new[]
            {
                PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED
            });
        }

        // KerbinTests.MainSailTinCanVacuum solved, flown through its burn, coast-during coast and burn
        [Fact]
        public void SolvedCoastDuringAscent()
        {
            Ascent ascent = Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06),
                    new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 0, 3531600000000, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            var flight = new Flight(solution);
            flight.Met = solution.T0;
            Flight f = Fly(flight, solution.Tf + 60);

            f.Statuses().ShouldEqual(new[]
            {
                PSGStatus.BURNING, PSGStatus.COASTING, PSGStatus.BURNING, PSGStatus.FINAL, PSGStatus.FINISHED
            });
            f.When(PSGStatus.COASTING).ShouldEqual(solution.EndTime(0), Flight.DT);
            f.When(PSGStatus.FINISHED).ShouldEqual(solution.Tf, 2 * Flight.DT);
            f.Staged.ShouldEqual(0);
        }
    }

    internal static class GuidanceAssertions
    {
        public static void ShouldEqual(this PSGStatus[] actual, PSGStatus[] expected) =>
            Assert.Equal(string.Join(", ", expected), string.Join(", ", actual));

        public static void ShouldEqual(this PSGStatus actual, PSGStatus expected) => Assert.Equal(expected, actual);
    }
}
