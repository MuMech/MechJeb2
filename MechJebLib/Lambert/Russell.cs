/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Runtime.CompilerServices;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

/*
 * Russell's vercosine algorithm for solving Lambert's problem.
 *
 * Ported to C# from Lambert.jl (MIT License), src/russell_solver.jl
 * (commit e05ff7e573902ad4dd7b995655c367cb6dd6a9ff), which itself implements:
 *
 *   Russell, R. P. "On the Solution to Every Lambert Problem." Celestial Mechanics
 *   and Dynamical Astronomy 131, Article 50 (2019).
 *
 *   Russell, R. P. "Complete Lambert Solver Including Second-Order Sensitivities."
 *   Journal of Guidance, Control, and Dynamics 45.2 (2022): 196-212.
 *
 * Russell's ivLam was only used to identify constants and as a numerical source of
 * truth.  Unlike ivLam this does not use the interpolated initial guess tables, so
 * the initial guesses come from Lambert.jl (Arora & Russell 2013 for the zero-rev
 * case).  The following changes were made to the Lambert.jl algorithm:
 *
 * - The convergence test is only applied after a full third order correction, like
 *   ivLam.  Lambert.jl applies it after safeguarded steps where the third order term
 *   is zeroed and declares convergence on the first iteration.
 * - For multi-rev, ivLam's small (2e-4) trust region on the step in k was tuned against
 *   the interpolated initial guess.  It is replaced by bracketing the requested branch
 *   of the time of flight curve at k_bottom, which is solved for with a safeguarded
 *   Newton iteration.
 * - The iteration also stops once the time of flight matches to within roundoff, which
 *   is needed for convergence very close to the double root at k_bottom.  Unlike ivLam
 *   this allows solutions arbitrarily close to the minimum multi-rev time of flight
 *   (Russell 2022 limits it to 1e-7 N above the minimum in units of S).
 * - The check that the iteration is not stuck on a steep cliff compares the residual to
 *   the time of flight, instead of max(1, time of flight).
 * - Fixed the k^7 coefficient in the series for W near k = 0 (Eq. 32 of Russell 2019), and
 *   the second and third derivatives in the series for large k (Algorithm 1 of Russell
 *   2019), which are wrong in Lambert.jl (ivLam has the same error in the series for the
 *   second derivative).
 * - Added the domain limits of Russell (2022): the geometry must not be too close to the
 *   r1 == r2 singularity, and the zero-rev time of flight must be at least 1e-3 times the
 *   parabolic time of flight.  Solutions outside these limits are inaccurate.
 *
 * The first-order partial derivatives of the velocities with respect to the positions and
 * time of flight are computed by differentiating the converged root-solve of Lambert's
 * equation (Russell 2022, Eqs. 16 and 17), using derivatives of the vercosine
 * formulation (Russell 2019, Eqs. 25-29 and Algorithm 2) derived for this port.
 */

// ReSharper disable InconsistentNaming
// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Lambert
{
    /// <summary>
    ///     Solves Lambert's problem using Russell's vercosine formulation, iterating on k with
    ///     up to third order corrections.  The formulation has no singularities other than
    ///     r1 == r2 (and the undefined transfer plane of the exact half-rev).
    /// </summary>
    public static class Russell
    {
        // Precision thresholds (ivLamThresh_*)
        private const double ZERO_BAND      = 0.02;   // ivLamThresh_zeroBand
        private const double PARABOLA_BAND  = 0.02;   // ivLamThresh_parabolaBand
        private const double BIG_K          = 1000.0; // ivLamThresh_bigKiterate
        private const double LITTLE_P       = 0.1;    // ivLamThresh_littlePiterate
        private const double HUGE_TOF       = 1e4;    // ivLamThresh_TofByShugeBoundary
        private const double ALT_TAU_THRESH = 1e-2;   // ivLamThresh_alternateTau
        private const double K_CLOSE_MSQRT2 = 1e-2;   // ivLamThresh_kClosetoMsqrt2

        // Solver safeguards (ivLamSafegrd_*)
        private const double SERIES_CONVERGE    = 1.01;       // ivLamSafegrd_seriesConvergeThresh
        private const double SERIES_TAMP        = 0.75;       // ivLamSafegrd_seriesConvergeTamp
        private const double SERIES_TAMP_THRESH = 2e-7;       // ivLamSafegrd_seriesConvergeTampThresh
        private const double BOTTOM_BUMP        = 2e-4 * 0.5; // ivLamSafegrd_maxkStepMrev * ivLamSafegrd_botBumpMult
        private const double K_BUMP_INIT        = 7e-7;       // ivLamSafegrd_kBumpInit
        private const double K_BUMP_WEIGHT      = 0.925;      // ivLamSafegrd_kBumpWeightVio
        private const double K_MARGIN           = 50.0 * EPS; // ivLamSafegrd_kmarginEpsMult

        // relative TOF residual which is considered roundoff (not from ivLam)
        private const double TOF_ROUNDOFF = 8.0 * EPS;

        // Domain limits (Russell 2022, Algorithms 1 and 3)
        private const double TAU_MARGIN      = 1e-7; // minimum distance of |tau| from sqrt(2)/2, which is the r1 == r2 singularity
        private const double MIN_TOF_OVER_TP = 1e-3; // minimum zero-rev time of flight as a fraction of the parabolic time of flight

        // sqrt of the smallest normal double
        private const double SQRT_TINY = 1.4916681462400413e-154;

        /// <summary>
        ///     Applies Russell's algorithm to solve Lambert's problem.
        /// </summary>
        /// <param name="mu">Gravitational parameter (mu)</param>
        /// <param name="r1">Initial position vector</param>
        /// <param name="r2">Final position vector</param>
        /// <param name="tof">Time of flight between both positions, must be positive (and at least 1e-3 times the parabolic time for n = 0)</param>
        /// <param name="direction">1 for the short way (transfer angle in [0, pi]), -1 for the long way (transfer angle in [pi, 2pi])</param>
        /// <param name="n">Number of full revolutions (+ long-period, - short-period for n != 0)</param>
        /// <param name="numiter">Maximum number of iterations</param>
        /// <returns>The initial (v1) and final (v2) velocity vectors</returns>
        public static (V3 v1, V3 v2) Solve(double mu, V3 r1, V3 r2, double tof, int direction = 1, int n = 0, int numiter = 50)
        {
            (V3 v1, V3 v2, _, _, _) = SolveWithState(mu, r1, r2, tof, direction, n, numiter);
            return (v1, v2);
        }

        /// <summary>
        ///     Applies Russell's algorithm to solve Lambert's problem, including the first-order partial derivatives of the
        ///     velocities with respect to the positions and time of flight.  The derivatives are singular at the minimum time
        ///     of flight for multi-rev transfers and at the half-rev, and lose precision close to them.
        /// </summary>
        /// <param name="mu">Gravitational parameter (mu)</param>
        /// <param name="r1">Initial position vector</param>
        /// <param name="r2">Final position vector</param>
        /// <param name="tof">Time of flight between both positions, must be positive (and at least 1e-3 times the parabolic time for n = 0)</param>
        /// <param name="direction">1 for the short way (transfer angle in [0, pi]), -1 for the long way (transfer angle in [pi, 2pi])</param>
        /// <param name="n">Number of full revolutions (+ long-period, - short-period for n != 0)</param>
        /// <param name="numiter">Maximum number of iterations</param>
        /// <returns>The initial (v1) and final (v2) velocity vectors, with derivatives along the dual parts of the inputs</returns>
        public static (DualV3 v1, DualV3 v2) Solve(double mu, DualV3 r1, DualV3 r2, Dual tof, int direction = 1, int n = 0, int numiter = 50)
        {
            (V3 v1, V3 v2, double k, double p, double tau) = SolveWithState(mu, r1.M, r2.M, tof.M, direction, n, numiter);

            // derivatives of the geometry: sigma = r1 + r2, S = sqrt(sigma^3 / mu) and tau = d sqrt(q) / sigma where
            // q = r1 r2 (1 + cos(theta)) = r1 r2 + r1vec . r2vec.
            double r1m = r1.M.magnitude;
            double r2m = r2.M.magnitude;
            double dr1m = V3.Dot(r1.M, r1.D) / r1m;
            double dr2m = V3.Dot(r2.M, r2.D) / r2m;
            double sigma = r1m + r2m;
            double dsigma = dr1m + dr2m;
            double S = sigma * Sqrt(sigma / mu);
            double dS = 1.5 * S * dsigma / sigma;
            double dq = dr1m * r2m + r1m * dr2m + V3.Dot(r1.D, r2.M) + V3.Dot(r1.M, r2.D);
            double dtau = dq / (2.0 * sigma * sigma * tau) - tau * dsigma / sigma;

            // implicit derivative of the converged k from Lambert's equation F = S L(k, tau) - tof = 0 (Russell 2022, Eq. 16)
            (double L, double Lk, double Ltau) = TofPartials(k, p, tau, Abs(n));
            double dk = -(S * Ltau * dtau + L * dS - tof.D) / (S * Lk);
            double dp = -tau * dk - k * dtau;

            // derivatives of the velocities from the Lagrange coefficients (Russell 2019, Algorithm 2 and Russell 2022, Eq. 17)
            double a1 = sigma / r1m;
            double a2 = sigma / r2m;
            double da1 = (dsigma - a1 * dr1m) / r1m;
            double da2 = (dsigma - a2 * dr2m) / r2m;
            double f = 1.0 - p * a1;
            double gdot = 1.0 - p * a2;
            double df = -dp * a1 - p * da1;
            double dgdot = -dp * a2 - p * da2;
            double sqrtp = Sqrt(p);
            double g = S * tau * sqrtp;
            double dg = (dS * tau + S * dtau) * sqrtp + S * tau * dp / (2.0 * sqrtp);

            V3 dv1 = (r2.D - df * r1.M - f * r1.D - dg * v1) / g;
            V3 dv2 = (dgdot * r2.M + gdot * r2.D - r1.D - dg * v2) / g;

            return (new DualV3(v1, dv1), new DualV3(v2, dv2));
        }

        /// <summary>
        ///     Solves Lambert's problem, and returns the converged iteration variable k, p = 1 - k tau and tau along with
        ///     the velocities.
        /// </summary>
        private static (V3 v1, V3 v2, double k, double p, double tau) SolveWithState(double mu, V3 r1, V3 r2, double tof, int direction, int n,
            int numiter)
        {
            Check.PositiveFinite(mu);
            Check.PositiveFinite(tof);
            Check.NonZeroFinite(r1);
            Check.NonZeroFinite(r2);

            if (direction != 1 && direction != -1)
                throw new ArgumentException("Russell's Lambert solver requires a direction of 1 (short way) or -1 (long way)");

            int nabs = Abs(n);
            bool zeroRev = n == 0;

            // Normalize to canonical units (mu = 1, |r1| = 1)
            double lRef = r1.magnitude;
            double tRef = Sqrt(lRef * lRef * lRef / mu);
            V3 r1Hat = r1 / lRef;
            V3 r2Hat = r2 / lRef;
            double r2Norm = r2Hat.magnitude;
            double tofHat = tof / tRef;

            // Geometry
            double r1pr2 = 1.0 + r2Norm;
            double r1r2 = r2Norm;
            double ctheta = Clamp(V3.Dot(r1Hat, r2Hat) / r2Norm, -1.0, 1.0);
            double onePctheta = ctheta + 1.0;
            double oneMctheta = 1.0 - ctheta;

            // Alternative form of tau for precision when theta is close to pi
            double abstau;
            if (onePctheta < ALT_TAU_THRESH)
            {
                double sthetar1r2 = V3.Cross(r1Hat, r2Hat).magnitude;
                abstau = Sqrt(1.0 / (oneMctheta * r1r2)) / r1pr2 * sthetar1r2;
            }
            else
            {
                abstau = Sqrt(r1r2 * onePctheta) / r1pr2;
            }

            if (SQRT2 / 2 - abstau < TAU_MARGIN)
                throw new ArgumentException("Russell's Lambert solver requires initial and final positions which are not too close to each other");

            // The Lambert equation is still solvable here, but the transfer plane is undefined so the velocities are not
            if (abstau < SQRT_TINY)
                throw new ArgumentException("Russell's Lambert solver cannot compute velocities for an exact half-revolution transfer");

            double tau = direction * abstau;
            double tau2 = tau * tau;
            double tau3 = tau2 * tau;
            double S = r1pr2 * Sqrt(r1pr2);
            double tofbyS = tofHat / S;

            if (zeroRev && tofbyS < MIN_TOF_OVER_TP * Sqrt(1.0 - SQRT2 * tau) * (tau + SQRT2) / 3.0)
                throw new ArgumentException("Russell's Lambert solver requires a time of flight of at least 1e-3 times the parabolic time of flight");

            // Root-solve log(T)-log(T*) for very large time of flight
            bool hugeTof = tofbyS > HUGE_TOF;
            double logTofbyS = hugeTof ? Log(tofbyS) : 0.0;

            // Range of k, the multi-rev range is further bracketed to the requested branch
            double kLeft = -SQRT2 + K_MARGIN;
            double kRight;
            if (zeroRev && tau > 0.0)
                kRight = 1.0 / tau * (1.0 - K_MARGIN);
            else if (zeroRev)
                kRight = 1e90;
            else
                kRight = SQRT2 - K_MARGIN;

            double k;
            if (zeroRev)
            {
                k = InitialGuessZeroRev(tau, S, tofHat, direction);

                // defensive, the interpolation should always produce a finite guess
                if (!IsFinite(k))
                    k = 0.0;
            }
            else
            {
                double kBottom, tofbySBottom;
                (k, kBottom, tofbySBottom) = InitialGuessMultiRev(tau, tofbyS, nabs, n > 0);

                if (tofbyS <= tofbySBottom)
                    throw new Exception("Russell's Lambert solver found no solution: time of flight is less than the minimum for this number of revolutions");

                if (n > 0)
                    kLeft = kBottom;
                else
                    kRight = kBottom;
            }

            k = Clamp(k, kLeft + K_BUMP_INIT, kRight - K_BUMP_INIT);
            double p = 1.0 - k * tau;

            // iterate on p instead of k when p is small (only happens for zero-rev)
            bool iterateOnP = zeroRev && p < LITTLE_P;

            // for multi-rev, if the TOF curve slope says we are on the other branch then bump back towards the requested branch
            double signBump = zeroRev ? 0.0 : n > 0 ? BOTTOM_BUMP : -BOTTOM_BUMP;

            bool converged = false;

            for (int iter = 0; iter < numiter; iter++)
            {
                double kLast = k;
                double pLast = p;

                bool hugeK = zeroRev && k > BIG_K;

                // relF0 is the TOF residual relative to the TOF (the log form is already relative)
                double F0, F1, F2, F3, relF0;

                if (hugeK)
                {
                    (F0, F1, F2, F3) = TofDerivsBigK(k, p, tau, tau2, tofbyS);
                    relF0 = F0 / tofbyS;
                }
                else
                {
                    (double W0, double W1, double W2, double W3) = GetW(k, nabs);
                    (F0, F1, F2, F3) = TofDerivs(p, tau, tau2, tau3, tofbyS, logTofbyS, W0, W1, W2, W3, hugeTof);
                    relF0 = hugeTof ? F0 : F0 / tofbyS;
                }

                int info;
                double dv3 = 0.0;
                double compVal = 0.0;

                if (signBump * F1 < 0.0)
                {
                    k = kLast + signBump;
                    p = 1.0 - k * tau;
                    info = -1;
                }
                else if (iterateOnP)
                {
                    double dv1, dv2;
                    (info, dv1, dv2, dv3) = Correction(F0, F1 / -tau, F2 / tau2, F3 / -tau3);
                    p += dv1 + dv2 + dv3;
                    compVal = LITTLE_P;
                    k = (1.0 - p) / tau;
                }
                else
                {
                    double dv1, dv2;
                    (info, dv1, dv2, dv3) = Correction(F0, F1, F2, F3);
                    k += dv1 + dv2 + dv3;
                    compVal = k;
                    p = 1.0 - k * tau;
                }

                // steps which violate the bounds go most of the way to the bound from the last step
                if (k < kLeft)
                {
                    k = (1.0 - K_BUMP_WEIGHT) * kLast + K_BUMP_WEIGHT * kLeft;
                    p = 1.0 - k * tau;
                }
                else if (k > kRight)
                {
                    k = (1.0 - K_BUMP_WEIGHT) * kLast + K_BUMP_WEIGHT * kRight;
                    p = 1.0 - k * tau;
                }

                if (F0 == 0.0)
                {
                    converged = true;
                    break;
                }

                // stop when the third order term of a full correction no longer changes the iteration variable, and
                // make sure we are not stuck on a steep cliff near a boundary.
                if (info == 0 && Abs(dv3) / Max(SQRT_EPS, Abs(compVal)) < EPS && Abs(relF0) < 1.0)
                {
                    converged = true;
                    break;
                }

                // Near the double root at the bottom of a multi-rev TOF curve the roundoff in F0 is amplified by the
                // small slope and the corrections never become clean, so also stop at the last evaluated point once
                // its TOF matches to within roundoff.
                if (info != -1 && Abs(relF0) <= TOF_ROUNDOFF)
                {
                    k = kLast;
                    p = pLast;
                    converged = true;
                    break;
                }
            }

            if (!converged)
                throw new Exception("Russell's Lambert solver failed to converge");

            // Reconstruct the velocities from the Lagrange coefficients
            double pr12 = p * r1pr2;
            double f = 1.0 - pr12;
            double g = S * tau * Sqrt(p);
            double gdot = 1.0 - pr12 / r2Norm;

            V3 v1 = (r2Hat - f * r1Hat) / g;
            V3 v2 = (gdot * r2Hat - r1Hat) / g;

            double vScale = lRef / tRef;

            return (v1 * vScale, v2 * vScale, k, p, tau);
        }

        /// <summary>
        ///     The vercosine W(k) function and its first three derivatives with respect to k.  W(k) plays
        ///     the role of the Stumpff functions in the vercosine formulation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double W, double dW1, double dW2, double dW3) GetW(double k, int nabs)
        {
            double k2 = k * k;
            double nu = k - SQRT2;
            double twoPiN = TAU * nabs;

            // near the parabola, series in nu = k - sqrt(2)
            if (k2 > ZERO_BAND * ZERO_BAND && nabs == 0 && Abs(nu) < PARABOLA_BAND)
                return GetWParabola(nu);

            double m = 2.0 - k2;
            double onebym = 1.0 / m;
            double tnpp = twoPiN + PI;

            double W;
            if (k2 <= ZERO_BAND * ZERO_BAND)
            {
                // near zero, series in k for precision
                double k3 = k2 * k;
                double k4 = k2 * k2;
                double k5 = k4 * k;
                double k6 = k4 * k2;
                double k7 = k4 * k3;
                double k8 = k4 * k4;
                W = 0.3535533905932738 * tnpp - k + 0.2651650429449553 * tnpp * k2 - 2.0 / 3.0 * k3 +
                    0.1657281518405971 * tnpp * k4 - 0.4 * k5 + 0.09667475524034829 * tnpp * k6 -
                    8.0 / 35.0 * k7 + 0.05437954982269591 * tnpp * k8;
            }
            else if (k2 <= 2.0)
            {
                // ellipse
                double k2m1 = k2 - 1.0;
                if (k > 0.0)
                {
                    W = (twoPiN + Acos(k2m1)) * Sqrt(onebym * onebym * onebym) - k * onebym;
                }
                else
                {
                    double kps2 = k + SQRT2;
                    if (kps2 < K_CLOSE_MSQRT2)
                    {
                        // series in k + sqrt(2) for smoothness near k = -sqrt(2)
                        double tNp = TAU + twoPiN;
                        double t1 = kps2 * kps2;
                        double t2 = Sqrt(kps2);
                        double t3 = t2 * t1;
                        double t9 = t1 * t1;
                        double t10 = t2 * t9;
                        W = 0.12110150049603174603174603174603e-7 / t3 * (
                            -0.38926398009946925989672338336519e8 * t3 -
                            0.16515072e8 * t2 * kps2 * t1 -
                            0.1976320e7 * t10 * (kps2 + 2.4532575164897006338798206469711) -
                            0.18246749067162621557658908595243e7 * t10 +
                            0.25959796716951899525909665607350e6 *
                            (t9 + 6.4646464646464646464646464646465 * t1 + 35.463203463203463203463203463203) *
                            tNp * t1 +
                            0.66750357442839860425810740303391e6 *
                            tNp * (t9 + 6.0952380952380952380952380952381 * t1 + 26.006349206349206349206349206349) *
                            kps2 - 645120.0 * t2 * kps2 * t9
                        );
                    }
                    else
                    {
                        W = (TAU + twoPiN - Acos(k2m1)) * Sqrt(onebym * onebym * onebym) - k * onebym;
                    }
                }
            }
            else
            {
                // hyperbola
                double k2m1 = k2 - 1.0;
                W = -Log(k2m1 + Sqrt(k2m1 * k2m1 - 1.0)) * Sqrt(-onebym * onebym * onebym) - k * onebym;
            }

            double t = 3.0 * W;
            double dW1 = (t * k - 2.0) * onebym;
            double dW2 = (5.0 * dW1 * k + t) * onebym;
            double dW3 = (7.0 * dW2 * k + 8.0 * dW1) * onebym;

            return (W, dW1, dW2, dW3);
        }

        /// <summary>
        ///     W(k) and its derivatives from the Taylor series near the parabola, where nu = k - sqrt(2).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double W, double dW1, double dW2, double dW3) GetWParabola(double nu)
        {
            double nu2 = nu * nu;
            double nu3 = nu2 * nu;
            double nu4 = nu2 * nu2;
            double nu5 = nu4 * nu;
            double nu6 = nu4 * nu2;
            double nu7 = nu4 * nu3;
            double nu8 = nu4 * nu4;

            double W = 0.47140452079103168 - 0.2 * nu + 0.080812203564176860 * nu2 -
                0.031746031746031746 * nu3 + 0.012244273267299524 * nu4 - 0.0046620046620046620 * nu5 +
                0.0017581520588942907 * nu6 - 0.00065816536404771699 * nu7 +
                0.00024494378529487022 * nu8;

            double dW1 = -0.2 + 0.16162440712835372 * nu - 0.095238095238095238 * nu2 +
                0.048977093069198097 * nu3 - 0.023310023310023310 * nu4 +
                0.010548912353365744 * nu5 - 0.0046071575483340189 * nu6 +
                0.0019595502823589617 * nu7;

            double dW2 = 0.16162440712835372 - 0.19047619047619048 * nu + 0.14693127920759429 * nu2 -
                0.093240093240093240 * nu3 + 0.052744561766828720 * nu4 -
                0.027642945290004114 * nu5 + 0.013716851976512732 * nu6;

            double dW3 = -0.19047619047619048 + 0.29386255841518858 * nu - 0.27972027972027972 * nu2 +
                0.21097824706731488 * nu3 - 0.13821472645002057 * nu4 + 0.082301111859076392 * nu5;

            return (W, dW1, dW2, dW3);
        }

        /// <summary>
        ///     The normalized TOF/S residual and its derivatives with respect to k.  For very large TOF/S the
        ///     residual is log(TOF/S) - log(TOF*/S) to maintain precision.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double F0, double F1, double F2, double F3) TofDerivs(double p, double tau, double tau2, double tau3, double tofbyS,
            double logTofbyS, double W0, double W1, double W2, double W3, bool hugeTof)
        {
            double sqrtp = Sqrt(p);

            double leftSide = sqrtp * (p * W0 + tau);

            double t7 = p * p;
            double onebyrootp = 1.0 / sqrtp;
            double F1 = (-3.0 * p * tau * W0 + 2.0 * t7 * W1 - tau2) * onebyrootp * 0.5;

            double onebyp = onebyrootp * onebyrootp;
            double onebyp32 = onebyrootp * onebyp;
            double p3dw0 = 3.0 * p * W0;
            double t7tau = t7 * tau;
            double F2 = (p3dw0 * tau2 + 4.0 * t7 * p * W2 - 12.0 * t7tau * W1 - tau3) * onebyp32 * 0.25;

            double F3 = (p3dw0 * tau3 + 18.0 * t7 * tau2 * W1 - 36.0 * t7tau * p * W2 + 8.0 * t7 * t7 * W3 - 3.0 * tau2 * tau2) *
                onebyp32 * onebyp * 0.125;

            if (!hugeTof)
                return (leftSide - tofbyS, F1, F2, F3);

            double F0 = Log(leftSide) - logTofbyS;
            double t1 = 1.0 / leftSide;
            double t10 = F1;
            double t20 = F2;
            double t30 = F3;
            F1 = t10 * t1;
            double t4 = F1 * F1;
            F2 = t20 * t1 - t4;
            double t2 = t1 * t1;
            F3 = t30 * t1 - 3.0 * t20 * t10 * t2 + 2.0 * t4 * F1;

            return (F0, F1, F2, F3);
        }

        /// <summary>
        ///     Series expansion in 1/k of the TOF/S residual and its derivatives for very large k (extreme hyperbolas),
        ///     where tau + pW loses precision.  This is Algorithm 1 of Russell (2019).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double F0, double F1, double F2, double F3) TofDerivsBigK(double k, double p, double tau, double tau2, double tofbyS)
        {
            double ka = 1.0 / k;
            double ka2 = ka * ka;
            double ka3 = ka2 * ka;
            double ka4 = ka2 * ka2;
            double ka5 = ka4 * ka;
            double ka6 = ka3 * ka3;
            double ka7 = ka4 * ka3;
            double ka8 = ka4 * ka4;
            double lnka = Log(ka);
            double t1 = -LN2 + 2.0 * lnka + 2.0;
            double t2 = -3.0 * LN2 + 6.0 * lnka + 5.0;

            // truncated series for Z = tau + pW and its derivatives with respect to k
            double Z0 = t2 * ka5 - t2 * tau * ka4 + t1 * ka3 - t1 * tau * ka2 + ka;
            double Z1 = (-5.0 * t2 - 6.0) * ka6 + (4.0 * tau * t2 + 6.0 * tau) * ka5 + (-3.0 * t1 - 2.0) * ka4 + (2.0 * tau * t1 + 2.0 * tau) * ka3 - ka2;
            double Z2 = (30.0 * t2 + 66.0) * ka7 + (-20.0 * tau * t2 - 54.0 * tau) * ka6 + (12.0 * t1 + 14.0) * ka5 + (-6.0 * tau * t1 - 10.0 * tau) * ka4 +
                2.0 * ka3;
            double Z3 = (-210.0 * t2 - 642.0) * ka8 + (120.0 * tau * t2 + 444.0 * tau) * ka7 + (-60.0 * t1 - 94.0) * ka6 +
                (24.0 * tau * t1 + 52.0 * tau) * ka5 - 6.0 * ka4;

            double pr = Sqrt(p);
            double pr3 = pr * p;
            double pr5 = pr3 * p;

            double F0 = pr * Z0 - tofbyS;
            double F1 = -Z0 * tau / (2.0 * pr) + pr * Z1;
            double F2 = -Z0 * tau2 / (4.0 * pr3) - Z1 * tau / pr + pr * Z2;
            double F3 = -3.0 * Z0 * tau2 * tau / (8.0 * pr5) - 3.0 * Z1 * tau2 / (4.0 * pr3) - 3.0 * Z2 * tau / (2.0 * pr) + pr * Z3;

            return (F0, F1, F2, F3);
        }

        /// <summary>
        ///     The normalized time of flight L = T/S = sqrt(p) (tau + pW) and its partial derivatives with respect to k (at
        ///     fixed tau) and tau (at fixed k), at the converged solution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double L, double Lk, double Ltau) TofPartials(double k, double p, double tau, int nabs)
        {
            double sqrtp = Sqrt(p);
            double L, Lk, Ltau;

            if (nabs == 0 && k > BIG_K)
            {
                // series in 1/k (Russell 2019, Algorithm 1), where L = sqrt(p) Z and dZ/dtau is from the same series
                (L, Lk, _, _) = TofDerivsBigK(k, p, tau, tau * tau, 0.0);
                double ka = 1.0 / k;
                double ka2 = ka * ka;
                double lnka = Log(ka);
                double t1 = -LN2 + 2.0 * lnka + 2.0;
                double t2 = -3.0 * LN2 + 6.0 * lnka + 5.0;
                double Ztau = -t2 * ka2 * ka2 - t1 * ka2;
                Ltau = -k * L / (2.0 * p) + sqrtp * Ztau;
            }
            else
            {
                (double W0, double W1, _, _) = GetW(k, nabs);
                double Z = tau + p * W0;
                L = sqrtp * Z;
                Lk = (-3.0 * p * tau * W0 + 2.0 * p * p * W1 - tau * tau) / (2.0 * sqrtp);
                Ltau = -k * Z / (2.0 * sqrtp) + sqrtp * (1.0 - k * W0);
            }

            return (L, Lk, Ltau);
        }

        /// <summary>
        ///     The root-solve correction, split into its first, second and third order terms, with checks that the series
        ///     is converging.  Info is 0 for a full third order correction and nonzero when a safeguard truncated it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int info, double dv1, double dv2, double dv3) Correction(double F0, double F1, double F2, double F3)
        {
            double monebydf = -1.0 / F1;
            double dv1 = F0 * monebydf;

            double dvm = dv1 * monebydf;
            double dkA = dv1 * dvm;
            double dv2 = 0.5 * dkA * F2;

            double absdv1 = Abs(dv1);
            if (Abs(dv2) * SERIES_CONVERGE > absdv1)
            {
                // too steep for the higher order terms, take a slightly shortened Newton step
                if (absdv1 > SERIES_TAMP_THRESH)
                    dv1 *= SERIES_TAMP;
                return (2, dv1, 0.0, 0.0);
            }

            double dv3 = dkA * dv1 * F3 / 6.0 + dv2 * F2 * dvm;

            if (Abs(dv3) * SERIES_CONVERGE * SERIES_CONVERGE > absdv1)
                return (3, dv1, dv2, 0.0);

            return (0, dv1, dv2, dv3);
        }

        /// <summary>
        ///     Zero-rev initial guess for k using the piecewise rational interpolation of Arora and Russell (2013).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double InitialGuessZeroRev(double tau, double S, double tofHat, int direction)
        {
            // parabolic time of flight
            double tofP = S * Sqrt(1.0 - SQRT2 * tau) * (tau + SQRT2) / 3.0;

            if (tofHat <= tofP)
            {
                // hyperbolic
                if (direction == 1)
                {
                    const double kn = SQRT2;
                    double km = 1.0 / tau;
                    double ki = (kn + km) / 2.0;
                    (double Wi, _, _, _) = GetW(ki, 0);
                    double Fi = Tof(ki, Wi, tau, S);
                    double x = AroraX(tofP, 0.0, Fi, tofHat, 1.0 / SQRT2, 0.5);
                    return kn + (km - kn) * x;
                }

                double tof20 = S * Sqrt(1.0 - 20.0 * tau) * (tau + 0.04940968903 * (1.0 - 20.0 * tau));
                double tof100 = S * Sqrt(1.0 - 100.0 * tau) * (tau + 0.00999209404 * (1.0 - 100.0 * tau));

                if (tofHat > tof20)
                {
                    // H1 region
                    const double kn = SQRT2;
                    const double km = 20.0;
                    const double ki = (2.0 * kn + km) / 3.0;
                    (double Wi, _, _, _) = GetW(ki, 0);
                    double Fi = Tof(ki, Wi, tau, S);
                    double x = AroraX(tofP, tof20, Fi, tofHat, 1.0 / 3.0, 1.0);
                    return kn + (km - kn) * x;
                }

                // H2 region
                double h2 = (tof100 * (tof20 - tofHat) * 10.0 - tof20 * Sqrt(20.0) * (tof100 - tofHat)) / (tofHat * (tof20 - tof100));
                return h2 * h2;
            }

            // elliptic
            double tofM141 = Tof(-1.41, 4839.684497246, tau, S);
            double tofM138 = Tof(-1.38, 212.087279879, tau, S);
            double tofM1 = Tof(-1.0, 5.712388981, tau, S);
            double tofM1Half = Tof(-0.5, 1.954946607, tau, S);
            double tof0 = Tof(0.0, 1.110720735, tau, S);
            double tof1OverSqrt2 = Tof(1.0 / SQRT2, 0.6686397730, tau, S);

            if (tofHat <= tof0)
                return SQRT2 * AroraX(tof0, tofP, tof1OverSqrt2, tofHat, 0.5, 1.0);

            if (tofHat <= tofM1)
                return -AroraX(tof0, tofM1, tofM1Half, tofHat, 0.5, 1.0);

            if (tofHat <= tofM138)
                return -AroraE(1.0 / tofM1, 1.0 / tofM138, 1.0 / tofHat, 540649.0 / 3125.0, 256.0, 1.0, 1.0, 16.0);

            return -AroraE(1.0 / tofM138, 1.0 / tofM141, 1.0 / tofHat, 49267.0 / 27059.0, 67286.0 / 17897.0, 2813.0 / 287443.0,
                4439.0 / 3156.0, 243.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double Tof(double k, double W, double tau, double S)
        {
            double p = 1.0 - k * tau;
            return S * Sqrt(p) * (tau + p * W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double AroraX(double F0, double F1, double Fi, double Fstar, double Z, double alpha) =>
            Pow(Z * (F0 - Fstar) * (F1 - Fi) / ((Fi - Fstar) * (F1 - F0) * Z + (F0 - Fi) * (F1 - Fstar)), 1.0 / alpha);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double AroraE(double Fn, double Fi, double Fstar, double c1, double c2, double c3, double c4, double alpha)
        {
            double gamma1 = Fi * (Fstar - Fn);
            double gamma2 = Fstar * (Fn - Fi);
            double gamma3 = Fn * (Fstar - Fi);
            return c4 * Pow(((gamma1 * c1 - c3 * gamma3) * c2 + c3 * c1 * gamma2) / (gamma3 * c1 - c3 * gamma1 - gamma2 * c2), 1.0 / alpha);
        }

        /// <summary>
        ///     Multi-rev initial guess for k.  Also returns the bottom of the TOF curve (kBottom, TOF/S at kBottom), which
        ///     separates the long-period (k > kBottom) and short-period (k &lt; kBottom) branches.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double k0, double kBottom, double tofbySBottom) InitialGuessMultiRev(double tau, double tofbyS, int nabs, bool longPeriod)
        {
            double tau2 = tau * tau;
            double tau3 = tau2 * tau;

            // start from the asymptotic value of kBottom as N -> infinity
            double kBottom = Abs(tau) < 1e-3 ? tau + 0.5 * tau3 : (1.0 - Sqrt(1.0 - 2.0 * tau2)) / tau;

            // Newton iteration on dT/dk = 0, safeguarded by bisection on the sign of dT/dk
            double lo = -SQRT2 + K_MARGIN;
            double hi = SQRT2 - K_MARGIN;
            kBottom = Clamp(kBottom, lo, hi);

            double pBottom, wBottom;

            for (int i = 0; i < 100; i++)
            {
                double W0, W1, W2, W3;
                (W0, W1, W2, W3) = GetW(kBottom, nabs);
                pBottom = 1.0 - kBottom * tau;
                (_, double F1, double F2, _) = TofDerivs(pBottom, tau, tau2, tau3, tofbyS, 0.0, W0, W1, W2, W3, false);

                if (F1 > 0.0)
                    hi = kBottom;
                else
                    lo = kBottom;

                double kNext = F2 > 0.0 ? kBottom - F1 / F2 : 0.5 * (lo + hi);
                if (!(kNext > lo && kNext < hi))
                    kNext = 0.5 * (lo + hi);

                double dk = kNext - kBottom;
                kBottom = kNext;

                if (Abs(dk) <= 4.0 * EPS * Max(1.0, Abs(kBottom)))
                    break;
            }

            (wBottom, _, _, _) = GetW(kBottom, nabs);
            pBottom = 1.0 - kBottom * tau;
            double tofbySBottom = Sqrt(pBottom) * (tau + pBottom * wBottom);

            // interpolate part of the way towards the far boundary of the requested branch
            double frac = Min(0.8, (tofbyS - tofbySBottom) / (tofbyS + tofbySBottom));

            double k0 = longPeriod ? kBottom + frac * (SQRT2 - K_MARGIN - kBottom) : kBottom - frac * (kBottom + SQRT2 - K_MARGIN);

            return (k0, kBottom, tofbySBottom);
        }
    }
}
