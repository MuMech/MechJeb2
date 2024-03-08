using System;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public class Segment : IDisposable
    {
        public int Offset = 0; // FIXME: two different offsets with the same name now

        private readonly Problem          _problem;
        private readonly Phase            _phase;
        private          IntegratorRecord _x0;
        private          IntegratorRecord _xf;
        private          double           _ti;
        private          double           _dt;
        private          double           _tf => _ti + _dt;
        private          Hn?              _interpolant;

        public Segment(Problem problem, Phase phase)
        {
            _problem = problem;
            _phase   = phase;
        }

        public void UnpackBurntimesFromPhases() => _dt = _phase.bt;

        public int UnpackVariables(double[] yin, int offset, bool first)
        {
            var sr = SegmentRecord.CreateFrom(yin, offset);

            _x0.CopyFrom(sr, 0);

            if (first)
            {
                _x0.R = _problem.R0;
                _x0.V = _problem.V0;
            }

            _x0.M = _phase.m0;

            _dt = _phase.OptimizeTime ? sr.Dt : _phase.bt;

            return offset + SegmentRecord.SEGMENT_REC_LEN;
        }

        public int PackVariables(double[] yin, int offset)
        {
            var sr = SegmentRecord.CreateFrom(_x0, _dt);

            sr.CopyTo(yin, offset);

            return offset + SegmentRecord.SEGMENT_REC_LEN;
        }

        public int BoundaryConditions(double[] bndl, double[] bndu, int offset)
        {
            for (int i = 0; i < SegmentRecord.SEGMENT_REC_LEN; i++)
            {
                bndl[offset + i] = double.NegativeInfinity;
                bndu[offset + i] = double.PositiveInfinity;
            }

            if (_phase is { OptimizeTime: true, Coast: false, Infinite: false })
                bndu[offset + SegmentRecord.DT_INDEX] = _phase.tau * (1.0 - 1e-5);

            if (_phase is { OptimizeTime: true, Coast: true })
            {
                bndl[offset + SegmentRecord.DT_INDEX] = _phase.mint;
                bndu[offset + SegmentRecord.DT_INDEX] = _phase.maxt;
            }

            return offset + IntegratorRecord.INTEGRATOR_REC_LEN;
        }

        public (double tf, double dvf, V3 u0) MultipleShooting(double ti, double dv, V3 u0)
        {
            using var yin = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);
            using var yout = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);

            _ti    = ti;

            _x0.DV = dv;
            _x0.CopyTo(yin);

            _phase.u0 = u0;

            if (_phase.Unguided && u0 == V3.zero)
                _phase.u0 = _x0.Pv.normalized;

            _interpolant?.Dispose();
            _interpolant = _phase.Integrate(yin, yout, 0, _dt);

            _xf.CopyFrom(yout);

            return (_tf, _xf.DV, u0);
        }

        public (double tf, IntegratorRecord xf, V3 u0) SingleShooting(double ti, IntegratorRecord x0, V3 u0)
        {
            using var yin = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);
            using var yout = Vn.Rent(IntegratorRecord.INTEGRATOR_REC_LEN);

            _ti   = ti;
            _x0   = x0;
            _x0.M = _phase.m0; // forced and tightly coupled to single shooting
            _x0.CopyTo(yin);

            _phase.u0 = u0;

            if (_phase.Unguided && u0 == V3.zero)
                _phase.u0 = _x0.Pv.normalized;

            _interpolant?.Dispose();
            _interpolant = _phase.Integrate(yin, yout, 0, _dt);

            _xf.CopyFrom(yout);

            return (_tf, _xf, u0);
        }

        public void Dispose() => _interpolant?.Dispose();

        public int CalculateInitialConstraints(double[] zout, int offset)
        {
            (_problem.R0 - _x0.R).CopyTo(zout, offset);
            (_problem.V0 - _x0.V).CopyTo(zout, offset + 3);
            zout[offset + 6] = _problem.M0 - _x0.M;
            if (_phase is { OptimizeTime: true, Coast: false, Infinite: true })
                zout[offset + 7] = 0;
            else
                zout[offset + 7] = 0;
            return offset + 8;
        }

        public int CalculateContinuityConstraints(double[] zout, int offset, Segment prev)
        {
            (prev._xf.R - _x0.R).CopyTo(zout, offset);
            (prev._xf.V - _x0.V).CopyTo(zout, offset + 3);
            (prev._xf.Pv - _x0.Pv).CopyTo(zout, offset + 6);
            (prev._xf.Pr - _x0.Pr).CopyTo(zout, offset + 9);
            zout[offset + 12] = _x0.M - _phase.m0;

            if (_phase is { OptimizeTime: true, Coast: false, Infinite: true })
                zout[offset + 13] = 0;
            else
                zout[offset + 13] = 0;

            return offset + 14;
        }

        public int CalculateContinuityConstraints(double[] zout, double[,] jac, int offset, Segment prev)
        {
            int newOffset = CalculateContinuityConstraints(zout, offset, prev);

            if (_phase.OptimizeTime)
            {
                // partial of constraints with respect to burntime
                jac[offset, Offset - 1]     = prev._xf.V.x;
                jac[offset + 1, Offset - 1] = prev._xf.V.y;
                jac[offset + 2, Offset - 1] = prev._xf.V.z;

                jac[offset + 6, Offset - 1] = prev._xf.Pr.x;
                jac[offset + 7, Offset - 1] = prev._xf.Pr.y;
                jac[offset + 8, Offset - 1] = prev._xf.Pr.z;
            }

            // partial of constraints with respect to initial state
            for(int i = 0; i < 12; i++)
                jac[offset + i, Offset + i] = -1;

            // FIXME: Implement Jacobian calculation

            return newOffset;
        }

        public int CalcluateTerminalConstraints(double[] zout, int offset)
        {
            _problem.Terminal.TerminalConstraints(_xf, zout, offset);

            return offset + 6;
        }

        public int CalcluateTerminalConstraints(double[] zout, double[,] jac, int offset)
        {
            _problem.Terminal.TerminalConstraints(_xf, zout, offset);

            return offset + 6;
        }

        public void GetSolution(Solution solution)
        {
            if (_interpolant == null)
                throw new InvalidOperationException("Segment has not been integrated");

            solution.AddSegment(_ti, _tf, _interpolant, _phase);
        }

        public double MassObjective() => _phase is { OptimizeTime: true, Coast: false } ? -_xf.M : 0;

        public double TimeObjective() => _phase is { OptimizeTime: true, Coast: false } ? _dt : 0;

        public bool NeedsObjectiveFunction() => _phase is { Coast: true, OptimizeTime: true };
    }
}
