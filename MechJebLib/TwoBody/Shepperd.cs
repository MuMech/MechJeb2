/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.TwoBody
{
    /// <summary>
    ///     Shepperd SW. Universal Keplerian state transition matrix. Celestial Mechanics. 1985 Feb;35(2):129–44. doi:10.1007/BF01227666
    ///     Shepperd SW. Naturally occurring continued fractions in the variation of Kepler’s equation. Celestial Mechanics. 1987 Mar;42(1–4):91–106. doi:10.1007/BF01232950
    /// </summary>
    public static class Shepperd
    {
        // More robust version of Shepperd's method that solves for U3 directly to just propagate state, with a
        // fallback to bisection.
        public static (V3 rf, V3 vf) Solve(double mu, double tau, V3 ri, V3 vi)
        {
            double tolerance = 1.0e-12;
            double u = 0;
            int imax = 50;
            double umax = double.MaxValue;
            double umin = double.MinValue;
            double orbits = 0;
            double tdesired = tau;
            double threshold = tolerance * Math.Abs(tdesired);
            double r0 = ri.magnitude;
            double n0 = V3.Dot(ri, vi);

            double beta = 2.0 * (mu / r0) - vi.sqrMagnitude;

            if (beta != 0.0)
            {
                umax = 1.0 / Math.Sqrt(Math.Abs(beta));
                umin = -1.0 / Math.Sqrt(Math.Abs(beta));
            }

            if (beta > 0.0)
            {
                orbits = beta * tau - 2 * n0;
                orbits = 1 + orbits * Math.Sqrt(beta) / (Math.PI * mu);
                orbits = Math.Floor(orbits / 2);
            }

            double uold = double.MinValue;
            double dtold = double.MinValue;
            double u1 = 0.0;
            double u2 = 0.0;
            double r1 = 0.0;

            for (int i = 1; i < imax; i++)
            {
                double q = beta * u * u;
                q /= 1.0 + q;

                double n = 0;
                double r = 1;
                double l = 1;
                double s = 1;
                double d = 3;
                double gcf = 1;
                double k = -5;

                double gold = 0;

                // this solves for U3 directly by using G(3, 0, 3/2, q)
                while (gcf != gold)
                {
                    k = -k;
                    l += 2;
                    d += 4 * l;
                    n += (1 + k) * l;
                    r = d / (d - n * r * q);
                    s = (r - 1) * s;
                    gold = gcf;
                    gcf = gold + s;
                }

                double h0 = 1 - 2 * q;
                double h1 = 2 * u * (1 - q);
                double u0 = 2 * h0 * h0 - 1;
                u1 = 2 * h0 * h1;
                u2 = 2 * h1 * h1;
                double u3 = 2 * h1 * u2 * gcf / 3;

                if (orbits != 0)
                {
                    u3 += 2 * Math.PI * orbits / (beta * Math.Sqrt(beta));
                }

                r1 = r0 * u0 + n0 * u1 + mu * u2;
                double dt = r0 * u1 + n0 * u2 + mu * u3;
                double slope = 4 * r1 / (1 + beta * u * u);

                double terror = tdesired - dt;

                if (Math.Abs(terror) < threshold)
                    break;

                if (i > 1 && u == uold)
                    break;

                if (i > 1 && dt == dtold)
                    break;

                uold = u;
                dtold = dt;

                double ustep = terror / slope;
                if (ustep > 0)
                {
                    umin = u;
                    u += ustep;

                    if (u > umax)
                        u = (umin + umax) / 2;
                }
                else
                {
                    umax = u;
                    u += ustep;

                    if (u < umin)
                        u = (umin + umax) / 2;
                }
            }

            double f = 1.0 - mu / r0 * u2;
            double gg = 1.0 - mu / r1 * u2;
            double g = r0 * u1 + n0 * u2;
            double ff = -mu * u1 / (r0 * r1);

            V3 rf = f * ri + g * vi;
            V3 vf = ff * ri + gg * vi;

            return (rf, vf);
        }

        // Aux function for derivatives if all you care about is w.r.t the time of flight
        public static (DualV3 rf, DualV3 vf) Solve(double mu, Dual tau, V3 ri, V3 vi)
        {
            (V3 rfM, V3 vfM) = Solve(mu, tau.M, ri, vi);
            double rfM3 = rfM.sqrMagnitude * rfM.magnitude;
            var rf = new DualV3(rfM, vfM * tau.D);
            var vf = new DualV3(vfM, -mu * rfM / rfM3 * tau.D);
            return (rf, vf);
        }

        // The STM is a 6x6 matrix which we return decomposed into 4 3x3 matrices
        //
        //  [ 𝛿r ] = [ stmRfR0 stmRfV0 ] [ r ]
        //  [ 𝛿v ] = [ stmVfR0 stmVfV0 ] [ v ]
        //
        // More robust version of Shepperd's method that solves for U3 directly to just propagate state, with a
        // fallback to bisection.  Then it solves for U5 with one additional continued fraction to generate U5
        // and the STM matrix.
        //
        public static (V3 rf, V3 vf, M3 stmRfR0, M3 stmRfV0, M3 stmVfR0, M3 stmVfV0) Solve2(double mu, double tau, V3 ri, V3 vi)

        {
            double tolerance = 1.0e-12;
            double u = 0;
            int imax = 50;
            double umax;
            double umin;
            double orbits = 0;
            double tdesired = tau;
            double threshold = tolerance * Math.Abs(tdesired);
            double r0 = ri.magnitude;
            double n0 = V3.Dot(ri, vi);

            double beta = 2.0 * (mu / r0) - vi.sqrMagnitude;

            if (beta != 0.0)
            {
                umax = 1.0 / Math.Sqrt(Math.Abs(beta));
            }
            else
            {
                umax = 1.0e24;
            }

            umin = -umax;

            double delu = 0.0;

            if (beta > 0.0)
            {
                orbits = beta * tau - 2 * n0;
                orbits = 1 + orbits * Math.Sqrt(beta) / (Math.PI * mu);
                orbits = Math.Floor(orbits / 2);
            }

            if (beta > 0.0)
            {
                double beta3 = beta * beta * beta;
                double p = 2.0 * Math.PI * mu * 1 / Math.Sqrt(beta3);
                double norb = Math.Truncate(1.0 / p * (tau + 0.5 * p - 2 * n0 / beta));
                delu = 2.0 * norb * Math.PI * 1 / Math.Sqrt(beta3 * beta * beta);
            }

            double uold = double.MinValue;
            double dtold = double.MinValue;
            double u0;
            double u1 = 0.0;
            double u2 = 0.0;
            double r1 = 0.0;
            double q = 0.0;

            double n, r, l, s, d, gcf, k, gold, h0, h1, u3;

            for (int i = 1; i < imax; i++)
            {
                q = beta * u * u;
                q /= 1.0 + q;

                n = 0;
                r = 1;
                l = 1;
                s = 1;
                d = 3;
                gcf = 1;
                k = -5;

                gold = 0;

                // this solves for U3 directly by using G(3, 0, 3/2, q)
                while (gcf != gold)
                {
                    k = -k;
                    l += 2;
                    d += 4 * l;
                    n += (1 + k) * l;
                    r = d / (d - n * r * q);
                    s = (r - 1) * s;
                    gold = gcf;
                    gcf = gold + s;
                }

                h0 = 1 - 2 * q;
                h1 = 2 * u * (1 - q);
                u0 = 2 * h0 * h0 - 1;
                u1 = 2 * h0 * h1;
                u2 = 2 * h1 * h1;
                u3 = 2 * h1 * u2 * gcf / 3;

                if (orbits != 0)
                {
                    u3 += 2 * Math.PI * orbits / (beta * Math.Sqrt(beta));
                }

                r1 = r0 * u0 + n0 * u1 + mu * u2;
                double dt = r0 * u1 + n0 * u2 + mu * u3;
                double slope = 4.0 * r1 * (1.0 - q);

                double terror = tdesired - dt;

                if (Math.Abs(terror) < threshold)
                    break;

                if (i > 1 && u == uold)
                    break;

                if (i > 1 && dt == dtold)
                    break;

                uold = u;
                dtold = dt;

                double ustep = terror / slope;
                if (ustep > 0)
                {
                    umin = u;
                    u += ustep;

                    if (u > umax)
                        u = (umin + umax) / 2;
                }
                else
                {
                    umax = u;
                    u += ustep;

                    if (u < umin)
                        u = (umin + umax) / 2;
                }
            }

            double fm = -mu * u2 / r0;
            double ggm = -mu * u2 / r1;

            double f = 1.0 + fm;
            double g = r0 * u1 + n0 * u2;
            double ff = -mu * u1 / (r0 * r1);
            double gg = 1.0 + ggm;

            V3 rf = f * ri + g * vi;
            V3 vf = ff * ri + gg * vi;

            n = 0;
            r = 1;
            l = 3;
            s = 1;
            d = 15;
            gcf = 1;
            k = -9;

            gold = 0;

            // this solves for U5 directly by using G(5, 0, 5/2, q)
            while (gcf != gold)
            {
                k = -k;
                l += 2;
                d += 4 * l;
                n += (1 + k) * l;
                r = d / (d - n * r * q);
                s = (r - 1) * s;
                gold = gcf;
                gcf = gold + s;
            }

            h0 = 1 - 2 * q;
            h1 = 2 * u * (1 - q);
            double uu = 16.0 / 15.0 * h1 * h1 * h1 * h1 * h1 * gcf + delu;
            u0 = 2 * h0 * h0 - 1;
            u1 = 2 * h0 * h1;
            u2 = 2 * h1 * h1;

            double w = g * u2 + 3 * mu * uu;
            double a0 = mu / (r0 * r0 * r0);
            double a1 = mu / (r1 * r1 * r1);

            var m = new M3(
                ff * (u0 / (r0 * r1) + 1.0 / (r0 * r0) + 1.0 / (r1 * r1)) - a0 * a1 * w,
                (ff * u1 + ggm / r1) / r1,
                ggm * u1 / r1 - a1 * w,
                -(ff * u1 + fm / r0) / r0,
                -ff * u2,
                -ggm * u2,
                fm * u1 / r0 - a0 * w,
                fm * u2,
                g * u2 - w
            );

            V3 t00 = rf * m[1, 0] + vf * m[2, 0];
            V3 t01 = rf * m[1, 1] + vf * m[2, 1];
            V3 t02 = rf * m[1, 2] + vf * m[2, 2];

            V3 t10 = rf * m[0, 0] + vf * m[1, 0];
            V3 t11 = rf * m[0, 1] + vf * m[1, 1];
            V3 t12 = rf * m[0, 2] + vf * m[1, 2];

            M3 stmRfR0 = V3.Outer(t00, ri) + V3.Outer(t01, vi) + M3.Diagonal(f);
            M3 stmRfV0 = V3.Outer(t01, ri) + V3.Outer(t02, vi) + M3.Diagonal(g);
            M3 stmVfR0 = -(V3.Outer(t10, ri) + V3.Outer(t11, vi)) + M3.Diagonal(ff);
            M3 stmVfV0 = -(V3.Outer(t11, ri) + V3.Outer(t12, vi)) + M3.Diagonal(gg);

            return (rf, vf, stmRfR0, stmRfV0, stmVfR0, stmVfV0);
        }
    }
}
