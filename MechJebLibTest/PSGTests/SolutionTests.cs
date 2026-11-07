/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.PSGTests
{
    public class SolutionTests
    {
        private const int SAMPLES = 64;

        // the solver warm starts from one copy while guidance flies another, so a copy has to agree bit-for-bit, own its T0 and
        // survive the original being disposed
        [Fact]
        public void DeepCopyIsIndependent()
        {
            double t0 = 82132.7494998545;

            // KerbinTests.MainSailTinCanVacuum
            Ascent ascent = Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), t0, 3531600000000, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            Solution solution = psg.Solution ?? throw new Exception("null solution");

            Solution copy = solution.DeepCopy();

            copy.Segments.ShouldEqual(solution.Segments);
            copy.T0.ShouldEqual(solution.T0, 0);
            copy.Tf.ShouldEqual(solution.Tf, 0);
            copy.Vgo(t0).ShouldEqual(solution.Vgo(t0), 0);
            copy.Tgo(t0).ShouldEqual(solution.Tgo(t0), 0);

            for (int p = 0; p < solution.Segments; p++)
            {
                copy.StartTime(p).ShouldEqual(solution.StartTime(p), 0);
                copy.EndTime(p).ShouldEqual(solution.EndTime(p), 0);
                (copy.CoastPhase(p) == solution.CoastPhase(p)).ShouldBeTrue();
                copy.KSPStage(p).ShouldEqual(solution.KSPStage(p));
            }

            var r = new V3[SAMPLES + 1];
            var v = new V3[SAMPLES + 1];
            var u = new V3[SAMPLES + 1];
            double[] m  = new double[SAMPLES + 1];
            double[] dv = new double[SAMPLES + 1];
            double tf = solution.Tf;

            for (int i = 0; i <= SAMPLES; i++)
            {
                double t = t0 + (tf - t0) * i / SAMPLES;
                r[i]  = solution.R(t);
                v[i]  = solution.V(t);
                u[i]  = solution.U(t);
                m[i]  = solution.M(t);
                dv[i] = solution.DV(t);

                copy.R(t).ShouldEqual(r[i], 0);
                copy.V(t).ShouldEqual(v[i], 0);
                copy.U(t).ShouldEqual(u[i], 0);
                copy.M(t).ShouldEqual(m[i], 0);
                copy.DV(t).ShouldEqual(dv[i], 0);
            }

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            (V3 rfCopy, V3 vfCopy) = copy.TerminalStateVectors();
            rfCopy.ShouldEqual(rf, 0);
            vfCopy.ShouldEqual(vf, 0);

            double solutionT0 = solution.T0;
            double copyT0     = copy.T0;
            copy.T0 += 10;
            solution.T0.ShouldEqual(solutionT0, 0);
            copy.T0 = copyT0;

            solution.Dispose();

            for (int i = 0; i <= SAMPLES; i++)
            {
                double t = t0 + (tf - t0) * i / SAMPLES;
                copy.R(t).ShouldEqual(r[i], 0);
                copy.V(t).ShouldEqual(v[i], 0);
                copy.U(t).ShouldEqual(u[i], 0);
                copy.M(t).ShouldEqual(m[i], 0);
            }

            copy.Dispose();
        }

        // A fixed booster can burn out a little before the frozen solution predicts, so the vessel is in the upper stage while
        // the clock is still in the booster phase.  Guidance must then see the whole upper stage burn ahead of it, and must not
        // compare against the end of the booster (hotstaging puts the vessel ahead of that state, which used to end the ascent
        // and kill the throttle at separation).
        [Fact]
        public void StagingAheadOfTheClock()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double t0 = 0;
            double rbody = 6.371e+6;
            double peR = rbody + 185e+3;
            double mu = 3.986004418e+14;

            // Titan2Tests.FlightPathAngle4Elliptical with a fixed booster and an optimized upper stage
            Ascent ascent = Ascent.Builder()
               .AddStage(153180, 35248.993172173155, 2194400, 296, 4, 4, allowShutdown: false)
               .AddStage(31980, 6125.823788668185, 443700, 315, 2, 2)
               .Initial(r0, v0, r0.normalized, t0, mu, rbody)
               .SetTarget(peR, peR, peR, Deg2Rad(28.608), Deg2Rad(270), 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            solution.OptimizeKSPStage().ShouldEqual(2);
            solution.TerminalKSPStage().ShouldEqual(2);

            double boosterEnd = solution.EndTime(0);
            double upperBurn  = solution.EndTime(1) - solution.StartTime(1);

            // the clock is still in the booster phase, but the upper stage burn is all ahead of us
            solution.TgoForKSPStage(boosterEnd - 0.1, 2).ShouldEqual(upperBurn, 1e-9);
            solution.TgoForKSPStage(boosterEnd + 0.1, 2).ShouldEqual(upperBurn - 0.1, 1e-9);
            solution.TgoForKSPStage(boosterEnd - 0.1, 4).ShouldEqual(0.1, 1e-9);
            // burned past the end of the booster
            solution.TgoForKSPStage(boosterEnd + 0.1, 4).ShouldBeZero();

            // ahead of the booster end state after hotstaging, but nowhere near the end of the upper stage
            double t = boosterEnd - 0.3;
            (V3 r, V3 v) = solution.StateVectors(t);
            solution.TerminalGuidanceSatisfied(r, v + 20 * v.normalized, t, 2).ShouldBeFalse();

            // a genuine cutoff at the end of the upper stage still fires
            double tf = solution.Tf;
            (V3 rf, V3 vf) = solution.StateVectors(tf);
            solution.TerminalGuidanceSatisfied(rf, vf + vf.normalized, tf - 0.1, 2).ShouldBeTrue();
            (V3 rEarly, V3 vEarly) = solution.StateVectors(tf - 5);
            solution.TerminalGuidanceSatisfied(rEarly, vEarly, tf - 5, 2).ShouldBeFalse();
        }
    }
}
