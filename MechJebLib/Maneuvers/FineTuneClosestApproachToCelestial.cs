using MechJebLib.Functions;
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
        private double _cosInc;
        private bool _onlyFeasible;

        private Scale _planetToMoonScale;
        private Scale _moonScale;
        private Scale _planetScale;

        private void NLPFunction(double[] x, double[] fi, object? obj)
        {
            var    dv  = new V3(x[0], x[1], x[2]);
            double dt1 = x[3];
            double dt2 = x[4];

            (V3 r0Burn, V3 v0Burn) = Shepperd.Solve(1.0, dt1, _r0, _v0);
            (V3 rf0, V3 vf0) = Shepperd.Solve(1.0, dt2, r0Burn, v0Burn + dv);
            (V3 rf1, V3 vf1) = Shepperd.Solve(1.0, dt1 + dt2, _r1, _v1);

            V3 rsoi = rf0 - rf1;
            V3 vsoi = vf0 - vf1;

            double per;
            if (rsoi.magnitude <= 1.1 * _soi)
                per = Astro.PeriapsisFromStateVectors(1.0, rsoi / _planetToMoonScale.LengthScale, vsoi / _planetToMoonScale.VelocityScale);
            else
                per = rsoi.magnitude / _planetToMoonScale.LengthScale;

            fi[0] = _onlyFeasible ? 0 : 0.025 * dv.sqrMagnitude;
            fi[1] = per - _peR;
            fi[2] = IsFinite(_cosInc) ? V3.Cross(rsoi, vsoi).normalized.z - _cosInc : 0;
            fi[3] = rsoi.magnitude - _soi;
            fi[4] = V3.Dot(rsoi.normalized, vsoi.normalized);
        }

        public (V3 dv, double dt1, double dt2) Maneuver(double mu0, V3 r0, V3 v0, double mu1, V3 r1, V3 v1, double soi, double tsoi, double peR, double inc=double.NaN)
        {
            Print($"FineTuneClosestApproachToCelestial.Maneuver({mu0:G17}, {r0}, {v0}, {mu1:G17}, {r1}, {v1}, {soi:G17}, {tsoi:G17}, {peR:G17}, {inc:G17})");
            Check.PositiveFinite(mu0);
            Check.Finite(r0);
            Check.Finite(v0);
            Check.PositiveFinite(mu1);
            Check.Finite(r1);
            Check.Finite(v1);
            Check.PositiveFinite(soi);
            Check.PositiveFinite(peR);

            _planetScale = Scale.Create(mu0, Sqrt(r0.magnitude * r1.magnitude));
            _moonScale = Scale.Create(mu1, peR);
            _planetToMoonScale = _planetScale.ConvertTo(_moonScale);

            _soi = soi / _planetScale.LengthScale;
            _peR = peR / _moonScale.LengthScale;
            _r0 = r0 / _planetScale.LengthScale;
            _v0 = v0 / _planetScale.VelocityScale;
            _r1 = r1 / _planetScale.LengthScale;
            _v1 = v1 / _planetScale.VelocityScale;
            _cosInc = Cos(inc);

            const int    NUM_EQUALITY_CONSTRAINTS   = 3;
            const int    NUM_INEQUALITY_CONSTRAINTS = 2;
            const double DIFFSTEP                   = 1e-8;
            const int    MAXITS                     = 500;

            const int NVARIABLES = 5;

            double[] x = new double[NVARIABLES];

            x[4] = tsoi / _planetScale.TimeScale;

            double[] bndl = new double[NVARIABLES];
            double[] bndu = new double[NVARIABLES];

            for (int i = 0; i < NVARIABLES; i++)
            {
                bndu[i] = double.PositiveInfinity;
                bndl[i] = double.NegativeInfinity;
            }

            bndl[3] = 0;
            bndu[3] = tsoi /  _planetScale.TimeScale;
            bndl[4] = 0;
            bndl[4] = tsoi / _planetScale.TimeScale;

            // converge once without cost metric to find feasible solution
            _onlyFeasible = true;
            alglib.minnlccreatef(x, DIFFSTEP, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, bndl, bndu);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond(state, 0, MAXITS);
            alglib.minnlcsetnlc(state, NUM_EQUALITY_CONSTRAINTS, NUM_INEQUALITY_CONSTRAINTS);
            alglib.minnlcoptimize(state, NLPFunction, null, null);
            alglib.minnlcresults(state, out double[] x2, out alglib.minnlcreport rep);

            double[] fi = new double[NUM_EQUALITY_CONSTRAINTS + NUM_INEQUALITY_CONSTRAINTS + 1];

            Print($"termination type: {rep.terminationtype}");
            Print($"iterations count: {rep.iterationscount}");
            Print($"num function evals: {rep.nfev}");
            NLPFunction(x2, fi, null);
            Print($"fi[1] = {fi[1]}");
            Print($"fi[2] = {fi[2]}");
            Print($"fi[3] = {fi[3]}");

            // converge again to move towards optimal solution
            _onlyFeasible = false;
            alglib.minnlccreatef(x2, DIFFSTEP, out alglib.minnlcstate state2);
            alglib.minnlcsetalgosqp(state2);
            alglib.minnlcsetcond(state2, 0, MAXITS);
            alglib.minnlcsetnlc(state2, NUM_EQUALITY_CONSTRAINTS, NUM_INEQUALITY_CONSTRAINTS);
            alglib.minnlcoptimize(state2, NLPFunction, null, null);
            alglib.minnlcresults(state2, out double[] x3, out alglib.minnlcreport rep2);

            Print($"termination type: {rep2.terminationtype}");
            Print($"iterations count: {rep2.iterationscount}");
            Print($"num function evals: {rep2.nfev}");
            NLPFunction(x3, fi, null);
            Print($"fi[1] = {fi[1]}");
            Print($"fi[2] = {fi[2]}");
            Print($"fi[3] = {fi[3]}");

            var dv = new V3(x3[0], x3[1], x3[2]);

            return (dv * _planetScale.VelocityScale, x3[3] * _planetScale.TimeScale, x3[4] * _planetScale.TimeScale);
        }
    }
}
