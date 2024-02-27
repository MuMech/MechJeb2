using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PVG;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PVGTests.AscentTests
{
    // test from buggy rockets that people have given me
    public class BuggyTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public BuggyTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        // A suborbital Thor that had convergence issues on the pad
        [Fact]
        public void SubOrbitalThor()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var r0 = new V3(1470817.12150277, -5396417.72296515, 3050610.07863681);
            var v0 = new V3(393.513790634158, 107.250777770179, 0.00161788515083675);
            var u0 = new V3(0.23076841882078, -0.846631607518583, 0.47954249382019);
            double t0 = 666733.571923551;
            double mu = 398600435436096;
            double rbody = 6371000;

            //double PeR = 6.371e+6 + 185e+3;
            //double ApR = 6.371e+6 + 10e+6;
            double incT = 0.499303792410538;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(6556000, 6557000, 6556000, 0.499303792410538, 0, 0, true, false)
                .AddStageUsingFinalMass(52831.1138786043, 6373.37509359599, 287.450291861922, 187.198692023839, 3, 3)
                .AddStageUsingFinalMass(2517.01227921406, 738.314292468521, 270.157637827755, 139.419410735907, 1, 1)
                .AddStageUsingFinalMass(81.5125042572618, 74.8691843417473, 250.000044703484, 0.678632409425986, 0, 0, false, true)
                .FixedBurnTime(true)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            pvg.Znorm.ShouldBeZero(1e-9);
            Assert.Equal(8, pvg.LmStatus);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(52831.1138786043);
            solution.Vgo(t0).ShouldEqual(9419.6763414588168, 1e-7);
            solution.Pv(t0).ShouldEqual(new V3(1.3834666440741792, -0.95667858377414616, 0.70183279386418346), 1e-7);

            smaf.ShouldEqual(8849932.3534340952, 1e-7);
            eccf.ShouldEqual(0.2592033771405583, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(3.4079112881114502, 1e-7);
            argpf.ShouldEqual(1.7723635725563422, 1e-7);
            ClampPi(tanof).ShouldBeZero(1e-7);
        }


        [Fact]
        // this is a buggy rocket, not an actual delta4 heavy, but it is being used to test that
        // some of the worst possible targets still converge and to test the fallback from failure
        // with the analytic thrust integrals to doing numerical integration.
        private void BuggyDelta4ExtremelyHeavy()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(5591854.96465599, 126079.439022067, 3050616.55737457);
            var v0 = new V3(-9.1936944030452, 407.764494724287, 0.000353003400966649);
            var u0 = new V3(0.877712545724556, 0.0197197822130759, 0.478781640529633);
            double t0 = 81786.024077168;
            double mu = 398600435436096;
            double rbody = 6371000;

            double decmass = 2403; // can be decreased to add more payload

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(6571000, 6571000, 6571000, 0.558505360638185, 0, 0.349065850398866, true, false)
                .AddStageUsingFinalMass(731995.326793174 - decmass, 328918.196876464 - decmass, 408.091742854086, 159.080051443926, 4, 4)
                .AddStageUsingFinalMass(268744.821741949 - decmass, 168530.570801559 - decmass, 408.091702159863, 118.652885602609, 2, 2)
                .AddStageUsingFinalMass(40763.8839289468 - decmass, 13796.4420050909 - decmass, 465.500076480742, 1118.13132581707, 1, 1, true)
                .FixedBurnTime(false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            pvg.Znorm.ShouldBeZero(1e-9);
            Assert.Equal(8, pvg.LmStatus);
        }

        [Fact]
        private void BiggerEarlyRocketMaybe()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var r0 = new V3(-4230937.57027061, -3658393.88789034, 3050613.04457008);
            var v0 = new V3(266.772640873606, -308.526291373473, 0.00117499917444357);
            var u0 = new V3(-0.664193346276844, -0.574214958863673, 0.478669494390488);
            double t0 = 134391.70903729;
            double mu = 398600435436096;
            double rbody = 6371000;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(6621000, 6623000, 6621000, 0.558505360638185, 0, 0.349065850398866, false, false)
                .AddStageUsingFinalMass(51814.6957082437, 12381.5182945184, 277.252333393375, 139.843831752395, 2, 2)
                .AddStageUsingFinalMass(9232.92402212159, 887.216491554419, 252.019434034823, 529.378964006269, 1, 1, true)
                .FixedBurnTime(false)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            pvg.Znorm.ShouldBeZero(1e-9);
            Assert.Equal(8, pvg.LmStatus);
        }
    }
}
