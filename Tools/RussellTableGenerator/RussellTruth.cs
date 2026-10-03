/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Lambert;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace RussellTableGenerator
{
    /// <summary>
    ///     Solutions of the vercosine form of Lambert's equation by a bracketed root-solve which does not depend on any
    ///     initial guess.
    /// </summary>
    internal static class RussellTruth
    {
        /// <summary>
        ///     The residual TOF/S - target and its derivative with respect to k, using the series in 1/k when k is large.
        /// </summary>
        private static (double f, double df) Residual(double k, double tau, double tofbyS, int nabs)
        {
            double p = 1.0 - k * tau;
            double tau2 = tau * tau;

            if (nabs == 0 && k > 1000)
            {
                (double F0, double F1, _, _) = Russell.TofDerivsBigK(k, p, tau, tau2, tofbyS);
                return (F0, F1);
            }

            (double W0, double W1, double W2, double W3) = Russell.GetW(k, nabs);
            (double G0, double G1, _, _) = Russell.TofDerivs(p, tau, tau2, tau2 * tau, tofbyS, 0.0, W0, W1, W2, W3, false);
            return (G0, G1);
        }

        /// <summary>
        ///     Safeguarded Newton iteration for the root of the residual on the open interval (a, b), where the residual is
        ///     increasing or decreasing in k.
        /// </summary>
        private static double Bracketed(double tau, double tofbyS, int nabs, double a, double b, bool increasing)
        {
            double k = 0.5 * (a + b);

            for (int i = 0; i < 500; i++)
            {
                (double f, double df) = Residual(k, tau, tofbyS, nabs);

                if (f == 0.0)
                    return k;

                if (f < 0.0 == increasing)
                    a = k;
                else
                    b = k;

                double kNext = k - f / df;

                // bisect when Newton leaves the bracket, and bisect geometrically for huge brackets
                if (!(kNext > a && kNext < b))
                    kNext = b - a > 1e3 * Max(1.0, Abs(a)) ? a + Sqrt(Max(1.0, Abs(a)) * (b - a)) : 0.5 * (a + b);

                if (Abs(kNext - k) <= 2.0 * EPS * Max(1.0, Abs(k)) || b - a <= 4.0 * EPS * Max(1.0, Abs(k)))
                    return kNext;

                k = kNext;
            }

            throw new Exception($"RussellTruth failed to converge: tau = {tau:R} tofbyS = {tofbyS:R} nabs = {nabs} a = {a:R} b = {b:R}");
        }

        /// <summary>
        ///     The solution k of the zero-rev problem, where TOF/S is decreasing in k.
        /// </summary>
        public static double ZeroRevK(double tau, double tofbyS)
        {
            double b;
            if (tau > 0.0)
            {
                b = 1.0 / tau;
            }
            else
            {
                b = 2.0;
                while (Residual(b, tau, tofbyS, 0).f > 0.0)
                    b *= 2.0;
            }

            return Bracketed(tau, tofbyS, 0, -SQRT2, b, false);
        }

        /// <summary>
        ///     The solution k of the multi-rev problem, given the bottom of the TOF curve.  TOF/S is increasing in k for the
        ///     long-period branch (k > kBottom) and decreasing for the short-period branch.
        /// </summary>
        public static double MultiRevK(double tau, double gamma, int nrev, double kBottom, double tofbySBottom)
        {
            double tofbyS = tofbySBottom + gamma;
            int nabs = Abs(nrev);
            return nrev > 0
                ? Bracketed(tau, tofbyS, nabs, kBottom, SQRT2, true)
                : Bracketed(tau, tofbyS, nabs, -SQRT2, kBottom, false);
        }

        /// <summary>
        ///     The error metric for k from Russell (2022), Eq. 28.
        /// </summary>
        public static double Error(double k, double kTrue) => Abs(k - kTrue) / Max(Abs(kTrue), 1.0);
    }
}
