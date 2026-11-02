/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using Xunit;
using static System.Math;

namespace MechJebLibTest.PSGTests
{
    // Kepler4 and Kepler5 in normalized units (mu = 1), see PSG-10.
    public class KeplerTerminalTests
    {
        private static double[] Evaluate(ITerminal terminal, V3 r, V3 v, out double[,] jac)
        {
            double[] x = { r.x, r.y, r.z, v.x, v.y, v.z };
            double[] f = new double[terminal.NumConstraints];
            alglib.sparsecreatecrsempty(x.Length, out alglib.sparsematrix j);

            int ci = 0;
            terminal.Constraints(x, (0, 1, 2), (3, 4, 5), f, j, ref ci);

            ci.ShouldEqual(terminal.NumConstraints);

            jac = new double[f.Length, x.Length];
            for (int i = 0; i < f.Length; i++)
            for (int k = 0; k < x.Length; k++)
                jac[i, k] = alglib.sparseget(j, i, k);

            return f;
        }

        private static (V3 r, V3 v) State(double sma, double ecc, double inc, double lan, double argp, double nu) =>
            Astro.StateVectorsFromKeplerian(1.0, sma * (1 - ecc * ecc), ecc, inc, lan, argp, nu);

        // circular through hyperbolic, prograde, polar and retrograde (h along +z, in the equator, and along -z)
        public static IEnumerable<object[]> Kepler5Targets()
        {
            var targets = new[]
            {
                new[] { 1, 0.1, 0.5, 1.0, 2.0 },
                new[] { 1, 0.1, 0, 1.0, 2.0 },
                new[] { 1, 0.1, PI, 1.0, 2.0 },
                new[] { 1, 0.1, PI / 2, 1.0, 2.0 },
                new[] { 1, 1e-4, 0.5, 1.0, 2.0 },
                new[] { 1, 0, 0.5, 1.0, 2.0 },
                new[] { 1, 0.9, 0.5, 1.0, 2.0 },
                new[] { -2, 1.5, 0.5, 1.0, 2.0 },
                new[] { 1.3, 0.2391, 0.4993, 4.712, 1.6625 }
            };

            foreach (double[] t in targets)
            foreach (double nu in new[] { 0, 1.0, -2.0, 3.0 })
            {
                // keep hyperbolic states well inside the asymptotes
                if (1 + t[1] * Cos(nu) < 0.1)
                    continue;

                yield return new object[] { t[0], t[1], t[2], t[3], t[4], nu };
            }
        }

        [Theory]
        [MemberData(nameof(Kepler5Targets))]
        public void Kepler5ConstraintsVanishOnTheTargetOrbit(double sma, double ecc, double inc, double lan, double argp, double nu)
        {
            var terminal = new Kepler5(sma, ecc, inc, lan, argp);
            (V3 r, V3 v) = State(sma, ecc, inc, lan, argp, nu);

            double[] f = Evaluate(terminal, r, v, out _);

            f.Length.ShouldEqual(5);
            foreach (double fi in f)
                fi.ShouldBeZero(1e-14);
        }

        // the PSG-10 regression: with all three components of e - eT the six rows were always rank 5
        [Theory]
        [MemberData(nameof(Kepler5Targets))]
        public void Kepler5JacobianHasFullRowRank(double sma, double ecc, double inc, double lan, double argp, double nu)
        {
            var terminal = new Kepler5(sma, ecc, inc, lan, argp);
            (V3 r, V3 v) = State(sma, ecc, inc, lan, argp, nu);

            Evaluate(terminal, r, v, out double[,] jac);

            alglib.rmatrixsvd(jac, 5, 6, 0, 0, 2, out double[] w, out _, out _);

            w[4].ShouldBeGreaterThan(1e-2);
        }

        [Theory]
        [MemberData(nameof(Kepler5Targets))]
        public void Kepler5JacobianMatchesFiniteDifferences(double sma, double ecc, double inc, double lan, double argp, double nu)
        {
            const double H = 1e-6;

            var terminal = new Kepler5(sma, ecc, inc, lan, argp);
            (V3 r, V3 v) = State(sma, ecc, inc, lan, argp, nu);

            Evaluate(terminal, r, v, out double[,] jac);

            for (int k = 0; k < 6; k++)
            {
                V3 dr = k < 3 ? H * Unit(k) : V3.zero;
                V3 dv = k < 3 ? V3.zero : H * Unit(k - 3);

                double[] fp = Evaluate(terminal, r + dr, v + dv, out _);
                double[] fm = Evaluate(terminal, r - dr, v - dv, out _);

                for (int i = 0; i < 5; i++)
                    jac[i, k].ShouldEqual((fp[i] - fm[i]) / (2 * H), 1e-8);
            }

            static V3 Unit(int i)
            {
                V3 u = V3.zero;
                u[i] = 1;
                return u;
            }
        }

        // the eccentricity rows are the components of e in a fixed basis, so moving the argument of periapsis rotates e
        // in the target plane by exactly that angle
        [Fact]
        public void Kepler5EccentricityRowsRotateWithArgumentOfPeriapsis()
        {
            const double ECC = 0.1;
            const double DARGP = 1e-3;

            var terminal = new Kepler5(1, ECC, 0.5, 1.0, 2.0);
            (V3 r, V3 v) = State(1, ECC, 0.5, 1.0, 2.0 + DARGP, 0.7);

            double[] f = Evaluate(terminal, r, v, out _);

            for (int i = 0; i < 3; i++)
                f[i].ShouldBeZero(1e-14);

            // |e - eT| = 2 ecc sin(dargp / 2)
            Sqrt(f[3] * f[3] + f[4] * f[4]).ShouldEqual(2 * ECC * Sin(DARGP / 2), 1e-12);
        }

        // the PSG-10 regression: |e|^2 - eccT^2 moved by only 2 eccT d|e| so the residual tolerance allowed large errors
        // in the eccentricity of nearly circular targets, |e| - eccT moves by d|e|
        [Fact]
        public void Kepler4EccentricityResidualIsFirstOrder()
        {
            const double ECC = 1e-3;
            const double DECC = 1e-5;

            var terminal = new Kepler4(1, ECC, 0.5, 1.0);

            (V3 r0, V3 v0) = State(1, ECC, 0.5, 1.0, 2.0, 1.0);

            double[] f0 = Evaluate(terminal, r0, v0, out double[,] jac);

            f0.Length.ShouldEqual(4);
            foreach (double fi in f0)
                fi.ShouldBeZero(1e-15);

            double norm = 0;
            for (int k = 0; k < 6; k++)
                norm += jac[0, k] * jac[0, k];

            Sqrt(norm).ShouldBeGreaterThan(0.5);

            // same semi-latus rectum, so the same angular momentum, but a larger eccentricity
            double l = 1 - ECC * ECC;
            (V3 r, V3 v) = Astro.StateVectorsFromKeplerian(1.0, l, ECC + DECC, 0.5, 1.0, 2.0, 1.0);

            double[] f = Evaluate(terminal, r, v, out _);

            (f[0] - DECC).ShouldBeZero(1e-14);
            for (int i = 1; i < 4; i++)
                f[i].ShouldBeZero(1e-15);
        }
    }
}
