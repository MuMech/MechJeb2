/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Diagnostics;
using MechJebLib.Functions;
using MechJebLib.Minimization;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.Maneuvers
{
    public class ReturnFromMoon
    {
        private struct Args
        {
            public double MoonSOI;

            public double PeR;

            //public double Inc;
            public V3 R0;
            public V3 V0;
            public V3 MoonR0;
            public V3 MoonV0;

            public Scale MoonToPlanetScale;
            public Scale MoonScale;
            public Scale PlanetScale;
        }

        private Args _args;

        private void NLPFunction(double[] x, double[] fi, double[,] jac, object? o)
        {
            /*
             * unpacking constants
             */
            double moonSOI = _args.MoonSOI;
            double peR = _args.PeR;
            //double inc = _args.Inc;
            V3 r0 = _args.R0;
            V3 v0 = _args.V0;
            V3 moonR0 = _args.MoonR0;
            V3 moonV0 = _args.MoonV0;
            Scale moonToPlanetScale = _args.MoonToPlanetScale;

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

            Dual p = DualV3.Dot(new DualV3(rf, new V3(1, 0, 0)), vf);
            fi[13]      = p.M;
            jac[13, 14] = p.D;
            jac[13, 15] = DualV3.Dot(new DualV3(rf, new V3(0, 1, 0)), vf).D;
            jac[13, 16] = DualV3.Dot(new DualV3(rf, new V3(0, 0, 1)), vf).D;
            jac[13, 17] = DualV3.Dot(rf, new DualV3(vf, new V3(1, 0, 0))).D;
            jac[13, 18] = DualV3.Dot(rf, new DualV3(vf, new V3(0, 1, 0))).D;
            jac[13, 19] = DualV3.Dot(rf, new DualV3(vf, new V3(0, 0, 1))).D;

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

            fi[15]     = moonDt1 + moonDt2 + burnTime - moonDt;
            jac[15, 3] = 1.0;
            jac[15, 4] = 1.0;
            jac[15, 5] = 1.0;
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

        private (V3 dv, double dt, V3 rf, V3 vf, double tt2) Guess(double inc, bool type2, bool full = false)
        {
            double moonSOI = _args.MoonSOI;
            double peR = _args.PeR;
            V3 r0 = _args.R0;
            V3 v0 = _args.V0;
            V3 moonR0 = _args.MoonR0;
            V3 moonV0 = _args.MoonV0;
            Scale planetScale = _args.PlanetScale;
            Scale moonScale = _args.MoonScale;
            Scale moonToPlanetScale = _args.MoonToPlanetScale;

            V3 moonRsoi = moonR0;
            V3 moonVsoi = moonV0;
            V3 dv = V3.maxvalue;
            V3 vf = V3.zero, rf = V3.zero;
            double dt = 0;
            double tt2 = 0;

            for (int i = 0; i < 2; i++)
            {
                // kick the moon's orbit to the new peR and inc to build the return ellipse
                V3 dv1 = ChangeOrbitalElement.ChangeApsis(1.0, moonRsoi, moonVsoi, peR);
                dv1 += Simple.DeltaVToChangeInclination(moonRsoi, moonVsoi + dv1, inc);

                // get the return ellipse keplerian elements
                (double sma, double ecc, double _, double lan, double argp, double _, double l) =
                    Astro.KeplerianFromStateVectors(1.0, moonRsoi, moonVsoi + dv1);

                // find approximate tanom of the moon's SOI on the return ellipse
                double moonSOI2 = moonSOI / moonToPlanetScale.LengthScale;
                double rsoi = 2 * sma - moonSOI2;
                double nuSOI = SafeAcos((sma * (1 - ecc * ecc) / rsoi - 1) / ecc);

                // type2 return a transfer that starts with tanom < 180
                if (!type2)
                    nuSOI = TAU - nuSOI;

                // get the SOI ejection point on the return ellipse
                (V3 rsoiPlanet, V3 vsoiPlanet) = Astro.StateVectorsFromKeplerian(1.0, 1.2 * l, ecc, inc, lan, argp, nuSOI);
                tt2                            = Astro.TimeToNextTrueAnomaly(1.0, rsoiPlanet, vsoiPlanet, 0);

                if (full)
                {
                    // get the final periapsis position on the return ellipse for actual initial guess
                    (rf, vf) = Astro.StateVectorsFromKeplerian(1.0, l, ecc, inc, lan, argp, 0);

                    (double sma3, double ecc3, double inc3, double lan3, double argp3, double tanom3, double l3) =
                        Astro.KeplerianFromStateVectors(1.0, rsoiPlanet, vsoiPlanet);

                    Print(
                        $"Guessed return ellipse:\nsma:{sma3 * planetScale.LengthScale} ecc:{ecc3} inc:{Rad2Deg(inc3)} lan:{Rad2Deg(lan3)} argp:{Rad2Deg(argp3)} tanom:{Rad2Deg(tanom3)}");

                    Print($"vinf fed to hyperbolic guesser: {(vsoiPlanet - moonVsoi) * moonToPlanetScale.VelocityScale * moonScale.VelocityScale}");
                }

                V3 vneg, vpos, rburn;

                (vneg, vpos, rburn, dt) =
                    Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, (vsoiPlanet - moonVsoi) * moonToPlanetScale.VelocityScale);

                dv = vpos - vneg;

                double tt = Astro.TimeToNextRadius(1.0, rburn, vpos, moonSOI);

                (moonRsoi, moonVsoi) = Shepperd.Solve(1.0, (dt + tt) / moonToPlanetScale.TimeScale, moonR0, moonV0);
            }

            return (dv, dt, rf, vf, tt2);
        }

        private (double inc, bool type2) GuessOptimizer()
        {
            double f, fx;
            double imin = 0, imincost = double.MaxValue;
            bool type2 = false;
            (f, fx) = BrentMin.Solve((x, o) => Guess(x, false).Item1.magnitude, -PI, 0, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = false;
            }

            (f, fx) = BrentMin.Solve((x, o) => Guess(x, false).Item1.magnitude, 0, PI, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = false;
            }

            (f, fx) = BrentMin.Solve((x, o) => Guess(x, true).Item1.magnitude, -PI, 0, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = true;
            }

            (f, fx) = BrentMin.Solve((x, o) => Guess(x, true).Item1.magnitude, 0, PI, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = true;
            }

            Scale moonScale = _args.MoonScale;
            Print($"{Rad2Deg(imin)} {imincost * moonScale.VelocityScale} {type2}");
            return (imin, type2);
        }

        private double[] GenerateInitialGuess()
        {
            var sw = new Stopwatch();
            sw.Start();

            double[] x = new double[NVARIABLES];

            double moonSOI = _args.MoonSOI;
            double peR = _args.PeR;
            V3 r0 = _args.R0;
            V3 v0 = _args.V0;
            V3 moonR0 = _args.MoonR0;
            V3 moonV0 = _args.MoonV0;
            Scale moonScale = _args.MoonScale;
            Scale moonToPlanetScale = _args.MoonToPlanetScale;

            double dt, tt2 = 0;
            V3 dv, rf = V3.zero, vf = V3.zero;

            if (Astro.EccFromStateVectors(1.0, r0, v0) < 1 && Astro.ApoapsisFromStateVectors(1.0, r0, v0) < moonSOI)
            {
                (double imin, bool type2) = GuessOptimizer();
                (dv, dt, rf, vf, tt2)     = Guess(imin, type2, true);
            }
            else
            {
                // if we're already on an escape trajectory, try an immediate burn and hope we're reasonably close
                (dv, dt, rf, vf, tt2) = InitiallyHyperbolicGuess();
            }

            (V3 rburn, V3 vburn) = Shepperd.Solve(1.0, dt, r0, v0);
            double tt1 = Astro.TimeToNextRadius(1.0, rburn, vburn + dv, moonSOI);
            (V3 rsoi, V3 vsoi) = Shepperd.Solve(1.0, tt1, rburn, vburn + dv);

            Print($"vsoi from analytic ejection guess: {vsoi * moonScale.VelocityScale}");

            (V3 rsoiPlanet, V3 vsoiPlanet) = Shepperd.Solve(1.0, -tt2, rf, vf);
            (V3 moonR1, V3 moonV1)         = Shepperd.Solve(1.0, dt + tt1, moonR0, moonV0);
            V3 vsoi2 = (vsoiPlanet - moonV1) * moonToPlanetScale.VelocityScale;

            Print($"vsoi working backwards from rf, vf: {vsoi2 * moonScale.VelocityScale}");

            V3 r2Sph = rsoi.cart2sph;
            //V3 v2Sph = vsoi2.cart2sph;
            // taking the average of the mismatch in vsoi and spreading the infeasibility over both SOIs seems to actually
            // produce better convergence properties.
            V3 v2Sph = ((vsoi2 + vsoi) / 2).cart2sph;

            Print($"vsoi in initial guess: {v2Sph.sph2cart * moonScale.VelocityScale}");

            x[0]  = dv.x;      // maneuver x (moonscale)
            x[1]  = dv.y;      // maneuver y (moonscale)
            x[2]  = dv.z;      // maneuver z (moonscale)
            x[3]  = dt;        // maneuver dt (moonscale)
            x[4]  = tt1 * 0.5; // 1/2 coast time through moon SOI (moonscale)
            x[5]  = tt1 * 0.5; // 1/2 coast time through moon SOI (moonscale)
            x[6]  = r2Sph[1];  // theta of SOI position (angle)
            x[7]  = r2Sph[2];  // phi of SOI position (angle)
            x[8]  = v2Sph[0];  // magnitude of SOI velocity (moonscale)
            x[9]  = v2Sph[1];  // theta of SOI velocity (angle)
            x[10] = v2Sph[2];  // phi of SOI velocity (angle)
            x[11] = tt1 + dt;  // total time in the moon SOI (moonscale)
            x[12] = tt2 * 0.5; // 1/2 coast time through planet SOI (planetscale)
            x[13] = tt2 * 0.5; // 1/2 coast time thorugh planet SOI (planetscale)
            x[14] = rf.x;      // final rx (planetscale)
            x[15] = rf.y;      // final ry (planetscale)
            x[16] = rf.z;      // final rz (planetscale)
            x[17] = vf.x;      // final vx (planetscale)
            x[18] = vf.y;      // final vy (planetscale)
            x[19] = vf.z;      // final vz (planetscale)

            sw.Stop();
            Print($"initial guess generation took {sw.ElapsedMilliseconds}ms");

            return x;
        }

        private (V3 dv, double dt, V3 rf, V3 vf, double tt2) InitiallyHyperbolicGuess()
        {
            double moonSOI = _args.MoonSOI;
            double peR = _args.PeR;
            V3 r0 = _args.R0;
            V3 v0 = _args.V0;
            V3 moonR0 = _args.MoonR0;
            V3 moonV0 = _args.MoonV0;
            Scale moonToPlanetScale = _args.MoonToPlanetScale;

            double tt1 = Astro.TimeToNextRadius(1.0, r0, v0, moonSOI);
            (V3 r2, V3 v2)         = Shepperd.Solve(1.0, tt1, r0, v0);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, tt1 / moonToPlanetScale.TimeScale, moonR0, moonV0);
            V3 r3 = r2 / moonToPlanetScale.LengthScale + moonR2;
            V3 v3 = v2 / moonToPlanetScale.VelocityScale + moonV2;
            (V3 rf, V3 vf) = Astro.StateVectorsAtTrueAnomaly(1.0, r3, v3, 0);
            double tt2 = Astro.TimeToNextTrueAnomaly(1.0, r3, v3, 0);

            rf = rf.normalized * peR;
            vf = V3.Cross(V3.Cross(rf, vf), rf).normalized * vf.magnitude;

            return (V3.zero, 0, rf, vf, tt2);
        }

        private const double DIFFSTEP = 1e-9;
        private const double EPSX     = 1e-5;
        private const double STPMAX   = 1e-4;
        private const int    MAXITS   = 10000;

        private const int NVARIABLES             = 20;
        private const int NEQUALITYCONSTRAINTS   = 17;
        private const int NINEQUALITYCONSTRAINTS = 0;

        private (V3 V, double dt, V3 vinf) ManeuverScaled(double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity,
            bool optguard = false)
        {
            double[] x = GenerateInitialGuess();
            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndl[i] = double.NegativeInfinity;
                bndu[i] = double.PositiveInfinity;
            }

            bndl[3] = dtmin;
            bndu[3] = dtmax;

            //alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlccreate(NVARIABLES, x, out alglib.minnlcstate state);

            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetstpmax(state, STPMAX);
            //double rho = 1000.0;
            //int outerits = 5;
            //alglib.minnlcsetalgoaul(state, rho, outerits);
            alglib.minnlcsetalgoslp(state);
            //alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            if (optguard)
            {
                alglib.minnlcoptguardsmoothness(state);
                alglib.minnlcoptguardgradient(state, DIFFSTEP);
            }

            double[] fi = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1];
            double[,] jac = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1, NVARIABLES];

            NLPFunction(x, fi, jac, null);

            var sw = new Stopwatch();
            sw.Start();

            alglib.minnlcoptimize(state, NLPFunction, null, null);

            sw.Stop();

            alglib.minnlcresults(state, out double[] x2, out alglib.minnlcreport rep);
            Print($"optimization took {sw.ElapsedMilliseconds}ms: {rep.iterationscount} iter, {rep.nfev} fev");

            NLPFunction(x2, fi, jac, null);

            for (int i = 0; i < x2.Length; i++)
                Print($"x[{i}]: {x2[i]} - {x[i]} = {x2[i] - x[i]} ({100 * (x2[i] - x[i]) / x2[i]}%)");


            if (optguard)
            {
                alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep);

                if (ogrep.badgradsuspected)
                    throw new Exception(
                        $"badgradsuspected: {ogrep.badgradfidx},{ogrep.badgradvidx}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, 1e-2)}");

                if (ogrep.nonc0suspected || ogrep.nonc1suspected)
                    throw new Exception("alglib optguard caught an error, i should report better on errors now");
            }

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver(): SQP solver terminated abnormally: {rep.terminationtype}"
                );

            if (rep.nlcerr > 1e-4)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver() no feasible solution found, constraint violation: {rep.nlcerr}");

            return (new V3(x2[0], x2[1], x2[2]), x2[3], new V3(x2[8], x2[9], x2[10]).sph2cart);
        }

        private (V3 V, double dt) Maneuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI,
            V3 r0, V3 v0, double peR, double inc, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity,
            bool optguard = false)
        {
            Print(
                $"ReturnFromMoon.Maneuver({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");

            var moonScale = Scale.Create(moonMu, Sqrt(r0.magnitude * moonSOI));
            var planetScale = Scale.Create(centralMu, Sqrt(moonR0.magnitude * peR));
            Scale moonToPlanetScale = moonScale.ConvertTo(planetScale);

            _args = new Args
            {
                MoonSOI = moonSOI / moonScale.LengthScale,
                PeR     = peR / planetScale.LengthScale,
                //Inc               = inc,
                R0                = r0 / moonScale.LengthScale,
                V0                = v0 / moonScale.VelocityScale,
                MoonR0            = moonR0 / planetScale.LengthScale,
                MoonV0            = moonV0 / planetScale.VelocityScale,
                MoonToPlanetScale = moonToPlanetScale,
                MoonScale         = moonScale,
                PlanetScale       = planetScale
            };

            LogStuff1(moonScale);

            /*
            for(double i = -PI; i <= PI; i += PI/36)
            {
                (V3 dv2, _, _, _, _) = Guess(i, false);
                Print($"{Rad2Deg(i)} false {dv2.magnitude * moonScale.VelocityScale}");
            }
            for(double i = -PI; i <= PI; i += PI/36)
            {
                (V3 dv2, _, _, _, _) = Guess(i, true);
                Print($"{Rad2Deg(i)} true {dv2.magnitude * moonScale.VelocityScale}");
            }
            */

            var sw = new Stopwatch();
            sw.Start();

            (V3 dv, double dt, V3 vinf) = ManeuverScaled(dtmin / moonScale.TimeScale, dtmax / moonScale.TimeScale, optguard);

            sw.Stop();
            Print($"total calculation took {sw.ElapsedMilliseconds}ms");

            LogStuff2(dv, dt, vinf);

            return (dv * moonScale.VelocityScale, dt * moonScale.TimeScale);
        }

        private void LogStuff1(Scale moonScale)
        {
            V3 r0 = _args.R0;
            V3 v0 = _args.V0;
            V3 moonR0 = _args.MoonR0;
            V3 moonV0 = _args.MoonV0;
            Scale planetScale = _args.PlanetScale;

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) =
                Astro.KeplerianFromStateVectors(1.0, moonR0, moonV0);

            Print(
                $"Secondary celestial orbit:\nsma:{sma * planetScale.LengthScale} ecc:{ecc} inc:{Rad2Deg(inc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");

            (sma, ecc, inc, lan, argp, tanom, _) =
                Astro.KeplerianFromStateVectors(1.0, r0, v0);

            Print(
                $"Parking orbit around Secondary:\nsma:{sma * moonScale.LengthScale} ecc:{ecc} inc:{Rad2Deg(inc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");
        }

        private void LogStuff2(V3 dv, double dt, V3 vinf)
        {
            double moonSOI = _args.MoonSOI;
            V3 r0 = _args.R0;
            V3 v0 = _args.V0;
            V3 moonR0 = _args.MoonR0;
            V3 moonV0 = _args.MoonV0;
            Scale moonToPlanetScale = _args.MoonToPlanetScale;
            Scale planetScale = _args.PlanetScale;
            Scale moonScale = _args.MoonScale;

            (V3 r1, V3 v1) = Shepperd.Solve(1.0, dt, r0, v0);
            double tt1 = Astro.TimeToNextRadius(1.0, r1, v1 + dv, moonSOI);
            (V3 r2, V3 v2)         = Shepperd.Solve(1.0, tt1, r1, v1 + dv);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, (dt + tt1) / moonToPlanetScale.TimeScale, moonR0, moonV0);
            V3 r3 = r2 / moonToPlanetScale.LengthScale + moonR2;
            V3 v3 = v2 / moonToPlanetScale.VelocityScale + moonV2;

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) =
                Astro.KeplerianFromStateVectors(1.0, r2, v2);

            Print(
                $"Ejection orbit around Secondary:\nsma:{sma * moonScale.LengthScale} ecc:{ecc} inc:{Rad2Deg(inc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");

            (sma, ecc, inc, lan, argp, tanom, _) =
                Astro.KeplerianFromStateVectors(1.0, r3, v3);

            Print(
                $"Return orbit around Primary:\nsma:{sma * planetScale.LengthScale} ecc:{ecc} inc:{Rad2Deg(inc)} lan:{Rad2Deg(lan)} argp:{Rad2Deg(argp)} tanom:{Rad2Deg(tanom)}");

            double peR = Astro.PeriapsisFromStateVectors(1.0, r3, v3);

            Print(
                $"deltaV: {dv * moonScale.VelocityScale}/{dv.magnitude * moonScale.VelocityScale} dt: {dt * moonScale.TimeScale} vinf: {vinf * moonScale.VelocityScale} PeR: {peR * planetScale.LengthScale} (solved)");

            if (Astro.EccFromStateVectors(1.0, r0, v0) < 1 && Astro.ApoapsisFromStateVectors(1.0, r0, v0) < moonSOI)
            {
                (V3 vneg, V3 vpos, V3 _, double dt2) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vinf);

                V3 dv2 = vpos - vneg;

                Print(
                    $"deltaV: {dv2 * moonScale.VelocityScale}/{dv2.magnitude * moonScale.VelocityScale} dt: {dt2 * moonScale.TimeScale} (analytic validation)");
            }
        }

        public (V3 dv, double dt, double newPeR) NextManeuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0,
            double moonSOI, V3 r0, V3 v0, double peR, double inc, double dtmin = 0, double dtmax = double.PositiveInfinity,
            bool optguard = false)
        {
            double dt;
            V3 dv;
            int i = 0;

            (double _, double ecc) = Astro.SmaEccFromStateVectors(moonMu, r0, v0);

            while (true)
            {
                (dv, dt) = Maneuver(centralMu, moonMu, moonR0, moonV0, moonSOI, r0, v0, peR, inc, dtmin, dtmax, optguard);
                if (dt > 0 || ecc >= 1)
                    break;
                if (i++ >= 5)
                    throw new Exception("Maximum iterations exceeded with no valid future solution");
                double period = Astro.PeriodFromStateVectors(moonMu, r0, v0);
                dtmin += period;
            }

            // propagate burn forward to determine newPeR as a validation step
            (V3 r1, V3 v1) = Shepperd.Solve(moonMu, dt, r0, v0);
            double tt1 = Astro.TimeToNextRadius(moonMu, r1, v1 + dv, moonSOI);
            (V3 r2, V3 v2)         = Shepperd.Solve(moonMu, tt1, r1, v1 + dv);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, dt + tt1, moonR0, moonV0);
            double newPeR = Astro.PeriapsisFromStateVectors(centralMu, moonR2 + r2, moonV2 + v2);

            return (dv, dt, newPeR);
        }
    }
}
