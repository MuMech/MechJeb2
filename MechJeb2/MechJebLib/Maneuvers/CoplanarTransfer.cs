using System;
using MechJebLib.Core;
using MechJebLib.Core.TwoBody;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static System.Math;

namespace MechJebLib.Maneuvers
{
    public class CoplanarTransfer
    {
        private struct Args
        {
            public V3   R1;
            public V3   V1;
            public V3   R2;
            public V3   V2;
            public bool Rendezvous;
        }

        private static (V3 dv1, V3 dv2) LambertFunction(double dt, double tt, double dt2, Args args)
        {
            V3 r1 = args.R1;
            V3 v1 = args.V1;
            V3 r2 = args.R2;
            V3 v2 = args.V2;

            (V3 rburn, V3 vburn) = Shepperd.Solve(1.0, dt, r1, v1);
            double targdt = dt + tt;
            if (!args.Rendezvous)
                targdt += dt2;
            (V3 rf2, V3 vf2) = Shepperd.Solve(1.0, targdt, r2, v2);
            (V3 vi, V3 vf)   = Gooding.Solve(1.0, rburn, vburn, rf2, tt, 0);

            return (vi - vburn, vf2 - vf);
        }

        private static void NLPFunction(double[] x, ref double func, object obj)
        {
            var args = (Args)obj;

            double dt = x[0];
            double tt = x[1];
            double dt2 = x[2];

            if (tt == 0)
            {
                func = 1e+300;
                return;
            }

            (V3 dv1, V3 dv2) = LambertFunction(dt, tt, dt2, args);

            func = dv1.magnitude + dv2.magnitude;

            if (args.Rendezvous) // drive unused dt2 to zero
                func += dt2 * dt2;
        }

        public static (V3 dv1, double dt, V3 dv2, double tt) Maneuver(double mu, V3 r1, V3 v1, V3 r2, V3 v2, double dtguess, double dt2guess,
            bool coplanar = true, bool rendezvous = true, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity,
            double ttmin = double.NegativeInfinity,
            double ttmax = double.PositiveInfinity, bool optguard = false)
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

            r1       /= scale.LengthScale;
            v1       /= scale.VelocityScale;
            r2       /= scale.LengthScale;
            v2       /= scale.VelocityScale;
            dtguess  /= scale.TimeScale;
            dt2guess /= scale.TimeScale;
            dtmin    /= scale.TimeScale;
            dtmax    /= scale.TimeScale;
            ttmin    /= scale.TimeScale;
            ttmax    /= scale.TimeScale;

            (_, _, double ttguess, _) = Maths.HohmannTransferParameters(1.0, r1, r2);

            x[0] = dtguess;
            x[1] = ttguess;
            x[2] = dt2guess;

            var args = new Args
            {
                R1         = r1,
                V1         = v1,
                R2         = r2,
                V2         = v2,
                Rendezvous = rendezvous
            };

            double[] bndl = { dtmin, ttmin, double.NegativeInfinity };
            double[] bndu = { dtmax, ttmax, double.PositiveInfinity };

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
                    $"CoplanarTransfer.Maneuver({mu}, {r1}, {v1}, {r2}, {v2}): CG solver terminated abnormally: {rep.terminationtype}"
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

            return (dv1 * scale.VelocityScale, x[0] * scale.TimeScale, dv2 * scale.VelocityScale, x[1] * scale.TimeScale);
        }

        public static (V3 dv1, double dt, V3 dv2, double tt) NextManeuver(double mu, V3 r1, V3 v1, V3 r2, V3 v2, int maxiter = 50,
            bool coplanar = true, bool rendezvous = true, bool optguard = false)
        {
            double synodicPeriod = Maths.SynodicPeriod(mu, r1, v1, r2, v2);
            double targetPeriod = Maths.PeriodFromStateVectors(mu, r2, v2);
            double dtguess = 0;

            for (int iter = 0; iter < maxiter; iter++)
            {
                (V3 dv1, double dt, V3 dv2, double tt) =
                    Maneuver(mu, r1, v1, r2, v2, dtguess, 0, coplanar, optguard: optguard, rendezvous: rendezvous);

                // we have to try the other side of the target orbit since we might get eg. the DN instead of the AN when the AN is closer
                // (this may be insufficient and may need more of a search box but then we're O(N^2) and i think basinhopping or porkchop
                // plots will be the better solution)
                if (!rendezvous)
                {
                    (V3 a, double b, V3 c, double d) =
                        Maneuver(mu, r1, v1, r2, v2, dtguess, targetPeriod * 0.5, coplanar, optguard: optguard, rendezvous: rendezvous);
                    if (b > 0 && (b < dt || dt < 0))
                    {
                        dv1 = a;
                        dt  = b;
                        dv2 = c;
                        tt  = d;
                    }
                }

                if (dt > 0)
                    return (dv1, dt, dv2, tt);
                dtguess += synodicPeriod * 0.10;
            }

            throw new MechJebLibException($"CoplanarTransfer.NextManeuver({mu}, {r1}, {v1}, {v2}, {r2}): too many iterations");
        }
    }
}
