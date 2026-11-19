/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.PSG.Terminal;
using MechJebLib.Utils;
using Xunit;
using static MechJebLib.Utils.Statics;
using Column = MechJebLib.PSG.SolutionTrace.Column;

namespace MechJebLibTest.PSGTests
{
    // The vertical rise presolved ahead of a solve on the ground (VerticalRise), and the Solution keeping it in front of its
    // phases
    public partial class SolutionTests
    {
        private const double RISE = 100;

        // TinCanBuilder's first stage
        private const double TIN_CAN_M0          = 79699.9986022711;
        private const double TIN_CAN_MF          = 15700.0014199451;
        private const double TIN_CAN_THRUST      = 1500000.09246676;
        private const double TIN_CAN_ISP         = 310.000018173591;
        private const double TIN_CAN_ISP_CURRENT = 285.387620615011;
        private const double KERBIN_MU           = 3531600000000;

        // the TinCan problem on the pad, with its first stage, normalized as AscentBuilder.Build makes them; with atmosphere
        // it has KerbinTests' atmosphere
        private static (Problem, PhaseCollection) TinCanPad(double thrust = TIN_CAN_THRUST, double mf = TIN_CAN_MF, bool atmosphere = false)
        {
            double h0 = atmosphere ? 6797.80562531277 : 0;
            double rho0CdAref = atmosphere ? 0.5 * 19.6 * 1.22497705725583 : 0;
            V3 w = atmosphere ? new V3(0, 0, 0.000291570900559802) : V3.zero;

            var problem = new Problem(_tinCanR0, _tinCanV0, _tinCanU0, TIN_CAN_M0, 0, KERBIN_MU, 600000, h0, rho0CdAref, 0, 0, w,
                new Kepler3(700000, 0, 0));

            using Phase stage = Phase.NewStage(TIN_CAN_M0, mf, thrust, TIN_CAN_ISP, 0, 0, ispCurrent: TIN_CAN_ISP_CURRENT, minThrottle: 0);
            return (problem, new PhaseCollection { stage.Rescale(problem.Scale) });
        }

        // The rise ends RISE up, where the problem and its first phase start, with the propellant it left, and the thrust of
        // every height stays what it was, though the back pressure is now relative to the top of the rise.
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PresolveStartsTheProblemAtTheTop(bool atmosphere)
        {
            (Problem pad, PhaseCollection phases) = TinCanPad(atmosphere: atmosphere);
            Phase before = phases[0].DeepCopy();
            double height = RISE / pad.Scale.LengthScale;

            Problem top = VerticalRise.Presolve(pad, phases, height);
            Phase phase = phases[0];

            Assert.NotNull(top.Rise);
            top.PadStart.ShouldBeTrue();
            top.Rise!.MinT.ShouldEqual(pad.T0, 0);
            top.Rise.MaxT.ShouldEqual(top.T0, 0);
            Assert.InRange(top.T0 * pad.Scale.TimeScale, 4.5, atmosphere ? 5.5 : 4.9);

            (top.R0.magnitude - pad.R0.magnitude).ShouldEqual(height, 1e-12);
            top.U0.ShouldEqual(top.R0.normalized, 1e-15);
            top.M0.ShouldEqual(pad.M0 - before.Mdot * top.T0, 1e-12);
            top.Scale.LengthScale.ShouldEqual(pad.Scale.LengthScale, 0);
            top.Scale.TimeScale.ShouldEqual(pad.Scale.TimeScale, 0);

            // straight up, which keeps the angular momentum (but for a whisper of drag across the radial)
            V3 rhat = top.R0.normalized;
            V3.Cross(top.R0, top.V0).ShouldEqual(V3.Cross(pad.R0, pad.V0), atmosphere ? 1e-5 : 1e-9);
            Assert.True(V3.Dot(top.V0, rhat) > 0);

            phase.M0.ShouldEqual(top.M0, 0);
            phase.Bt.ShouldEqual(before.Bt - top.T0, 1e-12);
            phase.MinT.ShouldEqual(0, 0);
            phase.MaxT.ShouldEqual(phase.Bt / phase.MinThrottle, 0);

            double invPad = AscentProblem.InvDensityR0(pad);
            double invTop = AscentProblem.InvDensityR0(top);

            foreach (double altitude in new double[] { 0, 50, RISE, 1000, 10000, 70000 })
            {
                V3 r = rhat * (pad.RBody + altitude / pad.Scale.LengthScale);
                V3 a0 = AscentProblem.FullThrottleAcceleration(pad, before, invPad, r, top.V0, top.M0, rhat);
                V3 a1 = AscentProblem.FullThrottleAcceleration(top, phase, invTop, r, top.V0, top.M0, rhat);
                a1.ShouldEqual(a0, 1e-12);
            }

            if (atmosphere)
                Assert.True(phase.VexCurrent > before.VexCurrent && phase.VexCurrent < phase.VexVacuum);
            else
                phase.VexCurrent.ShouldEqual(before.VexCurrent, 0);
        }

        // without a rise to fly the problem is the one on the pad, and the phases are left alone
        [Fact]
        public void NoRiseToFly()
        {
            void NoRise(Problem pad, PhaseCollection phases, double height)
            {
                double m0 = phases[0].M0, bt = phases[0].Bt, vex = phases[0].VexCurrent;

                Problem problem = VerticalRise.Presolve(pad, phases, height / pad.Scale.LengthScale);

                Assert.Null(problem.Rise);
                problem.T0.ShouldEqual(pad.T0, 0);
                problem.R0.ShouldEqual(pad.R0, 0);
                problem.M0.ShouldEqual(pad.M0, 0);
                phases[0].M0.ShouldEqual(m0, 0);
                phases[0].Bt.ShouldEqual(bt, 0);
                phases[0].VexCurrent.ShouldEqual(vex, 0);
            }

            // no height
            (Problem pad, PhaseCollection phases) = TinCanPad(atmosphere: true);
            NoRise(pad, phases, 0);

            // can't lift off, at a thrust-to-weight of 0.64
            (pad, phases) = TinCanPad(500000, atmosphere: true);
            NoRise(pad, phases, RISE);

            // burns out 0.02 s off the pad
            (pad, phases) = TinCanPad(mf: TIN_CAN_M0 - 10);
            NoRise(pad, phases, RISE);

            // the first phase coasts
            (pad, phases) = TinCanPad();
            using (Phase coast = Phase.NewCoast(TIN_CAN_M0, TIN_CAN_M0, 0, 450, 0, 0))
                phases.Insert(0, coast.Rescale(pad.Scale));
            NoRise(pad, phases, RISE);
        }

        // On the pad the solve starts at the top of a rise straight up at full throttle from the pad, which the solution keeps
        // in front of its phases, and the rise is the first phase's: it counts in that phase's time and delta-v left.
        [Fact]
        public void TinCanRisesStraightUpFromThePad()
        {
            Ascent ascent = TinCanBuilder().VerticalRise(RISE).Build();
            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            psg.Success().ShouldBeTrue();
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            double t0 = solution.T0;
            Logger.Print($"rise {t0} s, Vgo {solution.Vgo(0)} m/s, Tgo {solution.Tgo(0)} s");

            solution.HasRise.ShouldBeTrue();
            solution.FromPad.ShouldBeTrue();
            solution.TStart.ShouldEqual(0, 0);
            // 0.92 g up for 100 m
            Assert.InRange(t0, 4.5, 4.9);

            solution.R(0).ShouldEqual(_tinCanR0, 1e-12);
            solution.V(0).ShouldEqual(_tinCanV0, 1e-12);
            solution.M(0).ShouldEqual(TIN_CAN_M0, 1e-12);
            (solution.R(t0).magnitude - _tinCanR0.magnitude).ShouldEqual(RISE, 1e-7);

            // the first phase starts where the rise ends
            InterpolantLayout top = solution.RiseState(t0);
            InterpolantLayout first = solution.State(0, t0);
            first.R.ShouldEqual(top.R, 1e-12);
            first.V.ShouldEqual(top.V, 1e-12);
            first.M.ShouldEqual(top.M, 1e-12);

            // straight up at full throttle (at T0 U is the first phase's steering)
            double mdot = TIN_CAN_THRUST / (TIN_CAN_ISP * G0);
            top.M.ShouldEqual(TIN_CAN_M0 - mdot * t0, 1e-10);
            top.U.ShouldEqual(top.R.normalized, 1e-9);
            for (int i = 0; i < SAMPLES; i++)
            {
                double t = t0 * i / SAMPLES;
                solution.U(t).ShouldEqual(solution.R(t).normalized, 1e-9);
                solution.RiseState(t).T.ShouldEqual(1, 1e-14);
                Assert.True(solution.R(t).magnitude > _tinCanR0.magnitude - 1e-6);
            }

            double vex = TIN_CAN_ISP * G0;
            solution.DV(0).ShouldEqual(0, 0);
            solution.DV(t0).ShouldEqual(vex * Math.Log(TIN_CAN_M0 / top.M), 1e-10);
            solution.Vgo(0).ShouldEqual(solution.DV(solution.Tf), 0);
            solution.Tgo(0).ShouldEqual(solution.Tf, 0);
            solution.Tgo(0, 0).ShouldEqual(solution.EndTime(0), 0);
            solution.Tgo(t0 / 2, 0).ShouldEqual(solution.EndTime(0) - t0 / 2, 1e-12);
            solution.DV(0, 0).ShouldEqual(solution.DV(t0) + solution.DV(t0, 0), 1e-10);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            (double smaf, double eccf, double incf, _, _, _, _) = Astro.KeplerianFromStateVectors(KERBIN_MU, rf, vf);
            smaf.ShouldEqual(700000, 1e-7);
            eccf.ShouldBeZero(1e-6);
            incf.ShouldBeZero(1e-7);
        }

        // The rise goes with every copy of the solution and with the turn to the launch site, and a re-solve on the pad warm
        // started from the solution rises the same way, which puts the warm start right on the pinned initial state at the
        // top.  As in WarmStartFromATurnedLaunchSite the solution warm started is itself a warm start, since a warm start
        // from the cold start changes the objective (Tf goes from 609 s to 544 s here, and from 610 s to 543 s without the
        // rise) and takes many iterations wherever it is.
        [Fact]
        public void TinCanRiseRoundTrips()
        {
            Ascent cold = TinCanBuilder().VerticalRise(RISE).Build();
            cold.Run();

            Ascent ascent = TinCanBuilder().VerticalRise(RISE).OldSolution(cold.GetOptimizer()?.Solution ?? throw new Exception("null solution"))
               .Build();
            ascent.Run();

            Solution normalized = ascent.GetOptimizer()?.Solution ?? throw new Exception("null solution");
            Scale scale = normalized.Scale;

            using Solution solution       = normalized.DeNormalized();
            using Solution copy           = solution.DeepCopy();
            using Solution normalizedCopy = normalized.DeepCopy();

            double t0 = solution.T0;
            normalizedCopy.HasRise.ShouldBeTrue();
            copy.HasRise.ShouldBeTrue();

            for (int i = 0; i <= SAMPLES; i++)
            {
                double t = t0 * i / SAMPLES;
                InterpolantLayout x = solution.RiseState(t);

                InterpolantLayout c = copy.RiseState(t);
                c.R.ShouldEqual(x.R, 0);
                c.V.ShouldEqual(x.V, 0);
                c.M.ShouldEqual(x.M, 0);

                InterpolantLayout xbar = normalizedCopy.RiseState(t / scale.TimeScale);
                (xbar.R * scale.LengthScale).ShouldEqual(x.R, 1e-14);
                (xbar.V * scale.VelocityScale).ShouldEqual(x.V, 1e-14);
                (xbar.M * scale.MassScale).ShouldEqual(x.M, 1e-14);
            }

            copy.DV(t0).ShouldEqual(solution.DV(t0), 0);
            (normalizedCopy.DV(t0 / scale.TimeScale) * scale.VelocityScale).ShouldEqual(solution.DV(t0), 1e-14);

            // turned to the launch site the planet has turned to, the rise and all
            Q3 turn = Q3.AngleAxis(TURN, V3.northpole);
            using Solution turned = normalized.DeNormalized();
            turned.RotateToLaunchSite(turn * _tinCanR0);

            turned.R(0).ShouldEqual(turn * _tinCanR0, 1e-12);
            turned.RiseState(t0 / 2).R.ShouldEqual(turn * solution.RiseState(t0 / 2).R, 1e-12);
            for (int i = 0; i <= SAMPLES; i++)
            {
                double t = solution.Tf * i / SAMPLES;
                turned.R(t).ShouldEqual(turn * solution.R(t), 1e-12);
                turned.U(t).ShouldEqual(turn * solution.U(t), 1e-12);
                turned.DV(t).ShouldEqual(solution.DV(t), 0);
            }

            // as the solver does between solves on the pad
            Ascent again = TinCanBuilder().VerticalRise(RISE).OldSolution(normalized).Build();
            again.Run();

            Optimizer psg = again.GetOptimizer() ?? throw new Exception("null optimizer");
            psg.Success().ShouldBeTrue();
            psg.InitialPrimalFeasibility.ShouldBeZero(1e-6);
            Assert.InRange(psg.Iterations, 0, 5);
            using Solution s1 = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            s1.HasRise.ShouldBeTrue();
            s1.T0.ShouldEqual(t0, 0);
            s1.Tf.ShouldEqual(solution.Tf, 1e-6);
            s1.Vgo(0).ShouldEqual(solution.Vgo(0), 1e-6);
        }

        // Off the surface of a moon the vessel is on the ground long after MET zero: the rise starts at the MET there, and as
        // the solution doesn't start at liftoff it isn't turned to follow the launch site.
        [Fact]
        public void RiseOffTheGroundLaterInTheMission()
        {
            const double MET0 = 100;

            Ascent ascent = TinCanBuilder(met0: MET0).VerticalRise(RISE).Build();
            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            psg.Success().ShouldBeTrue();
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            solution.HasRise.ShouldBeTrue();
            solution.FromPad.ShouldBeFalse();
            solution.TStart.ShouldEqual(MET0, 0);
            Assert.InRange(solution.T0 - MET0, 4.5, 4.9);
            solution.R(MET0).ShouldEqual(_tinCanR0, 1e-12);
            solution.DV(MET0).ShouldEqual(0, 0);
            solution.Tgo(MET0, 0).ShouldEqual(solution.EndTime(0) - MET0, 0);

            using Solution turned = solution.DeepCopy();
            turned.RotateToLaunchSite(Q3.AngleAxis(TURN, V3.northpole) * _tinCanR0);
            turned.R(MET0).ShouldEqual(solution.R(MET0), 0);
            turned.R(solution.Tf).ShouldEqual(solution.R(solution.Tf), 0);
        }

        // TheStandardPeriapsis with a sea level Isp for its first stage, rising 100 m through the atmosphere off the pad
        [Fact]
        public void TheStandardRisesThroughTheAtmosphere()
        {
            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var v0 = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var u0 = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            const double MU = 3.986004418e+14;
            const double RBODY = 6.371e+6;

            const double PE_R = 6.371e+6 + 185e+3;
            const double AP_R = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            double thrust1 = Astro.ThrustFromMassesIspBurntime(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726);
            double thrust2 = Astro.ThrustFromMassesIspBurntime(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409);
            double thrust3 = Astro.ThrustFromMassesIspBurntime(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005);

            Ascent ascent = Ascent.Builder()
               .Initial(r0, v0, u0, 0, MU, RBODY)
               .AerodynamicConstants(0.5, 10, 1.225, 2000, 35000, 7200, 7.29211585e-5 * V3.northpole)
               .SetTarget(PE_R, AP_R, PE_R, incT, 0, 0, 0, true, false, false)
               .AddStage(49119.7842689869, 7114.2513992454, thrust1, 288.000034332275, 3, 3, ispCurrent: 260)
               .AddStage(2848.62586760223, 1363.71123994759, thrust2, 270.15767003304, 1, 1)
               .AddCoast(678.290157913434, 678.290157913434, 0, 450, 1, 1)
               .AddStage(678.290157913434, 177.582604389742, thrust3, 230.039271734103, 0, 0, true, false)
               .VerticalRise(RISE)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            Logger.Print($"rise {solution.T0} s, Vgo {solution.Vgo(0)} m/s, Tgo {solution.Tgo(0)} s");

            solution.HasRise.ShouldBeTrue();
            solution.FromPad.ShouldBeTrue();
            // 0.30 g up for 100 m, rising as the propellant burns
            Assert.InRange(solution.T0, 7, 9);
            solution.R(0).ShouldEqual(r0, 1e-12);
            (solution.R(solution.T0).magnitude - r0.magnitude).ShouldEqual(RISE, 1e-7);
            solution.M(0).ShouldEqual(49119.7842689869, 1e-12);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            (double smaf, double eccf, double incf, _, _, _, _) = Astro.KeplerianFromStateVectors(MU, rf, vf);
            smaf.ShouldEqual((PE_R + AP_R) / 2, 1e-4);
            eccf.ShouldEqual((AP_R - PE_R) / (AP_R + PE_R), 1e-4);
            incf.ShouldEqual(incT, 1e-4);
            rf.magnitude.ShouldEqual(PE_R, 1e-6);
        }

        // the rise is traced first, as phase -1 in the first phase's stage, without node or collocation markers
        [Fact]
        public void TraceSamplesTheRiseFirst()
        {
            using Solution solution = TheStandardLike(rise: true);
            var trace = new SolutionTrace();
            trace.Sample(solution, 8);

            trace.Count.ShouldEqual(6 * 9);
            trace.PhaseCount.ShouldEqual(5);
            trace.T0.ShouldEqual(4064, 0);
            trace.Tf.ShouldEqual(4384, 0);
            Assert.Equal(new double[] { 4064, 4096, 4160, 4192, 4224, 4352, 4384 }, trace.PhaseBoundaries);
            Assert.Equal(new double[] { 4096, 4160, 4160, 4192, 4192, 4224, 4224, 4352, 4352, 4384 }, trace.NodeBoundaries);
            Assert.Equal(trace.NodeBoundaries, trace.Knots);

            double vex = 256 * G0;
            for (int row = 0; row < 9; row++)
            {
                trace[Column.PHASE, row].ShouldEqual(-1);
                trace[Column.MET, row].ShouldEqual(4064 + 32.0 * row / 8, 1e-12);
                trace[Column.KSP_STAGE, row].ShouldEqual(4);
                trace[Column.MJ_PHASE, row].ShouldEqual(7);
                trace[Column.COAST, row].ShouldEqual(0);
                trace[Column.UNGUIDED, row].ShouldEqual(0);
                trace[Column.THROTTLE, row].ShouldEqual(1, 1e-12);
                trace[Column.MASS, row].ShouldEqual(16384 - 8192.0 * row / 8, 1e-9);
                trace[Column.DELTA_V, row].ShouldEqual(vex * Math.Log(16384 / trace[Column.MASS, row]), 1e-9);
            }

            // straight up, to the precision of the arcsine there
            trace[Column.PITCH, 0].ShouldEqual(90, 1e-5);
            trace[Column.PITCH, 8].ShouldEqual(90, 1e-5);

            // then the phases, from the top of the rise, where the booster has burned its share
            for (int row = 9; row < trace.Count; row++)
                trace[Column.PHASE, row].ShouldEqual((row - 9) / 9);
            trace[Column.MET, 8].ShouldEqual(trace[Column.MET, 9], 0);
            trace[Column.MASS, 8].ShouldEqual(trace[Column.MASS, 9], 1e-9);
            trace[Column.DELTA_V, 9].ShouldEqual(vex * Math.Log(2), 1e-9);

            for (int row = 0; row < trace.Count; row++)
                (trace[Column.DELTA_V, row] + trace[Column.DELTA_V_LEFT, row]).ShouldEqual(solution.DV(solution.Tf), 1e-9);
        }

        // the queries by time see the rise in front of the phases, and the phases are the same as without it
        [Fact]
        public void TheRiseIsInFrontOfThePhases()
        {
            using Solution solution = TheStandardLike(rise: true);
            using Solution plain = TheStandardLike();

            solution.HasRise.ShouldBeTrue();
            plain.HasRise.ShouldBeFalse();
            // not at liftoff, so it is never turned
            solution.FromPad.ShouldBeFalse();
            solution.TStart.ShouldEqual(4064, 0);
            solution.T0.ShouldEqual(4096, 0);
            solution.Segments.ShouldEqual(plain.Segments);
            for (int p = 0; p < solution.Segments; p++)
            {
                solution.StartTime(p).ShouldEqual(plain.StartTime(p), 0);
                solution.EndTime(p).ShouldEqual(plain.EndTime(p), 0);
            }

            solution.R(4080).ShouldEqual(ExpectedR(4080), 1e-12);
            solution.V(4080).ShouldEqual(ExpectedV(4080), 1e-12);
            solution.M(4080).ShouldEqual(12288, 1e-12);
            solution.M(4096).ShouldEqual(8192, 1e-12);
            solution.State(0, 4080).M.ShouldEqual(plain.State(0, 4080).M, 0);

            // the booster burns the rise: 16384 kg down to 8192 kg before its phase, then down to 4096 kg
            double vex = 256 * G0;
            double riseDV = vex * Math.Log(2);
            solution.DV(4064).ShouldEqual(0, 0);
            solution.DV(4080).ShouldEqual(vex * Math.Log(16384.0 / 12288), 1e-12);
            solution.DV(4096).ShouldEqual(riseDV, 1e-12);
            for (double t = 4096; t <= 4384; t += 16)
            {
                solution.DV(t).ShouldEqual(plain.DV(t) + riseDV, 1e-12);
                solution.Vgo(t).ShouldEqual(plain.Vgo(t), 1e-12);
                solution.Tgo(t).ShouldEqual(plain.Tgo(t), 0);
            }

            solution.Vgo(4064).ShouldEqual(plain.Vgo(4096) + riseDV, 1e-12);
            solution.Tgo(4064).ShouldEqual(320, 0);

            // the time and delta-v left in the booster count the rest of the rise, and the other phases' are the same
            solution.Tgo(4000, 0).ShouldEqual(96, 0);
            solution.Tgo(4080, 0).ShouldEqual(80, 0);
            solution.Tgo(4128, 0).ShouldEqual(plain.Tgo(4128, 0), 0);
            solution.DV(4000, 0).ShouldEqual(vex * Math.Log(4), 1e-12);
            solution.DV(4080, 0).ShouldEqual(vex * Math.Log(12288.0 / 4096), 1e-12);
            solution.DV(4128, 0).ShouldEqual(plain.DV(4128, 0), 0);
            for (int p = 1; p < solution.Segments; p++)
            {
                solution.Tgo(4080, p).ShouldEqual(plain.Tgo(4080, p), 0);
                solution.DV(4080, p).ShouldEqual(plain.DV(4080, p), 0);
            }

            // RiseState is on the rise whatever the time, and the copies carry it
            solution.RiseState(4080).M.ShouldEqual(12288, 1e-12);
            using Solution copy = solution.DeepCopy();
            copy.M(4080).ShouldEqual(solution.M(4080), 0);
            copy.TStart.ShouldEqual(4064, 0);

            Assert.ThrowsAny<Exception>(() => plain.RiseState(4080));
        }
    }
}
