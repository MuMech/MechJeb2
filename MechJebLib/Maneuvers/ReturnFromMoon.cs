/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.Maneuvers
{
    public class ReturnFromMoon
    {
        private double _soi;
        private double _peR;
        private double _cosInc;
        private double _inc;
        private TransferGeometry _direction = TransferGeometry.ShortWay;

        private V3 _r0;
        private V3 _v0;
        private V3 _moonR0;
        private V3 _moonV0;

        private Scale _moonToPlanetScale;

        private void NLPFunction(double[] x, double[] fi, object? o)
        {
            V3 rsoi    = new V3(_soi, x[2], x[3]).sph2cart;
            V3 vsoiPos = new V3(x[4], x[5], x[6]).sph2cart;

            (V3 rsoiPlanet, V3 vsoiPlanet, V3 dv1, V3 dv2) = GenerateValues(x);

            fi[0] = dv1.sqrMagnitude;
            fi[1] = dv2.x;
            fi[2] = dv2.y;
            fi[3] = dv2.z;
            fi[4] = Astro.PeriapsisFromStateVectors(1.0, rsoiPlanet, vsoiPlanet) - _peR;
            fi[5] = IsFinite(_cosInc) && _peR > 0 ? V3.Cross(rsoiPlanet, vsoiPlanet).normalized.z - _cosInc : 0;
            fi[6] = -V3.Dot(rsoi, vsoiPos);
        }

        private (V3 rsoiPlanet, V3 vsoiPlanet, V3 dv1, V3 dv2) GenerateValues(double[] x)
        {
            double tBurn  = x[0]; // moon scaled
            double tCoast = x[1]; // moon scaled

            V3 rSoi    = new V3(_soi, x[2], x[3]).sph2cart; // moon scaled
            V3 vSoiPos = new V3(x[4], x[5], x[6]).sph2cart; // moon scaled

            // All moon scaled
            (V3 rBurn, V3 vNeg) = Shepperd.Solve(1.0, tBurn, _r0, _v0);

            (V3 vPos, V3 vSoiNeg) = Izzo.Solve(1.0, rBurn, rSoi, tCoast, _direction);
            V3 dv1 = vPos - vNeg;
            V3 dv2 = vSoiPos - vSoiNeg;

            // All planet scaled
            (V3 moonRsoi, V3 moonVsoi) = Shepperd.Solve(1.0, (tBurn + tCoast) / _moonToPlanetScale.TimeScale, _moonR0, _moonV0);
            V3 rsoiPlanet = rSoi / _moonToPlanetScale.LengthScale + moonRsoi;
            V3 vsoiPlanet = vSoiPos / _moonToPlanetScale.VelocityScale + moonVsoi;

            return (rsoiPlanet, vsoiPlanet, dv1, dv2);
        }

        private void GenerateGuess(double[] x)
        {
            double tBurn, tCoast;
            V3     rSoi,  vSoi;


            double tSoi = 0;
            int    i    = 0;
            while (true)
            {
                Print($"{tSoi}");
                // propagate moon forward to guessed t_soi time
                (V3 moonRsoi, V3 moonVsoi) = Shepperd.Solve(1.0, tSoi, _moonR0, _moonV0);
                // get sma of the transfer orbit
                double sma = Astro.SmaFromApsides(_peR, moonRsoi.magnitude);
                // calcuate the earth interface state vectors
                V3 rEei = -moonRsoi.normalized * _peR;
                V3 vEei = Astro.VelocityFromRadiusSMA(1.0, _peR, sma) * moonRsoi.orthonormal;
                vEei = Astro.VelocityForInclination(rEei, vEei, IsFinite(_inc) ? _inc : Deg2Rad(-90));
                // fictitious apoapsis velocity of the transfer orbit
                V3 vApo = -vEei.normalized * Astro.VelocityFromRadiusSMA(1.0, moonRsoi.magnitude, sma);
                // radius from earth at the SOI
                double rSoiEarth = 2 * sma - _soi / _moonToPlanetScale.LengthScale;
                // calculate the fictitious escape time on the transfer orbit from the fictitious apoapsis
                double tFictitious = Astro.TimeToNextRadius(1.0, moonRsoi, vApo, rSoiEarth);

                // if this is the first time through the fictitious escape time is already a better guess
                if (i++ == 0)
                {
                    tSoi = tFictitious;
                    continue;
                }

                // find the state vectors at the SOI and use that to construct the vinf approximation
                (V3 rSoiPlanet, V3 vSoiPlanet) = Shepperd.Solve(1.0, tFictitious, moonRsoi, vApo);
                rSoi = rSoiPlanet - moonRsoi;
                vSoi = vSoiPlanet - moonVsoi;

                // solve the hyperbolic burn problem
                V3 vPos, rBurn;
                (_, vPos, rBurn, tBurn) = SingleImpulseHyperbolicBurn.Solve(1.0, _r0, _v0, vSoi * _moonToPlanetScale.VelocityScale);

                // find the coast time on the hyperbolic trajectory to the radius
                tCoast = Astro.TimeToNextRadius(1.0, rBurn, vPos, _soi);

                break;
            }

            rSoi = (rSoi * _moonToPlanetScale.LengthScale).cart2sph;
            vSoi = (vSoi * _moonToPlanetScale.VelocityScale).cart2sph;

            // all moon scaled
            x[0] = tBurn;
            x[1] = tCoast;
            x[2] = rSoi.y;
            x[3] = rSoi.z;
            x[4] = vSoi.x;
            x[5] = vSoi.y;
            x[6] = vSoi.z;
        }

        private const double DIFFSTEP = 1e-6;
        private const double EPSX = 1e-6;
        private const int MAXITS = 2500;
        private const int NVARIABLES = 7;
        private const int NEQUALITYCONSTRAINTS = 5;
        private const int NINEQUALITYCONSTRAINTS = 1;

        public (V3 dv, double dt) NextManeuver(double centralMu, double moonMu, V3 moonR0, V3 moonV0, double moonSOI, V3 r0, V3 v0, double peR, double inc = double.NaN)
        {
            Print(
                $"ReturnFromMoon.Maneuver({centralMu}, {moonMu}, new V3({moonR0}), new V3({moonV0}), {moonSOI}, new V3({r0}), new V3({v0}), {peR}, {inc})");

            var   moonScale         = Scale.Create(moonMu, Sqrt(r0.magnitude * moonSOI));
            var   planetScale       = Scale.Create(centralMu, Sqrt(peR * moonR0.magnitude));
            Scale moonToPlanetScale = moonScale.ConvertTo(planetScale);

            _soi = moonSOI / moonScale.LengthScale;
            _r0 = r0 / moonScale.LengthScale;
            _v0 = v0 / moonScale.VelocityScale;

            _peR = peR / planetScale.LengthScale;
            _moonR0 = moonR0 / planetScale.LengthScale;
            _moonV0 = moonV0 / planetScale.VelocityScale;
            _moonToPlanetScale = moonToPlanetScale;

            _cosInc = Cos(inc);
            _inc = inc;

            double[] x0   = new double[NVARIABLES];
            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            GenerateGuess(x0);

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndl[i] = double.NegativeInfinity;
                bndu[i] = double.PositiveInfinity;
            }

            bndl[0] = 0;
            bndl[1] = EPS;

            _direction = TransferGeometry.ShortWay;
            alglib.minnlccreatef(NVARIABLES, x0, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, 0, MAXITS);
            alglib.minnlcsetnlc(state, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);
            alglib.minnlcoptimize(state, NLPFunction, null, null);
            alglib.minnlcresults(state, out double[] x1, out alglib.minnlcreport rep1);

            double[] fi = new double[NEQUALITYCONSTRAINTS + NINEQUALITYCONSTRAINTS + 1];
            NLPFunction(x1, fi, null);
            double shortCost  = fi[0];
            double shortError = Max(Max(rep1.bcerr, rep1.lcerr), rep1.nlcerr);

            Print($"termination type: {rep1.terminationtype}");
            Print($"iterations count: {rep1.iterationscount}");
            Print($"num function evals: {rep1.nfev}");
            Print($"cost = {shortCost}");
            Print($"maxerr = {shortError}");
            for (int i = 0; i < fi.Length; i++)
                Print($"fi[{i}]: {fi[i]}");

            _direction = TransferGeometry.LongWay;
            alglib.minnlccreatef(NVARIABLES, x0, DIFFSTEP, out alglib.minnlcstate state2);
            alglib.minnlcsetbc(state2, bndl, bndu);
            alglib.minnlcsetalgosqp(state2);
            alglib.minnlcsetcond(state2, 0, MAXITS);
            alglib.minnlcsetnlc(state2, NEQUALITYCONSTRAINTS, NINEQUALITYCONSTRAINTS);
            alglib.minnlcoptimize(state2, NLPFunction, null, null);
            alglib.minnlcresults(state2, out double[] x2, out alglib.minnlcreport rep2);

            NLPFunction(x2, fi, null);
            double longCost  = fi[0];
            double longError = Max(Max(rep2.bcerr, rep2.lcerr), rep2.nlcerr);

            Print($"termination type: {rep1.terminationtype}");
            Print($"iterations count: {rep1.iterationscount}");
            Print($"num function evals: {rep1.nfev}");
            Print($"cost = {longCost}");
            Print($"maxerr = {longError}");
            for (int i = 0; i < fi.Length; i++)
                Print($"fi[{i}]: {fi[i]}");

            // if they're both less than 1e-10, decide based on cost
            // if either are larger than 1e-10, decide based on error

            bool decideOnError = longError > 1e-10 || shortError > 1e-10;

            bool pickShort = decideOnError ? shortError < longError : shortCost < longCost;

            if (pickShort)
            {
                _direction = TransferGeometry.ShortWay;
                (V3 _, V3 _, V3 dv, V3 _) = GenerateValues(x1);
                return (dv * moonScale.VelocityScale, x1[0] * moonScale.TimeScale);
            }
            else
            {
                _direction = TransferGeometry.LongWay;
                (V3 _, V3 _, V3 dv, V3 _) = GenerateValues(x2);
                return (dv * moonScale.VelocityScale, x2[0] * moonScale.TimeScale);
            }
        }
    }
}
