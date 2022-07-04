#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Maths;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public class Solution : IDisposable
    {
        public           double       T0;
        public           double       Tf => T0 + tmax * _timeScale;
        private readonly Scale        _scale;
        private readonly List<double> _tmin         = new List<double>();
        private readonly List<double> _tmax         = new List<double>();
        private readonly List<Hn>     _interpolants = new List<Hn>();
        public           int          CoastingKSPStage;

        public int    Segments      => _interpolants.Count;
        private double _timeScale     => _scale.timeScale;
        private double _lengthScale   => _scale.lengthScale;
        private double _velocityScale => _scale.velocityScale;
        private double _massScale     => _scale.massScale;

        public  double tmax => _tmax[Segments - 1];
        private double tmin => _tmin[0];

        public Solution(Problem problem)
        {
            _scale = problem.Scale;
            // t0 is a public API that can be updated while we're landed waiting for takeoff.
            T0 = problem.t0;
        }

        public void AddSegment(double t0, double tf, Hn interpolant)
        {
            _tmin.Add(t0);
            _tmax.Add(tf);
            _interpolants.Add(interpolant);
        }

        // convert kerbal time to normalized time
        public double Tbar(double t)
        {
            double tbar = (t - T0) / _timeScale;
            if (tbar < tmin)
                return tmin;

            if (tbar > tmax)
                return tmax;

            return tbar;
        }

        public V3 R(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return x.R * _lengthScale;
        }
        
        // constant integral of the motion, zero for no yaw steering.
        public V3 Constant(double tbar)
        {
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return V3.Cross(x.PR, x.R) + V3.Cross(x.PV, x.V);
        }

        public V3 V(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return x.V * _velocityScale;
        }

        public V3 Pv(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return PvBar(tbar);
        }

        public V3 PvBar(double tbar)
        {
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return x.PV;
        }

        public V3 Pr(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return PrBar(tbar);
        }

        public V3 PrBar(double tbar)
        {
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return x.PR;
        }

        public double M(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return x.M * _massScale;
        }
        
        public double Bt(int segment, double t)
        {
            double tbar = (t - T0) / _timeScale;
            
            return BtBar(segment, tbar) * _timeScale;
        }
        
        // burntime of the segment at the given normalized time, does not go below zero
        public double BtBar(int segment, double tbar)
        {
            double hi = _tmax[segment];
            double lo = Math.Min(Math.Max(_tmin[segment], tbar), hi);

            return hi - lo;
        }

        public double DV(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            return x.DV * _velocityScale;
        }

        public double Tgo(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return (tmax - tbar) * _timeScale;
        }

        public double Vgo(double t)
        {
            return DV(Tf) - DV(t);
        }
        
        public double Thrust(double t)
        {
            return 1.0;
        }

        public (double pitch, double heading) PitchAndHeading(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);
            (double pitch, double heading) = Functions.ECIToPitchHeading(x.R, x.PV);
            return (pitch, heading);
        }

        public (V3 r, V3 v) TerminalStateVectors()
        {
            using DD xraw = Interpolate(tmax);
            using var x = ArrayWrapper.Rent(xraw);
            return (x.R * _lengthScale, x.V * _velocityScale);
        }

        private DD Interpolate(double tbar)
        {
            for (int i = 0; i < _tmax.Count; i++)
                if (tbar < _tmax[i])
                    return _interpolants[i].Evaluate(tbar);

            return _interpolants[_tmax.Count - 1].Evaluate(tbar);
        }

        // this is for terminal guidance.
        public double TerminalGuidanceMetric(V3 pos, V3 vel)
        {
            var h = V3.Cross(pos, vel);
            var hf = V3.Cross(R(Tf), V(Tf));

            return (hf - h).magnitude;
        }

        public void Dispose()
        {
            // FIXME: need to dispose properly
        }
    }
}
