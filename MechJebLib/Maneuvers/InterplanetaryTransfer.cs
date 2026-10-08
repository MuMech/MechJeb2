/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Linq;
using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Maneuvers
{
    public class InterplanetaryTransfer
    {
        private V3 _r0;
        private V3 _v0;
        private V3 _r1;
        private V3 _v1;
        private V3 _r2;
        private V3 _v2;
        private double _soi1;
        private double _soi2;
        private double _peR;
        private double _cosInc;
        private bool _captureBurn;

        private bool _initialFeasibility;

        /// <summary>
        ///     The inclination that Maneuver() targeted, which is the requested inclination clamped to be at least
        ///     INC_CLAMP_MARGIN further from equatorial than the declination of the arrival v-infinity (NaN if no
        ///     inclination was requested).
        /// </summary>
        public double TargetInc { get; private set; } = double.NaN;

        private Scale _sourceScale;
        private Scale _targetScale;
        private Scale _helioScale;
        private Scale _sourceToHelioScale;
        private Scale _targetToHelioScale;

        private void NLPFunctionColumn(Dual[] x, double[] fi, double[,] jac, int i)
        {
            DualV3 rsoi1, vsoi1, rsoi2, vsoi2, dv1, dv2, dv3, dv4, dv5;

            try
            {
                (rsoi1, vsoi1, rsoi2, vsoi2, dv1, dv2, dv3, dv4, dv5) = EvaluateTrajectory(x);
            }
            catch (Exception) // FIXME: Exception types for the Lambert solvers and catch specific ones here
            {
                // The SQP solver can wander into bad parameter spaces of the Lambert solver, and this
                // value causes alglib to back off.
                fi[0] = 1e300;
                return;
            }

            Dual fi0 = 0.5 * dv1.sqrMagnitude;
            if (IsFinite(_peR) && _soi2 > 0 && _captureBurn)
            {
                Dual sma = Astro.SmaFromStateVectors(1.0, rsoi2, vsoi2);
                Dual dv = Astro.VelocityFromRadiusSMA(1.0, _peR, sma) - Astro.CircularVelocity(1.0, _peR);
                fi0 += dv * dv;
            }

            fi[0] = fi0.M;
            jac[0, i] = fi0.D;

            if (IsFinite(_peR) && _soi2 > 0)
            {
                Dual periapsis = Astro.PeriapsisFromStateVectors(1.0, rsoi2, vsoi2);
                Dual fi1 = _peR > 0 ? periapsis / _peR - 1.0 : DualV3.Dot(rsoi2.normalized, vsoi2.normalized) + 1.0;
                fi[1] = fi1.M;
                jac[1, i] = fi1.D;
            }
            else
            {
                fi[1] = 0;
                jac[1, i] = 0;
            }

            Dual fi2 = IsFinite(_cosInc) && IsFinite(_peR) && _peR > 0 && _soi2 > 0 ? DualV3.Cross(rsoi2, vsoi2).normalized.z - _cosInc : 0;
            fi[2] = fi2.M;
            jac[2, i] = fi2.D;
            fi[3] = dv2.x.M;
            jac[3, i] = dv2.x.D;
            fi[4] = dv2.y.M;
            jac[4, i] = dv2.y.D;
            fi[5] = dv2.z.M;
            jac[5, i] = dv2.z.D;
            fi[6] = dv3.x.M;
            jac[6, i] = dv3.x.D;
            fi[7] = dv3.y.M;
            jac[7, i] = dv3.y.D;
            fi[8] = dv3.z.M;
            jac[8, i] = dv3.z.D;
            fi[9] = dv4.x.M;
            jac[9, i] = dv4.x.D;
            fi[10] = dv4.y.M;
            jac[10, i] = dv4.y.D;
            fi[11] = dv4.z.M;
            jac[11, i] = dv4.z.D;
            fi[12] = dv5.x.M;
            jac[12, i] = dv5.x.D;
            fi[13] = dv5.y.M;
            jac[13, i] = dv5.y.D;
            fi[14] = dv5.z.M;
            jac[14, i] = dv5.z.D;
            Dual fi15 = -DualV3.Dot(rsoi1.normalized, vsoi1.normalized);
            fi[15] = fi15.M;
            jac[15, i] = fi15.D;
            Dual fi16 = _soi2 == 0 ? new Dual(0) : DualV3.Dot(rsoi2.normalized, vsoi2.normalized);
            fi[16] = fi16.M;
            jac[16, i] = fi16.D;

            if (_initialFeasibility)
            {
                fi[1] = fi[2] = 0;
                jac[1, i] = jac[2, i] = 0;
            }
        }

        private readonly Dual[] _duals = new Dual[NVARIABLES];

        private void NLPFunction(double[] x, double[] fi, double[,] jac, object? obj = null)
        {
            for (int i = 0; i < x.Length; i++)
            {
                for (int j = 0; j < x.Length; j++)
                    _duals[j] = new Dual(x[j], i == j ? 1.0 : 0.0);

                NLPFunctionColumn(_duals, fi, jac, i);
            }
        }

        private (DualV3 rsoi1, DualV3 vsoi1, DualV3 rsoi2, DualV3 vsoi2, DualV3 dv1, DualV3 dv2, DualV3 dv3, DualV3 dv4, DualV3 dv5) EvaluateTrajectory(Dual[] x)
        {
            if (x.Any(v => !IsFinite(v.M)))
                throw new Exception("invalid value");

            Dual dt1 = x[0]; // coast time on initial orbit to burn (source scale)
            Dual dt2 = x[1]; // coast time after burn to soi1 interface (source scale)
            Dual dt3 = x[2]; // arrival time at destination soi (helio scale)
            Dual heliocoast = dt3 - (dt1 + dt2) / _sourceToHelioScale.TimeScale; // coast time on heliocentric orbit (helio scale)

            var rsoiSph1 = new DualV3(_soi1, x[3], x[4]); // spherical position at soi1 boundary (source scale)
            var vsoiSph1 = new DualV3(x[5], x[6], x[7]); // spherical velocity at soi1 boundary (source scale)

            DualV3 rsoi1 = rsoiSph1.sph2cart;
            DualV3 vsoi1 = vsoiSph1.sph2cart;

            var rmidHelio = new DualV3(x[8], x[9], x[10]); // heliocentric position at the midpoint of the heliocentric coast (helio scale)

            DualV3 rsoiSph2 = _soi2 == 0 ? new DualV3(0, 0, 0) : new DualV3(_soi2, x[11], x[12]); // spherical position at soi2 boundary (target scale)
            var vsoiSph2 = new DualV3(x[13], x[14], x[15]); // spherical velocity at soi2 boundary (target scale)

            DualV3 rsoi2 = rsoiSph2.sph2cart;
            DualV3 vsoi2 = vsoiSph2.sph2cart;

            // propagate initial orbit to burn
            (DualV3 r0Burn, DualV3 v0Burn) = Shepperd.Solve(1.0, dt1, _r0, _v0);

            // propagate source celestial to soi1 intercept time
            (DualV3 r1soi1, DualV3 v1soi1) = Shepperd.Solve(1.0, (dt1 + dt2) / _sourceToHelioScale.TimeScale, _r1, _v1);

            // propagate target celestial to soi2 intercept time
            (DualV3 r2soi2, DualV3 v2soi2) = Shepperd.Solve(1.0, dt3, _r2, _v2);

            // convert soi1 intercept to heliocentric coordinates
            DualV3 rsoi1helio = r1soi1 + rsoi1 / _sourceToHelioScale.LengthScale;
            DualV3 vsoi1helio = v1soi1 + vsoi1 / _sourceToHelioScale.VelocityScale;

            // convert soi2 intercept to heliocentric coordinates
            DualV3 rsoi2helio = r2soi2 + rsoi2 / _targetToHelioScale.LengthScale;
            DualV3 vsoi2helio = v2soi2 + vsoi2 / _targetToHelioScale.VelocityScale;

            // solve from the burn to the soi1 interface
            // (this uses the prograde sense of the rsoi1 x vsoi1 plane, equivalent to the departure arc, which avoids lambert discontinuities)
            (DualV3 vi1, DualV3 vf1) = Russell.Solve(1.0, r0Burn, rsoi1, dt2, TransferGeometry.Prograde, h: V3.Cross(rsoi1.M, vsoi1.M));

            // solve the heliocentric trajectory from soi1 to soi2 as two equal-time legs through a free midpoint, so that
            // neither leg sits on the 180 degree lambert singularity when the overall transfer angle is near 180 degrees
            // (this uses prograde sense from the rsoi1helio x vsoi1helio plane, which avoids lambert discontinuities)
            V3 hHelio = V3.Cross(rsoi1helio.M, vsoi1helio.M);
            (DualV3 vi2, DualV3 vf2) = Russell.Solve(1.0, rsoi1helio, rmidHelio, 0.5 * heliocoast, TransferGeometry.Prograde, h: hHelio);
            (DualV3 vi3, DualV3 vf3) = Russell.Solve(1.0, rmidHelio, rsoi2helio, 0.5 * heliocoast, TransferGeometry.Prograde, h: hHelio);

            return (rsoi1, vsoi1, rsoi2, vsoi2, vi1 - v0Burn, vsoi1 - vf1, vsoi1helio - vi2, vi3 - vf2, vf3 - vsoi2helio);
        }

        private (V3 rsoi1, V3 vsoi1, V3 rsoi2, V3 vsoi2, V3 dv1, V3 dv2, V3 dv3, V3 dv4, V3 dv5) EvaluateTrajectory(double[] x)
        {
            for (int j = 0; j < x.Length; j++)
                _duals[j] = new Dual(x[j]);

            (DualV3 rsoi1, DualV3 vsoi1, DualV3 rsoi2, DualV3 vsoi2, DualV3 dv1, DualV3 dv2, DualV3 dv3, DualV3 dv4, DualV3 dv5) = EvaluateTrajectory(_duals);

            return (rsoi1.M, vsoi1.M, rsoi2.M, vsoi2.M, dv1.M, dv2.M, dv3.M, dv4.M, dv5.M);
        }

        private const int NUM_EQUALITY_CONSTRAINTS = 14;
        private const int NUM_INEQUALITY_CONSTRAINTS = 2;
        private const int MAXITS = 5000;
        private const double INC_CLAMP_MARGIN = 2 * DEG2RAD;
        private const int NVARIABLES = 16;

        public (V3 dv, double dt1, double dt2, double dt3) Maneuver(V3 r0, V3 v0, double mu1, V3 r1, V3 v1, double soi1, double mu2, V3 r2, V3 v2, double soi2, double mu3, double arrivalDT, double arrivalDTlower = 0, double arrivalDTupper = double.PositiveInfinity, double peR = double.PositiveInfinity, double inc = double.NaN, bool captureBurn = false, bool optguard = false)
        {
            Print($"InterplanetaryTransfer.Maneuver({r0}, {v0}, {mu1:G17}, {r1}, {v1}, {soi1:G17}, {mu2:G17}, {r2}, {v2}, {soi2:G17}, {mu3:G17}, {arrivalDT:G17}, arrivalDTlower: {arrivalDTlower:G17} arrivalDTupper: {arrivalDTupper:G17}, peR: {peR:G17}, inc: {inc:G17}, captureBurn: {captureBurn})");
            Check.PositiveFinite(mu1);
            Check.NonNegativeFinite(mu2);
            Check.PositiveFinite(mu3);
            Check.Finite(r0);
            Check.Finite(v0);
            Check.Finite(r1);
            Check.Finite(v1);
            Check.Finite(r2);
            Check.Finite(v2);
            Check.PositiveFinite(soi1);
            Check.NonNegativeFinite(soi2);
            Check.NonNegative(peR);

            // problem scaling

            _sourceScale = Scale.Create(mu1, Sqrt(r0.magnitude * soi1));
            _targetScale = Scale.Create(1, 1);
            if (soi2 > 0)
                _targetScale = Scale.Create(mu2, IsFinite(peR) && peR > 0 ? peR : soi2);
            _helioScale = Scale.Create(mu3, Sqrt(r1.magnitude * r2.magnitude));
            _sourceToHelioScale = _sourceScale.ConvertTo(_helioScale);
            _targetToHelioScale = _targetScale.ConvertTo(_helioScale);

            _soi1 = soi1 / _sourceScale.LengthScale;
            _soi2 = soi2 / _targetScale.LengthScale;
            _peR = peR / _targetScale.LengthScale;
            _r0 = r0 / _sourceScale.LengthScale;
            _v0 = v0 / _sourceScale.VelocityScale;
            _r1 = r1 / _helioScale.LengthScale;
            _v1 = v1 / _helioScale.VelocityScale;
            _r2 = r2 / _helioScale.LengthScale;
            _v2 = v2 / _helioScale.VelocityScale;
            _cosInc = Cos(inc);
            TargetInc = inc;
            _captureBurn = captureBurn;

            // initialization

            double[] x0 = new double[NVARIABLES];

            double arrivalUTscaled = arrivalDT / _helioScale.TimeScale;

            // propagate target celestial to soi2 intercept time
            (V3 r2soi2, V3 v2soi2) = Shepperd.Solve(1.0, arrivalUTscaled, _r2, _v2);

            // solve the ZSOI heliocentric trajectory from source to target
            (V3 viBootstrap, V3 _) = Russell.Solve(1.0, _r1, r2soi2, arrivalUTscaled, TransferGeometry.Prograde, h: V3.Cross(_r1, _v1));

            // estimate travel time to the SOI boundary and propagate the source celestial
            (V3 _, V3 vPosBootstrap, V3 rBurnBootstrap, double dt1Bootstrap) = Astro.SingleImpulseHyperbolicBurn(1.0, _r0, _v0, (viBootstrap - _v1) * _sourceToHelioScale.VelocityScale);
            double dt2Bootstrap = Astro.TimeToNextRadius(1.0, rBurnBootstrap, vPosBootstrap, _soi1);
            double dt1HelioBootstrap = (dt1Bootstrap + dt2Bootstrap) / _sourceToHelioScale.TimeScale;
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(1.0, dt1HelioBootstrap, _r1, _v1);

            // re-solve the ZSOI helicentric trajectory with estimated travel time to the first SOI boundary
            (V3 vi, V3 vf) = Russell.Solve(1.0, r1soi1, r2soi2, arrivalUTscaled - dt1HelioBootstrap, TransferGeometry.Prograde, h: V3.Cross(r1soi1, v1soi1));

            // refine the ZSOI solution into finite SOI
            (V3 _, V3 vsoi1) = Astro.StateVectorsAtDistance(1.0, r1soi1, vi, soi1 / _helioScale.LengthScale);
            //(V3 rsoi2, V3 vsoi2) = Astro.StateVectorsAtDistance(1.0, r2soi2, -vf, soi2 / _helioScale.LengthScale);

            vsoi1 = ((vsoi1 - v1soi1) * _sourceToHelioScale.VelocityScale).cart2sph;
            V3 vsoi2 = ((vf - v2soi2) * _targetToHelioScale.VelocityScale).cart2sph;

            x0[11] = 0;
            x0[12] = 0;
            x0[13] = vsoi2.x;
            x0[14] = vsoi2.y;
            x0[15] = vsoi2.z;

            (V3 _, V3 vPos, V3 rBurn, double dt1) = Astro.SingleImpulseHyperbolicBurn(1.0, _r0, _v0, vsoi1.sph2cart);
            double dt2 = Astro.TimeToNextRadius(1.0, rBurn, vPos, _soi1);
            (V3 rsoiSph1, V3 vsoiSph1) = Shepperd.Solve(1.0, dt2, rBurn, vPos);

            // midpoint of the heliocentric coast from the soi1 exit point to the target, using the refined departure times
            double dt12Helio = (dt1 + dt2) / _sourceToHelioScale.TimeScale;
            (V3 r1exit, V3 v1exit) = Shepperd.Solve(1.0, dt12Helio, _r1, _v1);
            V3 rexitHelio = r1exit + rsoiSph1 / _sourceToHelioScale.LengthScale;
            V3 vexitHelio = v1exit + vsoiSph1 / _sourceToHelioScale.VelocityScale;
            (V3 viExit, V3 _) = Russell.Solve(1.0, rexitHelio, r2soi2, arrivalUTscaled - dt12Helio, TransferGeometry.Prograde, h: V3.Cross(rexitHelio, vexitHelio));
            (V3 rmidHelio, V3 _) = Shepperd.Solve(1.0, 0.5 * (arrivalUTscaled - dt12Helio), rexitHelio, viExit);

            rsoiSph1 = rsoiSph1.cart2sph;
            vsoiSph1 = vsoiSph1.cart2sph;

            x0[0] = dt1;
            x0[1] = dt2;
            x0[2] = arrivalUTscaled;
            x0[3] = rsoiSph1.y;
            x0[4] = rsoiSph1.z;
            x0[5] = vsoiSph1.x;
            x0[6] = vsoiSph1.y;
            x0[7] = vsoiSph1.z;
            x0[8] = rmidHelio.x;
            x0[9] = rmidHelio.y;
            x0[10] = rmidHelio.z;

            // box constraints

            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndu[i] = double.PositiveInfinity;
                bndl[i] = double.NegativeInfinity;
            }

            double period = Astro.PeriodFromStateVectors(1.0, _r0, _v0);
            bndl[0] = -period;
            bndu[0] = period;
            bndl[1] = EPS;
            bndl[2] = Max(arrivalDTlower / _helioScale.TimeScale, EPS);
            bndu[2] = arrivalDTupper / _helioScale.TimeScale;
            bndl[13] = Sqrt(EPS);

            Solution sol;
            try
            {
                sol = RunOptimizer(x0, bndl, bndu, optguard);
            }
            catch (alglib.alglibexception e)
            {
                // alglibexception never passes its message to the base constructor, so Message is useless
                throw new Exception(e.msg, e);
            }

            Print($"dv: {sol.dv * _sourceScale.VelocityScale} dt1: {sol.dt1 * _sourceScale.TimeScale} dt2: {sol.dt2 * _sourceScale.TimeScale}, dt3: {sol.dt3 * _helioScale.TimeScale}");
            return (sol.dv * _sourceScale.VelocityScale, sol.dt1 * _sourceScale.TimeScale, sol.dt2 * _sourceScale.TimeScale, sol.dt3 * _helioScale.TimeScale);
        }

        private readonly struct Solution
        {
            public readonly V3 dv;
            public readonly double dt1;
            public readonly double dt2;
            public readonly double dt3;
            public readonly double cost;
            public readonly double err;

            public Solution(V3 dv, double dt1, double dt2, double dt3, double cost, double err)
            {
                this.dv = dv;
                this.dt1 = dt1;
                this.dt2 = dt2;
                this.dt3 = dt3;
                this.cost = cost;
                this.err = err;
            }
        }

        private (double[] x, alglib.minnlcreport rep) RunPass(double[] x0, double[] bndl, double[] bndu, bool optguard)
        {
            alglib.minnlccreate(x0, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, 0, MAXITS);
            alglib.minnlcsetnlc(state, NUM_EQUALITY_CONSTRAINTS, NUM_INEQUALITY_CONSTRAINTS);
#if DEBUG
            if (optguard)
                alglib.minnlcoptguardgradient(state, 1e-8);
#endif
            alglib.minnlcoptimize(state, NLPFunction, null, null);
            alglib.minnlcresults(state, out double[] x, out alglib.minnlcreport rep);

#if DEBUG
            if (optguard)
            {
                bool[] boxConstrained = new bool[NVARIABLES];

                alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep);

                if (ogrep.badgradsuspected)
                    if (!DoubleMatrixSparsityValidation(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2))
                        throw new Exception(
                            $"badgradsuspected:\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, null, boxConstrained, 1e-2)}");

                if (ogrep.nonc0suspected)
                    throw new Exception("nonc0suspected");

                if (ogrep.nonc1suspected)
                    throw new Exception("nonc1suspected");
            }
#endif

            return (x, rep);
        }

        private Solution RunOptimizer(double[] x0, double[] bndl, double[] bndu, bool optguard)
        {
            _initialFeasibility = true;
            double savedsoi2 = _soi2;
            _soi2 = 0;

            DebugPrint("initial constraint violation:");
            GetCost(x0);

            (double[] x1, alglib.minnlcreport rep1) = RunPass(x0, bndl, bndu, optguard);

            (double cost, double err) = AnalyzeSolution(x1, rep1);

            // - if we're significantly infeasible, don't bother trying the terminal conditions.
            // - and if we're only asking for a zerosoi (vessel intercept) then we're just done with the problem.
            if (err > 1e-4 || savedsoi2 == 0)
            {
                V3 dv1 = GetManeuverDeltaV(x1, ref cost, ref err);

                return new Solution(dv1, x1[0], x1[1], x1[2], cost, err);
            }

            // restore state to solve the full problem with target constraints
            _initialFeasibility = false;
            _soi2 = savedsoi2;

            // we should start with a near zero periapsis infalling vsoi2
            // nudge the solution in the b-plane to closer to the _peR that we want to target
            var vsoiSph2 = new V3(x1[13], x1[14], x1[15]);
            V3 vsoi2 = vsoiSph2.sph2cart;

            double vinf = vsoi2.magnitude;
            V3 vhat = vsoi2 / vinf;
            double b = _peR > 0 ? Min(_peR * Sqrt(1 + 2 / (_peR * vinf * vinf)), 0.99 * _soi2) : 0;
            V3 reference = Abs(vhat.z) < 0.9 ? new V3(0, 0, 1) : new V3(1, 0, 0);
            V3 nhat = V3.Cross(vhat, reference).normalized;
            V3 rsoi2 = b * nhat - Sqrt(_soi2 * _soi2 - b * b) * vhat;

            V3 rsoiSph2 = rsoi2.cart2sph;

            x1[11] = rsoiSph2.y;
            x1[12] = rsoiSph2.z;
            x1[13] = vsoiSph2.x;
            x1[14] = vsoiSph2.y;
            x1[15] = vsoiSph2.z;

            for (int i = 0; i < x1.Length; i++)
                DebugPrint($"x1[{i}] = {x1[i]}");

            // the plane of the arrival hyperbola contains v-infinity, so its inclination is no less than the declination of
            // v-infinity (and no more than 180 degrees minus it).  getting any closer to equatorial means bending the
            // heliocentric trajectory to rotate v-infinity, which is expensive and stalls the optimizer.  so solve with the
            // inclination free first, then clamp the inclination to what that v-infinity can reach and solve again.  the
            // ZSOI v-infinity is not good enough for this, at Jupiter its declination is off by up to ~5 degrees.
            double cosInc = _cosInc;
            _cosInc = double.NaN;

            DebugPrint("second-pass initial constraint violation:");
            GetCost(x1);

            (double[] x2, alglib.minnlcreport rep2) = RunPass(x1, bndl, bndu, optguard);

            (double cost2, double err2) = AnalyzeSolution(x2, rep2);

            _cosInc = cosInc;

            if (IsFinite(_cosInc) && IsFinite(_peR) && _peR > 0 && err2 <= 1e-4)
            {
                // the margin is because the inclination at the limit is degenerate (the two b-plane solutions merge), and
                // because rotating the plane moves the soi entry point which moves v-infinity a bit.
                (_, _, rsoi2, vsoi2, _, _, _, _, _) = EvaluateTrajectory(x2);
                double decl = Asin(Abs(IncomingAsymptote(1.0, rsoi2, vsoi2).z));
                double cosIncMax = Cos(Min(decl + INC_CLAMP_MARGIN, PI / 2));
                if (Abs(_cosInc) > cosIncMax)
                {
                    _cosInc = Sign(_cosInc) * cosIncMax;
                    TargetInc = Acos(_cosInc);
                    Print($"clamping inc to {Rad2Deg(TargetInc):G17} degrees, the arrival v-infinity has a declination of {Rad2Deg(decl):G17} degrees");
                }

                DebugPrint("third-pass initial constraint violation:");
                GetCost(x2);

                (x2, rep2) = RunPass(x2, bndl, bndu, optguard);

                (cost2, err2) = AnalyzeSolution(x2, rep2);
            }

            for (int i = 0; i < x2.Length; i++)
                DebugPrint($"x2[{i}] = {x2[i]}");

            V3 dv2 = GetManeuverDeltaV(x2, ref cost2, ref err2);

            return new Solution(dv2, x2[0], x2[1], x2[2], cost2, err2);
        }

        // direction of the incoming asymptote of a hyperbolic orbit
        private static V3 IncomingAsymptote(double mu, V3 r, V3 v)
        {
            V3 ecc = Astro.EccVecFromStateVectors(mu, r, v);
            double e = ecc.magnitude;
            V3 p = ecc / e;
            V3 q = V3.Cross(V3.Cross(r, v).normalized, p);
            return (p + Sqrt(e * e - 1) * q) / e;
        }

        private (double cost, double err) AnalyzeSolution(double[] x, alglib.minnlcreport rep)
        {
            DebugPrint($"termination type: {rep.terminationtype}");
            DebugPrint($"iterations count: {rep.iterationscount}");
            DebugPrint($"num function evals: {rep.nfev}");
            double err = Max(Max(rep.bcerr, rep.lcerr), rep.nlcerr);
            DebugPrint($"maxerr = {err}");
            double cost = GetCost(x);
            return (cost, err);
        }

        private readonly double[] _fi = new double[NUM_EQUALITY_CONSTRAINTS + NUM_INEQUALITY_CONSTRAINTS + 1];
        private readonly double[,] _jac = new double[NUM_EQUALITY_CONSTRAINTS + NUM_INEQUALITY_CONSTRAINTS + 1, NVARIABLES];

        private double GetCost(double[] x)
        {
            NLPFunction(x, _fi, _jac, true);
            double cost = _fi[0];

            DebugPrint($"cost = {cost}");
            for (int i = 0; i < _fi.Length; i++)
                DebugPrint($"fi[{i}]: {_fi[i]}");
            return cost;
        }

        /* WAS USEFUL FOR DIAGNOSING FINITE DIFFERENCING BUGS
        private void NoiseFloorCheck(double[] x)
        {
            int m = NUM_EQUALITY_CONSTRAINTS + NUM_INEQUALITY_CONSTRAINTS + 1;
            double[] fi0 = new double[m];
            NLPFunction(x, fi0, _jac);

            foreach (int i in new[]{11, 12, 13, 14, 15})
            {
                DebugPrint($"--- var {i} = {x[i]} ---");
                foreach (double h in new[]{1e-1, 1e-2, 1e-3,1e-4,1e-5,1e-6,1e-7,1e-8,1e-9,1e-10})
                {
                    double[] xp = (double[])x.Clone(); xp[i] += h;
                    double[] fip = new double[m];
                    NLPFunction(xp, fip, _jac);
                    // raw delta floors at ~δf; derivative shows convergence then jitter
                    DebugPrint($"h={h:E1} dPe={fip[1]-fi0[1]:E3} dPe={(fip[1]-fi0[1])/h:E3}");
                }
            }
        }
        */

        private V3 GetManeuverDeltaV(double[] x, ref double cost, ref double err)
        {
            V3 dv;
            try
            {
                (_, _, _, _, dv, _, _, _, _) = EvaluateTrajectory(x);
            }
            catch (Exception)
            {
                cost = double.PositiveInfinity;
                err = double.PositiveInfinity;
                dv = V3.positiveinfinity;
            }

            return dv;
        }
    }
}
