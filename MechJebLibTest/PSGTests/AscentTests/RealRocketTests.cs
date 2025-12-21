using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.PSGTests.AscentTests
{
    public class RealRocketTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public RealRocketTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Delta3()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var    r0    = new V3(5605222.973039, 0.000000, 3043387.760956);
            var    v0    = new V3(0.000000, 408.739353, 0.000000);
            double t0    = 0;
            double rbody = 6378145;
            double smaT  = 24361140;
            double eccT  = 0.7308;
            double incT  = Deg2Rad(28.5);
            double lanT  = Deg2Rad(269.8);
            double argpT = Deg2Rad(130.5);
            double mu    = 3.986012e14;
            double aref  = 4 * PI;
            double cd    = 0.5;
            double rho0  = 1.225;
            double h0    = 7200;
            V3     w     = 7.29211585e-5 * V3.northpole;

            double PeR = Astro.PeriapsisFromKeplerian(smaT, eccT);
            double ApR = Astro.ApoapsisFromKeplerian(smaT, eccT);

            Ascent ascent = Ascent.Builder()
                .AerodynamicConstants(cd, aref, rho0, h0, w)
                .AddStageUsingFinalMassAndThrust(301454.000000, 171863.885057, 4854100.000000, 75.200000, 4, 4)
                .AddStageUsingFinalMassAndThrust(158183.885057, 79623.770115, 2968600.000000, 75.200000, 3, 3)
                .AddStageUsingFinalMassAndThrust(72783.770115, 32294.000000, 1083100.000000, 110.600000, 2, 2)
                .AddStageUsingFinalMassAndThrust(23464.000000, 6644.000000, 110094.000000, 700.000000, 1, 1)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, lanT, argpT, 0, false, true, true)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double nuf, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(301454.000000, 1e-9);

            solution.Tgo(solution.T0, 0).ShouldEqual(75.200000, 1e-3);
            solution.Tgo(solution.T0, 1).ShouldEqual(75.200000, 1e-3);
            solution.Tgo(solution.T0, 2).ShouldEqual(110.600000, 1e-3);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Tgo(0).ShouldEqual(924.139, 1e-3);
            //solution.Vgo(0).ShouldEqual(8518.1366714811684, 1e-3);

            //solution.U(0).normalized.ShouldEqual(new V3(0.69734975209707417, 0.6074944955387559, 0.38033374967291772), 1e-2);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            smaf.ShouldEqual(smaT, 1e-5);
            eccf.ShouldEqual(eccT, 1e-5);
            perf.ShouldEqual(PeR, 1e-5);
            aprf.ShouldEqual(ApR, 1e-5);
            incf.ShouldEqual(incT, 1e-5);
            lanf.ShouldEqual(lanT, 1e-2);
            argpf.ShouldEqual(argpT, 1e-2);
        }
    }
}
