/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using JetBrains.Annotations;
using MechJebLib.Core;
using MechJebLib.Core.TwoBody;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Maneuvers
{
    public static class ReturnFromMoon
    {
        private struct Args
        {
            public double MoonSOI;
            public double PeR;
            public double Inc;
            public V3     R0;
            public V3     V0;
            public V3     MoonR0;
            public V3     MoonV0;
            public Scale  MoonToPlanetScale;
            public bool   OptimizeBurn;
            public double PerFactor;
        }

        private static void NLPFunction(double[] x, double[] fi, object obj)
        {
            var args = (Args)obj;

            double moonSOI = args.MoonSOI;
            double peR = args.PeR;
            double inc = args.Inc;
            V3 r0 = args.R0;
            V3 v0 = args.V0;
            V3 moonR0 = args.MoonR0;
            V3 moonV0 = args.MoonV0;
            Scale moonToPlanetScale = args.MoonToPlanetScale;
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

            double t2 = (soiHalfTime * 2 + burnTime) / moonToPlanetScale.TimeScale;
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, t2, moonR0, moonV0);

            V3 rsoiPlanet = rsoi / moonToPlanetScale.LengthScale + moonR2;
            V3 vsoiPlanet = vsoi / moonToPlanetScale.VelocityScale + moonV2;

            (V3 r2Minus, V3 v2Minus) = Shepperd.Solve(1.0, planetHalfTime, rsoiPlanet, vsoiPlanet);
            (V3 r2Plus, V3 v2Plus)   = Shepperd.Solve(1.0, -planetHalfTime, rf, vf);

            double objDv = optimizeBurn ? dv.sqrMagnitude : 0;
            double objPeR = Statics.Powi(rf.magnitude - peR, 2) * perFactor;

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

        [UsedImplicitly]
        public static (V3 V, double dt) Maneuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI,
            V3 r0, V3 v0, double peR, double inc, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity)
        {
            Statics.Log(
                $"ManeuverToReturnFromMoon({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");
            const double DIFFSTEP = 1e-9;
            const double EPSX = 1e-4;
            const int MAXITS = 1000;

            const int NVARIABLES = 17;
            const int NEQUALITYCONSTRAINTS = 13;
            const int NINEQUALITYCONSTRAINTS = 0;

            double[] x = new double[NVARIABLES];
            double[] fi = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1];
            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndl[i] = double.NegativeInfinity;
                bndu[i] = double.PositiveInfinity;
            }

            bndl[3] = dtmin;
            bndu[3] = dtmax;

            var moonScale = Scale.Create(moonMu, Math.Sqrt(r0.magnitude * moonSOI));
            var planetScale = Scale.Create(centralMu, Math.Sqrt(moonR0.magnitude * peR));
            Scale moonToPlanetScale = moonScale.ConvertTo(planetScale);


            (double _, double ecc) = Maths.SmaEccFromStateVectors(moonMu, r0, v0);

            double dt, tt1;
            V3 rf, vf, dv, r2, v2;

            if (ecc < 1)
            {
                // do the planet first which is analogous to heliocentric in a transfer
                V3 v1 = ChangeOrbitalElement.ChangePeriapsis(centralMu, moonR0, moonV0, peR);

                // then do the source moon SOI
                V3 vneg, vpos, rburn;
                (vneg, vpos, rburn, dt) = Maths.SingleImpulseHyperbolicBurn(moonMu, r0, v0, v1);
                dv                      = vpos - vneg;
                tt1                     = Maths.TimeToNextRadius(moonMu, rburn, vpos, moonSOI);
                (r2, v2)                = Shepperd.Solve(moonMu, tt1, rburn, vpos);
            }
            else
            {
                dt       = 0;
                dv       = V3.zero;
                tt1      = Maths.TimeToNextRadius(moonMu, r0, v0, moonSOI);
                (r2, v2) = Shepperd.Solve(moonMu, tt1, r0, v0);
            }

            // construct mostly feasible solution
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, tt1 + dt, moonR0, moonV0);
            V3 r2Sph = r2.cart2sph;
            V3 v2Sph = v2.cart2sph;
            V3 r2Planet = r2 + moonR2;
            V3 v2Planet = v2 + moonV2;
            double tt2 = Maths.TimeToNextPeriapsis(centralMu, r2Planet, v2Planet);
            (rf, vf) = Shepperd.Solve(centralMu, tt2, r2Planet, v2Planet);

            dv       /= moonScale.VelocityScale;
            dt       /= moonScale.TimeScale;
            tt1      /= moonScale.TimeScale;
            v2Sph[0] /= moonScale.VelocityScale;
            tt2      /= planetScale.TimeScale;
            rf       /= planetScale.LengthScale;
            vf       /= planetScale.VelocityScale;

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

            var args = new Args
            {
                MoonSOI           = moonSOI / moonScale.LengthScale,
                PeR               = peR / planetScale.LengthScale,
                Inc               = inc,
                R0                = r0 / moonScale.LengthScale,
                V0                = v0 / moonScale.VelocityScale,
                MoonR0            = moonR0 / planetScale.LengthScale,
                MoonV0            = moonV0 / planetScale.VelocityScale,
                MoonToPlanetScale = moonToPlanetScale,
                OptimizeBurn      = false,
                PerFactor         = 5
            };

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetstpmax(state, 1e-3);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);
            alglib.minnlcoptimize(state, NLPFunction, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);
            if (rep.terminationtype < 0)
                throw new Exception(
                    $"DeltaVToChangeApsis(): SQP solver terminated abnormally: {rep.terminationtype}"
                );
            NLPFunction(x, fi, args);
            if (Statics.DoubleArrayMagnitude(fi) > 1e-4)
                throw new Exception("DeltaVToChangeApsis() no feasible solution found");

            args.OptimizeBurn = true;

            for (int i = 1; i < 7; i += 2)
            {
                args.PerFactor = 5 * Statics.Powi(10, i);
                alglib.minnlcrestartfrom(state, x);
                alglib.minnlcoptimize(state, NLPFunction, null, args);
                alglib.minnlcresults(state, out x, out rep);
                if (rep.terminationtype < 0)
                    throw new Exception(
                        $"DeltaVToChangeApsis(): SQP solver terminated abnormally: {rep.terminationtype}"
                    );
                NLPFunction(x, fi, args);
                fi[0] = 0; // zero out the objective
                if (Statics.DoubleArrayMagnitude(fi) > 1e-4)
                    throw new Exception("DeltaVToChangeApsis() feasible solution was lost");
            }

            NLPFunction(x, fi, args);

            return (new V3(x[0], x[1], x[2]) * moonScale.VelocityScale, x[3] * moonScale.TimeScale);
        }

        public static (V3 dv, double dt, double newPeR) NextManeuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0,
            double moonSOI, V3 r0, V3 v0, double peR, double inc, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity)
        {
            double dt;
            V3 dv;
            int i = 0;

            (double _, double ecc) = Maths.SmaEccFromStateVectors(moonMu, r0, v0);

            while (true)
            {
                (dv, dt) = Maneuver(centralMu, moonMu, moonR0, moonV0, moonSOI, r0, v0, peR, inc, dtmin, dtmax);
                if (dt > 0 || ecc >= 1)
                    break;
                if (i++ >= 5)
                    throw new Exception("Maximum iterations exceeded with no valid future solution");
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
