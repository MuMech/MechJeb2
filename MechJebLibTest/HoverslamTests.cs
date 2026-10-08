/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.HoverslamSimulation;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest
{
    public class HoverslamTests
    {
        [Fact]
        private void VerticalDropApolloMoon15Km()
        {
            double mu = 4.9028e12;
            double rbody = 1737400;
            double t0 = 0;
            var r0 = new V3(rbody + 15000, 0, 0);
            V3 w = 2.6617e-6 * V3.northpole;
            var v0 = V3.Cross(w, r0);

            var manager = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.AddStage(15095, 15095 - 8200, 45040, 311, 0, 0)
               .Initial(r0, v0, t0, mu, w)
               .TargetConditions(rbody, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(rbody, 1e-8);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-8);
            (hoverslam.IgnitionUT - t0).ShouldEqual(94.755558282881168, 1e-8);
            hoverslam.Dv.ShouldEqual(313.25594640963067, 1e-8);
        }

        [Fact]
        private void MunLander()
        {
            var r0 = new V3(277180.537135037, 68093.9480158941, -0.0292773955469599);
            var v0 = new V3(-89.2646837517975, 208.578761949662, -0.000384582381211831);
            double t0 = 103226.904130211;
            double mu = 65138397520.7807;
            var w = new V3(0, 0, 4.52078533000628E-05);
            double height = 203258.308103023;

            var manager = new HoverslamSimulation.HoverslamSimulationManager(false);
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, 0)
               .AddStage(2223.31037385158, 870.000015944242, 78784.6518490292, 246.202054544791, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(430.07250464949175, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-8);
            hoverslam.Dv.ShouldEqual(487.37958512603763, 1e-5);

            long start = GC.GetAllocatedBytesForCurrentThread();

            manager.Reset()
               .Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, 0)
               .AddStage(2223.31037385158, 870.000015944242, 78784.6518490292, 246.202054544791, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(430.07250464949175, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-8);
            hoverslam.Dv.ShouldEqual(487.37958512603763, 1e-5);

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - start);
        }

        [Fact]
        private void MunLanderHyperbolic()
        {
            var r0 = new V3(-2118481.777241, -1066356.47144701, -4.04795492273187E-07);
            var v0 = new V3(255.065239081993, 74.1893623591615, 8.88384130349409E-11);
            double t0 = 103527.484130273;
            double mu = 65138397520.7807;
            var w = new V3(0, 0, 4.52078533000628E-05);
            double height = 203258.308103023;

            var manager = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, 0)
               .AddStage(2207.15807097463, 870.000015944242, 11817.6925209644, 246.201945036659, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(6295.6858380440681, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-6);
            hoverslam.Dv.ShouldEqual(887.5077419925741, 1e-3);
        }

        [Fact]
        private void MunLanderTwoStage()
        {
            var r0 = new V3(276714.567061612, 53069.6146485938, 0.00770833757526462);
            var v0 = new V3(-116.600502880196, 177.393895536045, 7.72615181983542E-05);
            double t0 = 104033.944130376;
            double mu = 65138397520.7807;
            var w = new V3(0, 0, 4.52078533000628E-05);
            double height = 203079.078627833;
            double descentSpeed = 111.496373594304;

            var manager = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, descentSpeed)
               .AddStage(1631.71526258703, 1478.0574232488, 64000.011946753, 290.00005572948, 2, 2)
               .AddStage(1041.14351191796, 733.827833241511, 64000.0112782489, 290.000052700321, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(352.36724527248589, 1e-4);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf) - descentSpeed * hoverslam.Rf.normalized, 1e-6);
            hoverslam.Dv.ShouldEqual(384.35206383554907, 1e-4);
        }

        [Fact]
        private void MunLanderTwoStageWithCoast()
        {
            var r0 = new V3(277354.901930395, 52073.8040485903, 0.00727448590091598);
            var v0 = new V3(-112.093343687681, 178.249208847387, 7.73831475858213E-05);
            double t0 = 104028.344130375;
            double mu = 65138397520.7807;
            var w = new V3(0, 0, 4.52078533000628E-05);
            double height = 203079.452561372;
            double descentSpeed = 111.503169115681;

            var manager = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, descentSpeed)
               .AddStage(1631.71526258703, 1478.0574232488, 64000.0164757482, 290.000076251489, 2, 2)
               .AddCoast(1041.14351191796, 1041.14351191796, 0.5625, 0, 0)
               .AddStage(1041.14351191796, 733.827833241511, 64000.0092904123, 290.000043692936, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(357.69288233217958, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf) - descentSpeed * hoverslam.Rf.normalized, 1e-6);
            hoverslam.Dv.ShouldEqual(384.68994637708414, 1e-4);
        }

        // just after liftoff (RSS Earth, TWR 1.5 booster under a TWR 0.8 upper stage) rising at 2.4 m/s with the
        // apoapsis 17 m above the target.  the ignition-at-apoapsis end of the bracket starts the burn at the steering
        // singularity, which used to stall the integrator and silently stage into the upper stage.
        [Fact]
        private void EarthJustAfterLiftoff()
        {
            var r0 = new V3(3389860.0273185601, -4449010.3867283156, 3050620.4984540241);
            var v0 = new V3(325.72327517256559, 245.49004963244238, 1.1570437177537884);
            double t0 = 672013.328102888;
            double mu = 398600435436096;
            var w = new V3(0, 0, 7.2921151467069236E-05);
            double height = 6371100;

            var manager = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, 0)
               .AddStage(554824.110033946, 146637.331161474, 8227080.50761488, 311.002968391946, 3, 3)
               .AddCoast(120917.292904309, 120917.292904309, 0.5625, 1, 1)
               .AddStage(120917.292904309, 14247.8429972215, 934119.950817851, 348.000880643115, 1, 1)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(1.34234016085975, 1e-5);
            (hoverslam.LandingUT - t0).ShouldEqual(3.43880722904578, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-8);
            hoverslam.Dv.ShouldEqual(31.246491642967, 1e-5);
        }
    }
}
