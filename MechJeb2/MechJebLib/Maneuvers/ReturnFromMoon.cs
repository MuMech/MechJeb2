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
using static MechJebLib.Utils.Statics;
using static System.Math;

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
        }

        private static void NLPFunction(double[] x, double[] fi, object obj)
        {
            /*
             * unpacking constants
             */
            var args = (Args)obj;

            double moonSOI = args.MoonSOI;
            double peR = args.PeR;
            double inc = args.Inc;
            V3 r0 = args.R0;
            V3 v0 = args.V0;
            V3 moonR0 = args.MoonR0;
            V3 moonV0 = args.MoonV0;
            Scale moonToPlanetScale = args.MoonToPlanetScale;

            /*
             * unpacking optimizer variables
             */
            var dv = new V3(x[0], x[1], x[2]);
            double burnTime = x[3];
            double moonDt1 = x[4];
            double moonDt2 = x[5];
            V3 rsoi = new V3(moonSOI, x[6], x[7]).sph2cart;
            V3 vsoi = new V3(x[8], x[9], x[10]).sph2cart;
            double planetDt1 = x[11];
            double planetDt2 = x[12];
            var rf = new V3(x[13], x[14], x[15]);
            var vf = new V3(x[16], x[17], x[18]);

            // forward propagation in the moon SOI
            (V3 rburn, V3 vburn)     = Shepperd.Solve(1.0, burnTime, r0, v0);
            (V3 r1Minus, V3 v1Minus) = Shepperd.Solve(1.0, moonDt1, rburn, vburn + dv);

            // reverse propagation in the moon SOI
            (V3 r1Plus, V3 v1Plus)   = Shepperd.Solve(1.0, -moonDt2, rsoi, vsoi);

            // propagation of the moon via the "ephemeris"
            double t2 = (moonDt1 + moonDt2 + burnTime) / moonToPlanetScale.TimeScale;
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, t2, moonR0, moonV0);

            V3 rsoiPlanet = rsoi / moonToPlanetScale.LengthScale + moonR2;
            V3 vsoiPlanet = vsoi / moonToPlanetScale.VelocityScale + moonV2;

            // forward propagation in the planet SOI
            (V3 r2Minus, V3 v2Minus) = Shepperd.Solve(1.0, planetDt1, rsoiPlanet, vsoiPlanet);

            // reverse propagation in the planet SOI
            (V3 r2Plus, V3 v2Plus)   = Shepperd.Solve(1.0, -planetDt2, rf, vf);

            // objective
            fi[0] = dv.sqrMagnitude;

            // meet in the moon's SOI
            fi[1] = r1Minus.x - r1Plus.x;
            fi[2] = r1Minus.y - r1Plus.y;
            fi[3] = r1Minus.z - r1Plus.z;
            fi[4] = v1Minus.x - v1Plus.x;
            fi[5] = v1Minus.y - v1Plus.y;
            fi[6] = v1Minus.z - v1Plus.z;
            // meet in the planet's SOI
            fi[7]  = r2Minus.x - r2Plus.x;
            fi[8]  = r2Minus.y - r2Plus.y;
            fi[9]  = r2Minus.z - r2Plus.z;
            fi[10] = v2Minus.x - v2Plus.x;
            fi[11] = v2Minus.y - v2Plus.y;
            fi[12] = v2Minus.z - v2Plus.z;
            // end at the periapsis
            fi[13] = V3.Dot(rf.normalized, vf.normalized);
            fi[14] = rf.magnitude * rf.magnitude - peR * peR;
            // put some constraints on when the midpoints meet
            fi[15] = moonDt1 - moonDt2;
            fi[16] = planetDt1 - planetDt2;
        }

        [UsedImplicitly]
        public static (V3 V, double dt) Maneuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI,
            V3 r0, V3 v0, double peR, double inc, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity)
        {
            Print(
                $"ReturnFromMoon.Maneuver({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");
            const double DIFFSTEP = 1e-9;
            const double EPSX = 1e-4;
            const int MAXITS = 1000;

            const int NVARIABLES = 19;
            const int NEQUALITYCONSTRAINTS = 17;
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

            var moonScale = Scale.Create(moonMu, Sqrt(r0.magnitude * moonSOI));
            var planetScale = Scale.Create(centralMu, Sqrt(moonR0.magnitude * peR));
            Scale moonToPlanetScale = moonScale.ConvertTo(planetScale);

            (double _, double ecc) = Maths.SmaEccFromStateVectors(moonMu, r0, v0);

            double dt, tt1;
            V3 rf, vf, dv, r2, v2;

            if (ecc < 1)
            {
                // approximate vsoi with the amount to kick the moon's orbit down to the periapsis
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

            V3 r2Sph = r2.cart2sph;
            V3 v2Sph = v2.cart2sph;

            // propagate the moon forward to the estimate of the time of the burn
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, dt, moonR0, moonV0);

            // construct rf + vf by propagating the moon forward to the burn time and changing the PeR to find a
            // somewhat feasible rf+vf
            V3 moonV3 = moonV2 + ChangeOrbitalElement.ChangePeriapsis(centralMu, moonR2, moonV2, peR);
            double tt2 = Maths.TimeToNextPeriapsis(centralMu, moonR0, moonV3);
            (rf, vf) = Shepperd.Solve(centralMu, tt2, moonR0, moonV3);

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
            x[5]  = tt1 / 2;  // 1/2 coast time through moon SOI
            x[6]  = r2Sph[1]; // theta of SOI position
            x[7]  = r2Sph[2]; // phi of SOI position
            x[8]  = v2Sph[0]; // r of SOI velocity
            x[9]  = v2Sph[1]; // theta of SOI velocity
            x[10] = v2Sph[2]; // phi of SOI velocity
            x[11] = tt2 / 2;  // 1/2 coast time through planet SOI
            x[12] = tt2 / 2;  // 1/2 coast time thorugh planet SOI
            x[13] = rf.x;     // final rx
            x[14] = rf.y;     // final ry
            x[15] = rf.z;     // final rz
            x[16] = vf.x;     // final vx
            x[17] = vf.y;     // final vy
            x[18] = vf.z;     // final vz

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
            };

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetstpmax(state, 1e-8);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);
            alglib.minnlcoptimize(state, NLPFunction, null, args);
            alglib.minnlcresults(state, out x, out alglib.minnlcreport rep);

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver(): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            NLPFunction(x, fi, args);
            fi[0] = 0; // zero out the objective
            if (DoubleArrayMagnitude(fi) > 1e-4)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver() no feasible solution found: constraint violation: {DoubleArrayMagnitude(fi)}");

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
