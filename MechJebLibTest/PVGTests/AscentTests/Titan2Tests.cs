/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using AssertExtensions;
using MechJebLib.Core;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using Xunit;
using static MechJebLib.Statics;

namespace MechJebLibTest.PVGTests
{
    public class Titan2Tests
    {
        // this is a forced periapsis attachment problem, which chooses an ApR such that it winds up burning the whole rocket.
        [Fact]
        public void FlightPathAngle4AllRocket()
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
                .AddStageUsingThrust(153180, 2194400, 296, 156, 4, 4)
                .AddStageUsingThrust(31980, 443700, 315, 180, 3, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, true, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(153180);

            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.Tmax).ShouldBeZero(1e-7);
            solution.Vgo(0).ShouldEqual(9327.0948576056289, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.24913591286181805, 0.23909088173239779, 0.13587844086293846), 1e-7);

            pvg.Znorm.ShouldBeZero(1e-9);
            smaf.ShouldEqual(8616511.1913318466, 1e-7);
            eccf.ShouldEqual(0.23913520746130185, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.7637313812499942, 1e-7);
            ClampPi(tanof).ShouldBeZero(1e-7);
        }

        // this is FlightPathAngle4AllRocket but using free attachment.
        [Fact]
        public void Kepler3()
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
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(157355.487476332);

            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.Tmax).ShouldBeZero(1e-7);
            solution.Vgo(0).ShouldEqual(9291.2315891261242, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.25236763236272763, 0.24870860524790397, 0.13764101483910374), 1e-7);

            pvg.Znorm.ShouldBeZero(1e-9);
            smaf.ShouldEqual(8616511.1913318466, 1e-7);
            eccf.ShouldEqual(0.23913520746130185, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.6667949762852059, 1e-7);
            ClampPi(tanof).ShouldEqual(0.090826664012324976, 1e-7);
        }

        // extreme 1010 x 1012 and still get free attachment
        [Fact]
        public void Kepler3ExtremeElliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 1.010e+6;
            double ApR = 6.371e+6 + 1.012e+6;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(157355.487476332);

            // the lower epsilon values below here probably indicate that the free attachment solution is breaking down
            solution.Constant(0).ShouldBeZero(1e-3);
            solution.Constant(solution.Tmax).ShouldBeZero(1e-3);

            solution.Vgo(0).ShouldEqual(18968.729888080918, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.39420604314896607, 0.024122181832433174, 0.21558209101216347), 1e-7);

            double aprf = Maths.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Maths.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-9);
            aprf.ShouldEqual(ApR, 1e-9);

            pvg.Znorm.ShouldBeZero(1e-5);
            smaf.ShouldEqual(7381999.9997271635, 1e-5);
            eccf.ShouldEqual(0.00013546395462809413, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(Deg2Rad(270), 1e-2);
            argpf.ShouldEqual(1.5561731710314275, 1e-5);
            tanof.ShouldEqual(0.036325746839258599, 1e-5);
        }

        // extreme 1130 x 1132 which breaks free attachment and falls back to periapsis
        [Fact]
        public void Kepler3MoreExtremerElliptical()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 1.130e+6;
            double ApR = 6.371e+6 + 1.132e+6;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(157355.487476332);

            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.Tmax).ShouldBeZero(1e-7);

            // this is 0.12 seconds before tau with an 18 kg final mass
            solution.Vgo(0).ShouldEqual(27198.792190399912, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.42013374453123059, 0.013974700407340215, 0.22914045811295167), 1e-7);

            double aprf = Maths.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Maths.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-9);
            aprf.ShouldEqual(ApR, 1e-9);

            pvg.Znorm.ShouldBeZero(1e-7);
            smaf.ShouldEqual(7502000.0002534958, 1e-7);
            eccf.ShouldEqual(0.0001332978218926981, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.6027749993714844, 1e-7);
            tanof.ShouldEqual(0.00011182292330591537, 1e-7);
        }

        [Fact]
        public void CircularTest()
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
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double _, double _, _) =
                Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(157355.487476332);

            solution.Constant(0).ShouldBeZero(1e-4);
            solution.Constant(solution.Tmax).ShouldBeZero(1e-4);
            solution.Vgo(0).ShouldEqual(8498.6702629355023, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.24008912999871768, 0.21525232653820714, 0.1309443327389036), 1e-7);

            double aprf = Maths.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Maths.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-6);
            aprf.ShouldEqual(ApR, 1e-6);

            pvg.Znorm.ShouldBeZero(1e-5);
            smaf.ShouldEqual(6556000, 1e-6);
            eccf.ShouldEqual(0, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(Deg2Rad(270), 1e-4);
        }
    }
}
