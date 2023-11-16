using System;
using MechJebLib.Core;
using MechJebLib.Core.Lambert;
using MechJebLib.Core.TwoBody;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static System.Math;

namespace MechJebLib.Maneuvers
{
    public static class TwoImpulseTransfer
    {
        private struct Args
        {
            public V3   R1;
            public V3   V1;
            public V3   R2;
            public V3   V2;
            public bool Capture;
        }

        private static (V3 dv1, V3 dv2) LambertFunction(double dt, double tt, double offset, Args args)
        {
            V3 r1 = args.R1;
            V3 v1 = args.V1;
            V3 r2 = args.R2;
            V3 v2 = args.V2;

            (V3 rburn, V3 vburn) = Shepperd.Solve(1.0, dt, r1, v1);
            (V3 rf2, V3 vf2)     = Shepperd.Solve(1.0, dt + tt + offset, r2, v2);
            (V3 vi, V3 vf)       = Gooding.Solve(1.0, rburn, vburn, rf2, tt, 0);

            return (vi - vburn, vf2 - vf);
        }

        private static void NLPFunction(double[] x, ref double func, object obj)
        {
            var args = (Args)obj;

            double dt = x[0];
            double tt = x[1];
            double offset = x[2];

            if (tt == 0)
            {
                func = 1e+300;
                return;
            }

            (V3 dv1, V3 dv2) = LambertFunction(dt, tt, offset, args);

            if (args.Capture)
                func = dv1.magnitude + dv2.magnitude;
            else
                func = dv1.magnitude;
        }

        private static (V3 dv1, double dt1, V3 dv2, double dt2) Maneuver(double mu, V3 r1, V3 v1, V3 r2, V3 v2, double dtguess, double offsetGuess,
            bool coplanar = true, bool capture = true, double dtmin = double.NegativeInfinity,
            double dtmax = double.PositiveInfinity,
            double ttmin = double.NegativeInfinity, double ttmax = double.PositiveInfinity, double offsetMin = double.NegativeInfinity,
            double offsetMax = double.PositiveInfinity, bool optguard = false)
        {
            if (!mu.IsFinite())
                throw new ArgumentException("bad mu in ChangeOrbitalElement");
            if (!r1.IsFinite())
                throw new ArgumentException("bad r1 in ChangeOrbitalElement");
            if (!v1.IsFinite())
                throw new ArgumentException("bad v1 in ChangeOrbitalElement");
            if (!r2.IsFinite())
                throw new ArgumentException("bad r2 in ChangeOrbitalElement");
            if (!v2.IsFinite())
                throw new ArgumentException("bad v2 in ChangeOrbitalElement");

            const double DIFFSTEP = 1e-6;
            const double EPSX = 1e-9;
            const int MAXITS = 1000;

            const int NVARIABLES = 3;

            double[] x = new double[NVARIABLES];

            var scale = Scale.Create(mu, Sqrt(r1.magnitude * r2.magnitude));

            if (coplanar)
            {
                // this rotation changes the target orbit to be coplanar
                V3 hhat1 = V3.Cross(r1, v1).normalized;
                V3 hhat2 = V3.Cross(r2, v2).normalized;
                var toCoplanar = Q3.FromToRotation(hhat2, hhat1);

                r2 = toCoplanar * r2;
                v2 = toCoplanar * v2;
            }

            r1          /= scale.LengthScale;
            v1          /= scale.VelocityScale;
            r2          /= scale.LengthScale;
            v2          /= scale.VelocityScale;
            dtguess     /= scale.TimeScale;
            offsetGuess /= scale.TimeScale;
            dtmin       /= scale.TimeScale;
            dtmax       /= scale.TimeScale;
            ttmin       /= scale.TimeScale;
            ttmax       /= scale.TimeScale;
            offsetMin   /= scale.TimeScale;
            offsetMax   /= scale.TimeScale;

            (_, _, double ttguess, _) = Maths.HohmannTransferParameters(1.0, r1, r2);

            x[0] = dtguess;
            x[1] = ttguess;
            x[2] = offsetGuess;

            var args = new Args
            {
                R1      = r1,
                V1      = v1,
                R2      = r2,
                V2      = v2,
                Capture = capture
            };

            double[] bndl = { dtmin, ttmin, offsetMin };
            double[] bndu = { dtmax, ttmax, offsetMax };

            alglib.minbleiccreatef(x, DIFFSTEP, out alglib.minbleicstate state);
            alglib.minbleicsetbc(state, bndl, bndu);
            alglib.minbleicsetcond(state, 0.0, 0.0, EPSX, MAXITS);
            alglib.minbleicsetstpmax(state, Maths.SynodicPeriod(1.0, r1, v1, r2, v2) / 2);

            if (optguard)
            {
                alglib.minbleicoptguardsmoothness(state);
            }

            alglib.minbleicoptimize(state, NLPFunction, null, args);
            alglib.minbleicresults(state, out x, out alglib.minbleicreport rep);

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"TwoImpulseTransfer.Maneuver({mu}, {r1}, {v1}, {r2}, {v2}): CG solver terminated abnormally: {rep.terminationtype}"
                );

            if (optguard)
            {
                alglib.minbleicoptguardresults(state, out alglib.optguardreport ogrep);
                if (ogrep.badgradsuspected)
                    throw new MechJebLibException("badgradsuspected error");
                if (ogrep.nonc0suspected)
                    throw new MechJebLibException("nonc0suspected error");
                if (ogrep.nonc1suspected)
                    throw new MechJebLibException("nonc1suspected error");
            }

            (V3 dv1, V3 dv2) = LambertFunction(x[0], x[1], x[2], args);

            return (dv1 * scale.VelocityScale, x[0] * scale.TimeScale, dv2 * scale.VelocityScale, (x[0] + x[1]) * scale.TimeScale);
        }

        private static (V3 dv1, double dt1, V3 dv2, double dt2) ManeuverInternal(double mu, V3 r1, V3 v1, V3 r2, V3 v2, double dtguess,
            double lagTime = double.NaN, bool coplanar = true, bool rendezvous = true, bool capture = true,
            bool fixedtime = false, bool optguard = false)
        {
            V3 dv1, dv2;
            double dt1, dt2;

            double dtmin = double.NegativeInfinity;
            double dtmax = double.PositiveInfinity;

            if (fixedtime)
            {
                dtmin = dtguess;
                dtmax = dtguess;
            }

            if (rendezvous)
            {
                double offsetGuess = 0;
                double offsetMin = 0;
                double offsetMax = 0;
                if (lagTime.IsFinite())
                {
                    offsetMin   = -lagTime;
                    offsetMax   = -lagTime;
                    offsetGuess = -lagTime;
                }

                (dv1, dt1, dv2, dt2) =
                    Maneuver(mu, r1, v1, r2, v2, dtguess, offsetGuess, dtmin: dtmin, dtmax: dtmax, offsetMin: offsetMin, offsetMax: offsetMax,
                        coplanar: coplanar, capture: capture, optguard: optguard);
            }
            else
            {
                (dv1, dt1, dv2, dt2) =
                    Maneuver(mu, r1, v1, r2, v2, dtguess, 0, dtmin: dtmin, dtmax: dtmax, coplanar: coplanar, capture: capture, optguard: optguard);

                // we have to try the other side of the target orbit since we might get eg. the DN instead of the AN when the AN is closer
                // (this may be insufficient and may need more of a search box but then we're O(N^2) and i think basinhopping or porkchop
                // plots will be the better solution)
                double targetPeriod = Maths.PeriodFromStateVectors(mu, r2, v2);

                (V3 a, double b, V3 c, double d) =
                    Maneuver(mu, r1, v1, r2, v2, dtguess, targetPeriod * 0.5, coplanar, capture, optguard: optguard);

                if (b > 0 && (b < dt1 || dt1 < 0))
                {
                    dv1 = a;
                    dt1 = b;
                    dv2 = c;
                    dt2 = d;
                }
            }

            return (dv1, dt1, dv2, dt2);
        }

        public static (V3 dv1, double dt1, V3 dv2, double dt2) NextManeuver(double mu, V3 r1, V3 v1, V3 r2, V3 v2, int maxiter = 50,
            double lagTime = double.NaN, bool coplanar = true, bool rendezvous = true, bool capture = true,
            bool fixedTime = false, bool optguard = false)
        {
            double synodicPeriod = Maths.SynodicPeriod(mu, r1, v1, r2, v2);

            if (fixedTime)
                return ManeuverInternal(mu, r1, v1, r2, v2, 0, coplanar: coplanar, rendezvous: rendezvous, capture: capture, optguard: optguard,
                    lagTime: lagTime, fixedtime: true);

            double dtguess = 0;
            for (int iter = 0; iter < maxiter; iter++)
            {
                (V3 dv1, double dt1, V3 dv2, double dt2) = ManeuverInternal(mu, r1, v1, r2, v2, dtguess, coplanar: coplanar, rendezvous: rendezvous,
                    capture: capture, optguard: optguard, lagTime: lagTime);

                if (dt1 > 0)
                    return (dv1, dt1, dv2, dt2);
                dtguess += synodicPeriod * 0.10;
            }

            throw new MechJebLibException($"TwoImpulseTransfer.NextManeuver({mu}, {r1}, {v1}, {v2}, {r2}): too many iterations");
        }
    }
}
