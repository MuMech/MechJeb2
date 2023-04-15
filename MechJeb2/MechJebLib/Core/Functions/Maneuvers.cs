using System;
using MechJebLib.Core.TwoBody;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using MuMech;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Core.Functions
{
    public class Maneuvers
    {
        public static V3 DeltaVToCircularizeAfterTime(double mu, V3 r, V3 v, double dt)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Finite(dt);
            Check.Positive(mu);
            Check.NonZero(r);

            (V3 r1, V3 v1) = Farnocchia.Solve(mu, dt, r, v);
            var h = V3.Cross(r1, v1);
            return Maths.CircularVelocityFromHvec(mu, r, h) - v1;
        }

        public static V3 DeltaVToCircularizeAtPeriapsis(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            double dt = Maths.TimeToNextPeriapsis(mu, r, v);

            Check.Finite(dt);

            return DeltaVToCircularizeAfterTime(mu, r, v, dt);
        }

        public static V3 DeltaVToCircularizeAtApoapsis(double mu, V3 r, V3 v)
        {
            Check.Finite(mu);
            Check.Finite(r);
            Check.Finite(v);
            Check.Positive(mu);
            Check.NonZero(r);

            double dt = Maths.TimeToNextApoapsis(mu, r, v);

            Check.Finite(dt);

            return DeltaVToCircularizeAfterTime(mu, r, v, dt);
        }

        public static V3 DeltaVToChangeInclination(V3 r, V3 v, double newInc)
        {
            return Maths.VelocityForInclination(r, v, newInc) - v;
        }

        public static V3 DeltaVToChangeFPA(V3 r, V3 v, double newFPA)
        {
            return Maths.VelocityForFPA(r, v, newFPA) - v;
        }

        private struct ChangeApsisArgs
        {
            public V3     R;
            public V3     V;
            public double NewR;
            public bool   Periapsis;
        }

        private static void ChangeApsisFunction(double[] x, double[] fi, object obj)
        {
            var dv = new V3(x[0], x[1], x[2]);

            var args = (ChangeApsisArgs)obj;
            V3 r = args.R;
            V3 v = args.V;
            double newR = args.NewR;
            bool periapsis = args.Periapsis;

            fi[0] = dv.sqrMagnitude;
            if (periapsis)
                fi[1] = Maths.PeriapsisFromStateVectors(1.0, r, v + dv) - newR;
            else
                fi[1] = 1.0 / Maths.ApoapsisFromStateVectors(1.0, r, v + dv) - 1.0 / newR;
        }

        public static V3 DeltaVToChangeApsis(double mu, V3 r, V3 v, double newR, bool periapsis = true)
        {
            if (periapsis)
            {
                if (newR < 0 || (newR > r.magnitude) | !newR.IsFinite())
                    throw new ArgumentException($"Bad periapsis in DeltaVToChangeApsis = {newR}");
            }
            else
            {
                if (!newR.IsFinite() || (newR > 0 && newR < r.magnitude))
                    throw new ArgumentException($"Bad apoapsis in DeltaVToChangeApsis = {newR}");
            }

            const double DIFFSTEP = 1e-10;
            const double EPSX = 1e-6;
            const int MAXITS = 1000;

            const int NVARIABLES = 3;
            const int NEQUALITYCONSTRAINTS = 1;
            const int NINEQUALITYCONSTRAINTS = 0;

            double[] x = new double[NVARIABLES];

            double scaleDistance = Math.Sqrt(r.magnitude * Math.Abs(newR));
            scaleDistance = Math.Min(scaleDistance, r.sqrMagnitude);
            double scaleVelocity = Math.Sqrt(mu / scaleDistance);

            x[0] = 0; // maneuver x
            x[1] = 0; // maneuver y
            x[2] = 0; // maneuver z

            var args = new ChangeApsisArgs { R = r / scaleDistance, V = v / scaleVelocity, NewR = newR / scaleDistance, Periapsis = periapsis };

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetstpmax(state, 1e-2);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            alglib.minnlcoptimize(state, ChangeApsisFunction, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"DeltaVToChangeApsis({mu}, {r}, {v}, {newR}, {periapsis}): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            return new V3(x[0], x[1], x[2]) * scaleVelocity;
        }

        public struct ReturnFromMoonArgs
        {
            public double MoonSOI;
            public double PeR;
            public double Inc;
            public V3     R0;
            public V3     V0;
            public V3     MoonR0;
            public V3     MoonV0;
            public double ScaleRMoonToPlanet;
            public double ScaleVMoonToPlanet;
            public double ScaleTMoonToPlanet;
            public bool   OptimizeBurn;
            public double PerFactor;
        }

        public static void ReturnFromMoonFunction(double[] x, double[] fi, object obj)
        {
            var args = (ReturnFromMoonArgs)obj;

            double moonSOI = args.MoonSOI;
            double peR = args.PeR;
            double inc = args.Inc;
            V3 r0 = args.R0;
            V3 v0 = args.V0;
            V3 moonR0 = args.MoonR0;
            V3 moonV0 = args.MoonV0;
            double scaleRMoonToPlanet = args.ScaleRMoonToPlanet;
            double scaleVMoonToPlanet = args.ScaleVMoonToPlanet;
            double scaleTMoonToPlanet = args.ScaleTMoonToPlanet;
            bool optimizeBurn = args.OptimizeBurn;
            double perFactor = args.PerFactor;

            var dv = new V3(x[0], x[1], x[2]);
            double burnTime = x[3];
            double soiHalfTime = x[4];
            V3 rsoi = new V3(moonSOI, x[5], x[6]).sph2cart;
            V3 vsoi = new V3(x[7], x[8], x[9]).sph2cart;
            double planetHalfTime = x[10];
            var rf = new V3(x[11], x[12], x[13]);
            var vf = new V3(x[14], x[15], x[16]);

            (V3 rburn, V3 vburn)     = Shepperd.Solve(1.0, burnTime, r0, v0);
            (V3 r1Minus, V3 v1Minus) = Shepperd.Solve(1.0, soiHalfTime, rburn, vburn + dv);
            (V3 r1Plus, V3 v1Plus)   = Shepperd.Solve(1.0, -soiHalfTime, rsoi, vsoi);

            double t2 = (soiHalfTime * 2 + burnTime) / scaleTMoonToPlanet;
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, t2, moonR0, moonV0);

            V3 rsoiPlanet = rsoi / scaleRMoonToPlanet + moonR2;
            V3 vsoiPlanet = vsoi / scaleVMoonToPlanet + moonV2;

            (V3 r2Minus, V3 v2Minus) = Shepperd.Solve(1.0, planetHalfTime, rsoiPlanet, vsoiPlanet);
            (V3 r2Plus, V3 v2Plus)   = Shepperd.Solve(1.0, -planetHalfTime, rf, vf);

            double objDv = optimizeBurn ? dv.sqrMagnitude : 0;
            double objPeR = Powi(rf.magnitude - peR, 2) * perFactor;

            fi[0] = objDv + objPeR; // FIXME: inc constraint

            // meet in the moon's SOI
            fi[1]  = r1Minus.x - r1Plus.x;
            fi[2]  = r1Minus.y - r1Plus.y;
            fi[3]  = r1Minus.z - r1Plus.z;
            fi[4]  = v1Minus.x - v1Plus.x;
            fi[5]  = v1Minus.y - v1Plus.y;
            fi[6]  = v1Minus.z - v1Plus.z;
            fi[7]  = r2Minus.x - r2Plus.x;
            fi[8]  = r2Minus.y - r2Plus.y;
            fi[9]  = r2Minus.z - r2Plus.z;
            fi[10] = v2Minus.x - v2Plus.x;
            fi[11] = v2Minus.y - v2Plus.y;
            fi[12] = v2Minus.z - v2Plus.z;
            fi[13] = V3.Dot(rf.normalized, vf.normalized);
        }

        public static (V3 V, double dt) ManeuverToReturnFromMoon(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI,
            V3 r0, V3 v0, double peR, double inc)
        {
            Log(
                $"ManeuverToReturnFromMoon({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");
            const double DIFFSTEP = 1e-9;
            const double EPSX = 1e-4;
            const int MAXITS = 1000;

            const int NVARIABLES = 17;
            const int NEQUALITYCONSTRAINTS = 13;
            const int NINEQUALITYCONSTRAINTS = 0;

            double[] x = new double[NVARIABLES];

            double scaleDistanceMoon = Math.Sqrt(r0.magnitude * moonSOI);
            double scaleVelocityMoon = Math.Sqrt(moonMu / scaleDistanceMoon);
            double scaleTimeMoon = scaleDistanceMoon / scaleVelocityMoon;

            double scaleDistancePlanet = Math.Sqrt(moonR0.magnitude * peR);
            double scaleVelocityPlanet = Math.Sqrt(centralMu / scaleDistancePlanet);
            double scaleTimePlanet = scaleDistancePlanet / scaleVelocityPlanet;

            double scaleRMoonToPlanet = scaleDistancePlanet / scaleDistanceMoon;
            double scaleVMoonToPlanet = scaleVelocityPlanet / scaleVelocityMoon;
            double scaleTMoonToPlanet = scaleTimePlanet / scaleTimeMoon;

            // do the planet first which is analogous to heliocentric in a transfer
            V3 v1 = DeltaVToChangeApsis(centralMu, moonR0, moonV0, peR);

            // then do the source moon SOI
            (V3 vneg, V3 vpos, V3 rburn, double dt) = Maths.SingleImpulseHyperbolicBurn(moonMu, r0, v0, v1);
            V3 dv = vpos - vneg;
            double tt1 = Maths.TimeToNextRadius(moonMu, rburn, vpos, moonSOI);

            // construct mostly feasible solution
            (V3 r2, V3 v2)         = Shepperd.Solve(moonMu, tt1, rburn, vpos);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, tt1 + dt, moonR0, moonV0);
            V3 r2Sph = r2.cart2sph;
            V3 v2Sph = v2.cart2sph;
            V3 r2Planet = r2 + moonR2;
            V3 v2Planet = v2 + moonV2;
            double tt2 = Maths.TimeToNextPeriapsis(centralMu, r2Planet, v2Planet);
            (V3 rf, V3 vf) = Shepperd.Solve(centralMu, tt2, r2Planet, v2Planet);

            dv       /= scaleVelocityMoon;
            dt       /= scaleTimeMoon;
            tt1      /= scaleTimeMoon;
            v2Sph[0] /= scaleVelocityMoon;
            tt2      /= scaleTimePlanet;
            rf       /= scaleDistancePlanet;
            vf       /= scaleVelocityPlanet;

            x[0]  = dv.x;     // maneuver x
            x[1]  = dv.y;     // maneuver y
            x[2]  = dv.z;     // maneuver z
            x[3]  = dt;       // maneuver dt
            x[4]  = tt1 / 2;  // 1/2 coast time through moon SOI
            x[5]  = r2Sph[1]; // theta of SOI position
            x[6]  = r2Sph[2]; // phi of SOI position
            x[7]  = v2Sph[0]; // r of SOI velocity
            x[8]  = v2Sph[1]; // theta of SOI velocity
            x[9]  = v2Sph[2]; // phi of SOI velocity
            x[10] = tt2 / 2;  // 1/2 coast time through planet SOI
            x[11] = rf.x;     // final rx
            x[12] = rf.y;     // final ry
            x[13] = rf.z;     // final rz
            x[14] = vf.x;     // final vx
            x[15] = vf.y;     // final vy
            x[16] = vf.z;     // final vz

            var args = new ReturnFromMoonArgs
            {
                MoonSOI            = moonSOI / scaleDistanceMoon,
                PeR                = peR / scaleDistancePlanet,
                Inc                = inc,
                R0                 = r0 / scaleDistanceMoon,
                V0                 = v0 / scaleVelocityMoon,
                MoonR0             = moonR0 / scaleDistancePlanet,
                MoonV0             = moonV0 / scaleVelocityPlanet,
                ScaleRMoonToPlanet = scaleRMoonToPlanet,
                ScaleVMoonToPlanet = scaleVMoonToPlanet,
                ScaleTMoonToPlanet = scaleTMoonToPlanet,
                OptimizeBurn       = false,
                PerFactor          = 5
            };

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetstpmax(state, 1e-3);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);
            alglib.minnlcoptimize(state, ReturnFromMoonFunction, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);
            if (rep.terminationtype < 0)
                throw new Exception(
                    $"DeltaVToChangeApsis(): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            args.OptimizeBurn = true;

            for (int i = 1; i < 7; i+=2)
            {
                args.PerFactor = 5 * Powi(10, i);
                alglib.minnlcrestartfrom(state, x);
                alglib.minnlcoptimize(state, ReturnFromMoonFunction, null, args);
                alglib.minnlcresults(state, out x, out rep);
                if (rep.terminationtype < 0)
                    throw new Exception(
                        $"DeltaVToChangeApsis(): SQP solver terminated abnormally: {rep.terminationtype}"
                    );
            }

            double[] fi = new double[15];

            ReturnFromMoonFunction(x, fi, args);

            return (new V3(x[0], x[1], x[2]) * scaleVelocityMoon, x[3] * scaleTimeMoon);
        }

        public static (V3 dv, double dt, double newPeR) NextManeuverToReturnFromMoon(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI,
            V3 r0, V3 v0, double peR, double inc)
        {
            double dt;
            V3 dv;

            while (true)
            {
                (dv, dt) = ManeuverToReturnFromMoon(centralMu, moonMu, moonR0, moonV0, moonSOI, r0, v0, peR, inc);
                if (dt > 0)
                    break;
                (r0, v0) = Shepperd.Solve(moonMu, Maths.PeriodFromStateVectors(moonMu, r0, v0), r0, v0);
            }

            (V3 r1, V3 v1) = Shepperd.Solve(moonMu, dt, r0, v0);
            double tt1 = Maths.TimeToNextRadius(moonMu, r1, v1 + dv, moonSOI);
            (V3 r2, V3 v2)         = Shepperd.Solve(moonMu, tt1, r1, v1 + dv);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, dt + tt1, moonR0, moonV0);
            double newPeR = Maths.PeriapsisFromStateVectors(centralMu, moonR2 + r2, moonV2 + v2);

            return (dv, dt, newPeR);
        }
    }
}
