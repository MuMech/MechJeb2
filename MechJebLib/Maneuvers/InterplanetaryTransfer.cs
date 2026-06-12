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

        private TransferGeometry _direction1;
        private TransferGeometry _direction2;
        private bool _initialFeasibility;

        private Scale _sourceScale;
        private Scale _targetScale;
        private Scale _helioScale;
        private Scale _sourceToHelioScale;
        private Scale _targetToHelioScale;

        private void NLPFunctionColumn(Dual[] x, double[] fi, double[,] jac, int i)
        {
            DualV3 rsoi1, vsoi1, rsoi2, vsoi2, dv1, dv2, dv3, dv4;

            try
            {
                (rsoi1, vsoi1, rsoi2, vsoi2, dv1, dv2, dv3, dv4) = EvaluateTrajectory(x);
            }
            catch (Exception)
            {
                fi[0] = 1e300;
                return;
            }

            Dual fi0 = 0.5 * dv1.sqrMagnitude;
            if (IsFinite(_peR) && _soi1 > 0 && _captureBurn)
            {
                Dual sma = Astro.SmaFromStateVectors(1.0, rsoi2, vsoi2);
                Dual dv = Astro.VelocityFromRadiusSMA(1.0, _peR, sma) - Astro.CircularVelocity(1.0, _peR);
                fi0 += dv * dv;
            }

            fi[0] = fi0.M;
            jac[0, i] = fi0.D;

            if (IsFinite(_peR) && _soi1 > 0)
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

            Dual fi2 = IsFinite(_cosInc) && IsFinite(_peR) && _peR > 0 && _soi1 > 0 ? DualV3.Cross(rsoi2, vsoi2).normalized.z - _cosInc : 0;
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
            Dual fi12 = -DualV3.Dot(rsoi1.normalized, vsoi1.normalized);
            fi[12] = fi12.M;
            jac[12, i] = fi12.D;
            Dual fi13 = _soi2 == 0 ? new Dual(0) : DualV3.Dot(rsoi2.normalized, vsoi2.normalized);
            fi[13] = fi13.M;
            jac[13, i] = fi13.D;

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

        private (DualV3 rsoi1, DualV3 vsoi1, DualV3 rsoi2, DualV3 vsoi2, DualV3 dv1, DualV3 dv2, DualV3 dv3, DualV3 dv4) EvaluateTrajectory(Dual[] x)
        {
            if (x.Any(v => !IsFinite(v.M)))
                throw new Exception("invalid value");

            Dual dt1 = x[0]; // coast time on initial orbit to burn (source scale)
            Dual dt2 = x[1]; // coast time after burn to soi1 interface (source scale)
            Dual dt3 = x[2]; // coast time on heliocentric orbit (helio scale)

            var rsoiSph1 = new DualV3(_soi1, x[3], x[4]); // spherical position at soi1 boundary (source scale)
            var vsoiSph1 = new DualV3(x[5], x[6], x[7]);  // spherical velocity at soi1 boundary (source scale)

            DualV3 rsoi1 = rsoiSph1.sph2cart;
            DualV3 vsoi1 = vsoiSph1.sph2cart;

            DualV3 rsoiSph2 = _soi2 == 0 ? new DualV3(0, 0, 0) : new DualV3(_soi2, x[8], x[9]); // spherical position at soi2 boundary (target scale)
            var vsoiSph2 = new DualV3(x[10], x[11], x[12]);                                     // spherical velocity at soi2 boundary (target scale)

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
            (DualV3 vi1, DualV3 vf1) = Izzo.Solve(1.0, r0Burn, rsoi1, dt2, _direction1, rtol: 1e-12);

            // solve the heliocentric trajectory from soi1 to soi2
            // (this uses prograde/retrograde sense from the rsoi1helio x vsoi1helio plane since shortway/longway has a 180
            // degree singularity that causes issues for SQP solvers and near-hohmann transfers)
            (DualV3 vi2, DualV3 vf2) = Izzo.Solve(1.0, rsoi1helio, rsoi2helio, dt3 - (dt1 + dt2) / _sourceToHelioScale.TimeScale, _direction2, rtol: 1e-12, h: V3.Cross(rsoi1helio.M, vsoi1helio.M));

            return (rsoi1, vsoi1, rsoi2, vsoi2, vi1 - v0Burn, vsoi1 - vf1, vsoi1helio - vi2, vf2 - vsoi2helio);
        }

        private (V3 rsoi1, V3 vsoi1, V3 rsoi2, V3 vsoi2, V3 dv1, V3 dv2, V3 dv3, V3 dv4) EvaluateTrajectory(double[] x)
        {
            for (int j = 0; j < x.Length; j++)
                _duals[j] = new Dual(x[j]);

            (DualV3 rsoi1, DualV3 vsoi1, DualV3 rsoi2, DualV3 vsoi2, DualV3 dv1, DualV3 dv2, DualV3 dv3, DualV3 dv4) = EvaluateTrajectory(_duals);

            return (rsoi1.M, vsoi1.M, rsoi2.M, vsoi2.M, dv1.M, dv2.M, dv3.M, dv4.M);
        }

        private const int NUM_EQUALITY_CONSTRAINTS = 11;
        private const int NUM_INEQUALITY_CONSTRAINTS = 2;
        private const int MAXITS = 5000;
        private const int NVARIABLES = 13;

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
            _captureBurn = captureBurn;

            // initialization

            double[] x0short = new double[NVARIABLES];
            double[] x0long = new double[NVARIABLES];

            double arrivalUTscaled = arrivalDT / _helioScale.TimeScale;

            // propagate target celestial to soi2 intercept time
            (V3 r2soi2, V3 v2soi2) = Shepperd.Solve(1.0, arrivalUTscaled, _r2, _v2);

            // solve the ZSOI heliocentric trajectory from source to target using both geometries
            (V3 vishortBootstrap, V3 _) = Izzo.Solve(1.0, _r1, r2soi2, arrivalUTscaled, TransferGeometry.Prograde, h: V3.Cross(_r1, _v1));
            (V3 vilongBootstrap, V3 _) = Izzo.Solve(1.0, _r1, r2soi2, arrivalUTscaled, TransferGeometry.Retrograde, h: V3.Cross(_r1, _v1));

            // estimate travel time to the SOI boundary and propagate the source celestial
            (V3 _, V3 vPosShortBootstrap, V3 rBurnShortBootstrap, double dt1ShortBootstrap) = Astro.SingleImpulseHyperbolicBurn(1.0, _r0, _v0, (vishortBootstrap - _v1) * _sourceToHelioScale.VelocityScale);
            double dt2ShortBootstrap = Astro.TimeToNextRadius(1.0, rBurnShortBootstrap, vPosShortBootstrap, _soi1);
            double dt1HelioShortBootstrap = (dt1ShortBootstrap + dt2ShortBootstrap) / _sourceToHelioScale.TimeScale;
            (V3 r1soi1Short, V3 v1soi1Short) = Shepperd.Solve(1.0, dt1HelioShortBootstrap, _r1, _v1);

            (V3 _, V3 vPosLongBootstrap, V3 rBurnLongBootstrap, double dt1LongBootstrap) = Astro.SingleImpulseHyperbolicBurn(1.0, _r0, _v0, (vilongBootstrap - _v1) * _sourceToHelioScale.VelocityScale);
            double dt2LongBootstrap = Astro.TimeToNextRadius(1.0, rBurnLongBootstrap, vPosLongBootstrap, _soi1);
            double dt1HelioLongBootstrap = (dt1LongBootstrap + dt2LongBootstrap) / _sourceToHelioScale.TimeScale;
            (V3 r1soi1Long, V3 v1soi1Long) = Shepperd.Solve(1.0, dt1HelioLongBootstrap, _r1, _v1);

            // re-solve the ZSOI helicentric trajectory with estimated travel time to the first SOI boundary
            (V3 vishort, V3 vfshort) = Izzo.Solve(1.0, r1soi1Short, r2soi2, arrivalUTscaled - dt1HelioShortBootstrap, TransferGeometry.Prograde, h: V3.Cross(r1soi1Short, v1soi1Short));
            (V3 vilong, V3 vflong) = Izzo.Solve(1.0, r1soi1Long, r2soi2, arrivalUTscaled - dt1HelioLongBootstrap, TransferGeometry.Retrograde, h: V3.Cross(r1soi1Long, v1soi1Long));

            // refine the ZSOI solution into finite SOI
            (V3 _, V3 vsoi1short) = Astro.StateVectorsAtDistance(1.0, r1soi1Short, vishort, soi1 / _helioScale.LengthScale);
            //(V3 rsoi2short, V3 vsoi2short) = Astro.StateVectorsAtDistance(1.0, r2soi2, -vfshort, soi2 / _helioScale.LengthScale);

            (V3 _, V3 vsoi1long) = Astro.StateVectorsAtDistance(1.0, r1soi1Long, vilong, soi1 / _helioScale.LengthScale);
            //(V3 rsoi2long, V3 vsoi2long) = Astro.StateVectorsAtDistance(1.0, r2soi2, -vflong, soi2 / _helioScale.LengthScale);

            vsoi1short = ((vsoi1short - v1soi1Short) * _sourceToHelioScale.VelocityScale).cart2sph;
            V3 vsoi2short = ((vfshort - v2soi2) * _targetToHelioScale.VelocityScale).cart2sph;

            vsoi1long = ((vsoi1long - v1soi1Long) * _sourceToHelioScale.VelocityScale).cart2sph;
            V3 vsoi2long = ((vflong - v2soi2) * _targetToHelioScale.VelocityScale).cart2sph;

            x0short[8] = 0;
            x0short[9] = 0;
            x0short[10] = vsoi2short.x;
            x0short[11] = vsoi2short.y;
            x0short[12] = vsoi2short.z;

            x0long[8] = 0;
            x0long[9] = 0;
            x0long[10] = vsoi2long.x;
            x0long[11] = vsoi2long.y;
            x0long[12] = vsoi2long.z;

            (V3 _, V3 vPosShort, V3 rBurnShort, double dt1Short) = Astro.SingleImpulseHyperbolicBurn(1.0, _r0, _v0, vsoi1short.sph2cart);
            double dt2Short = Astro.TimeToNextRadius(1.0, rBurnShort, vPosShort, _soi1);
            (V3 rsoi1short2, V3 vsoi1short2) = Shepperd.Solve(1.0, dt2Short, rBurnShort, vPosShort);

            rsoi1short2 = rsoi1short2.cart2sph;
            vsoi1short2 = vsoi1short2.cart2sph;

            x0short[0] = dt1Short;
            x0short[1] = dt2Short;
            x0short[2] = arrivalUTscaled;
            x0short[3] = rsoi1short2.y;
            x0short[4] = rsoi1short2.z;
            x0short[5] = vsoi1short2.x;
            x0short[6] = vsoi1short2.y;
            x0short[7] = vsoi1short2.z;

            (V3 _, V3 vPosLong, V3 rBurnLong, double dt1Long) = Astro.SingleImpulseHyperbolicBurn(1.0, _r0, _v0, vsoi1long.sph2cart);
            double dt2Long = Astro.TimeToNextRadius(1.0, rBurnLong, vPosLong, _soi1);
            (V3 rsoi1long2, V3 vsoi1long2) = Shepperd.Solve(1.0, dt2Short, rBurnLong, vPosLong);

            rsoi1long2 = rsoi1long2.cart2sph;
            vsoi1long2 = vsoi1long2.cart2sph;

            x0long[0] = dt1Long;
            x0long[1] = dt2Long;
            x0long[2] = arrivalUTscaled;
            x0long[3] = rsoi1long2.y;
            x0long[4] = rsoi1long2.z;
            x0long[5] = vsoi1long2.x;
            x0long[6] = vsoi1long2.y;
            x0long[7] = vsoi1long2.z;

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
            bndl[10] = Sqrt(EPS);

            // driving the optimizer with 4 different possible Lambert geometries

            var best = new Solution(V3.zero, 0.0, 0.0, 0.0, double.PositiveInfinity, double.PositiveInfinity);

            _direction1 = TransferGeometry.ShortWay;
            _direction2 = TransferGeometry.Prograde;
            Solution sol1 = RunOptimizer(x0short, bndl, bndu, optguard);
            best = UpdateBestSolution(sol1, best);

            _direction1 = TransferGeometry.LongWay;
            _direction2 = TransferGeometry.Prograde;
            Solution sol2 = RunOptimizer(x0short, bndl, bndu, optguard);
            best = UpdateBestSolution(sol2, best);

            _direction1 = TransferGeometry.ShortWay;
            _direction2 = TransferGeometry.Retrograde;
            Solution sol3 = RunOptimizer(x0long, bndl, bndu, optguard);
            best = UpdateBestSolution(sol3, best);

            _direction1 = TransferGeometry.LongWay;
            _direction2 = TransferGeometry.Retrograde;
            Solution sol4 = RunOptimizer(x0long, bndl, bndu, optguard);
            best = UpdateBestSolution(sol4, best);

            Print($"dv: {best.dv * _sourceScale.VelocityScale} dt1: {best.dt1 * _sourceScale.TimeScale} dt2: {best.dt2 * _sourceScale.TimeScale}, dt3: {best.dt3 * _helioScale.TimeScale}");
            return (best.dv * _sourceScale.VelocityScale, best.dt1 * _sourceScale.TimeScale, best.dt2 * _sourceScale.TimeScale, best.dt3 * _helioScale.TimeScale);
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

        // if all the solutions are worse thn 1e-4 error, return the solution that has the best error.
        // if any solutions are better than 1e-4 error, return the solution with the best cost out of those feasible solutions.
        private static Solution UpdateBestSolution(Solution sol, Solution best)
        {
            if (sol.err < 1e-4)
            {
                if (sol.cost < best.cost && sol.err < 1e-4)
                    return sol;
            }
            else
            {
                if (sol.err < best.err)
                    return sol;
            }

            return best;
        }

        private Solution RunOptimizer(double[] x0, double[] bndl, double[] bndu, bool optguard)
        {
            _initialFeasibility = true;
            double savedsoi2 = _soi2;
            _soi2 = 0;

            DebugPrint("initial constraint violation:");
            GetCost(x0);

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
            alglib.minnlcresults(state, out double[] x1, out alglib.minnlcreport rep1);

#if DEBUG
            bool[] boxConstrained = new bool[NVARIABLES];

            if (optguard)
            {
                alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep);


                if (ogrep.badgradsuspected)
                    if (!DoubleMatrixSparsityValidation(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2))
                        throw new Exception(
                            $"badgradsuspected: constraint: {ogrep.badgradfidx} variable: {ogrep.badgradvidx} user: {ogrep.badgraduser[ogrep.badgradfidx, ogrep.badgradvidx]:e} != numerical: {ogrep.badgradnum[ogrep.badgradfidx, ogrep.badgradvidx]:e}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2)}");

                if (ogrep.nonc0suspected)
                    throw new Exception("nonc0suspected");

                if (ogrep.nonc1suspected)
                    throw new Exception("nonc1suspected");
            }
#endif

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
            var vsoiSph2 = new V3(x1[10], x1[11], x1[12]);
            V3 vsoi2 = vsoiSph2.sph2cart;

            double vinf = vsoi2.magnitude;
            V3 vhat = vsoi2 / vinf;
            double b = _peR > 0 ? Min(_peR * Sqrt(1 + 2 / (_peR * vinf * vinf)), 0.99 * _soi2) : 0;
            V3 reference = Abs(vhat.z) < 0.9 ? new V3(0, 0, 1) : new V3(1, 0, 0);
            V3 nhat = V3.Cross(vhat, reference).normalized;
            V3 rsoi2 = b * nhat - Sqrt(_soi2 * _soi2 - b * b) * vhat;

            V3 rsoiSph2 = rsoi2.cart2sph;

            x1[8] = rsoiSph2.y;
            x1[9] = rsoiSph2.z;
            x1[10] = vsoiSph2.x;
            x1[11] = vsoiSph2.y;
            x1[12] = vsoiSph2.z;

            for (int i = 0; i < x1.Length; i++)
                DebugPrint($"x1[{i}] = {x1[i]}");

            DebugPrint("second-pass initial constraint violation:");
            GetCost(x1);

            alglib.minnlccreate(x1, out alglib.minnlcstate state2);
            alglib.minnlcsetbc(state2, bndl, bndu);
            alglib.minnlcsetalgosqp(state2);
            alglib.minnlcsetcond(state2, 0, MAXITS);
            alglib.minnlcsetnlc(state2, NUM_EQUALITY_CONSTRAINTS, NUM_INEQUALITY_CONSTRAINTS);
#if DEBUG
            if (optguard)
                alglib.minnlcoptguardgradient(state2, 1e-8);
#endif
            alglib.minnlcoptimize(state2, NLPFunction, null, null);
            alglib.minnlcresults(state2, out double[] x2, out alglib.minnlcreport rep2);

#if DEBUG
            if (optguard)
            {
                alglib.minnlcoptguardresults(state, out alglib.optguardreport ogrep2);

                if (ogrep2.badgradsuspected)
                    if (!DoubleMatrixSparsityValidation(ogrep2.badgraduser, ogrep2.badgradnum, boxConstrained, 1e-2))
                        throw new Exception(
                            $"badgradsuspected: constraint: {ogrep2.badgradfidx} variable: {ogrep2.badgradvidx} user: {ogrep2.badgraduser[ogrep2.badgradfidx, ogrep2.badgradvidx]:e} != numerical: {ogrep2.badgradnum[ogrep2.badgradfidx, ogrep2.badgradvidx]:e}\nuser:\n{DoubleMatrixString(ogrep2.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep2.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep2.badgraduser, ogrep2.badgradnum, boxConstrained, 1e-2)}");

                if (ogrep2.nonc0suspected)
                    throw new Exception("nonc0suspected");

                if (ogrep2.nonc1suspected)
                    throw new Exception("nonc1suspected");
            }
#endif

            for (int i = 0; i < x2.Length; i++)
                DebugPrint($"x2[{i}] = {x2[i]}");

            (double cost2, double err2) = AnalyzeSolution(x2, rep2);

            V3 dv2 = GetManeuverDeltaV(x2, ref cost2, ref err2);

            return new Solution(dv2, x2[0], x2[1], x2[2], cost2, err2);
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

            foreach (int i in new[]{8, 9, 10, 11, 12})
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
                (_, _, _, _, dv, _, _, _) = EvaluateTrajectory(x);
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
