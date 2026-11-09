/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Functions;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.PSG.Terminal;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PSGTests
{
    public class SolutionTests
    {
        private const int SAMPLES = 64;

        private const double TIN_CAN_T0 = 82132.7494998545;

        // KerbinTests.MainSailTinCanVacuum: a burn, a coast-during coast and the rest of the burn.  With a plan the three phases
        // are built from its stages 2, 1 and 0 (see TinCanFlightPlan).
        private static Ascent.AscentBuilder TinCanBuilder(FlightPlan? plan = null)
        {
            bool hasPlan = plan != null;

            Ascent.AscentBuilder builder = Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0,
                    flightPlanIndex: hasPlan ? 2 : -1)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true, flightPlanIndex: hasPlan ? 1 : -1)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true,
                    flightPlanIndex: hasPlan ? 0 : -1)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), TIN_CAN_T0, 3531600000000, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false);

            if (plan != null)
                builder.FlightPlan(plan);

            return builder;
        }

        // The plan for TinCanBuilder, listed from the top of the rocket down as FlightPlan.FromLogicalPlan does.  Below the
        // burn-coast-burn is a booster that has already burned out, so no phase is built from it and the phases don't line up
        // with the plan's positions.
        private static FlightPlan TinCanFlightPlan()
        {
            var plan = new FlightPlan();
            plan.Stages.Add(new FlightPlanStage(0, new FuelStats(), false, true, StageType.BURN, 0, false, -1));
            plan.Stages.Add(new FlightPlanStage(0, new FuelStats(), false, true, StageType.COAST_DURING, 0, false, -1));
            plan.Stages.Add(new FlightPlanStage(0, new FuelStats(), false, true, StageType.BURN, 0, true, -1));
            plan.Stages.Add(new FlightPlanStage(1, new FuelStats(), false, false, StageType.BURN, 1, true, -1) { Done = true });
            return plan;
        }

        // the solver warm starts from one copy while guidance flies another, so a copy has to agree bit-for-bit, own its T0 and
        // survive the original being disposed
        [Fact]
        public void DeepCopyIsIndependent()
        {
            double t0 = TIN_CAN_T0;

            Ascent ascent = TinCanBuilder().Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            Solution solution = psg.Solution ?? throw new Exception("null solution");

            Solution copy = solution.DeepCopy();

            copy.Segments.ShouldEqual(solution.Segments);
            copy.T0.ShouldEqual(solution.T0, 0);
            copy.Tf.ShouldEqual(solution.Tf, 0);
            copy.Vgo(t0).ShouldEqual(solution.Vgo(t0), 0);
            copy.Tgo(t0).ShouldEqual(solution.Tgo(t0), 0);

            for (int p = 0; p < solution.Segments; p++)
            {
                copy.StartTime(p).ShouldEqual(solution.StartTime(p), 0);
                copy.EndTime(p).ShouldEqual(solution.EndTime(p), 0);
                (copy.CoastPhase(p) == solution.CoastPhase(p)).ShouldBeTrue();
                copy.KSPStage(p).ShouldEqual(solution.KSPStage(p));
                copy.MJPhase(p).ShouldEqual(solution.MJPhase(p));

                // built without a FlightPlan
                copy.FlightPlanIndex(p).ShouldEqual(-1);
                Assert.Null(copy.FlightPlanStage(p));
            }

            Assert.Null(solution.FlightPlan);
            Assert.Null(copy.FlightPlan);

            // the stage flags SolutionBuilder set come along with the phases
            copy.TerminalKSPStage().ShouldEqual(solution.TerminalKSPStage());
            copy.TerminalMJPhase().ShouldEqual(solution.TerminalMJPhase());
            copy.OptimizeKSPStage().ShouldEqual(solution.OptimizeKSPStage());
            copy.CoastKSPStage().ShouldEqual(solution.CoastKSPStage());

            var r = new V3[SAMPLES + 1];
            var v = new V3[SAMPLES + 1];
            var u = new V3[SAMPLES + 1];
            double[] m  = new double[SAMPLES + 1];
            double[] dv = new double[SAMPLES + 1];
            double tf = solution.Tf;

            for (int i = 0; i <= SAMPLES; i++)
            {
                double t = t0 + (tf - t0) * i / SAMPLES;
                r[i]  = solution.R(t);
                v[i]  = solution.V(t);
                u[i]  = solution.U(t);
                m[i]  = solution.M(t);
                dv[i] = solution.DV(t);

                copy.R(t).ShouldEqual(r[i], 0);
                copy.V(t).ShouldEqual(v[i], 0);
                copy.U(t).ShouldEqual(u[i], 0);
                copy.M(t).ShouldEqual(m[i], 0);
                copy.DV(t).ShouldEqual(dv[i], 0);
            }

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            (V3 rfCopy, V3 vfCopy) = copy.TerminalStateVectors();
            rfCopy.ShouldEqual(rf, 0);
            vfCopy.ShouldEqual(vf, 0);

            double solutionT0 = solution.T0;
            double copyT0     = copy.T0;
            copy.T0 += 10;
            solution.T0.ShouldEqual(solutionT0, 0);
            copy.T0 = copyT0;

            solution.Dispose();

            for (int i = 0; i <= SAMPLES; i++)
            {
                double t = t0 + (tf - t0) * i / SAMPLES;
                copy.R(t).ShouldEqual(r[i], 0);
                copy.V(t).ShouldEqual(v[i], 0);
                copy.U(t).ShouldEqual(u[i], 0);
                copy.M(t).ShouldEqual(m[i], 0);
            }

            copy.Dispose();
        }

        // The Solution carries the snapshot of the FlightPlan that the builder took, which every optimizer of the solve shares,
        // and each phase finds the plan stage it was built from.  Copies and warm starts get their own plans.
        [Fact]
        public void FlightPlanFollowsTheSolution()
        {
            int[] index = { 2, 1, 0 };
            FlightPlan plan = TinCanFlightPlan();

            Ascent ascent = TinCanBuilder(plan).OptimizerTimeout(Timeout.Infinite).Build();

            // the main thread keeps changing its plan (ReconcileStages) while the job runs, which mustn't reach the solve
            plan.Stages[2].Done = true;

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");
            FlightPlan snapshot = solution.FlightPlan ?? throw new Exception("null flight plan");

            Assert.NotSame(plan, snapshot);
            Assert.Same(psg.FlightPlan, snapshot);
            snapshot.Stages.Count.ShouldEqual(plan.Stages.Count);
            snapshot.Stages[2].Done.ShouldBeFalse();
            snapshot.Stages[3].Done.ShouldBeTrue();

            solution.Segments.ShouldEqual(index.Length);
            for (int p = 0; p < solution.Segments; p++)
            {
                solution.FlightPlanIndex(p).ShouldEqual(index[p]);
                FlightPlanStage stage = solution.FlightPlanStage(p) ?? throw new Exception("null flight plan stage");
                Assert.Same(snapshot.Stages[index[p]], stage);
                Assert.Equal(solution.CoastPhase(p), stage.StageType == StageType.COAST_DURING);
            }

            // a copy has its own deep copy of the plan, and its phases find their stages in it
            using Solution copy = solution.DeepCopy();
            FlightPlan copyPlan = copy.FlightPlan ?? throw new Exception("null flight plan");

            Assert.NotSame(snapshot, copyPlan);
            copyPlan.Stages.Count.ShouldEqual(snapshot.Stages.Count);
            for (int p = 0; p < copy.Segments; p++)
            {
                copy.FlightPlanIndex(p).ShouldEqual(index[p]);
                FlightPlanStage stage = copy.FlightPlanStage(p) ?? throw new Exception("null flight plan stage");
                Assert.Same(copyPlan.Stages[index[p]], stage);
                Assert.NotSame(snapshot.Stages[index[p]], stage);
                Assert.NotSame(snapshot.Stages[index[p]].FuelStats, stage.FuelStats);
                Assert.Equal(snapshot.Stages[index[p]].StageType, stage.StageType);
            }

            // a warm start is built with the current plan, and its solution has the snapshot of that plan rather than the old
            // solution's
            FlightPlan plan2 = TinCanFlightPlan();
            Ascent ascent2 = TinCanBuilder(plan2).OptimizerTimeout(Timeout.Infinite).OldSolution(solution).Build();
            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = psg2.Solution ?? throw new Exception("null solution");
            FlightPlan snapshot2 = solution2.FlightPlan ?? throw new Exception("null flight plan");

            Assert.NotSame(plan2, snapshot2);
            Assert.NotSame(snapshot, snapshot2);
            for (int p = 0; p < solution2.Segments; p++)
            {
                solution2.FlightPlanIndex(p).ShouldEqual(index[p]);
                Assert.Same(snapshot2.Stages[index[p]], solution2.FlightPlanStage(p));
            }
        }

        // A fixed booster can burn out a little before the frozen solution predicts, so the vessel is in the upper stage while
        // the clock is still in the booster phase.  Guidance must then see the whole upper stage burn ahead of it, and must not
        // compare against the end of the booster (hotstaging puts the vessel ahead of that state, which used to end the ascent
        // and kill the throttle at separation).
        [Fact]
        public void StagingAheadOfTheClock()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double rbody = 6.371e+6;
            double peR = rbody + 185e+3;
            double mu = 3.986004418e+14;

            // Titan2Tests.FlightPathAngle4Elliptical with a fixed booster and an optimized upper stage
            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddStage(31980, 6125.823788668185, 443700, 315, 2, 2)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .SetTarget(peR, peR, peR, Deg2Rad(28.608), Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            solution.OptimizeKSPStage().ShouldEqual(2);
            solution.TerminalKSPStage().ShouldEqual(2);

            double boosterEnd = solution.EndTime(0);
            double upperBurn  = solution.EndTime(1) - solution.StartTime(1);

            // the clock is still in the booster phase, but the upper stage burn is all ahead of us
            solution.TgoForKSPStage(boosterEnd - 0.1, 2).ShouldEqual(upperBurn, 1e-9);
            solution.TgoForKSPStage(boosterEnd + 0.1, 2).ShouldEqual(upperBurn - 0.1, 1e-9);
            solution.TgoForKSPStage(boosterEnd - 0.1, 4).ShouldEqual(0.1, 1e-9);
            // burned past the end of the booster
            solution.TgoForKSPStage(boosterEnd + 0.1, 4).ShouldBeZero();

            // ahead of the booster end state after hotstaging, but nowhere near the end of the upper stage
            double t = boosterEnd - 0.3;
            (V3 r, V3 v) = solution.StateVectors(t);
            solution.TerminalGuidanceSatisfied(r, v + 20 * v.normalized, t, 2).ShouldBeFalse();

            // a genuine cutoff at the end of the upper stage still fires
            double tf = solution.Tf;
            (V3 rf, V3 vf) = solution.StateVectors(tf);
            solution.TerminalGuidanceSatisfied(rf, vf + vf.normalized, tf - 0.1, 2).ShouldBeTrue();
            (V3 rEarly, V3 vEarly) = solution.StateVectors(tf - 5);
            solution.TerminalGuidanceSatisfied(rEarly, vEarly, tf - 5, 2).ShouldBeFalse();
        }

        /*
         * Synthetic solutions, built straight from hand-made segments instead of an optimizer run.  Every component of every
         * segment is linear in time, so each accessor has an exact expected value.  TimeScale is exactly 128 s and the phase
         * boundaries are dyadic, so a phase's StartTime and EndTime convert back to exactly its normalized bounds.
         */

        private const double LENGTH_SCALE   = 1 << 20;
        private const double VELOCITY_SCALE = 1 << 13;
        private const double MASS_SCALE     = 1 << 10;
        private const double TIME_SCALE     = LENGTH_SCALE / VELOCITY_SCALE;
        private const double T0             = 4096;
        private const double MU             = 4e14;
        private const double RBODY          = 6e6;

        private static readonly Scale _scale = new Scale(LENGTH_SCALE, VELOCITY_SCALE, MASS_SCALE);

        private static readonly V3 _xHat = new V3(1, 0, 0);
        private static readonly V3 _yHat = new V3(0, 1, 0);
        private static readonly V3 _zHat = new V3(0, 0, 1);

        private static double Tbar(double t) => (t - T0) / TIME_SCALE;

        private static double Vex(double isp) => isp * G0;

        // R and V are one linear function of normalized time across the whole trajectory, so they are continuous across the
        // phases and r × v = (0, 0, 1 + tbar / 4) grows strictly
        private static V3 RBarAt(double tbar) => new V3(1, tbar / 8, 0);
        private static V3 VBarAt(double tbar) => new V3(0, 1 + tbar / 4, 0);

        private static V3 ExpectedR(double t) => RBarAt(Tbar(t)) * LENGTH_SCALE;
        private static V3 ExpectedV(double t) => VBarAt(Tbar(t)) * VELOCITY_SCALE;

        // a normalized burn phase for a stage that burns from m0 to mf (kg) in bt seconds at full throttle
        private static Phase NewBurn(int kspStage, int mjPhase, double m0, double mf, double isp, double bt, bool allowShutdown = true,
            bool unguided = false, double minThrottle = 1.0)
        {
            using var phase = Phase.NewStage(m0, mf, Astro.ThrustFromMassesIspBurntime(m0, mf, isp, bt), isp, kspStage, mjPhase,
                unguided, allowShutdown, minThrottle: minThrottle);
            return phase.Rescale(_scale);
        }

        private static Phase NewCoast(int kspStage, int mjPhase, double m)
        {
            using var phase = Phase.NewCoast(m, m, 0, 1000, kspStage, mjPhase);
            return phase.Rescale(_scale);
        }

        private readonly struct Seg
        {
            public readonly Phase             Phase;
            public readonly double            Tbar0;
            public readonly double            Tbar1;
            public readonly InterpolantLayout Y0;
            public readonly InterpolantLayout Y1;

            // a segment from the normalized state y0 at tbar0 to y1 at tbar1
            public Seg(Phase phase, double tbar0, double tbar1, InterpolantLayout y0, InterpolantLayout y1)
            {
                Phase = phase;
                Tbar0 = tbar0;
                Tbar1 = tbar1;
                Y0 = y0;
                Y1 = y1;
            }

            // a segment on the shared R and V, with its mass (kg), thrust direction and throttle going from the first value to
            // the second
            public Seg(Phase phase, double tbar0, double tbar1, double m0, double mf, V3 u0, V3 u1, double throttle0, double throttle1)
                : this(phase, tbar0, tbar1, State(tbar0, m0, u0, throttle0), State(tbar1, mf, u1, throttle1))
            {
            }

            private static InterpolantLayout State(double tbar, double m, V3 u, double throttle) =>
                new InterpolantLayout { R = RBarAt(tbar), V = VBarAt(tbar), M = m / MASS_SCALE, U = u, T = throttle };
        }

        // each segment is a single cubic Hermite node whose end slopes are both the chord's, so it interpolates linearly
        private static Solution Build(params Seg[] segs)
        {
            InterpolantLayout x0 = segs[0].Y0;
            var problem = new Problem(_scale, x0.R, x0.V, x0.U, x0.M, T0, MU, RBODY / LENGTH_SCALE, 0, 0, 0, 0, V3.zero,
                new Kepler3(1, 0, 0));
            var solution = new Solution(problem, null);

            using var y    = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var ynew = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var dy   = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

            foreach (Seg seg in segs)
            {
                double h = seg.Tbar1 - seg.Tbar0;
                // a zero-length segment (a pruned stage) has no slope, and CubicHermiteVecNode makes it a constant
                double invH = h > 0 ? 1 / h : 0;
                var slope = new InterpolantLayout
                {
                    R = (seg.Y1.R - seg.Y0.R) * invH,
                    V = (seg.Y1.V - seg.Y0.V) * invH,
                    M = (seg.Y1.M - seg.Y0.M) * invH,
                    U = (seg.Y1.U - seg.Y0.U) * invH,
                    T = (seg.Y1.T - seg.Y0.T) * invH
                };

                seg.Y0.CopyTo(y);
                seg.Y1.CopyTo(ynew);
                slope.CopyTo(dy);

                var interpolant = VecInterpolant.Rent();
                interpolant.Append(CubicHermiteVecNode.Rent(seg.Tbar0, h, y, dy, ynew, dy), seg.Tbar0, seg.Tbar1);
                solution.AddSegment(interpolant, seg.Phase);
            }

            return solution;
        }

        /*
         * Shaped like TheStandard (burn, precise shutdown burn, coast, fixed upper stage) behind a fixed booster, with a fairing
         * jettison in KSP stage 3, so no phase has that stage.  The MJ phases differ from the KSP stages so the two can't get
         * mixed up.
         *
         *   p  phase                            KSP  MJ   t (s)       mass (kg)       throttle
         *   0  unguided fixed booster            4    7   4096-4160   8192 -> 4096    1
         *   1  burn, 50% minimum throttle        2    5   4160-4192   2048 -> 1024    1 -> 0.25
         *   2  burn, precise shutdown            1    3   4192-4224    512 ->  384    1
         *   3  coast                             1    3   4224-4352    384            0
         *   4  fixed upper, terminal stage       0    1   4352-4384    256 ->  128    1
         *
         * The thrust direction is x in the booster, turns from x to y in p1, and turns from y to z in the coast.
         */
        private static Solution TheStandardLike()
        {
            Phase booster = NewBurn(4, 7, 8192, 4096, 256, 64, false, true);
            Phase burn1   = NewBurn(2, 5, 2048, 1024, 320, 32, minThrottle: 0.5);
            Phase burn2   = NewBurn(1, 3, 512, 384, 384, 32);
            burn2.PreciseShutdown = true;
            Phase coast = NewCoast(1, 3, 384);
            Phase upper = NewBurn(0, 1, 256, 128, 448, 32, false);
            upper.TerminalStage = true;

            return Build(
                new Seg(booster, 0, 0.5, 8192, 4096, _xHat, _xHat, 1, 1),
                new Seg(burn1, 0.5, 0.75, 2048, 1024, _xHat, _yHat, 1, 0.25),
                new Seg(burn2, 0.75, 1, 512, 384, _yHat, _yHat, 1, 1),
                new Seg(coast, 1, 2, 384, 384, _yHat, _zHat, 0, 0),
                new Seg(upper, 2, 2.25, 256, 128, _zHat, _zHat, 1, 1));
        }

        // a fixed booster (KSP stage 1, 4096-4160 s) and a fixed upper stage (KSP stage 0, 4160-4224 s) with no coast.  Without the
        // terminal flag it is like an AscentGuesser solution, which never went through SolutionBuilder.AnalyzeStages.
        private static Solution AllFixed(bool terminal = true)
        {
            Phase booster = NewBurn(1, 1, 4096, 2048, 300, 64, false);
            Phase upper   = NewBurn(0, 0, 1024, 512, 300, 64, false);
            upper.TerminalStage = terminal;

            return Build(
                new Seg(booster, 0, 0.5, 4096, 2048, _xHat, _xHat, 1, 1),
                new Seg(upper, 0.5, 1, 1024, 512, _xHat, _xHat, 1, 1));
        }

        // a solution computed during a coast-during coast: the rest of the coast (4096-4160 s), then the precise shutdown burn
        // (4160-4224 s), both in KSP stage 1
        private static Solution CoastFirst()
        {
            Phase coast = NewCoast(1, 1, 512);
            Phase burn  = NewBurn(1, 1, 512, 256, 300, 64);
            burn.PreciseShutdown = true;
            burn.TerminalStage = true;

            return Build(
                new Seg(coast, 0, 0.5, 512, 512, _yHat, _yHat, 0, 0),
                new Seg(burn, 0.5, 1, 512, 256, _yHat, _yHat, 1, 1));
        }

        // a single burn that ends in the state (rf, vf), in m and m/s
        private static Solution EndingAt(V3 rf, V3 vf)
        {
            Phase burn = NewBurn(0, 0, 1024, 512, 300, 64);
            burn.TerminalStage = true;

            var y1 = new InterpolantLayout { R = rf / LENGTH_SCALE, V = vf / VELOCITY_SCALE, M = 0.5, U = _xHat, T = 1 };
            InterpolantLayout y0 = y1;
            y0.M = 1;

            return Build(new Seg(burn, 0, 0.5, y0, y1));
        }

        [Fact]
        public void PhaseTimesAndMetadata()
        {
            using Solution solution = TheStandardLike();

            solution.Segments.ShouldEqual(5);
            solution.T0.ShouldEqual(T0, 0);
            solution.Problem.T0.ShouldEqual(T0, 0);
            solution.Tf.ShouldEqual(4384, 0);

            double[] start    = { 4096, 4160, 4192, 4224, 4352 };
            double[] end      = { 4160, 4192, 4224, 4352, 4384 };
            int[]    kspStage = { 4, 2, 1, 1, 0 };
            int[]    mjPhase  = { 7, 5, 3, 3, 1 };

            for (int p = 0; p < solution.Segments; p++)
            {
                solution.StartTime(p).ShouldEqual(start[p], 0);
                solution.EndTime(p).ShouldEqual(end[p], 0);
                solution.KSPStage(p).ShouldEqual(kspStage[p]);
                solution.MJPhase(p).ShouldEqual(mjPhase[p]);
                Assert.Equal(p == 3, solution.CoastPhase(p));
            }

            // T0 slides forward while the vessel waits on the pad, and the whole trajectory slides with it
            solution.T0 += 10;
            solution.Tf.ShouldEqual(4394, 0);
            solution.StartTime(3).ShouldEqual(4234, 0);
            solution.EndTime(3).ShouldEqual(4362, 0);
            solution.R(4138).ShouldEqual(ExpectedR(4128), 1e-12);
            solution.Coast(4234).ShouldBeTrue();
            solution.Tgo(T0 + 10).ShouldEqual(288, 0);
        }

        // AddSegment keeps its own copy of the phase, so the caller can reuse or dispose of the one it passed
        [Fact]
        public void AddSegmentCopiesThePhase()
        {
            Phase burn = NewBurn(1, 1, 2048, 1024, 320, 64);
            using Solution solution = Build(new Seg(burn, 0, 0.5, 2048, 1024, _xHat, _xHat, 1, 1));

            burn.KSPStage = 9;
            burn.Mdot = 0;

            solution.KSPStage(0).ShouldEqual(1);
            solution.CoastPhase(0).ShouldBeFalse();
        }

        [Fact]
        public void StateAccessors()
        {
            using Solution solution = TheStandardLike();

            // R and V follow the shared line through every phase, including the ends of the trajectory
            foreach (double t in new double[] { 4096, 4128, 4176, 4208, 4288, 4368, 4384 })
            {
                solution.R(t).ShouldEqual(ExpectedR(t), 1e-12);
                solution.V(t).ShouldEqual(ExpectedV(t), 1e-12);
                (V3 r, V3 v) = solution.StateVectors(t);
                r.ShouldEqual(ExpectedR(t), 1e-12);
                v.ShouldEqual(ExpectedV(t), 1e-12);
            }

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            rf.ShouldEqual(ExpectedR(4384), 1e-12);
            vf.ShouldEqual(ExpectedV(4384), 1e-12);

            // the mass follows each phase's own line, and holds through the coast
            solution.M(T0).ShouldEqual(8192, 1e-12);
            solution.M(4128).ShouldEqual(6144, 1e-12);
            solution.M(4208).ShouldEqual(448, 1e-12);
            solution.M(4288).ShouldEqual(384, 1e-12);
            solution.M(4384).ShouldEqual(128, 1e-12);

            // a phase boundary belongs to the later phase, so the staging drop in mass happens right at the end time
            solution.M(4159).ShouldEqual(4160, 1e-12);
            solution.M(4160).ShouldEqual(2048, 1e-12);
            solution.M(4351).ShouldEqual(384, 1e-12);
            solution.M(4352).ShouldEqual(256, 1e-12);

            // the thrust direction is interpolated and not renormalized
            solution.U(4128).ShouldEqual(_xHat, 1e-12);
            solution.U(4176).ShouldEqual(new V3(0.5, 0.5, 0), 1e-12);
            solution.U(4288).ShouldEqual(new V3(0, 0.5, 0.5), 1e-12);

            // the trajectory extrapolates past both ends: guidance keeps flying it when a burn runs long
            solution.R(4000).ShouldEqual(ExpectedR(4000), 1e-12);
            solution.V(4000).ShouldEqual(ExpectedV(4000), 1e-12);
            solution.R(4500).ShouldEqual(ExpectedR(4500), 1e-12);
            solution.V(4500).ShouldEqual(ExpectedV(4500), 1e-12);
            solution.M(4392).ShouldEqual(96, 1e-12);
        }

        [Fact]
        public void DeltaV()
        {
            using Solution solution = TheStandardLike();

            double dv0   = Vex(256) * Math.Log(2);
            double dv1   = Vex(320) * Math.Log(2);
            double dv2   = Vex(384) * Math.Log(512.0 / 384);
            double dv4   = Vex(448) * Math.Log(2);
            double total = dv0 + dv1 + dv2 + dv4;

            // the delta-v used since T0, which carries across the staging drops in mass and holds through the coast
            solution.DV(T0).ShouldBeZero();
            solution.DV(4128).ShouldEqual(Vex(256) * Math.Log(8192.0 / 6144), 1e-12);
            solution.DV(4160).ShouldEqual(dv0, 1e-12);
            solution.DV(4176).ShouldEqual(dv0 + Vex(320) * Math.Log(2048.0 / 1536), 1e-12);
            solution.DV(4208).ShouldEqual(dv0 + dv1 + Vex(384) * Math.Log(512.0 / 448), 1e-12);
            solution.DV(4224).ShouldEqual(dv0 + dv1 + dv2, 1e-12);
            solution.DV(4288).ShouldEqual(dv0 + dv1 + dv2, 1e-12);
            solution.DV(4384).ShouldEqual(total, 1e-12);

            solution.Vgo(T0).ShouldEqual(total, 1e-12);
            solution.Vgo(4208).ShouldEqual(Vex(384) * Math.Log(448.0 / 384) + dv4, 1e-12);
            solution.Vgo(4384).ShouldBeZero();

            // DV(t, n) is what is left of phase n: all of it before the phase starts, the rest of it during it, and none after it
            solution.DV(T0, 1).ShouldEqual(dv1, 1e-12);
            solution.DV(4176, 1).ShouldEqual(Vex(320) * Math.Log(1536.0 / 1024), 1e-12);
            solution.DV(4176, 0).ShouldBeZero();
            solution.DV(4000, 4).ShouldEqual(dv4, 1e-12);
            solution.DV(4500, 4).ShouldBeZero();
            // a coast has none
            solution.DV(T0, 3).ShouldBeZero();
            solution.DV(4288, 3).ShouldBeZero();

            double sum = 0;
            for (int p = 0; p < solution.Segments; p++)
                sum += solution.DV(T0, p);
            sum.ShouldEqual(solution.Vgo(T0), 1e-12);
        }

        [Fact]
        public void BurnTimes()
        {
            using Solution solution = TheStandardLike();

            double[] duration = { 64, 32, 32, 128, 32 };

            for (int p = 0; p < solution.Segments; p++)
            {
                // all of the phase is left before it starts, including before T0
                solution.Bt(p, 4000).ShouldEqual(duration[p], 0);
                solution.Tgo(4000, p).ShouldEqual(duration[p], 0);
                solution.Bt(p, T0).ShouldEqual(duration[p], 0);
                solution.Tgo(T0, p).ShouldEqual(duration[p], 0);
                // and none of it once it is over, including past Tf
                solution.Bt(p, 4384).ShouldBeZero();
                solution.Tgo(4500, p).ShouldBeZero();
            }

            // part way through p1
            solution.Bt(0, 4176).ShouldBeZero();
            solution.Bt(1, 4176).ShouldEqual(16, 0);
            solution.Tgo(4176, 1).ShouldEqual(16, 0);
            solution.Tgo(4176, 2).ShouldEqual(32, 0);

            // Tgo(t) runs negative past Tf, which HandleTerminal's Tgo <= 0 test relies on when a burn runs long
            solution.Tgo(T0).ShouldEqual(288, 0);
            solution.Tgo(4176).ShouldEqual(208, 0);
            solution.Tgo(4384).ShouldBeZero();
            solution.Tgo(4400).ShouldEqual(-16, 0);

            // the time left until the end of the phase the clock is in, which isn't clamped to the phase either
            solution.StageTimeLeft(4288).ShouldEqual(64, 0);
            solution.StageTimeLeft(4224).ShouldEqual(128, 0);
            solution.StageTimeLeft(4000).ShouldEqual(160, 0);
            solution.StageTimeLeft(4400).ShouldEqual(-16, 0);
        }

        [Fact]
        public void TgoForKSPStage()
        {
            using Solution solution = TheStandardLike();

            solution.TgoForKSPStage(T0, 4).ShouldEqual(64, 0);
            solution.TgoForKSPStage(T0, 2).ShouldEqual(32, 0);
            // stage 1's coast doesn't count, only its burn
            solution.TgoForKSPStage(T0, 1).ShouldEqual(32, 0);
            solution.TgoForKSPStage(T0, 0).ShouldEqual(32, 0);
            // the fairing jettison stage has no phase
            solution.TgoForKSPStage(T0, 3).ShouldBeZero();

            solution.TgoForKSPStage(4208, 1).ShouldEqual(16, 0);
            solution.TgoForKSPStage(4208, 0).ShouldEqual(32, 0);
            // burned out, and staying burned out when the stage runs long into the next phase
            solution.TgoForKSPStage(4208, 4).ShouldBeZero();
            solution.TgoForKSPStage(4288, 1).ShouldBeZero();
        }

        private static void ShouldSplit(Solution solution, double t, double burn1, double coast, double burn2)
        {
            (double actualBurn1, double actualCoast, double actualBurn2) = solution.TgoBarSplit(Tbar(t));
            actualBurn1.ShouldEqual(burn1, 0);
            actualCoast.ShouldEqual(coast, 0);
            actualBurn2.ShouldEqual(burn2, 0);
        }

        // the normalized burn time left before the coast, the coast time left, and the burn time after the coast
        [Fact]
        public void TgoBarSplitAroundTheCoast()
        {
            using Solution solution = TheStandardLike();

            ShouldSplit(solution, T0, 1.0, 1.0, 0.25);
            ShouldSplit(solution, 4176, 0.375, 1.0, 0.25);
            ShouldSplit(solution, 4224, 0, 1.0, 0.25);
            ShouldSplit(solution, 4288, 0, 0.5, 0.25);
            // past the coast everything left is the burn after it
            ShouldSplit(solution, 4368, 0, 0, 0.125);
            ShouldSplit(solution, 4384, 0, 0, 0);

            // with no coast, every burn counts as the burn after it
            using Solution allFixed = AllFixed();
            ShouldSplit(allFixed, T0, 0, 0, 1.0);
            ShouldSplit(allFixed, 4192, 0, 0, 0.25);
        }

        [Fact]
        public void CoastAndUnguidedQueries()
        {
            using Solution solution = TheStandardLike();

            double[] mid          = { 4128, 4176, 4208, 4288, 4368 };
            bool[]   coast        = { false, false, false, true, false };
            bool[]   willCoast    = { true, true, true, true, false };
            bool[]   coastFollows = { false, false, true, true, false };
            bool[]   unguided     = { true, false, false, false, false };

            for (int p = 0; p < solution.Segments; p++)
            {
                Assert.Equal(coast[p], solution.Coast(mid[p]));
                Assert.Equal(willCoast[p], solution.WillCoast(mid[p]));
                Assert.Equal(coastFollows[p], solution.CoastFollows(mid[p]));
                Assert.Equal(unguided[p], solution.Unguided(mid[p]));
            }

            // the coast runs from its start time up to its end time
            solution.Coast(4224).ShouldBeTrue();
            solution.Coast(4352).ShouldBeFalse();
            solution.WillCoast(4352).ShouldBeFalse();
            solution.CoastFollows(4160).ShouldBeFalse();
            solution.CoastFollows(4192).ShouldBeTrue();

            // before T0 the clock is in the first phase, and past Tf in the last
            solution.Unguided(4000).ShouldBeTrue();
            solution.WillCoast(4000).ShouldBeTrue();
            solution.Coast(4500).ShouldBeFalse();
            solution.WillCoast(4500).ShouldBeFalse();
            solution.CoastFollows(4500).ShouldBeFalse();
            solution.Unguided(4500).ShouldBeFalse();

            using Solution allFixed = AllFixed();
            foreach (double t in new double[] { T0, 4128, 4192 })
            {
                allFixed.Coast(t).ShouldBeFalse();
                allFixed.WillCoast(t).ShouldBeFalse();
                allFixed.CoastFollows(t).ShouldBeFalse();
            }
        }

        [Fact]
        public void StageQueries()
        {
            using Solution solution = TheStandardLike();

            solution.CoastKSPStage().ShouldEqual(1);
            solution.OptimizeKSPStage().ShouldEqual(1);
            solution.TerminalKSPStage().ShouldEqual(0);
            solution.TerminalMJPhase().ShouldEqual(1);

            // fixed burn time stages only: nothing to shut down precisely, and no coast
            using Solution allFixed = AllFixed();
            allFixed.OptimizeKSPStage().ShouldEqual(-1);
            allFixed.CoastKSPStage().ShouldEqual(-1);
            allFixed.TerminalKSPStage().ShouldEqual(0);
            allFixed.TerminalMJPhase().ShouldEqual(0);

            // asking a solution with no terminal stage for it is a bug
            using Solution unflagged = AllFixed(false);
            Assert.Throws<ArgumentException>(() => unflagged.TerminalKSPStage());
            Assert.Throws<ArgumentException>(() => unflagged.TerminalMJPhase());
        }

        [Fact]
        public void IndexForKSPStage()
        {
            using Solution solution = TheStandardLike();

            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            // the fairing jettison stage has no phase of its own, so it gets the next burn
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(2, false).ShouldEqual(1);
            // stage 1 burns and then coasts, and the burn is the earlier of its phases
            solution.IndexForKSPStage(1, false).ShouldEqual(2);
            solution.IndexForKSPStage(0, false).ShouldEqual(4);
            // a stage above every phase is still on the first phase, and a stage below every phase has none
            solution.IndexForKSPStage(5, false).ShouldEqual(0);
            solution.IndexForKSPStage(-1, false).ShouldEqual(-1);
            // coasting is the coast, whatever the stage
            solution.IndexForKSPStage(1, true).ShouldEqual(3);
            solution.IndexForKSPStage(4, true).ShouldEqual(3);

            using Solution allFixed = AllFixed();
            allFixed.IndexForKSPStage(1, false).ShouldEqual(0);
            allFixed.IndexForKSPStage(0, false).ShouldEqual(1);
            allFixed.IndexForKSPStage(0, true).ShouldEqual(-1);
        }

        [Fact]
        public void InertialGuidance()
        {
            using Solution solution = TheStandardLike();

            // halfway through p1 the direction is between x and y, and comes back normalized
            (V3 u, double throttle) = solution.InertialGuidance(4176, 0);
            u.ShouldEqual(new V3(1, 1, 0).normalized, 1e-12);
            // the throttle there is 0.625, mapped onto the KSP throttle range above the engines' minimum
            throttle.ShouldEqual(0.625, 1e-12);
            solution.InertialGuidance(4176, 0.5).Item2.ShouldEqual(0.25, 1e-12);
            // engines that can't throttle down that far get the 1% floor rather than a shutdown
            solution.InertialGuidance(4176, 0.75).Item2.ShouldEqual(0.01, 0);
            // and engines that can't throttle at all run at full
            solution.InertialGuidance(4176, 1).Item2.ShouldEqual(1, 0);

            // late in p1 the interpolated throttle (0.34375) is below the phase's 50% minimum, and the minimum wins
            solution.InertialGuidance(4188, 0).Item2.ShouldEqual(0.5, 1e-12);
            solution.InertialGuidance(4188, 0.5).Item2.ShouldEqual(0.01, 0);

            // a full throttle phase runs at full whatever the engines' minimum
            solution.InertialGuidance(4128, 0).Item2.ShouldEqual(1, 0);
            solution.InertialGuidance(4128, 0.5).Item2.ShouldEqual(1, 0);

            // the coast commands no thrust, which comes out as the floor (HandleThrottle cuts the engines in a coast)
            (V3 uCoast, double coastThrottle) = solution.InertialGuidance(4288, 0);
            uCoast.ShouldEqual(new V3(0, 1, 1).normalized, 1e-12);
            coastThrottle.ShouldEqual(0.01, 0);
        }

        // TerminalGuidanceSatisfied fires once the vessel's angular momentum passes that of the state it targets.  r × v grows along
        // the synthetic trajectories, so this pins down the target time: the state at tTarget satisfies it if it is a hair
        // faster, and doesn't if it is a hair slower.
        private static void ShouldTargetStateAt(Solution solution, double tTarget, double t, int kspStage)
        {
            (V3 r, V3 v) = solution.StateVectors(tTarget);
            solution.TerminalGuidanceSatisfied(r, v * (1 + 1e-9), t, kspStage).ShouldBeTrue();
            solution.TerminalGuidanceSatisfied(r, v * (1 - 1e-9), t, kspStage).ShouldBeFalse();
        }

        [Fact]
        public void TerminalGuidanceTarget()
        {
            using Solution solution = TheStandardLike();

            // p1 and the precise shutdown p2 are one multi-stage burn, which ends with p2
            ShouldTargetStateAt(solution, 4224, 4176, 2);
            ShouldTargetStateAt(solution, 4224, 4208, 1);
            // staged ahead of the clock out of the booster, the burn ahead is the target rather than the end of the booster
            ShouldTargetStateAt(solution, 4224, 4128, 2);
            // a shutdown running a little late into the coast still targets the end of the burn before it
            ShouldTargetStateAt(solution, 4224, 4288, 1);
            // still in stage 1 with the clock in the fixed upper stage, it backs up past the coast to the burn
            ShouldTargetStateAt(solution, 4224, 4368, 1);
            // in the fixed upper stage itself the target is the end of the trajectory, and past Tf it always is
            ShouldTargetStateAt(solution, 4384, 4368, 0);
            ShouldTargetStateAt(solution, 4384, 4400, 1);

            // a solution computed during the coast starts with it, and the target is the burn after it, even for a stage below
            // every burn
            using Solution coastFirst = CoastFirst();
            ShouldTargetStateAt(coastFirst, 4224, 4128, 1);
            ShouldTargetStateAt(coastFirst, 4224, 4128, 0);
        }

        [Fact]
        [InvariantCulture]
        public void TerminalString()
        {
            const double PE_R = 6.2e6;
            const double AP_R = 7.0e6;
            double sma = (PE_R + AP_R) / 2;
            double ecc = (AP_R - PE_R) / (AP_R + PE_R);
            (V3 rf, V3 vf) = Astro.StateVectorsFromKeplerian(MU, sma * (1 - ecc * ecc), ecc, Deg2Rad(30), Deg2Rad(45), Deg2Rad(60), Deg2Rad(90));

            using Solution solution = EndingAt(rf, vf);

            // 90° past periapsis: r = 6575.8 km, v = 7813.6 m/s and the flight path angle is atan(ecc) = 3.47°
            solution.TerminalString().ShouldEqual(string.Join(Environment.NewLine,
                "Orbit: 200.0 km x 1.000 Mm",
                "rT: 575.8 km vT: 7.814 km/s FPA: 3.5°",
                "sma: 6.600 Mm ecc: 0.061 inc: 30.0°",
                "lan: 45.0° argp: 60.0° ta: 90.0°"));

            // at periapsis with 1.5 times the escape energy the orbit is hyperbolic: sma = -r, ecc = 2, and the apoapsis is negative,
            // so it is printed as a radius rather than as an altitude
            const double R = 7e6;
            using Solution hyperbolic = EndingAt(new V3(R, 0, 0), new V3(0, Math.Sqrt(3 * MU / R), 0));
            string terminal = hyperbolic.TerminalString();
            terminal.ShouldStartWith("Orbit: 1.000 Mm x -21.00 Mm");
            terminal.ShouldContain("sma: -7.000 Mm ecc: 2.000 inc: 0.0°");
        }

        // like BuggyTests.FourStagesNeedsTwo: the optimizer left the last stage unburned, so it is a zero-length phase and the
        // burn before it is the terminal stage
        [Fact]
        public void ZeroLengthTrailingPhase()
        {
            Phase burn = NewBurn(2, 2, 2048, 1024, 300, 64);
            burn.PreciseShutdown = true;
            burn.TerminalStage = true;
            Phase pruned = NewBurn(1, 1, 512, 256, 300, 64);

            using Solution solution = Build(
                new Seg(burn, 0, 0.5, 2048, 1024, _xHat, _xHat, 1, 1),
                new Seg(pruned, 0.5, 0.5, 512, 512, _xHat, _xHat, 1, 1));

            solution.Segments.ShouldEqual(2);
            solution.StartTime(1).ShouldEqual(4160, 0);
            solution.EndTime(1).ShouldEqual(4160, 0);
            solution.Tf.ShouldEqual(4160, 0);

            solution.Bt(1, T0).ShouldBeZero();
            solution.Tgo(T0, 1).ShouldBeZero();
            solution.DV(T0, 1).ShouldBeZero();
            solution.TgoForKSPStage(T0, 1).ShouldBeZero();
            solution.IndexForKSPStage(1, false).ShouldEqual(1);

            solution.TerminalKSPStage().ShouldEqual(2);
            solution.OptimizeKSPStage().ShouldEqual(2);

            // the burn is the whole trajectory
            solution.Tgo(T0).ShouldEqual(64, 0);
            solution.Vgo(T0).ShouldEqual(Vex(300) * Math.Log(2), 1e-12);
            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            rf.ShouldEqual(ExpectedR(4160), 1e-12);
            vf.ShouldEqual(ExpectedV(4160), 1e-12);

            // the end of the burn is still the target after walking forward onto the pruned stage
            ShouldTargetStateAt(solution, 4160, 4159, 2);
        }

        // a copy taken after T0 slid forward on the pad keeps the moved T0 and the phases' flags, and outlives the original
        [Fact]
        public void DeepCopyKeepsTheMovedT0()
        {
            Solution solution = TheStandardLike();
            solution.T0 += 10;

            using Solution copy = solution.DeepCopy();
            solution.Dispose();

            copy.T0.ShouldEqual(T0 + 10, 0);
            copy.Tf.ShouldEqual(4394, 0);
            copy.R(4138).ShouldEqual(ExpectedR(4128), 1e-12);
            copy.M(4138).ShouldEqual(6144, 1e-12);
            copy.DV(4170).ShouldEqual(Vex(256) * Math.Log(2), 1e-12);
            copy.Unguided(4138).ShouldBeTrue();
            copy.InertialGuidance(4198, 0).Item2.ShouldEqual(0.5, 1e-12);
            copy.TerminalKSPStage().ShouldEqual(0);
            copy.TerminalMJPhase().ShouldEqual(1);
            copy.OptimizeKSPStage().ShouldEqual(1);
            copy.CoastKSPStage().ShouldEqual(1);
        }

        // disposing clears the interpolants, so disposing twice doesn't hand them back to their pool twice
        [Fact]
        public void DisposeTwice()
        {
            Solution solution = TheStandardLike();
            solution.Dispose();
            solution.Dispose();
        }
    }
}
