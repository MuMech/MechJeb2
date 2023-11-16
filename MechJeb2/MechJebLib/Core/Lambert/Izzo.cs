/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Runtime.CompilerServices;
using MechJebLib.Primitives;
using static System.Math;
using static MechJebLib.Statics;

#nullable enable

namespace MechJebLib.Core.Lambert
{
    /// <summary>
    ///     This is Izzo's method, copied from:
    ///     https://github.com/esa/pykep/blob/8b0e9444d09b909d7d1d11e951c8efcfde0a2ffd/src/lambert_problem.cpp
    ///     I made some changes to the API, largely around making it conform to the Gooding method API that we had
    ///     previously and not using the clockwise/counterclockwise API so it doesn't have that weird singularity
    ///     for perfectly polar orbits.
    ///     This implementation isn't any faster than the Gooding implementation in this repo for nrev=0.
    /// </summary>
    public class Izzo
    {
        public (V3 Vi, V3 Vf) Solve(double mu, V3 r1, V3 v1, V3 r2, double tof, int nrev)
        {
            lambert_problem2(r1, v1, r2, tof, mu, nrev);

            if (_nsol == 0)
                return (_v1[0], _v2[0]);

            double mindv = double.PositiveInfinity;
            int index = 0;

            for (int i = 0; i < _nsol; i++)
            {
                double dv = (_v1[i] - v1).sqrMagnitude;
                if (dv >= mindv)
                    continue;

                index = i;
                mindv = dv;
            }

            return (_v1[index], _v2[index]);
        }

        private int      _nsol;
        private V3[]     _v1    = new V3[5];
        private V3[]     _v2    = new V3[5];
        private int[]    _iters = new int[5];
        private double[] _x     = new double[5];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void lambert_problem2(V3 r1, V3 v1, V3 r2, double tof, double mu, int nrev)
        {
            V3 hhat = V3.Cross(r1, v1).normalized;
            V3 zhat = V3.Cross(r1.normalized, r2.normalized).normalized;

            /* if angle to orbit normal is greater than 90 degrees, then flip the orbit normal */
            bool flip = V3.Dot(hhat, zhat) < 0;

            lambert_problem(r1, r2, tof, mu, nrev, flip);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void lambert_problem(V3 r1, V3 r2, double tof, double mu, int nrev, bool flip = false)
        {
            // 0 - Sanity checks
            if (tof <= 0)
                throw new Exception("Time of flight is negative!");

            if (mu <= 0)
                throw new Exception("Gravity parameter is zero or negative!");

            // 1 - Getting lambda and T
            double c = (r2 - r1).magnitude;
            double r1M = r1.magnitude;
            double r2M = r2.magnitude;
            double s = (c + r1M + r2M) / 2.0;
            V3 ir1 = r1.normalized;
            V3 ir2 = r2.normalized;
            V3 ih = V3.Cross(ir1, ir2).normalized;

            double lambda2 = 1.0 - c / s;
            double lambda = Sqrt(lambda2);

            var it1 = V3.Cross(ih, ir1);
            var it2 = V3.Cross(ih, ir2);

            it1 = it1.normalized;
            it2 = it2.normalized;

            if (flip)
            {
                // Retrograde motion
                lambda = -lambda;
                it1[0] = -it1[0];
                it1[1] = -it1[1];
                it1[2] = -it1[2];
                it2[0] = -it2[0];
                it2[1] = -it2[1];
                it2[2] = -it2[2];
            }

            double lambda3 = lambda * lambda2;
            double t = Sqrt(2.0 * mu / s / s / s) * tof;

            // 2 - We now have lambda, T and we will find all x
            // 2.1 - Let us first detect the maximum number of revolutions for which there exists a solution
            int nmax = (int)(t / PI);
            double t00 = Acos(lambda) + lambda * Sqrt(1.0 - lambda2);
            double t0 = t00 + nmax * PI;
            double t1 = 2.0 / 3.0 * (1.0 - lambda3);
            if (nmax > 0)
            {
                if (t < t0)
                {
                    // We use Halley iterations to find xM and TM
                    int it = 0;
                    double tMin = t0;
                    double xOld = 0.0, xNew = 0.0;
                    while (true)
                    {
                        (double dt, double ddt, double dddt) = DTdx(lambda, xOld, tMin);
                        if (dt != 0.0)
                            xNew = xOld - dt * ddt / (ddt * ddt - dt * dddt / 2.0);

                        double err = Abs(xOld - xNew);
                        if (err < 1e-13 || it > 12)
                            break;

                        tMin = X2Tof(lambda, xNew, nmax);
                        xOld = xNew;
                        it++;
                    }

                    if (tMin > t)
                        nmax -= 1;
                }
            }

            // We exit this if clause with Nmax being the maximum number of revolutions
            // for which there exists a solution. We crop it to m_multi_revs
            nmax  = Min(nrev, nmax);
            _nsol = nmax * 2 + 1;

            // 2.2 We now allocate the memory for the output variables
            if (_nsol > _x.Length)
            {
                _v1    = new V3[nmax * 2 + 1];
                _v2    = new V3[nmax * 2 + 1];
                _iters = new int[nmax * 2 + 1];
                _x     = new double[nmax * 2 + 1];
            }

            // 3 - We may now find all solutions in x,y
            // 3.1 0 rev solution
            // 3.1.1 initial guess
            if (t >= t00)
                _x[0] = -(t - t00) / (t - t00 + 4);
            else if (t <= t1)
                _x[0] = t1 * (t1 - t) / (2.0 / 5.0 * (1 - lambda2 * lambda3) * t) + 1;
            else
                _x[0] = Pow(t / t00, 0.69314718055994529 / Log(t1 / t00)) - 1.0;

            // 3.1.2 Householder iterations
            _iters[0] = Householder(lambda, t, ref _x[0], 0, 1e-5, 15);
            // 3.2 multi rev solutions
            for (int i = 1; i < nmax + 1; ++i)
            {
                // 3.2.1 left Householder iterations
                double tmp = Pow((i * PI + PI) / (8.0 * t), 2.0 / 3.0);
                _x[2 * i - 1]     = (tmp - 1) / (tmp + 1);
                _iters[2 * i - 1] = Householder(lambda, t, ref _x[2 * i - 1], i, 1e-8, 15);
                // 3.2.1 right Householder iterations
                tmp           = Pow(8.0 * t / (i * PI), 2.0 / 3.0);
                _x[2 * i]     = (tmp - 1) / (tmp + 1);
                _iters[2 * i] = Householder(lambda, t, ref _x[2 * i], i, 1e-8, 15);
            }

            // 4 - For each found x value we reconstruct the terminal velocities
            double gamma = Sqrt(mu * s / 2.0);
            double rho = (r1M - r2M) / c;
            double sigma = Sqrt(1 - rho * rho);
            for (int i = 0; i < _nsol; ++i)
            {
                double y = Sqrt(1.0 - lambda2 + lambda2 * _x[i] * _x[i]);
                double vr1 = gamma * (lambda * y - _x[i] - rho * (lambda * y + _x[i])) / r1M;
                double vr2 = -gamma * (lambda * y - _x[i] + rho * (lambda * y + _x[i])) / r2M;
                double vt = gamma * sigma * (y + lambda * _x[i]);
                double vt1 = vt / r1M;
                double vt2 = vt / r2M;
                for (int j = 0; j < 3; ++j)
                    _v1[i][j] = vr1 * ir1[j] + vt1 * it1[j];
                for (int j = 0; j < 3; ++j)
                    _v2[i][j] = vr2 * ir2[j] + vt2 * it2[j];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Householder(double lambda, double t, ref double x0, int n, double eps, int maxIter)
        {
            int it = 0;
            double err = 1.0;
            while (err > eps && it < maxIter)
            {
                double tof = X2Tof(lambda, x0, n);
                (double dt, double ddt, double dddt) = DTdx(lambda, x0, tof);
                double delta = tof - t;
                double dt2 = dt * dt;
                double xnew = x0 - delta * (dt2 - delta * ddt / 2.0) / (dt * (dt2 - delta * ddt) + dddt * delta * delta / 6.0);
                err = Abs(x0 - xnew);
                x0  = xnew;
                it++;
            }

            return it;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ( double DT, double DDT, double DDDT) DTdx(double lambda, double x, double t)
        {
            double l2 = lambda * lambda;
            double l3 = l2 * lambda;
            double umx2 = 1.0 - x * x;
            double y = Sqrt(1.0 - l2 * umx2);
            double y2 = y * y;
            double y3 = y2 * y;
            double dt = 1.0 / umx2 * (3.0 * t * x - 2.0 + 2.0 * l3 * x / y);
            double ddt = 1.0 / umx2 * (3.0 * t + 5.0 * x * dt + 2.0 * (1.0 - l2) * l3 / y3);
            double dddt = 1.0 / umx2 * (7.0 * x * ddt + 8.0 * dt - 6.0 * (1.0 - l2) * l2 * l3 * x / y3 / y2);
            return (dt, ddt, dddt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double X2Tof2(double lambda, double x, int n)
        {
            double a = 1.0 / (1.0 - x * x);
            if (a > 0) // ellipse
            {
                double alfa = 2.0 * Acos(x);
                double beta = 2.0 * Asin(Sqrt(lambda * lambda / a));
                if (lambda < 0.0) beta = -beta;
                return a * Sqrt(a) * (alfa - Sin(alfa) - (beta - Sin(beta)) + 2.0 * PI * n) / 2.0;
            }
            else
            {
                double alfa = 2.0 * Acosh(x);
                double beta = 2.0 * Asinh(Sqrt(-lambda * lambda / a));
                if (lambda < 0.0) beta = -beta;
                return -a * Sqrt(-a) * (beta - Sinh(beta) - (alfa - Sinh(alfa))) / 2.0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double X2Tof(double lambda, double x, int n)
        {
            const double BATTIN = 0.01;
            const double LAGRANGE = 0.2;
            double dist = Abs(x - 1);
            if (dist < LAGRANGE && dist > BATTIN)
                // We use Lagrange tof expression
                return X2Tof2(lambda, x, n);

            double k = lambda * lambda;
            double e = x * x - 1.0;
            double rho = Abs(e);
            double z = Sqrt(1 + k * e);
            if (dist < BATTIN)
            {
                // We use Battin series tof expression
                double eta = z - lambda * x;
                double s1 = 0.5 * (1.0 - lambda - x * eta);
                double q = HypergeometricF(s1, 1e-11);
                q = 4.0 / 3.0 * q;
                return (eta * eta * eta * q + 4.0 * lambda * eta) / 2.0 + n * PI / Pow(rho, 1.5);
            }

            // We use Lancaster tof expresion
            double y = Sqrt(rho);
            double g = x * z - lambda * e;
            double d;
            if (e < 0)
            {
                double l = Acos(g);
                d = n * PI + l;
            }
            else
            {
                double f = y * (z - lambda * x);
                d = Log(f + g);
            }

            return (x - lambda * z - d / y) / e;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double HypergeometricF(double z, double tol)
        {
            double sj = 1.0;
            double cj = 1.0;
            double err = 1.0;
            int j = 0;
            while (err > tol)
            {
                double cj1 = cj * (3.0 + j) * (1.0 + j) / (2.5 + j) * z / (j + 1);
                double sj1 = sj + cj1;
                err =  Abs(cj1);
                sj  =  sj1;
                cj  =  cj1;
                j   += 1;
            }

            return sj;
        }
    }
}
