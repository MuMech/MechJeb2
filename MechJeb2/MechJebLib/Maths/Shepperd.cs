/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using MechJebLib.Primitives;

namespace MechJebLib.Maths
{
    public static class Shepperd
    {
        public static void Solve(double mu, double tau, V3 ri, V3 vi, out V3 rf, out V3 vf)
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
            double u0;
            double u1 = 0.0;
            double u2 = 0.0;
            double u3;
            double r1 = 0.0;

            double q, n, r, l, s, d, gcf, k, gold, dt, slope, terror, ustep, h0, h1;

            for (int i = 1; i < imax; i++)
            {
                q = beta * u * u;

                q = q / (1.0 + q);

                n   = 0;
                r   = 1;
                l   = 1;
                s   = 1;
                d   = 3;
                gcf = 1;
                k   = -5;

                gold = 0;

                while (gcf != gold)
                {
                    k    = -k;
                    l    = l + 2;
                    d    = d + 4 * l;
                    n    = n + (1 + k) * l;
                    r    = d / (d - n * r * q);
                    s    = (r - 1) * s;
                    gold = gcf;
                    gcf  = gold + s;
                }

                h0 = 1 - 2 * q;
                h1 = 2 * u * (1 - q);
                u0 = 2 * h0 * h0 - 1;
                u1 = 2 * h0 * h1;
                u2 = 2 * h1 * h1;
                u3 = 2 * h1 * u2 * gcf / 3;

                if (orbits != 0)
                {
                    u3 = u3 + 2 * Math.PI * orbits / (beta * Math.Sqrt(beta));
                }

                r1    = r0 * u0 + n0 * u1 + mu * u2;
                dt    = r0 * u1 + n0 * u2 + mu * u3;
                slope = 4 * r1 / (1 + beta * u * u);

                terror = tdesired - dt;

                if (Math.Abs(terror) < threshold)
                    break;

                if (i > 1 && u == uold)
                    break;

                if (i > 1 && dt == dtold)
                    break;

                uold  = u;
                dtold = dt;

                ustep = terror / slope;

                if (ustep > 0)
                {
                    umin = u;
                    u    = u + ustep;

                    if (u > umax)
                    {
                        u = (umin + umax) / 2;
                    }
                }
                else
                {
                    umax = u;
                    u    = u + ustep;

                    if (u < umin)
                    {
                        u = (umin + umax) / 2;
                    }
                }

                if (i == imax)
                {
                    // FIXME: throw
                }
            }

            double f = 1.0 - mu / r0 * u2;
            double gg = 1.0 - mu / r1 * u2;
            double g = r0 * u1 + n0 * u2;
            double ff = -mu * u1 / (r0 * r1);

            rf = new V3();
            vf = new V3();

            for (int i = 0; i < 3; i++)
            {
                rf[i] = f * ri[i] + g * vi[i];
                vf[i] = ff * ri[i] + gg * vi[i];
            }
        }

        // The STM is a 6x6 matrix which we return decomposed into 4 3x3 matricies
        //
        //  [ 𝛿r ] = [ stm00 stm01 ] [ r ]
        //  [ 𝛿v ] = [ stm10 stm11 ] [ v ]
        //
        // NOTE: is it not at all clear to me why this version differs in several ways from the prior version.
        //
        public static void Solve2(double mu, double tau, V3 ri, V3 vi, out V3 rf, out V3 vf, out M3 stm00, out M3 stm01, out M3 stm10, out M3 stm11)
        {
            double tol = 1.0e-12;
            double n0 = V3.Dot(ri, vi);
            double r0 = ri.magnitude;
            double beta = 2.0 * (mu / r0) - vi.sqrMagnitude;
            double u = 0;
            double umax = double.MaxValue;
            double umin = double.MinValue;
            stm00 = stm01 = stm10 = stm11 = M3.zero;

            if (beta != 0.0)
            {
                umax = 1.0 / Math.Sqrt(Math.Abs(beta));
            }
            else
            {
                umax = 1.0e24;
            }

            umin = -umax;

            double delu;

            if (beta <= 0.0)
            {
                delu = 0.0;
            }
            else
            {
                double beta3 = beta * beta * beta;
                double p = 2.0 * Math.PI * mu * 1 / Math.Sqrt(beta3);
                double norb = Math.Truncate(1.0 / p * (tau + 0.5 * p - 2 * n0 / beta));
                delu = 2.0 * norb * Math.PI * 1 / Math.Sqrt(beta3 * beta * beta);
            }

            double tsav = 1.0e99;

            int niter = 0;

            // kepler iteration loop

            double q, u0, u1, u2, u3, uu, usav, dtdu, du, n, l, d, k, a, b, gcf, gsav, r1, t, terr;

            while (true)
            {
                niter++;
                q = beta * u * u;
                q = q / (1.0 + q);

                u0 = 1.0 - 2 * q;
                u1 = 2.0 * u * (1 - q);

                // continued fraction iteration

                n   = 0;
                l   = 3;
                d   = 15;
                k   = -9;
                a   = 1;
                b   = 1;
                gcf = 1;

                while (true)
                {
                    gsav = gcf;

                    k   = -k;
                    l   = l + 2;
                    d   = d + 4 * l;
                    n   = n + (1 + k) * l;
                    a   = d / (d - n * a * q);
                    b   = (a - 1) * b;
                    gcf = gcf + b;

                    if (Math.Abs(gcf - gsav) < tol)
                        break;
                }

                uu   = 16.0 / 15.0 * u1 * u1 * u1 * u1 * u1 * gcf + delu;
                u2   = 2.0 * u1 * u1;
                u1   = 2.0 * u0 * u1;
                u0   = 2.0 * u0 * u0 - 1.0;
                u3   = beta * uu + u1 * u2 / 3.0;
                r1   = r0 * u0 + n0 * u1 + mu * u2;
                t    = r0 * u1 + n0 * u2 + mu * u3;
                dtdu = 4.0 * r1 * (1.0 - q);

                // check for time convergence
                if (Math.Abs(t - tsav) < tol)
                    break;


                usav = u;
                tsav = t;
                terr = tau - t;

                if (Math.Abs(terr) < Math.Abs(tau) * tol)
                    break;

                du = terr / dtdu;
                if (du < 0.0)
                {
                    umax = u;
                    u    = u + du;

                    if (u < umin)
                        u = 0.5 * (umin + umax);
                }
                else
                {
                    umin = u;
                    u    = u + du;

                    if (u > umax)
                        u = 0.5 * (umin + umax);
                }

                // check for independent variable convergence

                if (Math.Abs(u - usav) < tol)
                    break;


                // check for more than 20 iterations
                if (niter > 20)
                {
                    // FIXME: throw
                }
            }

            double fm = -mu * u2 / r0;
            double ggm = -mu * u2 / r1;

            double f = 1.0 + fm;
            double g = r0 * u1 + n0 * u2;
            double ff = -mu * u1 / (r0 * r1);
            double gg = 1.0 + ggm;

            // compute final state vector
            rf = f * ri + g * vi;
            vf = ff * ri + gg * vi;

            uu = g * u2 + 3 * mu * uu;
            double a0 = mu / (r0 * r0 * r0);
            double a1 = mu / (r1 * r1 * r1);

            M3 m = M3.zero;

            m[0, 0] = ff * (u0 / (r0 * r1) + 1.0 / (r0 * r0) + 1.0 / (r1 * r1));
            m[0, 1] = (ff * u1 + ggm / r1) / r1;
            m[0, 2] = ggm * u1 / r1;

            m[1, 0] = -(ff * u1 + fm / r0) / r0;
            m[1, 1] = -ff * u2;
            m[1, 2] = -ggm * u2;

            m[2, 0] = fm * u1 / r0;
            m[2, 1] = fm * u2;
            m[2, 2] = g * u2;

            m[0, 0] = m[0, 0] - a0 * a1 * uu;
            m[0, 2] = m[0, 2] - a1 * uu;
            m[2, 0] = m[2, 0] - a0 * uu;
            m[2, 2] = m[2, 2] - uu;

            for (int i = 0; i < 3; i++)
            {
                double t001 = rf[i] * m[1, 0] + vf[i] * m[1, 1];
                double t002 = rf[i] * m[2, 0] + vf[i] * m[2, 1];

                double t011 = rf[i] * m[1, 1] + vf[i] * m[1, 2];
                double t012 = rf[i] * m[2, 1] + vf[i] * m[2, 2];

                double t101 = rf[i] * m[0, 0] + vf[i] * m[0, 1];
                double t102 = rf[i] * m[1, 0] + vf[i] * m[1, 1];

                double t111 = rf[i] * m[0, 1] + vf[i] * m[0, 2];
                double t112 = rf[i] * m[1, 1] + vf[i] * m[1, 2];

                for (int j = 0; j < 3; j++)
                {
                    stm00[i, j] = t001 * ri[j] + t002 * vi[j];
                    stm01[i, j] = t011 * ri[j] + t012 * vi[j];
                    stm10[i, j] = -t101 * ri[j] + t102 * vi[j];
                    stm11[i, j] = -t111 * ri[j] + t112 * vi[j];
                }
            }

            for (int i = 0; i < 3; i++)
            {
                stm00[i, i] = stm00[i, i] + f;
                stm01[i, i] = stm01[i, i] + g;
                stm10[i, i] = stm10[i, i] + ff;
                stm11[i, i] = stm11[i, i] + gg;
            }
        }

        /*
        public static void Test()
        {
            V3 ri = new V3(1.31623307896919, 1.14467537127903, 2.29655036444701);
            V3 vi = new V3(2.38559970341119, 0.560617813663136, 1.46929318736469);
            V3 rf = V3.zero;
            V3 vf = V3.zero;
            M3 stm00 = M3.zero;
            M3 stm01 = M3.zero;
            M3 stm10 = M3.zero;
            M3 stm11 = M3.zero;
            Solve2(0.950222048838355, 0.0344460805029088, ri, vi, ref rf, ref vf, ref stm00, ref stm01, ref stm10, ref stm11);
            Debug.Log("rf = " + rf);
            Debug.Log("vf = " + vf);
            Debug.Log("stm00 = " + stm00);
            Debug.Log("stm01 = " + stm01);
            Debug.Log("stm10 = " + stm10);
            Debug.Log("stm11 = " + stm11);
        }
        */
    }
}
