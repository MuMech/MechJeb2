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

        private static V3 DeltaV(double mu, V3 r, V3 v, double value, Type type)
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

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            alglib.minnlcoptimize(state, NLPFunction, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"DeltaVToChangeApsis({mu}, {r}, {v}, {value}, {type}): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            return rot * new V3(x[0], x[1], 0) * scale.VelocityScale;
        }

        public static V3 ChangePeriapsis(double mu, V3 r, V3 v, double peR)
        {
            if (peR < 0 || (peR > r.magnitude) | !peR.IsFinite())
                throw new ArgumentException($"Bad periapsis in ChangeOrbitalElement = {peR}");

            return DeltaV(mu, r, v, peR, Type.PERIAPSIS);
        }

        public static V3 ChangeApoapsis(double mu, V3 r, V3 v, double apR)
        {
            if (!apR.IsFinite() || (apR > 0 && apR < r.magnitude))
                throw new ArgumentException($"Bad apoapsis in ChangeOrbitalElement = {apR}");

            return DeltaV(mu, r, v, apR, Type.APOAPSIS);
        }

        public static V3 ChangeSMA(double mu, V3 r, V3 v, double sma)
        {
            if (!sma.IsFinite())
                throw new ArgumentException($"Bad SMA in ChangeOrbitalElement = {sma}");

            return DeltaV(mu, r, v, sma, Type.SMA);
        }

        public static V3 ChangeECC(double mu, V3 r, V3 v, double ecc)
        {
            if (!ecc.IsFinite() || ecc < 0)
                throw new ArgumentException($"Bad Ecc in ChangeOrbitalElement = {ecc}");

            return DeltaV(mu, r, v, ecc, Type.ECC);
        }
    }
}
