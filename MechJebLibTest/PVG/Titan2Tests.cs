/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using AssertExtensions;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace PVG
{
    public class Titan2Tests
    {
        [Fact]
        public void FlightPathAngle4AllRocket()
        {
            // this should burn the entire rocket, based on the results of FlightPathAngleEnergy3()
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 4306022;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Optimizer pvg = Ascent.Builder()
                .AddStageUsingBurnTime(153180, 2194400, 296, 156, 4)
                .AddStageUsingBurnTime(31980, 443700, 315, 180, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), false, false)
                .Build()
                .Run();
            
            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof) =
                Functions.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, EPS);
            solution.V(0).ShouldEqual(v0, EPS);
            solution.M(0).ShouldEqual(153180, EPS);
            
            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.tmax).ShouldBeZero(1e-7);
            solution.Vgo(0).ShouldEqual(9332.8859331860731, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.25565104209238126,0.23733387212263146,0.13943178322206456), 1e-7);
            
            //Assert.True(pvg.status == Status.Success, "optimizer failed");
            pvg.Znorm.ShouldBeZero(1e-9);
            smaf.ShouldEqual(8616511.1913318466, 1e-7);
            eccf.ShouldEqual(0.23913520746130185, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.7643342516077158, 1e-7);
            ClampPi(tanof).ShouldBeZero(1e-7);
        }
        
           [Fact]
        public void FlightPathAngle4()
        {
            // this should burn the entire rocket, based on the results of FlightPathAngleEnergy3()
            //var r0 = new V3(-5847411.87414179, 1874758.01618158, -1698223.17057067);
            //var v0 = new V3(132.719068447083, 384.275111501156, -32.7772587640753);
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6;
            double rbody = 6.371e+6;
            double incT = Deg2Rad(28.608);
            double mu = 3.986004418e+14;

            Optimizer pvg = Ascent.Builder()
                .AddStageUsingBurnTime(157355.487476332, 2340000 ,301.817977905273, 148.102380138703, 4)
                .AddStageUsingBurnTime(32758.6353093992, 456100.006103516, 315.000112652779 ,178.63040653022, 3, true)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, Deg2Rad(270), false, false)
                .Build()
                .Run();
            
            Solution solution = pvg.GetSolution();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof) =
                Functions.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, EPS);
            solution.V(0).ShouldEqual(v0, EPS);
//            solution.M(0).ShouldEqual(153180, EPS);
            
            solution.Constant(0).ShouldBeZero(1e-7);
            solution.Constant(solution.tmax).ShouldBeZero(1e-7);
            solution.Vgo(0).ShouldEqual(9332.8859331860731, 1e-7);
            solution.Pv(0).ShouldEqual(new V3(0.25565104209238126,0.23733387212263146,0.13943178322206456), 1e-7);
            
            //Assert.True(pvg.status == Status.Success, "optimizer failed");
            pvg.Znorm.ShouldBeZero(1e-9);
            smaf.ShouldEqual(8616511.1913318466, 1e-7);
            eccf.ShouldEqual(0.23913520746130185, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(Deg2Rad(270), 1e-7);
            argpf.ShouldEqual(1.7643342516077158, 1e-7);
            ClampPi(tanof).ShouldBeZero(1e-7);
        }
    }
}
