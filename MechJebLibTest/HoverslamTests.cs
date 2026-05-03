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

            HoverslamSimulation hoverslam = HoverslamSimulation.Builder()
                                                               .AddStage(15095, 15095 - 8200, 45040, 311, 0, 0)
                                                               .Initial(r0, v0, t0, mu, w)
                                                               .TargetConditions(rbody, 0)
                                                               .Build();

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

            HoverslamSimulation hoverslam = HoverslamSimulation.Builder()
                                                               .Initial(r0, v0, t0, mu, w)
                                                               .TargetConditions(height, 0)
                                                               .AddStage(2223.31037385158, 870.000015944242, 78784.6518490292, 246.202054544791, 0, 0)
                                                               .Build();

            hoverslam.Run();

            hoverslam.Rf.magnitude.ShouldEqual(height, 1e-8);
            (hoverslam.IgnitionUT - t0).ShouldEqual(430.07250464949175, 1e-8);
            hoverslam.Vf.ShouldEqual(V3.Cross(w, hoverslam.Rf), 1e-8);
            hoverslam.Dv.ShouldEqual(487.37958512603763, 1e-8);
        }
    }
}
