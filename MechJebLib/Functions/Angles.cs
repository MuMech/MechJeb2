/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

#nullable enable

// ReSharper disable InconsistentNaming
namespace MechJebLib.Functions
{
    public static class Angles
    {
        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquation(double E, double M, double ecc)
        {
            Check.Finite(E);
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            return MFromE(E, ecc) - M;
        }

        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquationPrime(double E, double M, double ecc)
        {
            Check.Finite(E);
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            return 1 - ecc * Cos(E);
        }

        [UsedImplicitly]
        public static double NewtonElliptic(double E0, double M, double ecc)
        {
            Check.Finite(E0);
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            double tol = 1.48e-08;
            double E = E0;

            for (int i = 0; i < 50; i++)
            {
                double delta = KeplerEquation(E, M, ecc) / KeplerEquationPrime(E, M, ecc);
                if (Abs(delta) > PI)
                    delta = PI * Sign(delta);
                E -= delta;
                if (Abs(delta) < tol)
                    return E;
            }

            throw new Exception($"NewtonElliptic({E0}, {M}, {ecc}): Maximum iterations exceeded");
        }

        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquationHyper(double F, double M, double ecc)
        {
            Check.Finite(F);
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            return MFromF(F, ecc) - M;
        }

        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquationPrimeHyper(double F, double M, double ecc)
        {
            Check.Finite(F);
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            return ecc * Cosh(F) - 1;
        }

        [UsedImplicitly]
        public static double NewtonHyperbolic(double F0, double M, double ecc)
        {
            Check.Finite(F0);
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            double tol = 1.48e-08;
            double F = F0;

            for (int i = 0; i < 50; i++)
            {
                double delta = KeplerEquationHyper(F, M, ecc) / KeplerEquationPrimeHyper(F, M, ecc);
                if (Abs(delta) > PI)
                    delta = PI * Sign(delta);
                F -= delta;
                if (Abs(delta) < tol)
                    return F;
            }

            throw new Exception($"NewtonHyperbolic({F0}, {M}, {ecc}): Maximum iterations exceeded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuFromD(double D)
        {
            Check.Finite(D);

            return 2.0 * Atan(D);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DFromNu(double nu)
        {
            Check.Finite(nu);

            return Tan(nu / 2.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EFromNu(double nu, double ecc)
        {
            Check.Finite(nu);
            Check.NonNegativeFinite(ecc);

            return 2 * Atan(Sqrt((1 - ecc) / (1 + ecc)) * Tan(nu / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FFromNu(double nu, double ecc)
        {
            Check.Finite(nu);
            Check.NonNegativeFinite(ecc);

            return 2 * Atanh(Sqrt((ecc - 1) / (ecc + 1)) * Tan(nu / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuFromE(double E, double ecc)
        {
            Check.Finite(E);
            Check.NonNegativeFinite(ecc);

            return 2 * Atan(Sqrt((1 + ecc) / (1 - ecc)) * Tan(E / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuFromF(double F, double ecc)
        {
            Check.Finite(F);
            Check.NonNegativeFinite(ecc);

            return 2 * Atan(Sqrt((ecc + 1) / (ecc - 1)) * Tanh(F / 2));
        }

        public static double EFromM(double M, double ecc)
        {
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            double E0;
            if ((-PI < M && M < 0) || PI < M)
            {
                E0 = M - ecc;
            }
            else
            {
                E0 = M + ecc;
            }

            return NewtonElliptic(E0, M, ecc);
        }

        public static double FFromM(double M, double ecc)
        {
            Check.Finite(M);
            Check.NonNegativeFinite(ecc);

            double F0 = Asinh(M / ecc);
            return NewtonHyperbolic(F0, M, ecc);
        }

        public static double DFromM(double M)
        {
            Check.Finite(M);

            double B = 3.0 * M / 2.0;
            double A = Pow(B + Sqrt(1.0 + Pow(B, 2)), 2.0 / 3.0);
            return 2.0 * A * B / (1.0 + A + Pow(A, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MFromE(double E, double ecc)
        {
            Check.Finite(E);
            Check.NonNegativeFinite(ecc);

            return E - ecc * Sin(E);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MFromF(double F, double ecc)
        {
            Check.Finite(F);
            Check.NonNegativeFinite(ecc);

            return ecc * Sinh(F) - F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MFromD(double D)
        {
            Check.Finite(D);

            return D + Pow(D, 3) / 3.0;
        }

        public static double MFromNu(double nu, double ecc)
        {
            Check.Finite(nu);
            Check.NonNegativeFinite(ecc);

            if (ecc < 1)
                return MFromE(EFromNu(nu, ecc), ecc);

            if (ecc > 1)
                return MFromF(FFromNu(nu, ecc), ecc);

            return MFromD(DFromNu(nu));
        }
    }
}
