using System;
using System.Runtime.CompilerServices;
using MechJebLib.Utils;
using Steamworks;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Core.Functions
{
    public class Angles
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquation(double E, double M, double ecc)
        {
            return EToM(E, ecc) - M;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquationPrime(double E, double M, double ecc)
        {
            return 1 - ecc * Math.Cos(E);
        }

        public static double NewtonElliptic(double E0, double M, double ecc)
        {
            double tol = 1.48e-08;
            double E = E0;

            for (int i = 0; i < 50; i++)
            {
                double delta = KeplerEquation(E, M, ecc) / KeplerEquationPrime(E, M, ecc);
                if (Math.Abs(delta) > PI)
                    delta = PI * Math.Sign(delta);
                E -= delta;
                if (Math.Abs(delta) < tol)
                    return E;
            }

            throw new Exception($"NewtonElliptic({E0}, {M}, {ecc}): Maximum iterations exceeded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquationHyper(double F, double M, double ecc)
        {
            return FToM(F, ecc) - M;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double KeplerEquationPrimeHyper(double F, double M, double ecc)
        {
            return ecc * Math.Cosh(F) - 1;
        }

        public static double NewtonHyperbolic(double F0, double M, double ecc)
        {
            Check.Finite(F0);
            Check.Finite(M);
            Check.Finite(ecc);

            double tol = 1.48e-08;
            double F = F0;

            for (int i = 0; i < 50; i++)
            {
                double delta = KeplerEquationHyper(F, M, ecc) / KeplerEquationPrimeHyper(F, M, ecc);
                if (Math.Abs(delta) > PI)
                    delta = PI * Math.Sign(delta);
                F -= delta;
                if (Math.Abs(delta) < tol)
                    return F;
            }

            throw new Exception($"NewtonHyperbolic({F0}, {M}, {ecc}): Maximum iterations exceeded");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DToNu(double D)
        {
            return 2.0 * Math.Atan(D);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuToD(double nu)
        {
            return Math.Tan(nu / 2.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuToE(double nu, double ecc)
        {
            return 2 * Math.Atan(Math.Sqrt((1 - ecc) / (1 + ecc)) * Math.Tan(nu / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuToF(double nu, double ecc)
        {
            return 2 * Atanh(Math.Sqrt((ecc - 1) / (ecc + 1)) * Math.Tan(nu / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EToNu(double E, double ecc)
        {
            return 2 * Math.Atan(Math.Sqrt((1 + ecc) / (1 - ecc)) * Math.Tan(E / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FToNu(double F, double ecc)
        {
            return 2 * Math.Atan(Math.Sqrt((ecc + 1) / (ecc - 1)) * Math.Tanh(F / 2));
        }

        public static double MToE(double M, double ecc)
        {
            double E0;
            if ((-Math.PI < M && M < 0) || Math.PI < M)
            {
                E0 = M - ecc;
            }
            else
            {
                E0 = M + ecc;
            }

            return NewtonElliptic(E0, M, ecc);
        }

        public static double MToF(double M, double ecc)
        {
            Check.Finite(M);
            Check.Finite(ecc);

            double F0 = Asinh(M / ecc);
            return NewtonHyperbolic(F0, M, ecc);
        }

        public static double MToD(double M)
        {
            double B = 3.0 * M / 2.0;
            double A = Math.Pow(B + Math.Sqrt(1.0 + Math.Pow(B, 2)), 2.0 / 3.0);
            return 2.0 * A * B / (1.0 + A + Math.Pow(A, 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EToM(double E, double ecc)
        {
            return E - ecc * Math.Sin(E);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double FToM(double F, double ecc)
        {
            return ecc * Math.Sinh(F) - F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DToM(double D)
        {
            return D + Math.Pow(D, 3) / 3.0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NuToM(double nu, double ecc)
        {
            Check.Finite(nu);
            Check.PositiveFinite(ecc);

            if (ecc < 1)
                return EToM(NuToE(nu, ecc), ecc);

            if (ecc > 1)
                return FToM(NuToF(nu, ecc), ecc);

            return DToM(NuToD(nu));
        }
    }
}
