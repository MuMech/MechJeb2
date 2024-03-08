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
    public class TheStandardTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TheStandardTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        // Carnasa's TheStandard rocket (ish).
        // This tests optimality of a burn-optimizedBurn-coast-burn with all stages guided with periapsis attachment
        [Fact]
        public void TheStandardGuidedUpperPeriapsis()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var v0 = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var u0 = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0 = 661803.431918959;
            double mu = 3.986004418e+14;
            double rbody = 6.371e+6;

            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddOptimizedCoast(678.290157913434, 0, 450, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            pvg.Znorm.ShouldBeZero(1e-5);
            solution.Vgo(t0).ShouldEqual(9672.0422999260772, 1e-7);

            // Test that by using a one second shorter fixed coast that the solution is less optimal

            double shorterCoast = solution.Tgo(solution.T0, 2) - 1;

            Ascent ascent2 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, shorterCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0)
                //.OldSolution(solution)
                .Build();

            ascent2.Run();
            Optimizer pvg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution2 = pvg2.GetSolution();

            pvg2.Znorm.ShouldBeZero(1e-9);
            solution2.Vgo(t0).ShouldEqual(9672.0452475169841, 1e-7);

            // Test that by using a one second longer fixed coast that the solution is less optimal

            double longerCoast = solution.Tgo(solution.T0, 2) + 1;

            Ascent ascent3 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, longerCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0)
                //.OldSolution(solution)
                .Build();

            ascent3.Run();
            Optimizer pvg3 = ascent3.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution3 = pvg3.GetSolution();

            pvg3.Znorm.ShouldBeZero(1e-9);
            solution3.Vgo(t0).ShouldEqual(9672.0452337699862, 1e-7);

            Assert.True(solution.Vgo(t0) < solution2.Vgo(t0));
            Assert.True(solution.Vgo(t0) < solution3.Vgo(t0));

            // assert stuff about the original solution that should be true

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(234.71127446073979, 5e-4);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(9672.0422999260772, 1e-7);
            solution.Pv(t0).normalized.ShouldEqual(new V3(0.77936242810570966, -0.56640344307712909, 0.26792040856856347), 1e-4);

            smaf.ShouldEqual(11463499.98898875, 1e-6);
            eccf.ShouldEqual(0.4280978753095433, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(3.0482327430579215, 1e-6);
            argpf.ShouldEqual(1.9882953487020583, 1e-4);
            tanof.ShouldBeZeroRadians(1e-6);
        }

        // Carnasa's TheStandard rocket (ish).
        // This tests optimality of a burn-optimizedBurn-coast-burn with all stages guided with free attachment
        [Fact]
        public void TheStandardGuidedUpperFree()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var v0 = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var u0 = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0 = 661803.431918959;
            double mu = 3.986004418e+14;
            double rbody = 6.371e+6;

            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            const double OPTIMUMVGO = 9670.9023790445626;
            const double SHORTERVGO = 9670.9046199679851;
            const double LONGERVGO = 9670.9046061314057;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, false, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddOptimizedCoast(678.290157913434, 0, 450, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution = pvg.GetSolution();

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(221.93374918746366, 5e-4);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            pvg.Znorm.ShouldBeZero(1e-6);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(OPTIMUMVGO, 1e-7);
            solution.Pv(t0).normalized.ShouldEqual(new V3(0.77953530499830737, -0.56621745928946121, 0.26781056188467101), 1e-4);

            smaf.ShouldEqual(11463499.98898875, 1e-6);
            eccf.ShouldEqual(0.4280978753095433, 1e-6);
            incf.ShouldEqual(incT, 1e-6);
            lanf.ShouldEqual(3.0482353737384318, 1e-6);
            argpf.ShouldEqual(1.9465073215385686, 1e-4);
            tanof.ShouldEqual(0.029535094479713919, 1e-3);

            // Test that by using a one second shorter fixed coast that the solution is less optimal

            double shorterCoast = solution.Tgo(solution.T0, 2) - 1;

            Ascent ascent2 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, false, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, shorterCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0)
                //.OldSolution(solution)
                .Build();

            ascent2.Run();
            Optimizer pvg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution2 = pvg2.GetSolution();

            solution2.Vgo(t0).ShouldEqual(SHORTERVGO, 1e-7);

            // Test that by using a one second longer fixed coast that the solution is less optimal

            double longerCoast = solution.Tgo(solution.T0, 2) + 1;

            Ascent ascent3 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, false, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, longerCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0)
                //.OldSolution(solution)
                .Build();

            ascent3.Run();
            Optimizer pvg3 = ascent3.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution3 = pvg3.GetSolution();

            solution3.Vgo(t0).ShouldEqual(LONGERVGO, 1e-7);

            Assert.True(solution.Vgo(t0) < solution2.Vgo(t0));
            Assert.True(solution.Vgo(t0) < solution3.Vgo(t0));
        }

        // Carnasa's TheStandard rocket.
        // This tests optimality of a burn-optimizedBurn-coast-burn with all stages guided with periapsis attachment
        // optimum:  ctmin: 236.563629014638 dvmin: 9673.89237593156
        [Fact]
        public void TheStandardPeriapsis()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var v0 = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var u0 = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0 = 661803.431918959;
            double mu = 3.986004418e+14;
            double rbody = 6.371e+6;

            double PeR = 6.371e+6 + 185e+3;
            double ApR = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            const double OPTIMUMVGO = 9673.8952123189683;
            //const double SHORTERVGO = 9673.9039952312651;
            //const double LONGERVGO = 9673.8923774194827;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddOptimizedCoast(678.290157913434, 0, 450, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, false, true)
                .Build();

            ascent.Run();

            Optimizer pvg = ascent.GetOptimizer() ?? throw new Exception("null optimzer");
            pvg.ZnormTerminationLevel = 1e-15;

            using Solution solution = pvg.GetSolution();

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(236.563629014638, 2e-3);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(OPTIMUMVGO, 1e-4);
            solution.Pv(t0).normalized.ShouldEqual(new V3(0.7682570918854319, -0.5781444551934527, 0.27482727247637684), 1e-2);

            smaf.ShouldEqual(11463500, 1e-7);
            eccf.ShouldEqual(0.42809787586994197, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(3.0755839625819483, 1e-2);
            argpf.ShouldEqual(1.9657740551735055, 2e-2);
            tanof.ShouldBeZeroRadians(1e-7);

            // Test that by using a one second shorter fixed coast that the solution is less optimal

            double shorterCoast = solution.Tgo(solution.T0, 2) - 1;

            Ascent ascent2 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, shorterCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, false, true)
                .Build();

            ascent2.Run();
            Optimizer pvg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution2 = pvg2.GetSolution();

            // Test that by using a one second longer fixed coast that the solution is less optimal

            double longerCoast = solution.Tgo(solution.T0, 2) + 1;

            Ascent ascent3 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, longerCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, false, true)
                .Build();

            ascent3.Run();
            Optimizer pvg3 = ascent3.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution3 = pvg3.GetSolution();

            Assert.True(solution.Vgo(t0) < solution2.Vgo(t0));
            Assert.True(solution.Vgo(t0) < solution3.Vgo(t0));
        }
    }
}
