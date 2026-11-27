/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PSGTests.AscentTests
{
    [ParallelTestMethods]
    public class Titan2Tests
    {
        // guidance's default pitch start height, which the PSG simulation rises to straight up from the pad before it solves
        private const double RISE = 100;

        // this is a forced periapsis attachment problem, which chooses an ApR such that it winds up burning the whole rocket.
        [Fact]
        public void FlightPathAngle4Elliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, true, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(153180, 1e-6);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66800916662161625, 0.64863008334323413, 0.36475028209947058), 1e-2);
            solution.Vgo(0).ShouldEqual(9404.3677823357357, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            smaf.ShouldEqual(8616511.1913318466, 1e-5);
            eccf.ShouldEqual(0.23913520746130185, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        // this is FlightPathAngle4Elliptical but with fixed burn time.  FlightPathAngle4Elliptical lets its last stage burn
        // slightly past depletion, so this reaches a slightly lower orbit.
        [Fact]
        public void FlightPathAngle3EnergyElliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, true, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(153180, 1e-6);
            solution.Tgo(solution.TStart, 0).ShouldEqual(156, 1e-6);
            solution.Tgo(solution.TStart, 1).ShouldEqual(180, 1e-6);

            // all fixed burn time stages with no coast, so no stage needs a precise shutdown
            solution.OptimizeKSPStage().ShouldEqual(-1);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66883450021136781, 0.64767206625159712, 0.36494014019342491), 1e-3);
            solution.Vgo(0).ShouldEqual(9369.7098971395462, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            smaf.ShouldEqual(8501798.7938651778, 1e-5);
            eccf.ShouldEqual(0.22886907123134159, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        // all fixed burn time stages with a coast between them: the coast is AllowShutdown but must not take the PreciseShutdown
        // flag, so there is still no precise shutdown stage and the guidance controller's fixed burntime termination applies.
        [Fact]
        public void FlightPathAngle3EnergyEllipticalCoast()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddCoast(31980, 31980, 0, 450, 3, 3)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, true, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(-1);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(3);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(2);
            solution.IndexForKSPStage(3, true).ShouldEqual(1);

            solution.Tgo(solution.TStart, 0).ShouldEqual(156, 1e-6);
            solution.Tgo(solution.TStart, 1).ShouldEqual(41.763222532138712, 1e-2);
            solution.Tgo(solution.TStart, 2).ShouldEqual(180, 1e-6);
            solution.Vgo(0).ShouldEqual(9369.7101019898018, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            smaf.ShouldEqual(8590793.288745543, 1e-5);
            eccf.ShouldEqual(0.23685743799833212, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7884827346201035, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        [Fact]
        public void FlightPathAngle5Elliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, true, true, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(153180, 1e-6);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66818571265471838, 0.64862916685663541, 0.36442839805231036), 1e-2);
            solution.Vgo(0).ShouldEqual(9404.3677303224067, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        [Fact]
        public void FlightPathAngle4EnergyElliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, true, true, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(153180, 1e-6);
            solution.Tgo(solution.TStart, 0).ShouldEqual(156, 1e-6);
            solution.Tgo(solution.TStart, 1).ShouldEqual(180, 1e-6);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66889606779756527, 0.6476787226136359, 0.3648154639794256), 1e-3);
            solution.Vgo(0).ShouldEqual(9369.7098971395462, 1e-3);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            // all fixed burn time stages with no coast, so no stage needs a precise shutdown
            solution.OptimizeKSPStage().ShouldEqual(-1);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            smaf.ShouldEqual(8501798.8036084659, 1e-6);
            eccf.ShouldEqual(0.22886907212890106, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.7643342507444935, 1e-2);
            tanof.ShouldBeZeroRadians(1e-5);
        }

        // attaching above periapsis gives a non-zero target flight path angle
        [Fact]
        public void FlightPathAngle4EllipticalAbovePeriapsis()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 1500e+3;
            double AttR = 6.371e+6 + 385e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, AttR, incT, Deg2Rad(270), 0, 0, true, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, _, _, _, _) = Astro.KeplerianFromStateVectors(mu, rf, vf);
            (double smaT, double eccT) = Astro.SmaEccFromApsides(PeR, ApR);
            (_, double gammaT) = Astro.FPATargetFromApsides(PeR, ApR, AttR, mu);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            gammaT.ShouldBePositive();
            rf.magnitude.ShouldEqual(AttR, 1e-6);
            Astro.FlightPathAngle(rf, vf).ShouldEqual(gammaT, 1e-6);
            smaf.ShouldEqual(smaT, 1e-6);
            eccf.ShouldEqual(eccT, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
        }

        [Fact]
        public void FlightPathAngle5EllipticalAbovePeriapsis()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 1500e+3;
            double AttR = 6.371e+6 + 385e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, AttR, incT, Deg2Rad(270), 0, 0, true, true, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, _, _, _) = Astro.KeplerianFromStateVectors(mu, rf, vf);
            (double smaT, double eccT) = Astro.SmaEccFromApsides(PeR, ApR);
            (_, double gammaT) = Astro.FPATargetFromApsides(PeR, ApR, AttR, mu);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            gammaT.ShouldBePositive();
            rf.magnitude.ShouldEqual(AttR, 1e-6);
            Astro.FlightPathAngle(rf, vf).ShouldEqual(gammaT, 1e-6);
            smaf.ShouldEqual(smaT, 1e-6);
            eccf.ShouldEqual(eccT, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-6);
        }

        // fixed burn time (no shutdown) with a non-zero desired flight path angle
        [Fact]
        public void FlightPathAngle3EnergyNonZeroFPA()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double AttR = 6.371e+6 + 300e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double fpaT = Deg2Rad(5);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, PeR, AttR, incT, Deg2Rad(270), 0, fpaT, true, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (_, _, double incf, _, _, _, _) = Astro.KeplerianFromStateVectors(mu, rf, vf);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            // all fixed burn time stages with no coast, so no stage needs a precise shutdown
            solution.OptimizeKSPStage().ShouldEqual(-1);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            rf.magnitude.ShouldEqual(AttR, 1e-6);
            Astro.FlightPathAngle(rf, vf).ShouldEqual(fpaT, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
        }

        [Fact]
        public void FlightPathAngle4EnergyNonZeroFPA()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double AttR = 6.371e+6 + 300e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double fpaT = Deg2Rad(5);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddStage(31980, 6125.823788668185, 443700, 315, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, PeR, AttR, incT, Deg2Rad(270), 0, fpaT, true, true, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (_, _, double incf, double lanf, _, _, _) = Astro.KeplerianFromStateVectors(mu, rf, vf);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            // all fixed burn time stages with no coast, so no stage needs a precise shutdown
            solution.OptimizeKSPStage().ShouldEqual(-1);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            rf.magnitude.ShouldEqual(AttR, 1e-6);
            Astro.FlightPathAngle(rf, vf).ShouldEqual(fpaT, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-6);
        }

        [Fact]
        public void Kepler3Elliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            solution.Vgo(0).ShouldEqual(9369.4641184722932, 1e-3);
            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66075285413606588, 0.65818147618986356, 0.36083626501721638), 1e-2);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);
            solution.CoastIndex().ShouldEqual(-1);

            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.6625138637074874, 1e-2);
            ClampPi(tanof).ShouldEqual(0.095809701921327317, 1e-2);
        }

        // launches from 0.5 degrees north, so the guess is at 0.5 degrees inclination and the rocket has to dogleg slightly
        [Fact]
        public void Kepler3EllipticalEquatorial()
        {
            var r0 = new V3(6370757.411841836, 0, 55596.757660140334);
            var v0 = new V3(0, 464.5630141793475, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = 0.0;
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            solution.Vgo(0).ShouldEqual(9332.93928775276, 1e-3);
            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.75071643895089812, 0.654551834663789, -0.089368473340157739), 1e-2);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            smaf.ShouldEqual(8616510.99994895, 1e-6);
            eccf.ShouldEqual(0.2391351905624024, 1e-6);
            incf.ShouldBeZero(1e-7);
            // the LAN is undefined for equatorial orbits, but the longitude of periapsis is not
            Clamp2Pi(lanf + argpf).ShouldEqual(0.09276699518963305, 1e-2);
            ClampPi(tanof).ShouldEqual(0.096644488139039988, 1e-2);
        }

        [Fact]
        public void Kepler4Elliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, true, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            solution.Vgo(0).ShouldEqual(9369.4640563002649, 1e-3);
            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66094928215047744, 0.65817849624817071, 0.360481779707748), 1e-2);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.6625138637074874, 1e-2);
            ClampPi(tanof).ShouldEqual(0.095809701921327317, 1e-2);
        }

        [Fact]
        public void Kepler5Elliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 1.6625138637074874, 0, false, true, true)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            solution.Vgo(0).ShouldEqual(9369.4665174003785, 1e-3);
            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66093259680604444, 0.658201314687611, 0.36047070868895725), 1e-2);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            smaf.ShouldEqual(8616511.1913318466, 1e-6);
            eccf.ShouldEqual(0.23913520746130185, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.6625138637074874, 1e-2);
            ClampPi(tanof).ShouldEqual(0.095809701921327317, 1e-2);
        }

        /*
                // extreme 1130 x 1132 with free attachment
                [Fact]
                public void Kepler3ExtremeElliptical()
                {
                    var    r0    = new V3(5593203.65707947, 0, 3050526.81522927);
                    var    v0    = new V3(0, 407.862893197274, 0);
                    double t0    = 0;
                    double PeR   = 6.371e+6 + 1.130e+6;
                    double ApR   = 6.371e+6 + 1.132e+6;
                    double rbody = 6.371e+6;
                    double incT  = Deg2Rad(28.608);
                    double mu    = 3.986004418e+14;

                    Ascent ascent = Ascent.Builder()
                .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
           .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
                        .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                        .VerticalRise(RISE)
                        .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                        .Build();

                    ascent.Run();

                    Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
                    using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

                    (V3 rf, V3 vf) = solution.TerminalStateVectors();

                    (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                        Astro.KeplerianFromStateVectors(mu, rf, vf);

                    solution.R(0).ShouldEqual(r0, 1e-6);
                    solution.V(0).ShouldEqual(v0, 1e-6);
                    solution.M(0).ShouldEqual(157355.487476332, 1e-6);

                    // this is 0.12 seconds before tau with an 18 kg final mass
                    solution.Vgo(0).ShouldEqual(27198.789343451925, 1e-3);

                    tanof.ShouldEqual(0.0063452857807080321, 0.1);

                    solution.U(solution.T0).normalized.ShouldEqual(new V3(0.87754205429590737, 0.029189246151212624, 0.47861041657202014), 1e-2);

                    double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
                    double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

                    perf.ShouldEqual(PeR, 1e-6);
                    aprf.ShouldEqual(ApR, 1e-6);

                    psg.PrimalFeasibility.ShouldBeZero(1e-5);
                    smaf.ShouldEqual(7502000.0002534958, 1e-6);
                    eccf.ShouldEqual(0.00013329824589634466, 1e-6);
                    incf.ShouldEqual(incT, 1e-6);
                    lanf.ShouldEqual(Deg2Rad(270), 1e-2);
                    argpf.ShouldEqual(1.5965429879704516, 1e-2);
                }
                */

        [Fact]
        public void Circular()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            psg.InitialPrimalFeasibility.ShouldBeLessThan(1.0);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            solution.Vgo(0).ShouldEqual(8549.452067127284, 1e-3);
            solution.DV(0, 0).ShouldEqual(4034.1207426276151, 1e-3);
            solution.DV(0, 1).ShouldEqual(4515.3313124354372, 1e-3);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.68564570530840407, 0.624540692757553, 0.37395038425178395), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);

            Ascent ascent2 = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .OldSolution(psg.Solution ?? throw new Exception("null solution"))
               .Build();

            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = (psg2.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            psg2.InitialPrimalFeasibility.ShouldBeZero(1e-3);
        }

        // launches from 0.5 degrees north, so the guess is at 0.5 degrees inclination and the rocket has to dogleg slightly
        [Fact]
        public void CircularEquatorial()
        {
            var r0 = new V3(6370757.411841836, 0, 55596.757660140334);
            var v0 = new V3(0, 464.5630141793475, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = 0.0;
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            // the LAN is undefined for equatorial orbits
            (double smaf, double eccf, double incf, double _, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            psg.InitialPrimalFeasibility.ShouldBeLessThan(1.0);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            solution.Vgo(0).ShouldEqual(8519.5422820333151, 1e-3);
            solution.DV(0, 0).ShouldEqual(4034.1207312126335, 1e-3);
            solution.DV(0, 1).ShouldEqual(4485.4215386894521, 1e-3);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.77857265138749432, 0.61953845104396443, -0.099983669614055012), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldBeZero(1e-7);

            Ascent ascent2 = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .OldSolution(psg.Solution ?? throw new Exception("null solution"))
               .Build();

            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = (psg2.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            psg2.InitialPrimalFeasibility.ShouldBeZero(1e-3);
        }

        // Circular, with the launch site turning with the Earth for a minute before the warm start, as it does between solves
        // on the pad.  The old solution is turned about the pole to the launch site, which keeps its inclination, so it warm
        // starts the solve there as well as it does on the pad it was solved at.
        [Fact]
        public void CircularTurnedLaunchSite()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            // v0 is the launch site's velocity, w × r0
            Q3 turn = Q3.AngleAxis(60 * v0.magnitude / r0.x, V3.northpole);

            Ascent.AscentBuilder Builder(V3 r, V3 v) =>
                Ascent.Builder()
                   .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
                   .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
                   .Initial(r, v, r.normalized, t0, mu, rbody)
                   .VerticalRise(RISE)
                   .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false);

            Ascent ascent = Builder(r0, v0).Build();
            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            Solution old = psg.Solution ?? throw new Exception("null solution");

            old.RotateToLaunchSite(turn * r0);

            // the warm start's guess starts on the turned launch site.  Without the turn the guess misses the box constraints
            // pinning the initial state, so InitialPrimalFeasibility is 3.8e-3 instead of 1.4e-6, and the solve has to move the
            // whole trajectory onto the launch site, which takes 15 iterations instead of 2.
            (old.R(0) * old.Scale.LengthScale).ShouldEqual(turn * r0, 1e-6);
            (old.V(0) * old.Scale.VelocityScale).ShouldEqual(turn * v0, 1e-6);

            Ascent ascent2 = Builder(turn * r0, turn * v0).OldSolution(old).Build();
            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = (psg2.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            psg2.InitialPrimalFeasibility.ShouldBeZero(1e-3);
            Assert.InRange(psg2.Iterations, 0, 5);

            solution2.R(0).ShouldEqual(turn * r0, 1e-6);
            solution2.V(0).ShouldEqual(turn * v0, 1e-6);

            (V3 rf, V3 vf) = solution2.TerminalStateVectors();

            (double smaf, double eccf, double incf, double _, double _, double _, _) = Astro.KeplerianFromStateVectors(mu, rf, vf);

            Astro.PeriapsisFromKeplerian(smaf, eccf).ShouldEqual(PeR, 1e-5);
            Astro.ApoapsisFromKeplerian(smaf, eccf).ShouldEqual(ApR, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
        }

        [Fact]
        public void CircularOptimizedBooster()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(4);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(-1);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(-1);

            // the optimizer shuts the booster down with propellant left, so guidance cuts it at its planned end and stages
            solution.Phases[0].PropellantLeft.ShouldBeTrue();
            solution.Phases[1].PropellantLeft.ShouldBeFalse();
            solution.TerminalIndex().ShouldEqual(1);
            solution.CoastIndex().ShouldEqual(-1);

            solution.Vgo(0).ShouldEqual(8592.1679195577981, 1e-3);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.69939288441266212, 0.60443937333132947, 0.38144807929751939), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
        }

        [Fact]
        public void CircularCoast()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddCoast(32758.6353093992, 32758.6353093992, 0, 450, 3, 3)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            psg.InitialPrimalFeasibility.ShouldBeLessThan(1.0);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(3);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(2);
            solution.IndexForKSPStage(3, true).ShouldEqual(1);

            solution.Vgo(0).ShouldEqual(8512.6306429145188, 1e-3);

            solution.Tgo(solution.TStart, 0).ShouldBePositive();
            solution.Tgo(solution.TStart, 1).ShouldEqual(43.697861068036985, 1e-2);
            solution.Tgo(solution.TStart, 2).ShouldBePositive();

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.66402706101968934, 0.6541159160878357, 0.362215999859373), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);

            Ascent ascent2 = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddCoast(32758.6353093992, 32758.6353093992, 0, 450, 3, 3)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .OldSolution(psg.Solution ?? throw new Exception("null solution"))
               .Build();

            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = (psg2.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            psg2.InitialPrimalFeasibility.ShouldBeZero(1e-3);
        }

        [Fact]
        public void CircularCoastOptimizedBooster()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddCoast(32758.6353093992, 32758.6353093992, 0, 450, 3, 3)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, allowShutdown: false)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            solution.Tgo(solution.TStart, 0).ShouldBePositive();
            solution.Tgo(solution.TStart, 1).ShouldEqual(15.113269375351896, 1e-2);
            solution.Tgo(solution.TStart, 2).ShouldBePositive();
            solution.Vgo(0).ShouldEqual(8588.02885538735, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            // the booster before the coast gets the precise shutdown, not the coast
            solution.OptimizeKSPStage().ShouldEqual(4);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(3);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(2);
            solution.IndexForKSPStage(3, true).ShouldEqual(1);

            // a coast is never a burn with propellant left, and the fixed upper stage burns out
            solution.Phases[1].PropellantLeft.ShouldBeFalse();
            solution.Phases[2].PropellantLeft.ShouldBeFalse();
            solution.TerminalIndex().ShouldEqual(2);
            solution.CoastIndex().ShouldEqual(1);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.69601469152854378, 0.6096732662115627, 0.37929152065838617), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
        }

        [Fact]
        public void CircularCoastWithinStage()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .AddCoast(32758.6353093992, 6384.144733613521, 0, 450, 3, 3, massContinuity: true)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, massContinuity: true)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            psg.InitialPrimalFeasibility.ShouldBeLessThan(1.0);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(3);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(2);

            solution.Tgo(solution.TStart, 0).ShouldEqual(148.10238013870301, 1e-2);
            solution.Tgo(solution.TStart, 1).ShouldEqual(153.53757758636579, 1e-2);
            solution.Tgo(solution.TStart, 2).ShouldEqual(449.99973855863101, 1e-2);
            solution.Tgo(solution.TStart, 3).ShouldEqual(8.7489488121921113, 1e-2);

            solution.Vgo(0).ShouldEqual(8158.509566601695, 1e-3);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.4506283008932096, 0.85807077092551975, 0.24626954037676546), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);

            Ascent ascent2 = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4, allowShutdown: false)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .AddCoast(32758.6353093992, 6384.144733613521, 0, 450, 3, 3, massContinuity: true)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, massContinuity: true)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .OldSolution(psg.Solution ?? throw new Exception("null solution"))
               .Build();

            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = (psg2.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            psg2.InitialPrimalFeasibility.ShouldBeZero(1e-3);

            solution2.Vgo(0).ShouldEqual(8158.5092907149847, 1e-3);
        }

        // Re-solves from mid-flight after staging, so the new Problem has a different length/time/mass scale than the old
        // solution (MassScale drops ~4.8x), which the warm start must convert between.
        [Fact]
        public void CircularCoastWithinStageResolveAfterStaging()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 185e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .AddCoast(32758.6353093992, 6384.144733613521, 0, 450, 3, 3, massContinuity: true)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, massContinuity: true)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(3);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(2);

            // just after the stage 4 burnout at ~148s
            double t1 = 160;
            double m1 = solution.M(t1);

            Ascent ascent2 = Ascent.Builder()
               .AddStage(m1, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .AddCoast(m1, 6384.144733613521, 0, 450, 3, 3, massContinuity: true)
               .AddStage(m1, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, massContinuity: true)
               .Initial(solution.R(t1), solution.V(t1), solution.U(t1).normalized, t1, mu, rbody)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .OldSolution(psg.Solution ?? throw new Exception("null solution"))
               .Build();

            ascent2.Run();

            Optimizer psg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = (psg2.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution2.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            // ~0.33 when the warm start doesn't convert between the old and new scales
            psg2.InitialPrimalFeasibility.ShouldBeZero(1e-3);

            solution2.Vgo(t1).ShouldEqual(solution.Vgo(t1), 1e-4);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(6556000, 1e-5);
            eccf.ShouldEqual(0, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
        }

        // This started off as a test of the Fischer-Burmeister constraint that collapses the first burn in
        // a burn-coast-burn with zero coast--but a better initial guess turned up a more optimal
        // solution, so it is now a test of the AscentGuesser.
        [Fact]
        public void HighlyEllipticalCoastWithinStage()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 140e+3;
            double ApR = 6.371e+6 + 18500e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
               .AddStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)
               .AddCoast(32758.6353093992, 32758.6353093992, 0, 450, 3, 3, massContinuity: true)
               .AddStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3, massContinuity: true)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .VerticalRise(RISE)
               .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = (psg.Solution ?? throw new Exception("null solution")).DeNormalized();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-6);
            solution.V(0).ShouldEqual(v0, 1e-6);
            solution.M(0).ShouldEqual(157355.487476332, 1e-6);

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            psg.InitialPrimalFeasibility.ShouldBeLessThan(1.0);

            solution.OptimizeKSPStage().ShouldEqual(3);
            solution.TerminalKSPStage().ShouldEqual(3);
            solution.TerminalMJPhase().ShouldEqual(3);
            solution.CoastKSPStage().ShouldEqual(3);
            solution.IndexForKSPStage(4, false).ShouldEqual(0);
            solution.IndexForKSPStage(3, false).ShouldEqual(1);
            solution.IndexForKSPStage(3, true).ShouldEqual(2);

            solution.Vgo(0).ShouldEqual(10013.332037161936, 1e-3);

            solution.Tgo(solution.TStart, 0).ShouldBePositive();
            solution.Tgo(solution.TStart, 1).ShouldEqual(176.13643532153222, 1e-2);
            solution.Tgo(solution.TStart, 2).ShouldEqual(449.99999999922886, 1e-2);
            solution.Tgo(solution.TStart, 3).ShouldEqual(12.597983444291344, 1e-2);

            solution.U(solution.T0).normalized.ShouldEqual(new V3(0.36938563329430579, 0.9072664476031892, 0.20105185144448243), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            smaf.ShouldEqual(15691000, 1e-5);
            eccf.ShouldEqual(0.58504875412073198, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
        }
    }
}
