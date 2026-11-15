/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.PSG.Terminal;
using Xunit;
using static System.Math;
using Column = MechJebLib.PSG.SolutionTrace.Column;

namespace MechJebLibTest.PSGTests
{
    // SolutionTrace and AeroModel, on the synthetic solutions of SolutionTests
    public partial class SolutionTests
    {
        [Fact]
        public void TraceSamplesEveryNodeOfEveryPhase()
        {
            using Solution solution = TheStandardLike();
            var trace = new SolutionTrace();
            trace.Sample(solution, 8);

            // each synthetic phase is one node: 8 samples across it and its end
            trace.Count.ShouldEqual(5 * 9);
            trace.T0.ShouldEqual(4096, 0);
            trace.Tf.ShouldEqual(4384, 0);
            Assert.Equal(new double[] { 4096, 4160, 4192, 4224, 4352, 4384 }, trace.PhaseBoundaries);
            Assert.Equal(new double[] { 4096, 4160, 4160, 4192, 4192, 4224, 4224, 4352, 4352, 4384 }, trace.NodeBoundaries);
            // a synthetic node interpolates through its two ends
            Assert.Equal(trace.NodeBoundaries, trace.Knots);

            for (int row = 0; row < trace.Count; row++)
            {
                int p = row / 9;
                trace[Column.PHASE, row].ShouldEqual(p);
                trace[Column.MET, row].ShouldEqual(solution.StartTime(p) + (solution.EndTime(p) - solution.StartTime(p)) * (row % 9) / 8, 1e-12);
                trace[Column.KSP_STAGE, row].ShouldEqual(solution.KSPStage(p));
                trace[Column.COAST, row].ShouldEqual(p == 3 ? 1 : 0);
                trace[Column.UNGUIDED, row].ShouldEqual(p == 0 ? 1 : 0);
                Assert.True(Finite(trace[Column.PITCH, row]) && Finite(trace[Column.HEADING, row]));
            }

            // each phase is sampled on its own interpolant: the drop in mass at the booster's end shows as two rows at one MET
            trace[Column.MET, 8].ShouldEqual(trace[Column.MET, 9], 0);
            trace[Column.MASS, 8].ShouldEqual(4096, 1e-9);
            trace[Column.MASS, 9].ShouldEqual(2048, 1e-9);

            // p1's throttle runs from 1 down to 0.25, and the coast has none
            trace[Column.THROTTLE, 9].ShouldEqual(1, 1e-12);
            trace[Column.THROTTLE, 17].ShouldEqual(0.25, 1e-12);
            trace[Column.THROTTLE, 31].ShouldEqual(0, 0);

            // the delta-v left runs down to zero at the end, from all of it at the start
            trace[Column.DELTA_V_LEFT, 0].ShouldEqual(solution.DV(solution.Tf), 1e-9);
            trace[Column.DELTA_V_LEFT, trace.Count - 1].ShouldEqual(0, 1e-9);
            for (int row = 0; row < trace.Count; row++)
                (trace[Column.DELTA_V, row] + trace[Column.DELTA_V_LEFT, row]).ShouldEqual(solution.DV(solution.Tf), 1e-9);

            // no atmosphere, no limits
            Assert.Equal((0.0, 0.0), trace.Range(Column.Q_ALPHA_FRACTION));
            Assert.Equal((0.0, 0.0), trace.Range(Column.Q_FRACTION));
        }

        [Fact]
        public void TraceRefusesANormalizedSolution()
        {
            using Solution solution = TheStandardLike(true);
            Assert.ThrowsAny<Exception>(() => new SolutionTrace().Sample(solution));
        }

        [Fact]
        public void TraceWritesCsv()
        {
            using Solution solution = TheStandardLike();
            var trace = new SolutionTrace();
            trace.Sample(solution, 2);

            CultureInfo culture = CultureInfo.CurrentCulture;
            try
            {
                // a decimal comma must not reach the file
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var writer = new StringWriter();
                trace.WriteCsv(writer);

                string[] lines = writer.ToString().Split(new[] { writer.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                lines.Length.ShouldEqual(trace.Count + 1);
                Assert.Equal(string.Join(",", Enum.GetNames(typeof(Column))), lines[0]);
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] fields = lines[i].Split(',');
                    fields.Length.ShouldEqual(SolutionTrace.ColumnCount);
                    double.Parse(fields[(int)Column.MET], CultureInfo.InvariantCulture).ShouldEqual(trace[Column.MET, i - 1], 0);
                    double.Parse(fields[(int)Column.MASS], CultureInfo.InvariantCulture).ShouldEqual(trace[Column.MASS, i - 1], 0);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [Fact]
        public void AeroModelFractions()
        {
            const double R_BODY = 600000, H0 = 5000, RHO0 = 1.2, Q_ALPHA_MAX = 2000, Q_MAX = 40000;
            var aero = new AeroModel(R_BODY, H0, RHO0 / Q_ALPHA_MAX, RHO0 / Q_MAX, V3.zero);

            // one scale height up, 300 m/s, the thrust 0.1 rad off the velocity
            var r = new V3(R_BODY + H0, 0, 0);
            var v = new V3(0, 300, 0);
            const double ALPHA = 0.1;
            var u = new V3(0, Cos(ALPHA), Sin(ALPHA));

            aero.RelativeDensity(r).ShouldEqual(Exp(-1), 1e-12);
            aero.AngleOfAttack(r, v, u).ShouldEqual(ALPHA, 1e-12);

            double q = 0.5 * RHO0 * Exp(-1) * 300 * 300;
            aero.QFraction(r, v).ShouldEqual(q / Q_MAX, 1e-12);
            aero.QAlphaFraction(r, v, u).ShouldEqual(q * ALPHA / Q_ALPHA_MAX, 1e-12);

            // without the limits, or without an atmosphere, the fractions are zero
            var noLimits = new AeroModel(R_BODY, H0, 0, 0, V3.zero);
            noLimits.QFraction(r, v).ShouldEqual(0, 0);
            noLimits.QAlphaFraction(r, v, u).ShouldEqual(0, 0);
            var vacuum = new AeroModel(R_BODY, 0, RHO0 / Q_ALPHA_MAX, RHO0 / Q_MAX, V3.zero);
            vacuum.QFraction(r, v).ShouldEqual(0, 0);
            vacuum.RelativeDensity(r).ShouldEqual(0, 0);
        }

        // the velocity of a point turning with the planet is zero relative to its surface
        [Fact]
        public void AeroModelTurnsWithThePlanet()
        {
            const double R_BODY = 600000, OMEGA = 2 * PI / 21600;
            var aero = new AeroModel(R_BODY, 5000, 1.0 / 2000, 0, new V3(0, 0, OMEGA));
            var r = new V3(R_BODY, 0, 0);
            var v = new V3(0, OMEGA * R_BODY, 0);

            aero.SurfaceVelocity(r, v).magnitude.ShouldEqual(0, 1e-9);
            aero.AngleOfAttack(r, v, new V3(1, 0, 0)).ShouldEqual(0, 0);
            aero.QAlphaFraction(r, v, new V3(1, 0, 0)).ShouldEqual(0, 0);
        }

        // the normalized model the optimizer constrains and the SI one guidance's solution carries give the same fractions
        [Fact]
        public void AeroModelKeepsItsFractionsInSIUnits()
        {
            const double R_BODY = 600000, H0 = 5600, RHO0 = 1.225, Q_ALPHA_MAX = 2000, Q_MAX = 30000;
            var w = new V3(0, 0, 2 * PI / 21549.425);
            var r0 = new V3(R_BODY + 70, 0, 0);
            var v0 = new V3(0, 174.9, 0);

            var problem = new Problem(r0, v0, new V3(1, 0, 0), 30000, 0, 3.5316e12, R_BODY, H0, 0.3 * 10 * RHO0, RHO0 / Q_ALPHA_MAX,
                RHO0 / Q_MAX, w, new Kepler3(700000, 0, 0));
            Scale scale = problem.Scale;

            using var normalized = new Solution(problem, null);
            using Solution si = normalized.DeNormalized();

            si.Aero.RBody.ShouldEqual(R_BODY, 1e-12);
            si.Aero.H0.ShouldEqual(H0, 1e-12);
            si.Aero.Rho0InvQAlphaMax.ShouldEqual(RHO0 / Q_ALPHA_MAX, 1e-12);
            si.Aero.Rho0InvQMax.ShouldEqual(RHO0 / Q_MAX, 1e-12);
            (si.Aero.W - w).magnitude.ShouldEqual(0, 1e-15);

            var r = new V3(R_BODY + 9000, 2000, 300);
            var v = new V3(200, 900, 50);
            var u = new V3(0.3, 0.9, 0.1).normalized;

            normalized.Aero.QAlphaFraction(r / scale.LengthScale, v / scale.VelocityScale, u)
               .ShouldEqual(si.Aero.QAlphaFraction(r, v, u), 1e-12);
            normalized.Aero.QFraction(r / scale.LengthScale, v / scale.VelocityScale).ShouldEqual(si.Aero.QFraction(r, v), 1e-12);
            Assert.True(si.Aero.QAlphaFraction(r, v, u) > 0);
        }

        private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }
}
