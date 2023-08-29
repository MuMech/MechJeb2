/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Diagnostics;
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
            public Scale  MoonScale;
            public Scale  PlanetScale;
        }

        /* I was going to try to implement the differnetial correction procedure in Ocampo and Saudemont as a
           least-squares minimization problem, but decided not to, but this is the constraint function if I ever
           come back to it
        private static void GuessFunction(double[] x, double[] fi, object o)
        {
            var args = (Args)o;

            double moonSOI = args.MoonSOI;
            double peR = args.PeR;
            double inc = args.Inc;
            V3 r0 = args.R0;
            V3 v0 = args.V0;
            V3 moonR0 = args.MoonR0;
            V3 moonV0 = args.MoonV0;
            Scale moonToPlanetScale = args.MoonToPlanetScale;

            double tf = x[0];
            var rf = new V3(x[1], x[2], x[3]);
            var vf = new V3(x[4], x[5], x[6]);
            double tsoiMinus = x[7];
            double tsoiPlus = x[8];
            double burnTime = x[9];
            var dv = new V3(x[10], x[11], x[12]);

            (V3 rburn, V3 vburn)   = Shepperd.Solve(1.0, burnTime, r0, v0);
            (V3 rsoi, V3 vsoi)     = Shepperd.Solve(1.0, tsoiMinus, rburn, vburn + dv);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, tsoiMinus, moonR0, moonV0);

            V3 rsoiMinus = rsoi / moonToPlanetScale.LengthScale + moonR2;
            V3 vsoiMinus = vsoi / moonToPlanetScale.VelocityScale + moonV2;

            (V3 rsoiPlus, V3 vsoiPlus) = Shepperd.Solve(1.0, tf - tsoiPlus, rf, vf);

            fi[0] = rsoiPlus.x - rsoiMinus.x;
            fi[1] = rsoiPlus.y - rsoiMinus.y;
            fi[2] = rsoiPlus.z - rsoiMinus.z;
            fi[3] = vsoiPlus.x - vsoiMinus.x;
            fi[4] = vsoiPlus.y - vsoiMinus.y;
            fi[5] = vsoiPlus.z - vsoiMinus.z;
            fi[6] = tsoiPlus - tsoiMinus;
            fi[7] = V3.Dot(rf.normalized, vf.normalized);
            fi[8] = rf.sqrMagnitude - peR * peR;
        }
        */

        private static void NLPFunction(double[] x, double[] fi, double[,] jac, object o)
        {
            /*
             * unpacking constants
             */
            var args = (Args)o;

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
            double moonDt = x[11];
            double planetDt1 = x[12];
            double planetDt2 = x[13];
            var rf = new V3(x[14], x[15], x[16]);
            var vf = new V3(x[17], x[18], x[19]);

            // forward propagation in the moon SOI
            (V3 rburn, V3 vburn) = Shepperd.Solve(1.0, burnTime, r0, v0);
            V3 aburn = -rburn / (rburn.sqrMagnitude * rburn.magnitude);
            (V3 r1Minus, V3 v1Minus, M3 v1MinusS00, M3 v1MinusS01, M3 v1MinusS10, M3 v1MinusS11) = Shepperd.Solve2(1.0, moonDt1, rburn, vburn + dv);
            V3 a1Minus = -r1Minus / (r1Minus.sqrMagnitude * r1Minus.magnitude);

            // reverse propagation in the moon SOI
            (V3 r1Plus, V3 v1Plus, M3 v1PlusS00, M3 v1PlusS01, M3 v1PlusS10, M3 v1PlusS11) = Shepperd.Solve2(1.0, -moonDt2, rsoi, vsoi);
            V3 a1Plus = -r1Plus / (r1Plus.sqrMagnitude * r1Plus.magnitude);

            // propagation of the moon via the "ephemeris"
            double t2 = moonDt / moonToPlanetScale.TimeScale;
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, t2, moonR0, moonV0);
            V3 moonA2 = -moonR2 / (moonR2.sqrMagnitude * moonR2.magnitude);

            V3 rsoiPlanet = rsoi / moonToPlanetScale.LengthScale + moonR2;
            V3 vsoiPlanet = vsoi / moonToPlanetScale.VelocityScale + moonV2;

            // forward propagation in the planet SOI
            (V3 r2Minus, V3 v2Minus, M3 v2MinusS00, M3 v2MinusS01, M3 v2MinusS10, M3 v2MinusS11) =
                Shepperd.Solve2(1.0, planetDt1, rsoiPlanet, vsoiPlanet);
            V3 a2Minus = -r2Minus / (r2Minus.sqrMagnitude * r2Minus.magnitude);

            // reverse propagation in the planet SOI
            (V3 r2Plus, V3 v2Plus, M3 v2PlusS00, M3 v2PlusS01, M3 v2PlusS10, M3 v2PlusS11) = Shepperd.Solve2(1.0, -planetDt2, rf, vf);
            V3 a2Plus = -r2Plus / (r2Plus.sqrMagnitude * r2Plus.magnitude);

            // objective
            Dual obj = new DualV3(dv, new V3(1, 0, 0)).sqrMagnitude;
            fi[0]     = obj.M;
            jac[0, 0] = obj.D;
            jac[0, 1] = new DualV3(dv, new V3(0, 1, 0)).sqrMagnitude.D;
            jac[0, 2] = new DualV3(dv, new V3(0, 0, 1)).sqrMagnitude.D;

            /*
             * meet in the moon's SOI
             */

            fi[1] = r1Minus.x - r1Plus.x;
            fi[2] = r1Minus.y - r1Plus.y;
            fi[3] = r1Minus.z - r1Plus.z;
            fi[4] = v1Minus.x - v1Plus.x;
            fi[5] = v1Minus.y - v1Plus.y;
            fi[6] = v1Minus.z - v1Plus.z;

            v1MinusS01.CopyTo(jac, 1, 0);
            v1MinusS11.CopyTo(jac, 4, 0);

            (v1MinusS00 * vburn + v1MinusS01 * aburn).CopyTo(jac, 1, 3);
            (v1MinusS10 * vburn + v1MinusS11 * aburn).CopyTo(jac, 4, 3);

            v1Minus.CopyTo(jac, 1, 4);
            a1Minus.CopyTo(jac, 4, 4);

            v1Plus.CopyTo(jac, 1, 5);
            a1Plus.CopyTo(jac, 4, 5);

            DualV3 rsoi6 = new DualV3(moonSOI, x[6], x[7], 0, 1, 0).sph2cart;
            DualV3 rsoi7 = new DualV3(moonSOI, x[6], x[7], 0, 0, 1).sph2cart;

            var d2 = new M3(rsoi6.D, rsoi7.D, V3.zero);

            DualV3 vsoi8 = new DualV3(x[8], x[9], x[10], 1, 0, 0).sph2cart;
            DualV3 vsoi9 = new DualV3(x[8], x[9], x[10], 0, 1, 0).sph2cart;
            DualV3 vsoi10 = new DualV3(x[8], x[9], x[10], 0, 0, 1).sph2cart;

            var d3 = new M3(vsoi8.D, vsoi9.D, vsoi10.D);

            (-v1PlusS00 * d2).CopyTo(jac, 1, 6);
            (-v1PlusS01 * d3).CopyTo(jac, 1, 8);
            (-v1PlusS10 * d2).CopyTo(jac, 4, 6);
            (-v1PlusS11 * d3).CopyTo(jac, 4, 8);

            /*
             * meet in the planet's SOI
             */

            fi[7]  = r2Minus.x - r2Plus.x;
            fi[8]  = r2Minus.y - r2Plus.y;
            fi[9]  = r2Minus.z - r2Plus.z;
            fi[10] = v2Minus.x - v2Plus.x;
            fi[11] = v2Minus.y - v2Plus.y;
            fi[12] = v2Minus.z - v2Plus.z;

            double l = moonToPlanetScale.LengthScale;
            double v = moonToPlanetScale.VelocityScale;
            double t = moonToPlanetScale.TimeScale;

            DualV3 rsoiPlanet6 = new DualV3(moonSOI, x[6], x[7], 0, 1, 0).sph2cart / l + moonR2;
            DualV3 rsoiPlanet7 = new DualV3(moonSOI, x[6], x[7], 0, 0, 1).sph2cart / l + moonR2;

            var d0 = new M3(rsoiPlanet6.D, rsoiPlanet7.D, V3.zero);

            DualV3 vsoiPlanet8 = new DualV3(x[8], x[9], x[10], 1, 0, 0).sph2cart / v + moonV2;
            DualV3 vsoiPlanet9 = new DualV3(x[8], x[9], x[10], 0, 1, 0).sph2cart / v + moonV2;
            DualV3 vsoiPlanet10 = new DualV3(x[8], x[9], x[10], 0, 0, 1).sph2cart / v + moonV2;

            var d1 = new M3(vsoiPlanet8.D, vsoiPlanet9.D, vsoiPlanet10.D);

            (v2MinusS00 * d0).CopyTo(jac, 7, 6);
            (v2MinusS01 * d1).CopyTo(jac, 7, 8);
            (v2MinusS10 * d0).CopyTo(jac, 10, 6);
            (v2MinusS11 * d1).CopyTo(jac, 10, 8);

            ((v2MinusS00 * moonV2 + v2MinusS01 * moonA2) / t).CopyTo(jac, 7, 11);
            ((v2MinusS10 * moonV2 + v2MinusS11 * moonA2) / t).CopyTo(jac, 10, 11);

            v2Minus.CopyTo(jac, 7, 12);
            a2Minus.CopyTo(jac, 10, 12);

            v2Plus.CopyTo(jac, 7, 13);
            a2Plus.CopyTo(jac, 10, 13);

            (-v2PlusS00).CopyTo(jac, 7, 14);
            (-v2PlusS01).CopyTo(jac, 7, 17);
            (-v2PlusS10).CopyTo(jac, 10, 14);
            (-v2PlusS11).CopyTo(jac, 10, 17);

            /*
             * periapsis condition
             */

            Dual p = DualV3.Dot(new DualV3(rf, new V3(1, 0, 0)).normalized, vf.normalized);
            fi[13]      = p.M;
            jac[13, 14] = p.D;
            jac[13, 15] = DualV3.Dot(new DualV3(rf, new V3(0, 1, 0)).normalized, vf.normalized).D;
            jac[13, 16] = DualV3.Dot(new DualV3(rf, new V3(0, 0, 1)).normalized, vf.normalized).D;
            jac[13, 17] = DualV3.Dot(rf.normalized, new DualV3(vf, new V3(1, 0, 0)).normalized).D;
            jac[13, 18] = DualV3.Dot(rf.normalized, new DualV3(vf, new V3(0, 1, 0)).normalized).D;
            jac[13, 19] = DualV3.Dot(rf.normalized, new DualV3(vf, new V3(0, 0, 1)).normalized).D;

            /*
             * periapsis constraint
             */

            fi[14]      = rf.sqrMagnitude - peR * peR;
            jac[14, 14] = (new DualV3(rf, new V3(1, 0, 0)).sqrMagnitude - peR * peR).D;
            jac[14, 15] = (new DualV3(rf, new V3(0, 1, 0)).sqrMagnitude - peR * peR).D;
            jac[14, 16] = (new DualV3(rf, new V3(0, 0, 1)).sqrMagnitude - peR * peR).D;

            /*
             * time constraints
             */

            fi[15]      = moonDt1 + moonDt2 + burnTime - moonDt;
            jac[15, 3]  = 1.0;
            jac[15, 4]  = 1.0;
            jac[15, 5]  = 1.0;
            jac[15, 11] = -1.0;

            /*
             * midpoint time constraints
             */

            fi[16]      = moonDt1 - moonDt2;
            jac[16, 4]  = 1.0;
            jac[16, 5]  = -1.0;
            fi[17]      = planetDt1 - planetDt2;
            jac[17, 12] = 1.0;
            jac[17, 13] = -1.0;
        }

        private static double[] GenerateInitialGuess(Args args)
        {
            var sw = new Stopwatch();
            sw.Start();

            double[] x = new double[NVARIABLES];

            double moonSOI = args.MoonSOI;
            double peR = args.PeR;
            V3 r0 = args.R0;
            V3 v0 = args.V0;
            V3 moonR0 = args.MoonR0;
            V3 moonV0 = args.MoonV0;
            Scale moonToPlanetScale = args.MoonToPlanetScale;

            double dt, tt1;
            V3 rf, vf, dv, rsoi, vsoi;

            // kick the moon's orbit to the new apsis to build the transfer orbit
            V3 dv1 = ChangeOrbitalElement.ChangeApsis(1.0, moonR0, moonV0, peR);

            // get the transfer orbit keplerian elements
            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) =
                Maths.KeplerianFromStateVectors(1.0, moonR0, moonV0 + dv1);

            // calculate the true anomaly of the SOI point on the transfer orbit (using the planet scale)
            double moonSOI2 = moonSOI / moonToPlanetScale.LengthScale;
            double nuSOI = TAU - SafeAcos((sma * (1 - ecc * ecc) / moonSOI2 - 1) / ecc); // FIXME: probably assumes we're going to a periapsis?

            if (Maths.EccFromStateVectors(1.0, r0, v0) < 1 && Maths.ApoapsisFromStateVectors(1.0, r0, v0) < moonSOI)
            {
                // get the rsoi/vsoi in the planet-centric coordinates
                (V3 _, V3 vsoiPlanet) = Maths.StateVectorsFromKeplerian(1.0, sma, ecc, inc, lan, argp, nuSOI);

                // now compute the lunar hyperbolic burn.
                V3 vneg, vpos, rburn;
                (vneg, vpos, rburn, dt) = Maths.SingleImpulseHyperbolicBurn(1.0, r0, v0, (vsoiPlanet - moonV0) * moonToPlanetScale.VelocityScale);

                dv           = vpos - vneg;
                tt1          = Maths.TimeToNextRadius(1.0, rburn, vpos, moonSOI);
                (rsoi, vsoi) = Shepperd.Solve(1.0, tt1, rburn, vpos);
            }
            else
            {
                // if we're already on an escape trajectory, try an immediate burn and hope we're reasonably close
                dt           = 0;
                dv           = V3.zero;
                tt1          = Maths.TimeToNextRadius(1.0, r0, v0, moonSOI);
                (rsoi, vsoi) = Shepperd.Solve(1.0, tt1, r0, v0);
            }

            // get the rsoi/vsoi in planet-coordinates of the forward-propagated feasible orbit from the burn.
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, (dt + tt1) / moonToPlanetScale.TimeScale, moonR0, moonV0);
            V3 rsoiPlanet = rsoi / moonToPlanetScale.LengthScale + moonR2;
            V3 vsoiPlanet2 = vsoi / moonToPlanetScale.VelocityScale + moonV2;

            // construct rf + vf by introducing a discontinuity at the SOI
            V3 vsoiPlanet3 = vsoiPlanet2 + ChangeOrbitalElement.ChangeApsis(1.0, rsoiPlanet, vsoiPlanet2, peR);

            // forward propagate that to find rf+vf conditions and transfer time
            double tt2 = Maths.TimeToNextPeriapsis(1.0, rsoiPlanet, vsoiPlanet3);
            (rf, vf) = Shepperd.Solve(1.0, tt2, rsoiPlanet, vsoiPlanet3);

            V3 r2Sph = rsoi.cart2sph;
            //V3 v2Sph = vsoi.cart2sph;
            // taking the average of the mismatch in vsoi and spreading the infeasibility over both SOIs seems to actually
            // produce better convergence properties.
            V3 v2Sph = (((vsoiPlanet3 - moonV2) * moonToPlanetScale.VelocityScale + vsoi) / 2).cart2sph;

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
            x[11] = tt1 + dt; // total time in the moon SOI
            x[12] = tt2 / 2;  // 1/2 coast time through planet SOI
            x[13] = tt2 / 2;  // 1/2 coast time thorugh planet SOI
            x[14] = rf.x;     // final rx
            x[15] = rf.y;     // final ry
            x[16] = rf.z;     // final rz
            x[17] = vf.x;     // final vx
            x[18] = vf.y;     // final vy
            x[19] = vf.z;     // final vz

            sw.Stop();
            Print($"initial guess generation took {sw.ElapsedMilliseconds}ms");

            return x;
        }

        private const double DIFFSTEP = 1e-9;
        private const double EPSX     = 1e-4;
        private const int    MAXITS   = 10000;

        private const int NVARIABLES             = 20;
        private const int NEQUALITYCONSTRAINTS   = 17;
        private const int NINEQUALITYCONSTRAINTS = 0;

        private static (V3 V, double dt) ManeuverScaled(Args args, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity)
        {
            Scale planetScale = args.PlanetScale;
            V3 moonR0 = args.MoonR0;
            V3 moonV0 = args.MoonV0;

            double[] x = GenerateInitialGuess(args);
            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndl[i] = double.NegativeInfinity;
                bndu[i] = double.PositiveInfinity;
            }

            bndl[3] = dtmin;
            bndu[3] = dtmax;

            var sw = new Stopwatch();
            sw.Start();

            //alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlccreate(NVARIABLES, x, out alglib.minnlcstate state);

            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetstpmax(state, 1e-2);
            //double rho = 1000.0;
            //int outerits = 5;
            //alglib.minnlcsetalgoaul(state, rho, outerits);
            //alglib.minnlcsetalgoslp(state);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            /*
            alglib.minnlcoptguardsmoothness(state);
            alglib.minnlcoptguardgradient(state, DIFFSTEP);
            */

            double[] fi = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1];
            double[,] jac = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1, NVARIABLES];

            NLPFunction(x, fi, jac, args);

            alglib.minnlcoptimize(state, NLPFunction, null, args);
            alglib.minnlcresults(state, out double[] x2, out alglib.minnlcreport rep);

            sw.Stop();
            Print($"optimization took {sw.ElapsedMilliseconds}ms: {rep.iterationscount} iter, {rep.nfev} fev");

            /*
            alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep);

            if (ogrep.badgradsuspected)
                throw new Exception(
                    $"badgradsuspected: {ogrep.badgradfidx},{ogrep.badgradvidx}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, 1e-2)}");

            if (ogrep.nonc0suspected || ogrep.nonc1suspected)
                throw new Exception("alglib optguard caught an error, i should report better on errors now");
                */

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver(): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            if (rep.nlcerr > 1e-4)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver() no feasible solution found, constraint violation: {rep.nlcerr}");

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) =
                Maths.KeplerianFromStateVectors(1.0, new V3(x2[14], x2[15], x2[16]), new V3(x2[17], x2[18], x2[19]));

            Print(
                $"Earth transfer orbit:\nsma:{sma * planetScale.LengthScale} ecc:{ecc} inc:{Rad2Deg(inc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");

            (sma, ecc, inc, lan, argp, tanom, _) =
                Maths.KeplerianFromStateVectors(1.0, moonR0, moonV0);

            Print(
                $"Lunar orbit:\nsma:{sma * planetScale.LengthScale} ecc:{ecc} inc:{Rad2Deg(inc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");

            return (new V3(x2[0], x2[1], x2[2]), x2[3]);
        }

        private static (V3 V, double dt) Maneuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI,
            V3 r0, V3 v0, double peR, double inc, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity)
        {
            var sw = new Stopwatch();
            sw.Start();

            Print(
                $"ReturnFromMoon.Maneuver({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");

            (double sma, double ecc, double incc, double lan, double argp, double tanom, _) =
                Maths.KeplerianFromStateVectors(moonMu, r0, v0);

            Print($"sma:{sma} ecc:{ecc} inc:{Rad2Deg(incc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");

            var moonScale = Scale.Create(moonMu, Sqrt(r0.magnitude * moonSOI));
            var planetScale = Scale.Create(centralMu, Sqrt(moonR0.magnitude * peR));
            Scale moonToPlanetScale = moonScale.ConvertTo(planetScale);

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
                MoonScale         = moonScale,
                PlanetScale       = planetScale
            };

            (V3 dv, double dt) = ManeuverScaled(args, dtmin / moonScale.TimeScale, dtmax / moonScale.TimeScale);

            sw.Stop();
            Print($"total calculation took {sw.ElapsedMilliseconds}ms");

            return (dv * moonScale.VelocityScale, dt * moonScale.TimeScale);
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
