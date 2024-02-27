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

            pvg.Znorm.ShouldBeZero(1e-9);
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
            solution.Tgo(solution.T0, 2).ShouldEqual(234.71127446073979, 1e-6);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(9672.0422999260772, 1e-7);
            solution.Pv(t0).normalized.ShouldEqual(new V3(0.77936242810570966, -0.56640344307712909, 0.26792040856856347), 1e-7);

            smaf.ShouldEqual(11463499.98898875, 1e-7);
            eccf.ShouldEqual(0.4280978753095433, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(3.0482327430579215, 1e-7);
            argpf.ShouldEqual(1.9882953487020583, 1e-7);
            tanof.ShouldBeZeroRadians(1e-7);
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
            solution.Tgo(solution.T0, 2).ShouldEqual(221.93374918746366, 1e-6);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            pvg.Znorm.ShouldBeZero(1e-9);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(OPTIMUMVGO, 1e-7);
            solution.Pv(t0).normalized.ShouldEqual(new V3(0.77953530499830737, -0.56621745928946121, 0.26781056188467101), 1e-7);

            smaf.ShouldEqual(11463499.98898875, 1e-7);
            eccf.ShouldEqual(0.4280978753095433, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(3.0482353737384318, 1e-7);
            argpf.ShouldEqual(1.9465073215385686, 1e-7);
            tanof.ShouldEqual(0.029535094479713919,1e-7);

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

        // these only produce one transversality condition:
        // start of coast:
        // H0f(1) = 0
        // H0i(2) = 0
        // end of coast:
        // H0f(2) = 0
        // H0i(3) = 0

        // this has to be the second transversality condition?
        // Hi(1) = 0

        // these are degenerate with H0=0 conditions
        // Hf(1) = 0
        // Hi(2) = 0
        // Hf(2) = 0
        // so Hi(3) != 0 becauseo of a jump in the hamiltonian due to the constraints on the control in the 4th phase
        // and Si(3) != 0 as well.

        // this is imposed to scale the mass costate of the second stage
        // Sf(1) = 0
        // that doesn't actually seem useful?
        // optimum:  ctmin: 236.563629014638 dvmin: 9673.89237593156
        [Fact]
        public void TheStandardFuckery()
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

            V3 pv0 = new V3(0.0131705073396151,-0.00992075635699253,0.00471657341333147);
            V3 pr0 = new V3(-0.00215175659811265,-0.0245451738885,0.0134364409252076);

                Ascent ascent2 = Ascent.Builder()
                    .Initial(r0, v0, u0, t0, mu, rbody)
                    .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                    .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                    .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                    //.AddFixedCoast(678.290157913434, 236.563629014638, 1, 1)
                    .AddOptimizedCoast(678.290157913434, 0, 500, 1, 1)
                    .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, false, true)
                    .Build();

                ascent2.Test(pv0, pr0);
                Optimizer pvg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimzer");
                Solution solution2 = pvg2.GetSolution();

                solution2.Tgo(solution2.T0, 2).ShouldEqual(236.563629014638, 1e-10);

                solution2.Vgo(t0);
        }

        // Carnasa's TheStandard rocket (ish).
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
            const double SHORTERVGO = 9673.9039952312651;
            const double LONGERVGO = 9673.8923774194827;

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
            solution.Tgo(solution.T0, 2).ShouldEqual(236.563629014638);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            pvg.Znorm.ShouldBeZero(1e-9);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(OPTIMUMVGO, 1e-15);
            //solution.Pv(t0).normalized.ShouldEqual(new V3(0.7679501685731982, -0.57846262060949616, 0.27501551800942209), 1e-7);

            //smaf.ShouldEqual(11463499.98898875, 1e-7);
            //eccf.ShouldEqual(0.4280978753095433, 1e-7);
            //incf.ShouldEqual(incT, 1e-7);
            //lanf.ShouldEqual(3.0481683077792203, 1e-7);
            //argpf.ShouldEqual(1.9887149870712548, 1e-7);
            //tanof.ShouldBeZeroRadians(1e-7);

            // Test that by using a one second shorter fixed coast that the solution is less optimal

            double shorterCoast = solution.Tgo(solution.T0, 2) - 1;

            Ascent ascent2 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, shorterCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, false, true)
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
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1, true)
                .AddFixedCoast(678.290157913434, longerCoast, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, false, true)
                //.OldSolution(solution)
                .Build();

            ascent3.Run();
            Optimizer pvg3 = ascent3.GetOptimizer() ?? throw new Exception("null optimzer");
            Solution solution3 = pvg3.GetSolution();

            solution3.Vgo(t0).ShouldEqual(LONGERVGO, 1e-7);

            Assert.True(solution.Vgo(t0) < solution2.Vgo(t0));
            Assert.True(solution.Vgo(t0) < solution3.Vgo(t0));
        }

        // A suborbital Thor that had convergence issues on the pad
        [Fact]
        public void SubOrbitalThor()
        {
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
            solution.Pv(t0).normalized.ShouldEqual(new V3(0.75907214457397088, -0.52490464222178934, 0.38507738950227655), 1e-7);

            smaf.ShouldEqual(8849932.3565993849, 1e-7);
            eccf.ShouldEqual(0.2592033774111514, 1e-7);
            incf.ShouldEqual(incT, 1e-7);
            lanf.ShouldEqual(3.4079112881246774, 1e-7);
            argpf.ShouldEqual(1.7723635725701117, 1e-7);
            ClampPi(tanof).ShouldBeZero(1e-7);
        }

        // this is a buggy rocket, not an actual delta4 heavy, but it is being used to test that
        // some of the worst possible targets still converge and to test the fallback from failure
        // with the analytic thrust integrals to doing numerical integration.
        [Fact]
        private void BuggyDelta4ExtremelyHeavy()
        {
            var r0 = new V3(5591854.96465599, 126079.439022067, 3050616.55737457);
            var v0 = new V3(-9.1936944030452, 407.764494724287, 0.000353003400966649);
            var u0 = new V3(0.877712545724556, 0.0197197822130759, 0.478781640529633);
            double t0 = 81786.024077168;
            double mu = 398600435436096;
            double rbody = 6371000;

            double decmass = 2403; // can be DECREASED to add more payload

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

            // re-run fully integrated instead of the analytic bootstrap
            Ascent ascent2 = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(6571000, 6571000, 6571000, 0.558505360638185, 0, 0.349065850398866, true, false)
                .AddStageUsingFinalMass(731995.326793174 - decmass, 328918.196876464 - decmass, 408.091742854086, 159.080051443926, 4, 4)
                .AddStageUsingFinalMass(268744.821741949 - decmass, 168530.570801559 - decmass, 408.091702159863, 118.652885602609, 2, 2)
                .AddStageUsingFinalMass(40763.8839289468 - decmass, 13796.4420050909 - decmass, 465.500076480742, 1118.13132581707, 1, 1, true)
                .FixedBurnTime(false)
                .OldSolution(solution)
                .Build();

            ascent2.Run();

            Optimizer pvg2 = ascent2.GetOptimizer() ?? throw new Exception("null optimzer");

            using Solution solution2 = pvg2.GetSolution();
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

            pvg.Znorm.ShouldBeZero(1e-8);
            Assert.Equal(8, pvg.LmStatus);
        }
    }
}
