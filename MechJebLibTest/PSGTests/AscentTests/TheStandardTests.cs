/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿using System;
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

            double thrust1 = Astro.ThrustFromMassesIspBurntime(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726);
            double thrust2 = Astro.ThrustFromMassesIspBurntime(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409);
            double thrust3 = Astro.ThrustFromMassesIspBurntime(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005);

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, 0, true, false, false)
                .AddStage(49119.7842689869, 7114.2513992454, thrust1, 288.000034332275,  3, 3)
                .AddStage(2848.62586760223, 1363.71123994759, thrust2,270.15767003304,  1, 1)
                .AddCoast(678.290157913434, 0, 450, 1, 1)
                .AddStage(678.290157913434, 177.582604389742, thrust3,230.039271734103,  0, 0, allowShutdown: false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(t0).ShouldEqual(9669.6899155829542, 1e-3);

            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(234.78752267826667, 1e-3);
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

            double thrust1 = Astro.ThrustFromMassesIspBurntime(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726);
            double thrust2 = Astro.ThrustFromMassesIspBurntime(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409);
            double thrust3 = Astro.ThrustFromMassesIspBurntime(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005);

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, 0, false, false, false)
                .AddStage(49119.7842689869, 7114.2513992454, thrust1, 288.000034332275,  3, 3)
                .AddStage(2848.62586760223, 1363.71123994759, thrust2,270.15767003304,  1, 1)
                .AddCoast(678.290157913434, 0, 450, 1, 1)
                .AddStage(678.290157913434, 177.582604389742, thrust3,230.039271734103,  0, 0, allowShutdown: false)
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

            double thrust1 = Astro.ThrustFromMassesIspBurntime(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726);
            double thrust2 = Astro.ThrustFromMassesIspBurntime(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409);
            double thrust3 = Astro.ThrustFromMassesIspBurntime(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005);

            double       aref        = 10;
            const double CD          = 0.5;
            const double RHO0        = 1.225;
            const double H0          = 7200;
            const double Q_ALPHA_MAX = 2000;
            const double Q_MAX       = 35000;
            V3           w           = 7.29211585e-5 * V3.northpole;

            Ascent ascent = Ascent.Builder()
                .Initial(r0, v0, u0, t0, mu, rbody)
                .AerodynamicConstants(aref, CD, RHO0, Q_ALPHA_MAX, Q_MAX, H0, w)
                .SetTarget(PeR, ApR, PeR, incT, 0, 0, 0, true, false, false)
                .AddStage(49119.7842689869, 7114.2513992454, thrust1, 288.000034332275,  3, 3)
                .AddStage(2848.62586760223, 1363.71123994759, thrust2,270.15767003304,  1, 1)
                .AddCoast(678.290157913434, 0, 450, 1, 1)
                .AddStage(678.290157913434, 177.582604389742, thrust3,230.039271734103,  0, 0, unguided: true, allowShutdown: false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");


            solution.Tgo(solution.T0, 0).ShouldBePositive();
            solution.Tgo(solution.T0, 1).ShouldBePositive();
            solution.Tgo(solution.T0, 2).ShouldEqual(135.22924418942029, 1e-2);
            solution.Tgo(solution.T0, 3).ShouldBePositive();

            psg.PrimalFeasibility.ShouldBeZero(1e-5);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(t0).ShouldEqual(r0);
            solution.V(t0).ShouldEqual(v0);
            solution.M(t0).ShouldEqual(49119.7842689869);
            solution.Vgo(t0).ShouldEqual(10510.620490322281, 1e-3);
            solution.U(t0).normalized.ShouldEqual(new V3(-0.012214116666567524, -0.87839128647452269, 0.47778610611830125), 1e-2);

            smaf.ShouldEqual(11463499.98898875, 1e-4);
            eccf.ShouldEqual(0.4280978753095433, 1e-4);
            incf.ShouldEqual(incT, 1e-4);
            lanf.ShouldEqual(3.0481683077792203, 1e-2);
            argpf.ShouldEqual(1.8907189606177459, 1e-2);
            tanof.ShouldBeZeroRadians(1e-3);
        }
    }
}
