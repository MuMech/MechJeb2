/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Core;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

#nullable enable

namespace MechJebLib.Maneuvers
{
    public static class ChangeOrbitalElement
    {
        private enum Type { PERIAPSIS, APOAPSIS, SMA, ECC }

        private struct Args
        {
            public V3     P;
            public V3     Q;
            public double Value;
            public Type   Type;
        }

        private static void NLPFunction2(double[] x, double[] fi, double[,] jac, object obj)
        {
            var dv0 = new DualV3(x[0], x[1], 0, 1, 0, 0);
            var dv1 = new DualV3(x[0], x[1], 0, 0, 1, 0);

            var args = (Args)obj;
            V3 p = args.P;
            V3 q = args.Q;
            double value = args.Value;
            Type type = args.Type;

            Dual sqM0 = dv0.sqrMagnitude;
            Dual sqM1 = dv1.sqrMagnitude;

            fi[0]     = sqM0.M;
            jac[0, 0] = sqM0.D;
            jac[0, 1] = sqM1.D;

            switch (type)
            {
                case Type.PERIAPSIS:
                    Dual peR0 = Maths.PeriapsisFromStateVectors(1.0, p, q + dv0) - value;
                    Dual peR1 = Maths.PeriapsisFromStateVectors(1.0, p, q + dv1) - value;
                    fi[1]     = peR0.M;
                    jac[1, 0] = peR0.D;
                    jac[1, 1] = peR1.D;
                    break;
                case Type.APOAPSIS:
                    Dual apR0 = 1.0 / Maths.ApoapsisFromStateVectors(1.0, p, q + dv0) - 1.0 / value;
                    Dual apR1 = 1.0 / Maths.ApoapsisFromStateVectors(1.0, p, q + dv1) - 1.0 / value;
                    fi[1]     = apR0.M;
                    jac[1, 0] = apR0.D;
                    jac[1, 1] = apR1.D;
                    break;
                case Type.SMA:
                    Dual sma0 = 1.0 / Maths.SmaFromStateVectors(1.0, p, q + dv0) - 1.0 / value;
                    Dual sma1 = 1.0 / Maths.SmaFromStateVectors(1.0, p, q + dv1) - 1.0 / value;
                    fi[1]     = sma0.M;
                    jac[1, 0] = sma0.D;
                    jac[1, 1] = sma1.D;
                    break;
                case Type.ECC:
                    Dual ecc0 = Maths.EccFromStateVectors(1.0, p, q + dv0) - value;
                    Dual ecc1 = Maths.EccFromStateVectors(1.0, p, q + dv1) - value;
                    fi[1]     = ecc0.M;
                    jac[1, 0] = ecc0.D;
                    jac[1, 1] = ecc1.D;
                    break;
            }
        }

        private static void NLPFunction(double[] x, double[] fi, object obj)
        {
            var dv = new V3(x[0], x[1], 0);

            var args = (Args)obj;
            V3 p = args.P;
            V3 q = args.Q;
            double value = args.Value;
            Type type = args.Type;

            fi[0] = dv.sqrMagnitude;

            switch (type)
            {
                case Type.PERIAPSIS:
                    fi[1] = Maths.PeriapsisFromStateVectors(1.0, p, q + dv) - value;
                    break;
                case Type.APOAPSIS:
                    fi[1] = 1.0 / Maths.ApoapsisFromStateVectors(1.0, p, q + dv) - 1.0 / value;
                    break;
                case Type.SMA:
                    fi[1] = 1.0 / Maths.SmaFromStateVectors(1.0, p, q + dv) - 1.0 / value;
                    break;
                case Type.ECC:
                    double ecc = Maths.EccFromStateVectors(1.0, p, q + dv);
                    fi[1] = ecc - value;
                    break;
            }
        }

        private static V3 DeltaV(double mu, V3 r, V3 v, double value, Type type, bool optguard=false)
        {
            if (!mu.IsFinite())
                throw new ArgumentException("bad mu in ChangeOrbitalElement");
            if (!r.IsFinite())
                throw new ArgumentException("bad r in ChangeOrbitalElement");
            if (!v.IsFinite())
                throw new ArgumentException("bad v in ChangeOrbitalElement");

            const double DIFFSTEP = 1e-7;
            const double EPSX = 1e-15;
            const int MAXITS = 1000;

            const int NVARIABLES = 2;
            const int NEQUALITYCONSTRAINTS = 1;
            const int NINEQUALITYCONSTRAINTS = 0;

            double[] x = new double[NVARIABLES];

            var scale = Scale.Create(mu, r.magnitude);

            (V3 p, V3 q, Q3 rot) = Maths.PerifocalFromStateVectors(mu, r, v);

            p /= scale.LengthScale;
            q /= scale.VelocityScale;

            if (type == Type.ECC)
            {
                // changing the ECC is actually a global optimization problem due to basins around parabolic ecc == 1.0
                double boost = Sign(value - 1.0) * 0.1 + Sqrt(2);

                V3 dv = Core.Functions.Maneuvers.DeltaVRelativeToCircularVelocity(1.0, p, q, boost);
                x[0] = dv.x;
                x[1] = dv.y;
            }
            else
            {
                x[0] = 0; // maneuver vy
                x[1] = 0; // maneuver vy
            }

            var args = new Args { P = p, Q = q, Value = type == Type.ECC ? value : value / scale.LengthScale, Type = type };

            alglib.minnlccreate(NVARIABLES, x, out alglib.minnlcstate state);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            if (optguard)
            {
                alglib.minnlcoptguardsmoothness(state);
                alglib.minnlcoptguardgradient(state, DIFFSTEP);
            }

            alglib.minnlcoptimize(state, NLPFunction2, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"ChangeOrbitalElement.DeltaV({mu}, {r}, {v}, {value}, {type}): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            if (optguard)
            {
                alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep);
                if (ogrep.badgradsuspected || ogrep.nonc0suspected || ogrep.nonc1suspected)
                    throw new Exception("alglib optguard caught an error, i should report better on errors now");
            }

            return rot * new V3(x[0], x[1], 0) * scale.VelocityScale;
        }

        public static V3 ChangePeriapsis(double mu, V3 r, V3 v, double peR, bool optguard = false)
        {
            if (peR < 0 || (peR > r.magnitude) | !peR.IsFinite())
                throw new ArgumentException($"Bad periapsis in ChangeOrbitalElement = {peR}");

            return DeltaV(mu, r, v, peR, Type.PERIAPSIS, optguard);
        }

        public static V3 ChangeApoapsis(double mu, V3 r, V3 v, double apR, bool optguard = false)
        {
            if (!apR.IsFinite() || (apR > 0 && apR < r.magnitude))
                throw new ArgumentException($"Bad apoapsis in ChangeOrbitalElement = {apR}");

            return DeltaV(mu, r, v, apR, Type.APOAPSIS, optguard);
        }

        public static V3 ChangeSMA(double mu, V3 r, V3 v, double sma, bool optguard = false)
        {
            if (!sma.IsFinite())
                throw new ArgumentException($"Bad SMA in ChangeOrbitalElement = {sma}");

            return DeltaV(mu, r, v, sma, Type.SMA, optguard);
        }

        public static V3 ChangeECC(double mu, V3 r, V3 v, double ecc, bool optguard = false)
        {
            if (!ecc.IsFinite() || ecc < 0)
                throw new ArgumentException($"Bad Ecc in ChangeOrbitalElement = {ecc}");

            return DeltaV(mu, r, v, ecc, Type.ECC, optguard);
        }
    }
}
