using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PSGTests.AscentTests
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

            var    r0    = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var    v0    = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var    u0    = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0    = 661803.431918959;
            double mu    = 3.986004418e+14;
            double rbody = 6.371e+6;

            double PeR  = 6.371e+6 + 185e+3;
            double ApR  = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1)
                .AddCoast(678.290157913434, 0, 450, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, allowShutdown: false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(t0).ShouldEqual(9664.3818394067875, 1e-4);

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(235.03888133255938, 1e-3);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(9664.381839407506, 1e-3);
            solution.U(t0).normalized.ShouldEqual(new V3(0.77936242810570966, -0.56640344307712909, 0.26792040856856347), 1e-1);

            smaf.ShouldEqual(11463499.98898875, 1e-4);
            eccf.ShouldEqual(0.4280978753095433, 1e-4);
            incf.ShouldEqual(incT, 1e-4);
            lanf.ShouldEqual(3.0482327430579215, 1e-2);
            argpf.ShouldEqual(1.9882953487020583, 1e-2);
            tanof.ShouldBeZeroRadians(1e-2);
        }

        // Carnasa's TheStandard rocket (ish).
        // This tests optimality of a burn-optimizedBurn-coast-burn with all stages guided with free attachment
        [Fact]
        public void TheStandardGuidedUpperFree()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var    r0    = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var    v0    = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var    u0    = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0    = 661803.431918959;
            double mu    = 3.986004418e+14;
            double rbody = 6.371e+6;

            double PeR  = 6.371e+6 + 185e+3;
            double ApR  = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            const double OPTIMUMVGO = 9670.9023790445626;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, false, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1)
                .AddCoast(678.290157913434, 0, 450, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, allowShutdown: false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(221.93374918746366, 1e-2);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(OPTIMUMVGO, 1e-2);
            solution.U(t0).normalized.ShouldEqual(new V3(0.77953530499830737, -0.56621745928946121, 0.26781056188467101), 1e-1);

            smaf.ShouldEqual(11463499.98898875, 1e-4);
            eccf.ShouldEqual(0.4280978753095433, 1e-4);
            incf.ShouldEqual(incT, 1e-4);
            lanf.ShouldEqual(3.0482353737384318, 1e-2);
            argpf.ShouldEqual(1.9465073215385686, 1e-2);
            tanof.ShouldEqual(0.029535094479713919, 1e-2);
        }

        // Carnasa's TheStandard rocket.
        // This tests optimality of a burn-optimizedBurn-coast-burn with all stages guided with periapsis attachment
        // optimum:  ctmin: 236.563629014638 dvmin: 9673.89237593156
        [Fact]
        public void TheStandardPeriapsis()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var    r0    = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var    v0    = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var    u0    = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double t0    = 661803.431918959;
            double mu    = 3.986004418e+14;
            double rbody = 6.371e+6;

            double PeR  = 6.371e+6 + 185e+3;
            double ApR  = 6.371e+6 + 10e+6;
            double incT = Deg2Rad(28.608);

            const double OPTIMUMVGO = 9673.8952123189683;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, true, false)
                .AddStageUsingFinalMass(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726, 3, 3)
                .AddStageUsingFinalMass(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409, 1, 1)
                .AddCoast(678.290157913434, 0, 450, 1, 1)
                .AddStageUsingFinalMass(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005, 0, 0, true, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");


            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(236.563629014638, 1e-2);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(OPTIMUMVGO, 1e-3);
            solution.U(t0).normalized.ShouldEqual(new V3(0.77930143698028731, -0.56665965776430649, 0.26755579340185925), 1e-2);

            smaf.ShouldEqual(11463499.98898875, 1e-4);
            eccf.ShouldEqual(0.4280978753095433, 1e-4);
            incf.ShouldEqual(incT, 1e-4);
            lanf.ShouldEqual(3.0481683077792203, 1e-2);
            argpf.ShouldEqual(1.9887149870712548, 1e-2);
            tanof.ShouldBeZeroRadians(1e-3);
        }
    }
}
