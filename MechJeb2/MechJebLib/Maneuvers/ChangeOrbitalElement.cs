using System;
using MechJebLib.Primitives;

namespace MuMech.MechJebLib.Maneuvers
{
    public static class ChangeOrbitalElement
    {
        public enum Type { PERIAPSIS, APOAPSIS }

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
            double newR = args.Value;
            Type type = args.Type;

            fi[0] = dv.sqrMagnitude;
            switch (type)
            {
                case Type.PERIAPSIS:
                    fi[1] = global::MechJebLib.Core.Maths.PeriapsisFromStateVectors(1.0, p, q + dv) - newR;
                    break;
                case Type.APOAPSIS:
                    fi[1] = 1.0 / global::MechJebLib.Core.Maths.ApoapsisFromStateVectors(1.0, p, q + dv) - 1.0 / newR;
                    break;
            }
        }

        public static V3 DeltaV(double mu, V3 r, V3 v, double value, Type type)
        {
            switch (type)
            {
                case Type.PERIAPSIS:
                    if (value < 0 || (value > r.magnitude) | !value.IsFinite())
                        throw new ArgumentException($"Bad periapsis in DeltaVToChangeApsis = {value}");
                    break;
                case Type.APOAPSIS:
                    if (!value.IsFinite() || (value > 0 && value < r.magnitude))
                        throw new ArgumentException($"Bad apoapsis in DeltaVToChangeApsis = {value}");
                    break;
            }

            const double DIFFSTEP = 1e-10;
            const double EPSX = 1e-6;
            const int MAXITS = 1000;

            const int NVARIABLES = 2;
            const int NEQUALITYCONSTRAINTS = 1;
            const int NINEQUALITYCONSTRAINTS = 0;

            double[] x = new double[NVARIABLES];

            double scaleDistance = Math.Sqrt(r.magnitude * Math.Abs(value));
            scaleDistance = Math.Min(scaleDistance, r.sqrMagnitude);
            double scaleVelocity = Math.Sqrt(mu / scaleDistance);

            (V3 p, V3 q, Q3 rot) = global::MechJebLib.Core.Maths.PerifocalFromStateVectors(mu, r, v);

            x[0] = 0; // maneuver x
            x[1] = 0; // maneuver y

            var args = new Args { P = p / scaleDistance, Q = q / scaleVelocity, Value = value / scaleDistance, Type = type };

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetstpmax(state, 1e-2);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            alglib.minnlcoptimize(state, NLPFunction, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"DeltaVToChangeApsis({mu}, {r}, {v}, {value}, {type}): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            return rot * new V3(x[0], x[1], 0) * scaleVelocity;
        }
    }
}
