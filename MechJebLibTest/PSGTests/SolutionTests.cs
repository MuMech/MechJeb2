/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;

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
    }
}
