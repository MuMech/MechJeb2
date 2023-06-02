/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using AssertExtensions;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PVG
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
                .AddStageUsingThrust(153180, 2194400, 296, 156, 4)
                .AddStageUsingThrust(31980, 443700, 315, 180, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, true, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                MechJebLib.Core.Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(153180);

            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.tmax).ShouldBeZero(1e-7);
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
            ;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                MechJebLib.Core.Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(157355.487476332);

            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.tmax).ShouldBeZero(1e-7);
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

        // target a 185x186 without getting apopasis attachment.  unfortuantely, i don't have a use case that
        // exercizes the logic to re-pin to the periapsis because it grabs the apopasis incorrectly.
        [Fact]
        public void Kepler3NoApoapsisAttachment()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 186e+3;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Ascent ascent = Ascent.Builder()
                .AddStageUsingThrust(157355.487476332, 2340000, 301.817977905273, 148.102380138703, 4)
                .AddStageUsingThrust(32758.6353093992, 456100.006103516, 315.000112652779, 178.63040653022, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), 0, false, false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                MechJebLib.Core.Maths.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0);
            solution.V(0).ShouldEqual(v0);
            solution.M(0).ShouldEqual(157355.487476332);

            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.tmax).ShouldBeZero(1e-7);
            solution.Vgo(0).ShouldEqual(8498.9352440057573, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.24009395607850137, 0.21526467329187604, 0.13094696426599889), 1e-7);

            double aprf = MechJebLib.Core.Maths.ApoapsisFromKeplerian(smaf, eccf);
            double perf = MechJebLib.Core.Maths.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PeR, 1e-9);
            aprf.ShouldEqual(ApR, 1e-9);

            pvg.Znorm.ShouldBeZero(1e-9);
            smaf.ShouldEqual(6556500.0000104867, 1e-7);
            eccf.ShouldEqual(7.6259809968559891E-05, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.6494150858682159, 1e-7);
            tanof.ShouldEqual(0.09094016908398217, 1e-7);
        }
    }
}
