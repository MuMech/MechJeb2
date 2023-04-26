/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Copyright Sebastien Gaggini (sebastien.gaggini@gmail.com)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

// ReSharper disable InconsistentNaming
// ReSharper disable CompareOfFloatsByEqualityOperator

#nullable enable

namespace MechJebLib.Core
{
    public static class Gooding
    {
        /*
         * mu = gravitational parameter of central body
         * r1 = position at t0
         * v1 = velocity at t0
         * r2 = position at t1
         * tof  = time of flight (t1 - t0) (+ posigrade "shortway", - retrograde "longway")
         * nrev = number of full revolutions (+ left-branch, - right-branch for nrev != 0)
         * Vi = initial velocity vector of transfer orbit
         * Vf = final velocity vector of transfer orbit
         */

        public static (V3 Vi, V3 Vf) Solve(double mu, V3 r1, V3 v1, V3 r2, double tof, int nrev)
        {
            /* most of this function lifted from https://www.mathworks.com/matlabcentral/fileexchange/39530-lambert-s-problem/content/glambert.m */

            // if we don't catch this edge condition, the solver will spin forever (internal state will NaN and produce great sadness)
            if (tof == 0)
                throw new Exception("MechJeb's Gooding Lambert Solver does not support zero time of flight (teleportation)");

            V3 Vi, Vf;

            V3 ur1xv1 = V3.Cross(r1, v1).normalized;

            V3 ux1 = r1.normalized;
            V3 ux2 = r2.normalized;

            V3 uz1 = V3.Cross(ux1, ux2).normalized;

            /* calculate the minimum transfer angle (radians) */

            double theta = SafeAcos(V3.Dot(ux1, ux2));

            /* calculate the angle between the orbit normal of the initial orbit and the fundamental reference plane */

            double angle_to_on = SafeAcos(V3.Dot(ur1xv1, uz1));

            /* if angle to orbit normal is greater than 90 degrees and posigrade orbit, then flip the orbit normal and the transfer angle */

            if (angle_to_on > 0.5 * PI && tof > 0.0)
            {
                theta = TAU - theta;
                uz1   = -uz1;
            }

            if (angle_to_on < 0.5 * PI && tof < 0.0)
            {
                theta = TAU - theta;
                uz1   = -uz1;
            }

            V3 uz2 = uz1;

            V3 uy1 = V3.Cross(uz1, ux1).normalized;

            V3 uy2 = V3.Cross(uz2, ux2).normalized;

            theta += TAU * Math.Abs(nrev);

            double VR11, VT11, VR12, VT12;
            double VR21, VT21, VR22, VT22;
            int n;

            (n, VR11, VT11, VR12, VT12, VR21, VT21, VR22, VT22) = VLAMB(mu, r1.magnitude, r2.magnitude, theta, tof);

            if (nrev > 0)
            {
                switch (n)
                {
                    case -1:
                        throw new Exception("Gooding Solver found no tminimum");
                    case 0:
                        throw new Exception("Gooding Solver found no solution time");
                }
            }

            /* compute transfer orbit initial and final velocity vectors */

            if (nrev > 0 && n > 1)
            {
                Vi = VR21 * ux1 + VT21 * uy1;
                Vf = VR22 * ux2 + VT22 * uy2;
            }
            else
            {
                Vi = VR11 * ux1 + VT11 * uy1;
                Vf = VR12 * ux2 + VT12 * uy2;
            }

            return (Vi, Vf);
        }

        /*
         * Goodings Method
         *
         * MMMMMmmmmmm..... Smells like Fortran....
         *
         * Shield your eyes lest ye be blinded by goto statements...
         *
         * Keep in mind that Gooding optimized the math to reduce loss of precision so "cleaning up" these functions without knowing the values
         * that the variables typically hold could result in two very small or very large numbers being multiplied together and resultant loss
         * of precision, and that rearrangement will make it incredibly difficult to spot simple transcription typos.  It has been deliberately
         * kept as super-fugly looking C# code for those reasons.
         */

        internal static ( int N, double VR11, double VT11, double VR12, double VT12, double VR21, double VT21, double VR22,
            double VT22) VLAMB(double GM, double R1, double R2, double TH, double TDELT)
        {
            Check.Finite(GM);
            Check.Finite(R1);
            Check.Finite(R1);
            Check.Finite(TH);
            Check.Finite(TDELT);

            double VR11 = 0.0, VT11 = 0.0, VR12 = 0.0, VT12 = 0.0;
            double VR21 = 0.0, VT21 = 0.0, VR22 = 0.0, VT22 = 0.0;
            int M = Convert.ToInt32(Math.Floor(TH / (2.0 * Math.PI)));
            double THR2 = TH / 2.0 - M * Math.PI;
            double DR = R1 - R2;
            double R1R2 = R1 * R2;
            double R1R2TH = 4.0 * R1R2 * Math.Pow(Math.Sin(THR2), 2);
            double CSQ = Math.Pow(DR, 2) + R1R2TH;
            double C = Math.Sqrt(CSQ);
            double S = (R1 + R2 + C) / 2.0;
            double GMS = Math.Sqrt(GM * S / 2.0);
            double QSQFM1 = C / S;
            double Q = Math.Sqrt(R1R2) * Math.Cos(THR2) / S;
            double RHO;
            double SIG;
            if (C != 0.0)
            {
                RHO = DR / C;
                SIG = R1R2TH / CSQ;
            }
            else
            {
                RHO = 0.0;
                SIG = 1.0;
            }

            double T = 4.0 * GMS * TDELT / Math.Pow(S, 2);

            (int N, double X1, double X2) = XLAMB(M, Q, QSQFM1, T);

            for (int I = 1; I <= N; I++)
            {
                double X = I == 1 ? X1 : X2;

                double QZMINX;
                double QZPLX;
                double ZPLQX;

                (_, QZMINX, QZPLX, ZPLQX) = TLAMB(M, Q, QSQFM1, X, -1);

                double VT2 = GMS * ZPLQX * Math.Sqrt(SIG);
                double VR1 = GMS * (QZMINX - QZPLX * RHO) / R1;
                double VT1 = VT2 / R1;
                double VR2 = -GMS * (QZMINX + QZPLX * RHO) / R2;
                VT2 /= R2;

                if (I == 1)
                {
                    VR11 = VR1;
                    VT11 = VT1;
                    VR12 = VR2;
                    VT12 = VT2;
                }
                else
                {
                    VR21 = VR1;
                    VT21 = VT1;
                    VR22 = VR2;
                    VT22 = VT2;
                }
            }

            return (N, VR11, VT11, VR12, VT12, VR21, VT21, VR22, VT22);
        }

        private static ( int N, double X, double XPL) XLAMB(int M, double Q, double QSQFM1, double TIN)
        {
            Check.Finite(M);
            Check.Finite(Q);
            Check.Finite(QSQFM1);
            Check.Finite(TIN);

            const double TOL = 3e-7;
            const double C0 = 1.7;
            const double C1 = 0.5;
            const double C2 = 0.03;
            const double C3 = 0.15;
            const double C41 = 1.0;
            const double C42 = 0.24;
            double THR2 = Math.Atan2(QSQFM1, 2.0 * Q) / Math.PI;
            double T, T0, DT, D2T, D3T;
            double D2T2 = 0.0;
            double TMIN = 0.0;
            double TDIFF;
            double TDIFFM = 0.0;
            double XM = 0.0;
            double W;
            double X = 0.0;
            double XPL = 0.0;
            int N;
            if (M == 0)
            {
                /* "SINGLE-REV STARTER FROM T (AT X = 0) & BILINEAR (USUALLY)" -- Gooding */
                N = 1;

                (T0, _, _, _) = TLAMB(M, Q, QSQFM1, 0.0, 0);

                TDIFF = TIN - T0;
                if (TDIFF <= 0.0)
                {
                    X = T0 * TDIFF / (-4.0 * TIN);
                    /* "-4 IS THE VALUE OF DT, FOR X = 0" -- Gooding */
                }
                else
                {
                    X = -TDIFF / (TDIFF + 4.0);
                    W = X + C0 * Math.Sqrt(2.0 * (1.0 - THR2));
                    if (W < 0.0)
                        X -= Math.Sqrt(Math.Pow(-W, 1.0 / 8.0)) * (X + Math.Sqrt(TDIFF / (TDIFF + 1.5 * T0)));
                    W =  4.0 / (4.0 + TDIFF);
                    X *= 1.0 + X * (C1 * W - C2 * X * Math.Sqrt(W));
                }
            }
            else
            {
                /* "WITH MUTIREVS, FIRST GET T(MIN) AS BASIS FOR STARTER */
                XM = 1.0 / (1.5 * (M + 0.5) * Math.PI);
                if (THR2 < 0.5)
                    XM = Math.Pow(2.0 * THR2, 1.0 / 8.0) * XM;
                if (THR2 > 0.5)
                    XM = (2.0 - Math.Pow(2.0 - 2.0 * THR2, 1.0 / 8.0)) * XM;
                /* "STARTER FOR TMIN" */
                for (int I = 1; I <= 12; I++)
                {
                    (TMIN, DT, D2T, D3T) = TLAMB(M, Q, QSQFM1, XM, 3);
                    if (D2T == 0.0)
                        goto Two;
                    double XMOLD = XM;
                    XM -= DT * D2T / (D2T * D2T - DT * D3T / 2.0);
                    double XTEST = Math.Abs(XMOLD / XM - 1.0);
                    if (XTEST <= TOL)
                        goto Two;
                }

                N = -1;
                return (N, X, XPL);
                /* "(BREAK OFF & EXIT IF TMIN NOT LOCATED - SHOULD NEVER HAPPEN)" */
                /* "NOW PROCEED FROM T(MIN) TO FULL STARTER" -- Gooding */
                Two:
                TDIFFM = TIN - TMIN;
                if (TDIFFM < 0.0)
                {
                    N = 0;
                    return (N, X, XPL);
                    /* "EXIT IF NO SOLUTION ALTREADY FROM X(TMIN)" -- Gooding */
                }

                if (TDIFFM == 0.0)
                {
                    X = XM;
                    N = 1;
                    return (N, X, XPL);
                    /* "EXIT IF UNIQUE SOLUTION ALREADY FROM X(TMIN) -- Gooding */
                }

                N = 3;
                if (D2T == 0.0)
                    D2T = 6.0 * M * Math.PI;
                X    = Math.Sqrt(TDIFFM / (D2T / 2.0 + TDIFFM / Math.Pow(1.0 - XM, 2.0)));
                W    = XM + X;
                W    = W * 4.0 / (4.0 + TDIFFM) + Math.Pow(1.0 - W, 2.0);
                X    = X * (1.0 - (1.0 + M + C41 * (THR2 - 0.5)) / (1.0 + C3 * M) * X * (C1 * W + C2 * X * Math.Sqrt(W))) + XM;
                D2T2 = D2T / 2.0;
                if (X >= 1.0)
                {
                    N = 1;
                    goto Three;
                }
                /* "(NO FINITE SOLUTION WITH X > XM)" -- Gooding */
            }

            /* "(NOW HAVE A STARTER, SO PROCEED BY HALLEY)" -- Gooding */
            Five:
            for (int I = 1; I <= 3; I++)
            {
                (T, DT, D2T, _) = TLAMB(M, Q, QSQFM1, X, 2);

                T = TIN - T;
                if (DT != 0.0)
                    X += T * DT / (DT * DT + T * D2T / 2.0);
            }

            if (N != 3)
                return (N, X, XPL);
            /* "(EXIT IF ONLY ONE SOLUTION, NORMALLY WHEN M = 0)" */

            N   = 2;
            XPL = X;
            Three:
            /* "(SECOND MULTI-REV STARTER)" */
            (T0, _, _, _) = TLAMB(M, Q, QSQFM1, 0.0, 0);

            double TDIFF0 = T0 - TMIN;
            TDIFF = TIN - T0;
            if (TDIFF <= 0.0)
            {
                X = XM - Math.Sqrt(TDIFFM / (D2T2 - TDIFFM * (D2T2 / TDIFF0 - 1.0 / Math.Pow(XM, 2))));
            }
            else
            {
                X = -TDIFF / (TDIFF + 4.0);
                W = X + C0 * Math.Sqrt(2.0 * (1.0 - THR2));
                if (W < 0.0)
                    X -= Math.Sqrt(Math.Pow(-W, 1.0 / 8.0)) * (X + Math.Sqrt(TDIFF / (TDIFF + 1.5 * T0)));
                W =  4.0 / (4.0 + TDIFF);
                X *= 1.0 + (1.0 + M + C42 * (THR2 - 0.5)) / (1.0 + C3 * M) * X * (C1 * W - C2 * X * Math.Sqrt(W));
                if (X <= -1.0)
                {
                    N -= 1;
                    /* "(NO FINITE SOLUTION WITH X < XM)" -- Gooding */
                    if (N == 1)
                        X = XPL;
                }
            }

            goto Five;
        }

        private static ( double T, double DT, double D2T, double D3T) TLAMB(int M, double Q, double QSQFM1, double X, int N)
        {
            Check.Finite(X);
            Check.Finite(QSQFM1);
            Check.Finite(Q);
            Check.Finite(M);
            Check.Finite(N);

            const double SW = 0.4;
            bool LM1 = N == -1;
            bool L1 = N >= 1;
            bool L2 = N >= 2;
            bool L3 = N == 3;
            double QSQ = Q * Q;
            double XSQ = X * X;
            double U = (1.0 - X) * (1.0 + X);
            double T = 0.0;

            double DT = 0.0;
            double D2T = 0.0;
            double D3T = 0.0;

            if (!LM1)
            {
                /* "NEEDED IF SERIES AND OTHERWISE USEFUL WHEN Z = 0" -- Gooding */
                DT  = 0.0;
                D2T = 0.0;
                D3T = 0.0;
            }

            if (LM1 || M > 0.0 || X < 0.0 || Math.Abs(U) > SW)
            {
                /* "DIRECT COMPUTATION (NOT SERIES)" -- Gooding */
                double Y = Math.Sqrt(Math.Abs(U));
                double Z = Math.Sqrt(QSQFM1 + QSQ * XSQ);
                double QX = Q * X;

                double A = 0.0;
                double B = 0.0;
                double AA = 0.0;
                double BB = 0.0;

                if (QX <= 0.0)
                {
                    A = Z - QX;
                    B = Q * Z - X;
                }

                if (QX < 0.0 && LM1)
                {
                    AA = QSQFM1 / A;
                    BB = QSQFM1 * (QSQ * U - XSQ) / B;
                }

                if ((QX == 0.0 && LM1) || QX > 0.0)
                {
                    AA = Z + QX;
                    BB = Q * Z + X;
                }

                if (QX > 0.0)
                {
                    A = QSQFM1 / AA;
                    B = QSQFM1 * (QSQ * U - XSQ) / BB;
                }

                if (!LM1)
                {
                    double G;
                    if (QX * U >= 0.0)
                    {
                        G = X * Z + Q * U;
                    }
                    else
                    {
                        G = (XSQ - QSQ * U) / (X * Z - Q * U);
                    }

                    double F = A * Y;
                    if (X <= 1.0)
                    {
                        T = M * Math.PI + Math.Atan2(F, G);
                    }
                    else
                    {
                        if (F > SW)
                        {
                            T = Math.Log(F + G);
                        }
                        else
                        {
                            double FG1 = F / (G + 1.0);
                            double TERM = 2.0 * FG1;
                            double FG1SQ = FG1 * FG1;
                            T = TERM;
                            double TWOI1 = 1.0;
                            double TOLD;
                            do
                            {
                                TWOI1 += 2.0;
                                TERM  *= FG1SQ;
                                TOLD  =  T;
                                T     += TERM / TWOI1;
                            } while (T != TOLD); /* "CONTINUE LOOPING FOR THE INVERSE TANH" -- Gooding */
                        }
                    }

                    T = 2.0 * (T / Y + B) / U;
                    if (L1 && Z != 0.0)
                    {
                        double QZ = Q / Z;
                        double QZ2 = QZ * QZ;
                        QZ *= QZ2;
                        DT =  (3.0 * X * T - 4.0 * (A + QX * QSQFM1) / Z) / U;
                        if (L2)
                        {
                            D2T = (3.0 * T + 5.0 * X * DT + 4.0 * QZ * QSQFM1) / U;
                        }

                        if (L3)
                        {
                            D3T = (8.0 * DT + 7.0 * X * D2T - 12.0 * QZ * QZ2 * X * QSQFM1) / U;
                        }
                    }
                }
                else
                {
                    DT  = B;
                    D2T = BB;
                    D3T = AA;
                }
            }
            else
            {
                /* "COMPUTE BY SERIES" -- Gooding */
                double U0I = 1.0;
                double U1I = 0.0;
                double U2I = 0.0;
                double U3I = 0.0;

                if (L1)
                    U1I = 1.0;
                if (L2)
                    U2I = 1.0;
                if (L3)
                    U3I = 1.0;
                double TERM = 4.0;
                double TQ = Q * QSQFM1;
                int I = 0;
                double TQSUM = 0.0;
                if (Q < 0.5)
                    TQSUM = 1.0 - Q * QSQ;
                if (Q >= 0.5)
                    TQSUM = (1.0 / (1.0 + Q) + Q) * QSQFM1;
                double TTMOLD = TERM / 3.0;
                T = TTMOLD * TQSUM;
                double TOLD;
                do
                {
                    I++;
                    int P = I;
                    U0I *= U;
                    if (L1 && I > 1)
                        U1I *= U;
                    if (L2 && I > 2)
                        U2I *= U;
                    if (L3 && I > 3)
                        U3I *= U;
                    TERM  =  TERM * (P - 0.5) / P;
                    TQ    *= QSQ;
                    TQSUM += TQ;
                    TOLD  =  T;
                    double TTERM = TERM / (2.0 * P + 3.0);
                    double TQTERM = TTERM * TQSUM;
                    T      -= U0I * ((1.5 * P + 0.25) * TQTERM / (P * P - 0.25) - TTMOLD * TQ);
                    TTMOLD =  TTERM;
                    TQTERM *= P;
                    if (L1)
                        DT += TQTERM * U1I;
                    if (L2)
                        D2T += TQTERM * U2I * (P - 1.0);
                    if (L3)
                        D3T += TQTERM * U3I * (P - 1.0) * (P - 2.0);
                } while (I < N || T != TOLD);

                if (L3)
                    D3T = 8.0 * X * (1.5 * D2T - XSQ * D3T);
                if (L2)
                    D2T = 2.0 * (2.0 * XSQ * D2T - DT);
                if (L1)
                    DT = -2.0 * X * DT;
                T /= XSQ;
            }

            return (T, DT, D2T, D3T);
        }
    }
}
