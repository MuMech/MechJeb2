using System;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.PSGTests.AscentTests
{
    public class KerbinTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public KerbinTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        void MainSailTinCanVacuum()
        {
            double t0 = 82132.7494998545;

            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            Ascent ascent = Ascent.Builder()
                .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
                .AddCoast(79699.9986022711, 0, 450, 0, 0)
                .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity:true)
                .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), t0, 3531600000000, 600000)
                .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Tgo(t0).ShouldEqual(1050.4212938233472, 1e-3);
            solution.Vgo(t0).ShouldEqual(2429, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
        }

        [Fact]
        void MainSailTinCanAtmo()
        {
            double t0 = 82132.7494998545;

            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            Ascent ascent = Ascent.Builder()
                .AerodynamicConstants(0.5,  19.6, 1.22497705725583, 4000, 0, 6797.80562531277, new V3(0, 0, 0.000291570900559802))
                .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
                .AddCoast(79699.9986022711, 0, 450, 0, 0)
                .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity:true)
                .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), t0, 3531600000000, 600000)
                .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Tgo(t0).ShouldEqual(303.81036578362705, 1e-3);
            solution.Vgo(t0).ShouldEqual(3541.2477099301864, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
        }
    }
}
