/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using MechJebLib.Core.Functions;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Core.TwoBody
{
    public static class Farnocchia
    {
        public static double dS_x_alt(double ecc, double x, double atol = 1e-12)
        {
            if (Math.Abs(x) >= 1)
            {
                throw new ArgumentException("The value of x must be less than 1.");
            }

            double S = 0;
            int k = 0;
            while (true)
            {
                double S_old = S;
                S += (ecc - 1.0 / (2 * k + 3)) * (2 * k + 3) * Math.Pow(x, k);
                k += 1;
                if (Math.Abs(S - S_old) < atol)
                {
                    return S;
                }
            }
        }

        public static double S_x(double ecc, double x, double atol = 1e-12)
        {
            if (Math.Abs(x) >= 1)
            {
                throw new ArgumentException("The value of x must be less than 1.");
            }

            double S = 0;
            double k = 0;

            while (true)
            {
                double S_old = S;
                S += (ecc - 1 / (2 * k + 3)) * Math.Pow(x, k);
                k++;

                if (Math.Abs(S - S_old) < atol)
                {
                    return S;
                }
            }
        }

        public static double KeplerEquationNearParabolic(double D, double M, double ecc)
        {
            return DToMNearParabolic(D, ecc) - M;
        }

        public static double KeplerEquationPrimeNearParabolic(double D, double M, double ecc)
        {
            double x = (ecc - 1.0) / (ecc + 1.0) * Math.Pow(D, 2);
            if (Math.Abs(x) > 1)
                throw new ArgumentException("x");
            double S = dS_x_alt(ecc, x);
            return Math.Sqrt(2.0 / (1.0 + ecc)) + Math.Sqrt(2.0 / Math.Pow(1.0 + ecc, 3)) * Math.Pow(D, 2) * S;
        }

        public static double DToMNearParabolic(double D, double ecc)
        {
            double x = (ecc - 1.0) / (ecc + 1.0) * (D * D);
            if (Math.Abs(x) >= 1)
            {
                throw new ArgumentOutOfRangeException("x", "The value of x is out of range");
            }

            double S = S_x(ecc, x);
            return Math.Sqrt(2.0 / (1.0 + ecc)) * D + Math.Sqrt(2.0 / Math.Pow(1.0 + ecc, 3)) * (D * D * D) * S;
        }

        public static double MToDNearParabolic(double M, double ecc, double tol = 1.48e-08, int maxiter = 50)
        {
            double D0 = Angles.DFromM(M);

            for (int i = 0; i < maxiter; i++)
            {
                double fval = KeplerEquationNearParabolic(D0, M, ecc);
                double fder = KeplerEquationPrimeNearParabolic(D0, M, ecc);

                double newton_step = fval / fder;
                double D = D0 - newton_step;

                if (Math.Abs(D - D0) < tol)
                {
                    return D;
                }

                D0 = D;
            }

            return double.NaN;
        }

        public static double DeltaTFromNu(double nu, double ecc, double mu, double q, double delta = 1e-2)
        {
            // ReSharper disable InconsistentNaming
            nu = ClampPi(nu);

            if (ecc < 1 - delta)
            {
                double E = Angles.EFromNu(nu, ecc);
                double M = Angles.MFromE(E, ecc);
                double n = Math.Sqrt(mu * Math.Pow(1 - ecc, 3) / Math.Pow(q, 3));
                Check.Finite(M / n);
                return M / n;
            }

            if (1 - delta <= ecc && ecc < 1)
            {
                double E = Angles.EFromNu(nu, ecc);
                if (delta <= 1 - ecc * Math.Cos(E))
                {
                    double M = Angles.MFromE(E, ecc);
                    double n = Math.Sqrt(mu * Math.Pow(1 - ecc, 3) / Math.Pow(q, 3));
                    Check.Finite(M / n);
                    return M / n;
                }
                else
                {
                    double D = Angles.DFromNu(nu);
                    double M = DToMNearParabolic(D, ecc);
                    double n = Math.Sqrt(mu / (2 * Math.Pow(q, 3)));
                    Check.Finite(M / n);
                    return M / n;
                }
            }

            if (ecc == 1)
            {
                double D = Angles.DFromNu(nu);
                double M = Angles.MFromD(D);
                double n = Math.Sqrt(mu / (2 * Math.Pow(q, 3)));
                Check.Finite(M / n);
                return M / n;
            }

            if (1 + ecc * Math.Cos(nu) < 0) // WAT?
                throw new Exception("Invalid eccentricity");

            if (1 < ecc && ecc <= 1 + delta)
            {
                double F = Angles.FFromNu(nu, ecc);
                if (delta <= ecc * Math.Cosh(F) - 1)
                {
                    double M = Angles.MFromF(F, ecc);
                    double n = Math.Sqrt(mu * Math.Pow(ecc - 1, 3) / Math.Pow(q, 3));
                    Check.Finite(M / n);
                    return M / n;
                }
                else
                {
                    double D = Angles.DFromNu(nu);
                    double M = DToMNearParabolic(D, ecc);
                    double n = Math.Sqrt(mu / (2 * Math.Pow(q, 3)));
                    Check.Finite(M / n);
                    return M / n;
                }
            }

            if (1 + delta < ecc)
            {
                double F = Angles.FFromNu(nu, ecc);
                double M = Angles.MFromF(F, ecc);
                double n = Math.Sqrt(mu * Math.Pow(ecc - 1, 3) / Math.Pow(q, 3));
                Check.Finite(M / n);
                return M / n;
            }

            throw new Exception("Invalid eccentricity");
            // ReSharper restore InconsistentNaming
        }

        public static double NuFromDeltaT(double delta_t, double ecc, double k = 1.0, double q = 1.0, double delta = 1e-2)
        {
            Check.Finite(delta_t);
            Check.Finite(ecc);
            Check.Finite(k);
            Check.Finite(q);
            Check.Finite(delta);

            double n, M, E, nu, E_delta, D, F, F_delta;

            if (ecc < 1 - delta)
            {
                n  = Math.Sqrt(k * Math.Pow(1 - ecc, 3) / Math.Pow(q, 3));
                M  = n * delta_t;
                E  = Angles.EFromM((M + Math.PI) % (2 * Math.PI) - Math.PI, ecc);
                nu = Angles.NuFromE(E, ecc);
            }
            else if (1 - delta <= ecc && ecc < 1)
            {
                E_delta = Math.Acos((1 - delta) / ecc);
                n       = Math.Sqrt(k * Math.Pow(1 - ecc, 3) / Math.Pow(q, 3));
                M       = n * delta_t;
                if (Angles.MFromE(E_delta, ecc) <= Math.Abs(M))
                {
                    E  = Angles.EFromM((M + Math.PI) % (2 * Math.PI) - Math.PI, ecc);
                    nu = Angles.NuFromE(E, ecc);
                }
                else
                {
                    n  = Math.Sqrt(k / (2 * Math.Pow(q, 3)));
                    M  = n * delta_t;
                    D  = MToDNearParabolic(M, ecc);
                    nu = Angles.NuFromD(D);
                }
            }
            else if (ecc == 1)
            {
                n  = Math.Sqrt(k / (2 * Math.Pow(q, 3)));
                M  = n * delta_t;
                D  = Angles.DFromM(M);
                nu = Angles.NuFromD(D);
            }
            else if (1 < ecc && ecc <= 1 + delta)
            {
                F_delta = Acosh((1 + delta) / ecc);
                n       = Math.Sqrt(k * Math.Pow(ecc - 1, 3) / Math.Pow(q, 3));
                M       = n * delta_t;
                if (Angles.MFromF(F_delta, ecc) <= Math.Abs(M))
                {
                    F  = Angles.FFromM(M, ecc);
                    nu = Angles.NuFromF(F, ecc);
                }
                else
                {
                    n  = Math.Sqrt(k / (2 * Math.Pow(q, 3)));
                    M  = n * delta_t;
                    D  = MToDNearParabolic(M, ecc);
                    nu = Angles.NuFromD(D);
                }
            }
            else
            {
                n  = Math.Sqrt(k * Math.Pow(ecc - 1, 3) / Math.Pow(q, 3));
                M  = n * delta_t;
                F  = Angles.FFromM(M, ecc);
                nu = Angles.NuFromF(F, ecc);
            }

            return nu;
        }

        public static double FarnocchiaFromKeplerian(double mu, double tau, double p, double ecc, double tanom)
        {
            Check.Finite(mu);
            Check.Finite(tau);
            Check.Finite(p);
            Check.Finite(ecc);
            Check.Finite(tanom);

            double q = p / (1 + ecc);

            double delta_t0 = DeltaTFromNu(tanom, ecc, mu, q);
            Check.Finite(delta_t0);
            double delta_t = delta_t0 + tau;

            return NuFromDeltaT(delta_t, ecc, mu, q);
        }

        public static (V3 rf, V3 vf) Solve(double mu, double tau, V3 ri, V3 vi)
        {
            (double sma, double ecc, double inc, double lan, double argp, double tanom, double l) =
                Maths.KeplerianFromStateVectors(mu, ri, vi);
            tanom = FarnocchiaFromKeplerian(mu, tau, l, ecc, tanom);
            return Maths.StateVectorsFromKeplerian(mu, l, ecc, inc, lan, argp, tanom);
        }
    }
}
