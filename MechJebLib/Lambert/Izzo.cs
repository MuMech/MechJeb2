/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Runtime.CompilerServices;
using MechJebLib.Lambert;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.Statics;

/*
 * Izzo's algorithm for solving Lambert's problem.
 *
 * Ported to C# from poliastro (MIT License), poliastro/core/iod.py
 * (commit 21fd7719e89a7d22b4eac63141a60a7f1b01768c), which itself implements:
 *
 *   Izzo, D. "Revisiting Lambert's problem." Celestial Mechanics and
 *   Dynamical Astronomy 121.1 (2015): 1-15.
 */

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Maths
{
    /// <summary>
    ///     Solves Lambert's problem using Izzo's algorithm (Householder iteration on the
    ///     non-dimensional time-of-flight equation).
    /// </summary>
    public static class Izzo
    {
        /// <summary>
        ///     Applies Izzo's algorithm to solve Lambert's problem.
        /// </summary>
        /// <param name="mu">Gravitational parameter (mu)</param>
        /// <param name="r1">Initial position vector</param>
        /// <param name="r2">Final position vector</param>
        /// <param name="tof">Time of flight between both positions</param>
        /// <param name="direction">Which of the two Lambert arcs to solve for (see <see cref="TransferGeometry" />)</param>
        /// <param name="nrev">Number of full revolutions (+ long-period, - short-period for nrev != 0)</param>
        /// <param name="h">Axis to use for prograde/retrograde (does not need to be normalized)</param>
        /// <param name="numiter">Maximum number of iterations</param>
        /// <param name="rtol">Error tolerance</param>
        /// <returns>The initial (v1) and final (v2) velocity vectors</returns>
        public static (V3 v1, V3 v2) Solve(double mu, V3 r1, V3 r2, double tof,
            TransferGeometry direction = TransferGeometry.ShortWay, int nrev = 0, V3? h = null,
            int numiter = 35, double rtol = 1e-8)
        {
            // Check preconditions
            Check.PositiveFinite(tof);
            Check.PositiveFinite(mu);

            // Check collinearity of r1 and r2
            if (V3.Cross(r1, r2) == V3.zero)
                throw new ArgumentException("Lambert solution cannot be computed for collinear vectors");

            // negative directions flip the path of the multi-revolution case
            bool lowpath = nrev >= 0;
            nrev = Abs(nrev);

            // Chord
            V3     c      = r2 - r1;
            double cNorm  = c.magnitude;
            double r1Norm = r1.magnitude;
            double r2Norm = r2.magnitude;

            // Semiperimeter
            double s = (r1Norm + r2Norm + cNorm) * 0.5;

            // Versors
            V3 iR1 = r1 / r1Norm;
            V3 iR2 = r2 / r2Norm;
            V3 iH  = V3.Cross(iR1, iR2).safeNormalized;

            // Geometry of the problem
            double ll = Sqrt(1 - Min(1.0, cNorm / s));

            bool flip = direction == TransferGeometry.LongWay || direction == TransferGeometry.Retrograde;
            if (direction == TransferGeometry.Prograde || direction == TransferGeometry.Retrograde)
            {
                if (h == null)
                    throw new Exception("Prograde or Retrograde directions require a normal vector");
                flip ^= V3.Dot(iH, h.Value) < 0;
            }

            // Compute the fundamental tangential directions
            V3 iT1, iT2;
            if (flip)
            {
                ll = -ll;
                iT1 = V3.Cross(iR1, iH);
                iT2 = V3.Cross(iR2, iH);
            }
            else
            {
                iT1 = V3.Cross(iH, iR1);
                iT2 = V3.Cross(iH, iR2);
            }

            // Non-dimensional time of flight
            double t = Sqrt(2 * mu / (s * s * s)) * tof;

            // Find solutions
            (double x, double y) = FindXY(ll, t, nrev, numiter, lowpath, rtol);

            // Reconstruct
            double gamma = Sqrt(mu * s / 2);
            double rho   = (r1Norm - r2Norm) / cNorm;
            double sigma = Sqrt(1 - rho * rho);

            // Compute the radial and tangential components at r1 and r2
            (double vr1, double vr2, double vt1, double vt2) = Reconstruct(x, y, r1Norm, r2Norm, ll, gamma, rho, sigma);

            // Solve for the initial and final velocity
            V3 v1 = vr1 * (r1 / r1Norm) + vt1 * iT1;
            V3 v2 = vr2 * (r2 / r2Norm) + vt2 * iT2;

            return (v1, v2);
        }

        public static (DualV3 v1, DualV3 v2) Solve(double mu, DualV3 r1, DualV3 r2, Dual tof,
            TransferGeometry direction = TransferGeometry.ShortWay, int nrev = 0, V3? h = null,
            int numiter = 35, double rtol = 1e-8)
        {
            (V3 v1M, V3 v2M) = Solve(mu, r1.M, r2.M, tof.M, direction, nrev, h, numiter, rtol);
            (V3 _, V3 _, M3 stmRfR0, M3 stmRfV0, M3 stmVfR0, M3 stmVfV0) = Shepperd.Solve2(mu, tof.M, r1.M, v1M);

            M3 stmRfV0Inv = stmRfV0.inverse;
            M3 stmRfV0InvRfR0 = stmRfV0Inv * stmRfR0;

            M3 v1R1 = -stmRfV0InvRfR0;
            M3 v1R2 = stmRfV0Inv;
            M3 v2R1 = stmVfR0 - stmVfV0 * stmRfV0InvRfR0;
            M3 v2R2 = stmVfV0 * stmRfV0Inv;

            V3 v1Tof = -stmRfV0Inv * v2M;
            V3 a2 = -mu / (r2.M.magnitude * r2.M.sqrMagnitude) * r2.M;
            V3 v2Tof = a2 - stmVfV0 * (stmRfV0Inv * v2M);

            var v1 = new DualV3(v1M, v1R1 * r1.D + v1R2 * r2.D + v1Tof * tof.D);
            var v2 = new DualV3(v2M, v2R1 * r1.D + v2R2 * r2.D + v2Tof * tof.D);

            return (v1, v2);
        }

        /// <summary>
        ///     Reconstruct solution velocity components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ( double Vr1, double Vr2, double Vt1, double Vt2) Reconstruct(double x, double y, double r1, double r2, double ll, double gamma, double rho,
            double sigma)
        {
            double vr1 = gamma * (ll * y - x - rho * (ll * y + x)) / r1;
            double vr2 = -gamma * (ll * y - x + rho * (ll * y + x)) / r2;
            double vt1 = gamma * sigma * (y + ll * x) / r1;
            double vt2 = gamma * sigma * (y + ll * x) / r2;
            return (vr1, vr2, vt1, vt2);
        }

        /// <summary>
        ///     Computes x, y for the given number of revolutions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double x, double y) FindXY(double ll, double t, int m, int numiter, bool lowpath, double rtol)
        {
            // For abs(ll) == 1 the derivative is not continuous
            Check.True(Abs(ll) < 1);
            Check.True(t > 0); // Mistake in the original paper

            // NOTE: we dropped the determination of mMax and prefer to just let
            // the householder iteration fail to converge if the caller asks for
            // something infeasible.  The poliastro implementation of ComputeTMin
            // is also buggy.

            // Initial guess
            double x0 = InitialGuess(t, ll, m, lowpath);

            // Start Householder iterations from x0 and find x, y
            double x = Householder(x0, t, ll, m, rtol, numiter);
            double y = Sqrt(1 - ll * ll * (1 - x * x));
            return (x, y);
        }

        /// <summary>
        ///     Computes psi, the auxiliary angle (Eq. 17), via the appropriate inverse function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ComputePsi(double x, double y, double ll)
        {
            if (x >= -1 && x < 1)
                // Elliptic motion. Use arc cosine to avoid numerical errors.
                return SafeAcos(x * y + ll * (1 - x * x));

            if (x > 1)
                // Hyperbolic motion. The hyperbolic sine is bijective.
                return Asinh((y - x * ll) * Sqrt(x * x - 1));

            // Parabolic motion
            return 0.0;
        }

        /// <summary>
        ///     Time-of-flight equation with externally computed y.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double TofEquationY(double x, double y, double t0, double ll, int m)
        {
            double t;
            if (m == 0 && Sqrt(0.6) < x && x < Sqrt(1.4))
            {
                double eta = y - ll * x;
                double s1  = (1 - ll - x * eta) * 0.5;
                double q   = 4.0 / 3.0 * Hyp2F1B(s1);
                t = (eta * eta * eta * q + 4 * ll * eta) * 0.5;
            }
            else
            {
                double psi = ComputePsi(x, y, ll);
                t = ((psi + m * PI) / Sqrt(Abs(1 - x * x)) - x + ll * y) / (1 - x * x);
            }

            return t - t0;
        }

        /// <summary>
        ///     Initial guess.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double InitialGuess(double t, double ll, int m, bool lowpath)
        {
            // NOTE: this is the m == 0 initial guess algorithm from pykep, not from the
            // paper, or from poliastro.
            // See https://github.com/esa/pykep/commit/fef5f271984f34c390b4fb85f0781bedb737702c
            if (m == 0)
            {
                double ll2 = ll * ll;
                double ll3 = ll * ll2;

                // Single revolution
                double t0 = SafeAcos(ll) + ll * Sqrt(1 - ll2); // Equation 19
                double t1 = 2 * (1 - ll3) / 3.0;               // Equation 21

                if (t >= t0)
                    return -(t - t0) / (t - t0 + 4);
                if (t < t1)
                    return t1 * (t1 - t) / (2.0 / 5.0 * (1 - ll2 * ll3) * t) + 1;

                // constant is just Log(2)
                return Pow(t / t0, 0.69314718055994529 / Log(t1 / t0)) - 1.0;
            }

            // we change the meaning of lowpath compared to poliastro here out of smoothness concerns
            if (lowpath)
            {
                double r   = Pow(8 * t / (m * PI), 2.0 / 3.0);
                double x0R = (r - 1) / (r + 1);
                return x0R;
            }

            double l   = Pow((m * PI + PI) / (8 * t), 2.0 / 3.0);
            double x0L = (l - 1) / (l + 1);
            return x0L;
        }

        /// <summary>
        ///     Find a zero of the time-of-flight equation using the Householder method.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Householder(double p0, double t0, double ll, int m, double tol, int maxiter)
        {
            for (int ii = 0; ii < maxiter; ii++)
            {
                double y     = Sqrt(1 - ll * ll * (1 - p0 * p0));
                double fval  = TofEquationY(p0, y, t0, ll, m);
                double t     = fval + t0;
                double fder  = (3 * t * p0 - 2 + 2 * ll * ll * ll * p0 / y) / (1 - p0 * p0);
                double fder2 = (3 * t + 5 * p0 * fder + 2 * (1 - ll * ll) * ll * ll * ll / (y * y * y)) / (1 - p0 * p0);
                double fder3 = (7 * p0 * fder2 + 8 * fder - 6 * (1 - ll * ll) * Powi(ll, 5) * p0 / Powi(y, 5)) / (1 - p0 * p0);

                // Householder step (quartic)
                double p = p0 - fval * ((fder * fder - fval * fder2 / 2)
                    / (fder * (fder * fder - fval * fder2) + fder3 * fval * fval / 6));

                if (Abs(p - p0) < tol)
                    return p;
                p0 = p;
            }

            throw new Exception("Failed to converge");
        }

        /// <summary>
        ///     Hypergeometric function 2F1(3, 1; 5/2; x), see Battin. Ported from
        ///     poliastro/_math/special.py.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Hyp2F1B(double x)
        {
            if (x >= 1.0)
                return double.PositiveInfinity;

            double res  = 1.0;
            double term = 1.0;
            int    ii   = 0;

            while (true)
            {
                term = term * (3 + ii) * (1 + ii) / (2.5 + ii) * x / (ii + 1);
                double resOld = res;
                res += term;
                if (resOld == res)
                    return res;
                ii += 1;
            }
        }
    }
}
