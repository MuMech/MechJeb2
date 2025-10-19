/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Diagnostics;
using MechJebLib.Functions;
using MechJebLib.Lambert;
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

            public V3 R0;
            public V3 V0;
            public V3 MoonR0;
            public V3 MoonV0;

            public Scale MoonToPlanetScale;
            public Scale MoonScale;
            public Scale PlanetScale;
        }

        private Args _args;

        private (V3 V, double dt, V3 vinf) SolutionToManeuver(double[] x)
        {
            /*
             * unpacking constants
             */
            double moonSOI = _args.MoonSOI;
            V3     r0      = _args.R0;
            V3     v0      = _args.V0;

            /*
             * unpacking optimizer variables
             */

            double dt   = x[0];
            double tt1  = x[1];
            V3     rsoi = new V3(moonSOI, x[2], x[3]).sph2cart;

            (V3 rburn, V3 vneg) = Shepperd.Solve(1.0, dt, r0, v0);
            (V3 vpos, V3 vsoi)  = Gooding.Solve(1.0, rburn, vneg, rsoi, tt1, 0, true);
            V3 dv = vpos - vneg;
            Print($"dot(rsoi,vsoi): {V3.Dot(rsoi, vsoi)}");

            return (dv, dt, vsoi);
        }

        private void NLPFunction(double[] x, double[] fi, object? o)
        {
            /*
             * unpacking constants
             */
            double moonSOI           = _args.MoonSOI;
            double peR               = _args.PeR;
            V3     r0                = _args.R0;
            V3     v0                = _args.V0;
            V3     moonR0            = _args.MoonR0;
            V3     moonV0            = _args.MoonV0;
            Scale  moonToPlanetScale = _args.MoonToPlanetScale;

            /*
             * unpacking optimizer variables
             */

            double dt   = x[0];
            double tt1  = x[1];
            V3     rsoi = new V3(moonSOI, x[2], x[3]).sph2cart;
            double tt2  = x[4];
            V3     reei = new V3(peR, x[5], x[6]).sph2cart;

            (V3 rburn, V3 vneg) = Shepperd.Solve(1.0, dt, r0, v0);
            (V3 vpos, V3 vsoi)  = Gooding.Solve(1.0, rburn, vneg, rsoi, tt1, 0, true);
            V3 dv = vpos - vneg;

            (V3 moonRsoi, V3 moonVsoi) = Shepperd.Solve(1.0, (dt + tt1) / moonToPlanetScale.TimeScale, moonR0, moonV0);
            V3 rsoiPlanet = rsoi / moonToPlanetScale.LengthScale + moonRsoi;
            V3 vsoiPlanet = vsoi / moonToPlanetScale.VelocityScale + moonVsoi;
            (V3 vsoiPlanet2, V3 veei) = Gooding.Solve(1.0, rsoiPlanet, vsoiPlanet, reei, tt2, 0);

            fi[0] = dv.sqrMagnitude;
            fi[1] = vsoiPlanet2.x - vsoiPlanet.x;
            fi[2] = vsoiPlanet2.y - vsoiPlanet.y;
            fi[3] = vsoiPlanet2.z - vsoiPlanet.z;
            fi[4] = V3.Dot(reei, veei);
        }

        private (V3 dv, double dt, double tt1, V3 rsoi, double tt2, V3 rf) Guess(double inc, bool type2, int periodOffset, bool full = false)
        {
            double moonSOI           = _args.MoonSOI;
            double peR               = _args.PeR;
            V3     r0                = _args.R0;
            V3     v0                = _args.V0;
            V3     moonR0            = _args.MoonR0;
            V3     moonV0            = _args.MoonV0;
            Scale  planetScale       = _args.PlanetScale;
            Scale  moonScale         = _args.MoonScale;
            Scale  moonToPlanetScale = _args.MoonToPlanetScale;

            V3     dv     = V3.maxvalue;
            V3     rf     = V3.zero;
            V3     rsoi   = V3.zero;
            double period = Astro.PeriodFromStateVectors(1.0, r0, v0);
            double dt     = periodOffset * period;
            double tt2    = 0;
            double tt1    = 0;

            for (int i = 0; i < 2; i++)
            {
                (V3 moonRsoi, V3 moonVsoi) = Shepperd.Solve(1.0, (dt + tt1) / moonToPlanetScale.TimeScale, moonR0, moonV0);

                // kick the moon's orbit to the new peR and inc to build the return ellipse
                V3 dv1 = ChangeOrbitalElement.ChangeApsis(1.0, moonRsoi, moonVsoi, peR);
                dv1 += Simple.DeltaVToChangeInclination(moonRsoi, moonVsoi + dv1, inc);

                // get the return ellipse keplerian elements
                (double sma, double ecc, double _, double lan, double argp, double _, double l) =
                    Astro.KeplerianFromStateVectors(1.0, moonRsoi, moonVsoi + dv1);

                // find approximate tanom of the moon's SOI on the return ellipse
                double moonSOI2  = moonSOI / moonToPlanetScale.LengthScale;
                double soiradius = 2 * sma - moonSOI2;
                double nuSOI     = SafeAcos((sma * (1 - ecc * ecc) / soiradius - 1) / ecc);

                // type2 return a transfer that starts with tanom < 180
                if (!type2)
                    nuSOI = TAU - nuSOI;

                // get the SOI ejection point on the return ellipse
                (V3 rsoiPlanet, V3 vsoiPlanet) = Astro.StateVectorsFromKeplerian(1.0, l, ecc, inc, lan, argp, nuSOI);

                if (full)
                {
                    // get the final periapsis position on the return ellipse for actual initial guess
                    tt2     = Astro.TimeToNextTrueAnomaly(1.0, rsoiPlanet, vsoiPlanet, 0);
                    (rf, _) = Astro.StateVectorsFromKeplerian(1.0, l, ecc, inc, lan, argp, 0);

                    (double sma3, double ecc3, double inc3, double lan3, double argp3, double tanom3, double l3) =
                        Astro.KeplerianFromStateVectors(1.0, rsoiPlanet, vsoiPlanet);

                    Print(
                        $"Guessed return ellipse:\nsma:{sma3 * planetScale.LengthScale} ecc:{ecc3} inc:{Rad2Deg(inc3)} lan:{Rad2Deg(lan3)} argp:{Rad2Deg(argp3)} tanom:{Rad2Deg(tanom3)}");

                    Print($"vinf fed to hyperbolic guesser: {(vsoiPlanet - moonVsoi) * moonToPlanetScale.VelocityScale * moonScale.VelocityScale}");
                }

                V3 vneg, vpos, rburn;

                (vneg, vpos, rburn, dt) =
                    Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, (vsoiPlanet - moonVsoi) * moonToPlanetScale.VelocityScale);

                dt += period * periodOffset;

                dv = vpos - vneg;

                tt1 = Astro.TimeToNextRadius(1.0, rburn, vpos, moonSOI);

                if (full)
                {
                    (rsoi, _) = Shepperd.Solve(1.0, dt + tt1, r0, v0);
                    Print($"dt guess = {dt}, tt1 guess = {tt1} tt2 guess = {tt2}");
                }
            }

            return (dv, dt, tt1, rsoi, tt2, rf);
        }

        private (double inc, bool type2) GuessOptimizer(int periodOffset)
        {
            double f,         fx;
            double imin  = 0, imincost = double.MaxValue;
            bool   type2 = false;
            (f, fx) = BrentMin.Solve((x, o) => Guess(x, false, periodOffset).Item1.magnitude, -PI, 0, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = false;
            }

            (f, fx) = BrentMin.Solve((x, o) => Guess(x, false, periodOffset).Item1.magnitude, 0, PI, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = false;
            }

            (f, fx) = BrentMin.Solve((x, o) => Guess(x, true, periodOffset).Item1.magnitude, -PI, 0, rtol: 1e-2);
            if (fx < imincost)
            {
                imin     = f;
                imincost = fx;
                type2    = true;
            }

            (f, fx) = BrentMin.Solve((x, o) => Guess(x, true, periodOffset).Item1.magnitude, 0, PI, rtol: 1e-2);
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

        private double[] GenerateInitialGuess(int periodOffset)
        {
            var sw = new Stopwatch();
            sw.Start();

            double[] x = new double[NVARIABLES];

            double moonSOI = _args.MoonSOI;
            V3     r0      = _args.R0;
            V3     v0      = _args.V0;

            double dt,   tt2, tt1;
            V3     rsoi, rf;

            if (Astro.EccFromStateVectors(1.0, r0, v0) < 1 && Astro.ApoapsisFromStateVectors(1.0, r0, v0) < moonSOI)
            {
                (double imin, bool type2)   = GuessOptimizer(periodOffset);
                (_, dt, tt1, rsoi, tt2, rf) = Guess(imin, type2, periodOffset, true);
            }
            else
            {
                // if we're already on an escape trajectory, try an immediate burn and hope we're reasonably close
                (dt, tt1, rsoi, tt2, rf) = InitiallyHyperbolicGuess();
            }

            V3 r2Sph  = rsoi.cart2sph;
            V3 eeiSph = rf.cart2sph;

            x[0] = dt;        // maneuver dt (moonscale)
            x[1] = tt1;       // coast time through moon SOI (moonscale)
            x[2] = r2Sph[1];  // theta of SOI position (angle)
            x[3] = r2Sph[2];  // phi of SOI position (angle)
            x[4] = tt2;       // coast time through planet SOI (planetscale)
            x[5] = eeiSph[1]; // theta of EEI position (angle)
            x[6] = eeiSph[2]; // phi of EEI position (angle)

            sw.Stop();
            Print($"initial guess generation took {sw.ElapsedMilliseconds}ms");

            return x;
        }

        private (double dt, double tt1, V3 rsoi, double tt2, V3 rf) InitiallyHyperbolicGuess()
        {
            double moonSOI           = _args.MoonSOI;
            double peR               = _args.PeR;
            V3     r0                = _args.R0;
            V3     v0                = _args.V0;
            V3     moonR0            = _args.MoonR0;
            V3     moonV0            = _args.MoonV0;
            Scale  moonToPlanetScale = _args.MoonToPlanetScale;

            double tt1 = Astro.TimeToNextRadius(1.0, r0, v0, moonSOI);
            (V3 rsoi, V3 vsoi)     = Shepperd.Solve(1.0, tt1, r0, v0);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(1.0, tt1 / moonToPlanetScale.TimeScale, moonR0, moonV0);
            V3 r3 = rsoi / moonToPlanetScale.LengthScale + moonR2;
            V3 v3 = vsoi / moonToPlanetScale.VelocityScale + moonV2;
            (V3 rf, V3 _) = Astro.StateVectorsAtTrueAnomaly(1.0, r3, v3, 0);
            double tt2 = Astro.TimeToNextTrueAnomaly(1.0, r3, v3, 0);

            rf = rf.normalized * peR;

            return (0, tt1, rsoi, tt2, rf);
        }

        private const double DIFFSTEP = 1e-9;
        private const double EPSX     = 1e-6;
        private const double STPMAX   = 1e-4;
        private const int    MAXITS   = 10000;

        private const int NVARIABLES             = 7;
        private const int NEQUALITYCONSTRAINTS   = 4;
        private const int NINEQUALITYCONSTRAINTS = 0;

        private (V3 V, double dt, V3 vinf) ManeuverScaled(double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity,
            int periodOffset = 0,
            bool optguard = false)
        {
            double[] x    = GenerateInitialGuess(periodOffset);
            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];
            bool[]   boxConstrained =  new bool[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndl[i] = double.NegativeInfinity;
                bndu[i] = double.PositiveInfinity;
            }

            bndl[0] = dtmin;
            bndu[0] = dtmax;

            alglib.minnlccreatef(NVARIABLES, x, DIFFSTEP, out alglib.minnlcstate state);
            //alglib.minnlccreate(NVARIABLES, x, out alglib.minnlcstate state);

            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetstpmax(state, STPMAX);
            //double rho = 1000.0;
            //int outerits = 5;
            //alglib.minnlcsetalgoaul(state, rho, outerits);
            //alglib.minnlcsetalgoslp(state);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, EPSX, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);

            if (optguard)
            {
                alglib.minnlcoptguardsmoothness(state);
                alglib.minnlcoptguardgradient(state, DIFFSTEP);
            }

            double[]  fi  = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1];
            double[,] jac = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1, NVARIABLES];

            //NLPFunction(x, fi, jac, null);

            var sw = new Stopwatch();
            sw.Start();

            alglib.minnlcoptimize(state, NLPFunction, null, null);

            sw.Stop();

            alglib.minnlcresults(state, out double[] x2, out alglib.minnlcreport rep);
            Print($"optimization took {sw.ElapsedMilliseconds}ms: {rep.iterationscount} iter, {rep.nfev} fev");

            NLPFunction(x2, fi, null);

            for (int i = 0; i < fi.Length; i++)
                Print($"fi[{i}]: {fi[i]}");

            for (int i = 0; i < x2.Length; i++)
                Print($"x[{i}]: {x2[i]} - {x[i]} = {x2[i] - x[i]} ({100 * (x2[i] - x[i]) / x2[i]}%)");


            if (optguard)
            {
                alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep);

                if (ogrep.badgradsuspected)
                    throw new Exception(
                        $"badgradsuspected: {ogrep.badgradfidx},{ogrep.badgradvidx}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2)}");

                if (ogrep.nonc0suspected || ogrep.nonc1suspected)
                    throw new Exception("alglib optguard caught an error, i should report better on errors now");
            }

            if (rep.terminationtype < 0)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver(): SLP solver terminated abnormally: {rep.terminationtype}"
                );

            if (rep.nlcerr > 1e-2)
                throw new Exception(
                    $"ReturnFromMoon.Maneuver() no feasible solution found, constraint violation: {rep.nlcerr}");

            return SolutionToManeuver(x2);
        }

        private (V3 V, double dt) Maneuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI, V3 r0, V3 v0, double peR,
            double inc, double dtmin = double.NegativeInfinity, double dtmax = double.PositiveInfinity, int periodOffset = 0,
            bool optguard = false)
        {
            Print(
                $"ReturnFromMoon.Maneuver({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");

            var   moonScale         = Scale.Create(moonMu, Sqrt(r0.magnitude * moonSOI));
            var   planetScale       = Scale.Create(centralMu, Sqrt(moonR0.magnitude * peR));
            Scale moonToPlanetScale = moonScale.ConvertTo(planetScale);

            _args = new Args
            {
                MoonSOI           = moonSOI / moonScale.LengthScale,
                PeR               = peR / planetScale.LengthScale,
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
                (V3 dv2, _, _, _, _, _) = Guess(i, false, periodOffset);
                Print($"{Rad2Deg(i)} false {dv2.magnitude * moonScale.VelocityScale}");
            }
            for(double i = -PI; i <= PI; i += PI/36)
            {
                (V3 dv2, _, _, _, _, _) = Guess(i, true, periodOffset);
                Print($"{Rad2Deg(i)} true {dv2.magnitude * moonScale.VelocityScale}");
            }
            */

            var sw = new Stopwatch();
            sw.Start();

            (V3 dv, double dt, V3 vinf) = ManeuverScaled(dtmin / moonScale.TimeScale, dtmax / moonScale.TimeScale, periodOffset, optguard);

            sw.Stop();
            Print($"total calculation took {sw.ElapsedMilliseconds}ms");

            LogStuff2(dv, dt, vinf);

            return (dv * moonScale.VelocityScale, dt * moonScale.TimeScale);
        }

        private void LogStuff1(Scale moonScale)
        {
            V3    r0          = _args.R0;
            V3    v0          = _args.V0;
            V3    moonR0      = _args.MoonR0;
            V3    moonV0      = _args.MoonV0;
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
            double moonSOI           = _args.MoonSOI;
            V3     r0                = _args.R0;
            V3     v0                = _args.V0;
            V3     moonR0            = _args.MoonR0;
            V3     moonV0            = _args.MoonV0;
            Scale  moonToPlanetScale = _args.MoonToPlanetScale;
            Scale  planetScale       = _args.PlanetScale;
            Scale  moonScale         = _args.MoonScale;

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

            Print($"vinf: {vinf * moonScale.VelocityScale} PeR: {peR * planetScale.LengthScale}");

            Print(
                $"deltaV: {dv * moonScale.VelocityScale}/{dv.magnitude * moonScale.VelocityScale} dt: {dt * moonScale.TimeScale} (solved)");

            if (Astro.EccFromStateVectors(1.0, r0, v0) < 1 && Astro.ApoapsisFromStateVectors(1.0, r0, v0) < moonSOI)
            {
                (V3 vneg, V3 vpos, V3 _, double dt2) = Astro.SingleImpulseHyperbolicBurn(1.0, r0, v0, vinf);

                V3 dv2 = vpos - vneg;

                Print(
                    $"deltaV: {dv2 * moonScale.VelocityScale}/{dv2.magnitude * moonScale.VelocityScale} dt: {dt2 * moonScale.TimeScale} (analytic validation)");
            }
        }

        public (V3 dv, double dt, double newPeR) NextManeuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI, V3 r0, V3 v0,
            double peR, double inc, bool optguard = false)
        {
            double dt;
            V3     dv;

            (double _, double ecc) = Astro.SmaEccFromStateVectors(moonMu, r0, v0);

            if (ecc >= 1)
            {
                (dv, dt) = Maneuver(centralMu, moonMu, moonR0, moonV0, moonSOI, r0, v0, peR, inc, 0, optguard: optguard);
            }
            else
            {
                double period       = Astro.PeriodFromStateVectors(moonMu, r0, v0);
                int    i            = 0;
                int    periodOffset = 0;
                double dtmin        = 0;
                double dtmax        = period;
                while (true)
                {
                    (dv, dt) = Maneuver(centralMu, moonMu, moonR0, moonV0, moonSOI, r0, v0, peR, inc, dtmin, dtmax, periodOffset, optguard);
                    if (dt <= 1e-4)
                    {
                        periodOffset += 1;
                        dtmax        += period;
                    }
                    else if (NearlyEqual(dt, dtmax, 1e-4) || dt >= dtmax)
                    {
                        dtmax += period;
                    }
                    else
                    {
                        break;
                    }

                    if (i++ >= 5)
                        throw new Exception("Maximum iterations exceeded with no valid future solution");
                }
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
