using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Maneuvers
{
    public static class ChangeOrbitalElement
    {
        public enum Type { PERIAPSIS, APOAPSIS, SMA, ECC }

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
                    fi[1] = Core.Maths.PeriapsisFromStateVectors(1.0, p, q + dv) - value;
                    break;
                case Type.APOAPSIS:
                    fi[1] = 1.0 / Core.Maths.ApoapsisFromStateVectors(1.0, p, q + dv) - 1.0 / value;
                    break;
                case Type.SMA:
                    fi[1] = 1.0 / Core.Maths.SmaFromStateVectors(1.0, p, q + dv) - 1.0 / value;
                    break;
                case Type.ECC:
                    double ecc = Core.Maths.EccFromStateVectors(1.0, p, q + dv);
                    fi[1] = ecc - value;
                    break;
            }
        }

        public static V3 DeltaV(double mu, V3 r, V3 v, double value, Type type)
        {
            if (!mu.IsFinite())
                throw new ArgumentException("bad mu in ChangeOrbitalElement");
            if (!r.IsFinite())
                throw new ArgumentException("bad r in ChangeOrbitalElement");
            if (!v.IsFinite())
                throw new ArgumentException("bad v in ChangeOrbitalElement");

            switch (type)
            {
                case Type.PERIAPSIS:
                    if (value < 0 || (value > r.magnitude) | !value.IsFinite())
                        throw new ArgumentException($"Bad periapsis in ChangeOrbitalElement = {value}");
                    break;
                case Type.APOAPSIS:
                    if (!value.IsFinite() || (value > 0 && value < r.magnitude))
                        throw new ArgumentException($"Bad apoapsis in ChangeOrbitalElement = {value}");
                    break;
                case Type.SMA:
                    if (!value.IsFinite())
                        throw new ArgumentException($"Bad SMA in ChangeOrbitalElement = {value}");
                    break;
                case Type.ECC:
                    if (!value.IsFinite() || value < 0)
                        throw new ArgumentException($"Bad Ecc in ChangeOrbitalElement = {value}");
                    break;
            }

            const double DIFFSTEP = 1e-7;
            const double EPSX = 1e-15;
            const int MAXITS = 1000;

            const int NVARIABLES = 2;
            const int NEQUALITYCONSTRAINTS = 1;
            const int NINEQUALITYCONSTRAINTS = 0;

            double[] x = new double[NVARIABLES];

            var scale = Scale.Create(mu, r.magnitude);

            (V3 p, V3 q, Q3 rot) = Core.Maths.PerifocalFromStateVectors(mu, r, v);

            p /= scale.LengthScale;
            q /= scale.VelocityScale;

            if (type == Type.ECC)
            {
                // changing the ECC is actually a global optimization problem due to basins around parabolic ecc == 1.0
                double boost = Math.Sign(value - 1.0) * 0.1 + Math.Sqrt(2);

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
    }
}
