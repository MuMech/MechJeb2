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
        public void Delta3GTO()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var          r0          = new V3(5605222.973039, 0.000000, 3043387.760956);
            var          v0          = new V3(0.000000, 408.739353, 0.000000);
            double       t0          = 0;
            double       rbody       = 6378145;
            double       smaT        = 24361140;
            double       eccT        = 0.7308;
            double       incT        = Deg2Rad(28.5);
            double       lanT        = Deg2Rad(269.8);
            double       argpT       = Deg2Rad(130.5);
            double       mu          = 3.986012e14;
            double       aref        = 4 * PI;
            double       cd          = 0.5;
            double       rho0        = 1.225;
            double       h0          = 7200;
            const double Q_ALPHA_MAX = 0;
            const double Q_MAX       = 0;
            V3           w           = 7.29211585e-5 * V3.northpole;

            double PeR = Astro.PeriapsisFromKeplerian(smaT, eccT);
            double ApR = Astro.ApoapsisFromKeplerian(smaT, eccT);

            double isp1 = Astro.IspFromMassesThrustBurntime(301454.000000, 171863.885057, 4854100.000000, 75.200000);
            double isp2 = Astro.IspFromMassesThrustBurntime(158183.885057, 79623.770115, 2968600.000000, 75.200000);
            double isp3 = Astro.IspFromMassesThrustBurntime(72783.770115, 32294.000000, 1083100.000000, 110.600000);
            double isp4 = Astro.IspFromMassesThrustBurntime(23464.000000, 6644.000000, 110094.000000, 700.000000);

            Ascent ascent = Ascent.Builder()
                .AerodynamicConstants(cd, cd, aref, rho0, Q_ALPHA_MAX, Q_MAX, h0, w)
                .AddStage(301454.000000, 171863.885057, 4854100.000000, isp1, 4, 4)
                .AddStage(158183.885057, 79623.770115, 2968600.000000, isp2, 3, 3)
                .AddStage(72783.770115, 32294.000000, 1083100.000000, isp3, 2, 2)
                .AddStage(23464.000000, 6644.000000, 110094.000000, isp4, 1, 1)
                .Initial(r0, v0, r0.normalized, t0, mu, rbody)
                .SetTarget(PeR, ApR, PeR, incT, lanT, argpT, 0, false, true, true)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double _, _) =
                Astro.KeplerianFromStateVectors(mu, rf, vf);

            solution.R(0).ShouldEqual(r0, 1e-9);
            solution.V(0).ShouldEqual(v0, 1e-9);
            solution.M(0).ShouldEqual(301454.000000, 1e-9);

            solution.Tgo(solution.T0, 0).ShouldEqual(75.200000, 1e-3);
            solution.Tgo(solution.T0, 1).ShouldEqual(75.200000, 1e-3);
            solution.Tgo(solution.T0, 2).ShouldEqual(110.600000, 1e-3);
            //solution.Tgo(solution.T0, 3).ShouldEqual(663.139, 1e-3);

            solution.Tgo(0).ShouldEqual(924.139, 1e-3);
            //solution.Vgo(0).ShouldEqual(8518.1366714811684, 1e-3);
            psg.PrimalFeasibility.ShouldBeZero(1e-5);

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

        [Fact]
        public void Falcon9ExpendableGTO()
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            var          r0          = new V3(5605222.973039, 0.000000, 3043387.760956);
            var          v0          = new V3(0.000000, 408.739353, 0.000000);
            const double T0          = 0;
            const double R_BODY      = 6_378_145;
            const double APR_T       = 35_786_000 + R_BODY;
            const double PER_T       = 180_000 + R_BODY;
            const double INC_T       = 28.5 * DEG2RAD;
            const double LAN_T       = 270.0 * DEG2RAD;
            const double ARGP_T      = 180.0 * DEG2RAD;
            const double MU          = 3.986012e14;
            double       aref        = Powi(3.7 / 2, 2) * PI;
            const double CA          = 0.75;
            const double CN          = 2.00;
            const double RHO0        = 1.225;
            const double H0          = 7200;
            const double Q_ALPHA_MAX = 2000;
            const double Q_MAX       = 35000;
            V3           w           = 7.29211585e-5 * V3.northpole;

            const double FIRST_STAGE_DRY  = 22200;
            const double FIRST_STAGE_WET  = 433100;
            const double FIRST_ISP        = 310;
            const double FIRST_THRUST     = 7607000;
            const double SECOND_STAGE_DRY = 4000;
            const double SECOND_STAGE_WET = 111500;
            const double SECOND_ISP       = 348;
            const double SECOND_THRUST    = 934000;
            const double PAYLOAD          = 8300;

            const double FIRST_STAGE_M0  = PAYLOAD + SECOND_STAGE_WET + FIRST_STAGE_WET;
            const double FIRST_STAGE_MF  = PAYLOAD + SECOND_STAGE_WET + FIRST_STAGE_DRY;
            const double SECOND_STAGE_M0 = PAYLOAD + SECOND_STAGE_WET;
            const double SECOND_STAGE_MF = PAYLOAD + SECOND_STAGE_DRY;

            const double FIRST_MDOT  = FIRST_THRUST / (FIRST_ISP * G0);
            const double FIRST_BT    = (FIRST_STAGE_M0 - FIRST_STAGE_MF) / FIRST_MDOT;
            const double SECOND_MDOT = SECOND_THRUST / (SECOND_ISP * G0);
            const double SECOND_BT   = (SECOND_STAGE_M0 - SECOND_STAGE_MF) / SECOND_MDOT;

            Ascent ascent = Ascent.Builder()
                //.AerodynamicConstants(CA, CN, aref, RHO0, Q_ALPHA_MAX, Q_MAX, H0, w)
                .AddStage(FIRST_STAGE_M0, FIRST_STAGE_MF, FIRST_THRUST, FIRST_ISP, 2, 2, allowShutdown: false)
                .AddStage(SECOND_STAGE_M0, SECOND_STAGE_MF, SECOND_THRUST, SECOND_ISP, 1, 1)
                .AddCoast(SECOND_STAGE_M0, 0, 1600, 1, 1, massContinuity: true)
                .AddStage(SECOND_STAGE_M0, SECOND_STAGE_MF, SECOND_THRUST, SECOND_ISP, 1, 1, massContinuity: true)
                .Initial(r0, v0, r0.normalized, T0, MU, R_BODY)
                .SetTarget(PER_T, APR_T, PER_T, INC_T, LAN_T, ARGP_T, 0, false, true, false)
                .Build();

            ascent.Run();

            Optimizer      psg      = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            Ascent ascent2 = Ascent.Builder()
                //.AerodynamicConstants(CA, CN, aref, RHO0, Q_ALPHA_MAX, Q_MAX, H0, w)
                .AddStage(FIRST_STAGE_M0, FIRST_STAGE_MF, FIRST_THRUST, FIRST_ISP, 2, 2, allowShutdown: false)
                .AddStage(SECOND_STAGE_M0, SECOND_STAGE_MF, SECOND_THRUST, SECOND_ISP, 1, 1)
                .AddCoast(SECOND_STAGE_M0, 0, 1600, 1, 1, massContinuity: true)
                .AddStage(SECOND_STAGE_M0, SECOND_STAGE_MF, SECOND_THRUST, SECOND_ISP, 1, 1, massContinuity: true)
                .Initial(r0, v0, r0.normalized, T0, MU, R_BODY)
                .SetTarget(PER_T, APR_T, PER_T, INC_T, LAN_T, ARGP_T, 0, false, true, true)
                .OldSolution(solution)
                .Build();

            ascent2.Run();

            Optimizer      psg2      = ascent2.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution2 = psg2.Solution ?? throw new Exception("null solution");

            Ascent ascent3 = Ascent.Builder()
                .AerodynamicConstants(CA, CN, aref, RHO0, Q_ALPHA_MAX, Q_MAX, H0, w)
                .AddStage(FIRST_STAGE_M0, FIRST_STAGE_MF, FIRST_THRUST, FIRST_ISP, 2, 2, allowShutdown: false)
                .AddStage(SECOND_STAGE_M0, SECOND_STAGE_MF, SECOND_THRUST, SECOND_ISP, 1, 1)
                .AddCoast(SECOND_STAGE_M0, 0, 1600, 1, 1, massContinuity: true)
                .AddStage(SECOND_STAGE_M0, SECOND_STAGE_MF, SECOND_THRUST, SECOND_ISP, 1, 1, massContinuity: true)
                .Initial(r0, v0, r0.normalized, T0, MU, R_BODY)
                .SetTarget(PER_T, APR_T, PER_T, INC_T, LAN_T, ARGP_T, 0, false, true, true)
                .OldSolution(solution2)
                .Build();

            ascent3.Run();

            Optimizer      psg3      = ascent3.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution3 = psg3.Solution ?? throw new Exception("null solution");

            solution2.R(0).ShouldEqual(r0, 1e-9);
            solution2.V(0).ShouldEqual(v0, 1e-9);
            solution2.M(0).ShouldEqual(FIRST_STAGE_M0, 1e-9);

            solution2.Tgo(solution2.T0, 0).ShouldEqual(FIRST_BT, 1e-3);
            solution2.Tgo(solution2.T0, 1).ShouldBeLessThanOrEqual(SECOND_BT);
            solution2.Tgo(solution2.T0, 2).ShouldEqual(1055.0665736765161, 1e-3);
            (solution2.Tgo(solution2.T0, 1) + solution2.Tgo(solution2.T0, 3)).ShouldEqual(381.26954084989723, 1e-3);

            psg2.PrimalFeasibility.ShouldBeZero(1e-5);
            solution2.Tgo(0).ShouldEqual(1600.5671186447382, 1e-3);
            solution2.Vgo(0).ShouldEqual(11121.768749617131, 1e-3);

            (double pitch, double heading) = Astro.ECIToPitchHeading(r0, solution2.U(0));
            Rad2Deg(pitch).ShouldEqual(83.373269308213963, 1e-3);
            Rad2Deg(heading).ShouldEqual(89.53966801098629, 1e-3);

            (V3 rf1, _) = solution2.StateVectors(solution2.EndTime(1));

            (rf1.magnitude - R_BODY).ShouldEqual(101459.50200797338, 1e-5);

            (V3 rf, V3 vf) = solution2.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double nuf, _) =
                Astro.KeplerianFromStateVectors(MU, rf, vf);

            double aprf = Astro.ApoapsisFromKeplerian(smaf, eccf);
            double perf = Astro.PeriapsisFromKeplerian(smaf, eccf);

            perf.ShouldEqual(PER_T, 1e-5);
            aprf.ShouldEqual(APR_T, 1e-5);
            incf.ShouldEqual(INC_T, 1e-5);
            lanf.ShouldEqual(LAN_T, 1e-2);
            argpf.ShouldEqual(ARGP_T, 1e-2);
            nuf.ShouldEqual(Deg2Rad(5.0640956966565094), 1e-2);
        }
    }
}
