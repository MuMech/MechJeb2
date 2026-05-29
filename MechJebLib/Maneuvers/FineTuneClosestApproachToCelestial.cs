using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Maneuvers
{
    public class FineTuneClosestApproachToCelestial
    {
        private V3 _r0;
        private V3 _v0;
        private V3 _r1;
        private V3 _v1;
        private double _soi;
        private double _peR;
        private double _mu1;
        private double _cosInc;
        private TransferGeometry _direction;

        private Scale _planetScale;

        private void NLPFunction(double[] x, double[] fi, object? obj)
        {
            (V3 rsoi, V3 vsoi, V3 dv1, V3 dv2) = DeriveValues(x);

            fi[0] = 0.5 * dv1.sqrMagnitude;
            double periapsis = Astro.PeriapsisFromStateVectors(_mu1, rsoi, vsoi);
            // when _peR is zero, drive the periapsis to zero directly rather than normalizing (which would be NaN/Inf)
            fi[1] = _peR > 0 ? periapsis / _peR - 1.0 : periapsis;
            fi[2] = IsFinite(_cosInc) && _peR > 0 ? V3.Cross(rsoi, vsoi).normalized.z - _cosInc : 0;
            fi[3] = dv2.x;
            fi[4] = dv2.y;
            fi[5] = dv2.z;
            fi[6] = V3.Dot(rsoi.normalized, vsoi.normalized);
        }

        private (V3 rsoi, V3 vsoi, V3 dv1, V3 dv2) DeriveValues(double[] x)
        {
            double dt1 = x[0]; // coast time on initial orbit to burn
            double dt2 = x[1]; // coast time after burn to soi interface

            var rsoiSph = new V3(_soi, x[2], x[3]); // spherical position at soi boundary
            var vsoiSph = new V3(x[4], x[5], x[6]); // spherical velocity at soi boundary

            V3 rsoi = rsoiSph.sph2cart;
            V3 vsoi = vsoiSph.sph2cart;

            // propagate initial orbit to burn
            (V3 r0Burn, V3 v0Burn) = Shepperd.Solve(1.0, dt1, _r0, _v0);
            // propagate celestial to soi intercept time
            (V3 rf1, V3 vf1) = Shepperd.Solve(1.0, dt1 + dt2, _r1, _v1);

            // convert soi intercept to central-body coordinates
            V3 rsoi2 = rf1 + rsoi;
            V3 vsoi2 = vf1 + vsoi;

            (V3 vi, V3 vf) = Gooding.Solve(1.0, r0Burn, v0Burn, rsoi2, dt2, 0, _direction);
            return (rsoi, vsoi, vi - v0Burn, vsoi2 - vf);
        }

        public (V3 dv, double dt1, double dt2) Maneuver(double mu0, V3 r0, V3 v0, double mu1, V3 r1, V3 v1, double soi, double tsoi, double peR, double dt = double.NaN, double inc = double.NaN)
        {
            Print($"FineTuneClosestApproachToCelestial.Maneuver({mu0:G17}, {r0}, {v0}, {mu1:G17}, {r1}, {v1}, {soi:G17}, {tsoi:G17}, {peR:G17}, {dt:G17}, {inc:G17})");
            Check.PositiveFinite(mu0);
            Check.Finite(r0);
            Check.Finite(v0);
            Check.PositiveFinite(mu1);
            Check.Finite(r1);
            Check.Finite(v1);
            Check.PositiveFinite(soi);
            Check.NonNegativeFinite(peR);

            _planetScale = Scale.Create(mu0, Sqrt(r0.magnitude * r1.magnitude));

            _soi = soi / _planetScale.LengthScale;
            _peR = peR / _planetScale.LengthScale;
            _mu1 = mu1 / mu0; // moon's GM in planet-normalized units (mu0 -> 1)
            _r0 = r0 / _planetScale.LengthScale;
            _v0 = v0 / _planetScale.VelocityScale;
            _r1 = r1 / _planetScale.LengthScale;
            _v1 = v1 / _planetScale.VelocityScale;
            dt = dt / _planetScale.TimeScale;
            _cosInc = Cos(inc);

            const int    NUM_EQUALITY_CONSTRAINTS   = 5;
            const int    NUM_INEQUALITY_CONSTRAINTS = 1;
            const double DIFFSTEP                   = 1e-6;
            const int    MAXITS                     = 500;

            const int NVARIABLES = 7;

            double[] x0 = new double[NVARIABLES];

            x0[0] = 0;
            x0[1] = tsoi / _planetScale.TimeScale;

            (V3 rsoi2, V3 vsoi2) = Shepperd.Solve(1.0, x0[1], _r0, _v0);
            (V3 rf1, V3 vf1) = Shepperd.Solve(1.0, x0[1], _r1, _v1);

            V3 rsoi = rsoi2 - rf1;
            V3 vsoi = vsoi2 - vf1;

            V3 rsoiSph = rsoi.cart2sph;
            V3 vsoiSph = vsoi.cart2sph;

            x0[2] = rsoiSph.y;
            x0[3] = rsoiSph.z;
            x0[4] = vsoiSph.x;
            x0[5] = vsoiSph.y;
            x0[6] = vsoiSph.z;

            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndu[i] = double.PositiveInfinity;
                bndl[i] = double.NegativeInfinity;
            }

            bndl[0] = 0;
            bndu[0] = IsFinite(dt) ? dt : tsoi / _planetScale.TimeScale;
            bndl[1] = EPS;
            bndu[1] = tsoi / _planetScale.TimeScale;

            _direction = TransferGeometry.ShortWay;
            alglib.minnlccreatef(x0, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, 0, MAXITS);
            alglib.minnlcsetnlc(state, NUM_EQUALITY_CONSTRAINTS, NUM_INEQUALITY_CONSTRAINTS);
            alglib.minnlcoptimize(state, NLPFunction, null, null);
            alglib.minnlcresults(state, out double[] x1, out alglib.minnlcreport rep1);

            double[] fi = new double[NUM_EQUALITY_CONSTRAINTS + NUM_INEQUALITY_CONSTRAINTS + 1];

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
            alglib.minnlccreatef(x0, DIFFSTEP, out alglib.minnlcstate state2);
            alglib.minnlcsetbc(state2, bndl, bndu);
            alglib.minnlcsetalgosqp(state2);
            alglib.minnlcsetcond(state2, 0, MAXITS);
            alglib.minnlcsetnlc(state2, NUM_EQUALITY_CONSTRAINTS, NUM_INEQUALITY_CONSTRAINTS);
            alglib.minnlcoptimize(state2, NLPFunction, null, null);
            alglib.minnlcresults(state2, out double[] x2, out alglib.minnlcreport rep2);

            NLPFunction(x2, fi, null);
            double longCost  = fi[0];
            double longError = Max(Max(rep2.bcerr, rep2.lcerr), rep2.nlcerr);

            Print($"termination type: {rep2.terminationtype}");
            Print($"iterations count: {rep2.iterationscount}");
            Print($"num function evals: {rep2.nfev}");
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
                (V3 _, V3 _, V3 dv, V3 _) = DeriveValues(x1);
                return (dv * _planetScale.VelocityScale, x1[0] * _planetScale.TimeScale, x1[1] * _planetScale.TimeScale);
            }
            else
            {
                _direction = TransferGeometry.LongWay;
                (V3 _, V3 _, V3 dv, V3 _) = DeriveValues(x2);
                return (dv * _planetScale.VelocityScale, x2[0] * _planetScale.TimeScale, x2[1] * _planetScale.TimeScale);
            }
        }
    }
}
