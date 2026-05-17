using System;
using MechJebLib.HoverslamSimulation;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest
{
    public class HoverslamTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public HoverslamTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        private void VerticalDropApolloMoon15Km()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            double mu    = 4.9028e12;
            double rbody = 1737400;
            double t0    = 0;
            var    r0    = new V3(rbody + 15000, 0, 0);
            V3     w     = 2.6617e-6 * V3.northpole;
            var    v0    = V3.Cross(w, r0);

            var manager   = new HoverslamSimulation.HoverslamSimulationManager();
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
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var    r0     = new V3(277180.537135037, 68093.9480158941, -0.0292773955469599);
            var    v0     = new V3(-89.2646837517975, 208.578761949662, -0.000384582381211831);
            double t0     = 103226.904130211;
            double mu     = 65138397520.7807;
            var    w      = new V3(0, 0, 4.52078533000628E-05);
            double height = 203258.308103023;

            var manager   = new HoverslamSimulation.HoverslamSimulationManager(false);
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, 0)
               .AddStage(2223.31037385158, 870.000015944242, 78784.6518490292, 246.202054544791, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-8);
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

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-8);
            (hoverslam.IgnitionUT - t0).ShouldEqual(430.07250464949175, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-8);
            hoverslam.Dv.ShouldEqual(487.37958512603763, 1e-5);

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - start);
        }

        [Fact]
        private void MunLanderHyperbolic()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var    r0     = new V3(-2118481.777241, -1066356.47144701, -4.04795492273187E-07);
            var    v0     = new V3(255.065239081993, 74.1893623591615, 8.88384130349409E-11);
            double t0     = 103527.484130273;
            double mu     = 65138397520.7807;
            var    w      = new V3(0, 0, 4.52078533000628E-05);
            double height = 203258.308103023;

            var manager   = new HoverslamSimulation.HoverslamSimulationManager();
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
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var    r0           = new V3(276714.567061612, 53069.6146485938, 0.00770833757526462);
            var    v0           = new V3(-116.600502880196, 177.393895536045, 7.72615181983542E-05);
            double t0           = 104033.944130376;
            double mu           = 65138397520.7807;
            var    w            = new V3(0, 0, 4.52078533000628E-05);
            double height       = 203079.078627833;
            double descentSpeed = 111.496373594304;

            var manager   = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, descentSpeed)
               .AddStage(1631.71526258703, 1478.0574232488, 64000.011946753, 290.00005572948, 2, 2)
               .AddStage(1041.14351191796, 733.827833241511, 64000.0112782489, 290.000052700321, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(352.36724527248589, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf) - descentSpeed * hoverslam.Rf.normalized, 1e-6);
            hoverslam.Dv.ShouldEqual(384.35206383554907, 1e-4);
        }

        [Fact]
        private void MunLanderTwoStageWithCoast()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var    r0           = new V3(277354.901930395, 52073.8040485903, 0.00727448590091598);
            var    v0           = new V3(-112.093343687681, 178.249208847387, 7.73831475858213E-05);
            double t0           = 104028.344130375;
            double mu           = 65138397520.7807;
            var    w            = new V3(0, 0, 4.52078533000628E-05);
            double height       = 203079.452561372;
            double descentSpeed = 111.503169115681;

            var manager   = new HoverslamSimulation.HoverslamSimulationManager();
            var hoverslam = new HoverslamSimulation();

            manager.Initial(r0, v0, t0, mu, w)
               .TargetConditions(height, descentSpeed)
               .AddStage(1631.71526258703, 1478.0574232488, 64000.0164757482, 290.000076251489, 2, 2)
               .AddCoast(1041.14351191796, 0.5625, 0, 0)
               .AddStage(1041.14351191796, 733.827833241511, 64000.0092904123, 290.000043692936, 0, 0)
               .Reconfigure(hoverslam);

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-6);
            (hoverslam.IgnitionUT - t0).ShouldEqual(357.69288233217958, 1e-5);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf) - descentSpeed * hoverslam.Rf.normalized, 1e-6);
            hoverslam.Dv.ShouldEqual(384.68994637708414, 1e-4);
        }
    }
}
