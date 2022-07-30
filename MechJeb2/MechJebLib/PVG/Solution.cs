/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

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
        private readonly List<Phase>  _phases       = new List<Phase>();
        private readonly double       _mu;
        private          double       _rbody;

        public  int    Segments       => _interpolants.Count;
        private double _timeScale     => _scale.timeScale;
        private double _lengthScale   => _scale.lengthScale;
        private double _velocityScale => _scale.velocityScale;
        private double _massScale     => _scale.massScale;

        public  double tmax => _tmax[Segments - 1];
        private double tmin => _tmin[0];

        public Solution(Problem problem)
        {
            _scale = problem.Scale;
            _mu    = problem.Mu;
            _rbody = problem.Rbody;
            // t0 is a public API that can be updated while we're landed waiting for takeoff.
            T0 = problem.T0;
        }

        public void AddSegment(double t0, double tf, Hn interpolant, Phase phase)
        {
            _tmin.Add(t0);
            _tmax.Add(tf);
            _phases.Add(phase); // FIXME: probably need to dup() the phase, and will need to return the phase after pooling is implemented
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
        
        public double DV(double t, int n)
        {
            double tbar = (t - T0) / _timeScale;
            double min = tbar > _tmin[n] ? tbar : _tmin[n];
            double max = _tmax[n];
            using DD ddmin = Interpolate(min);
            using var xmin = ArrayWrapper.Rent(ddmin);
            using DD ddmax = Interpolate(max);
            using var xmax = ArrayWrapper.Rent(ddmax);
            return Math.Max(xmax.DV - xmin.DV, 0) * _velocityScale;
        }

        public double Tgo(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return (tmax - tbar) * _timeScale;
        }
        
        public double Tgo(double t, int n)
        {
            double tbar = (t - T0) / _timeScale;
            return TgoBar(tbar, n) * _timeScale;
        }

        public double TgoBar(double tbar, int n)
        {
            if (tbar > _tmin[n])
                return Math.Max(_tmax[n] - tbar, 0);
            return _tmax[n] - _tmin[n];
        }

        public double Vgo(double t)
        {
            return DV(Tf) - DV(t);
        }

        public bool Coast(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return _phases[PhaseForTbar(tbar)].Coast;
        }

        public bool Unguided(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return _phases[PhaseForTbar(tbar)].Unguided;
        }

        public V3 U0(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return _phases[PhaseForTbar(tbar)].u0;
        }

        public int Stage(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return _phases[PhaseForTbar(tbar)].KSPStage;
        }

        public double StageTimeLeft(double t)
        {
            double tbar = (t - T0) / _timeScale;
            int phase = PhaseForTbar(tbar);
            return (_tmax[phase] - tbar) * _timeScale;
        }

        public (double pitch, double heading) PitchAndHeading(double t)
        {
            double tbar = (t - T0) / _timeScale;
            V3 u;

            using DD xraw = Interpolate(tbar);
            using var x = ArrayWrapper.Rent(xraw);

            int phase = PhaseForTbar(tbar);

            // FIXME: this logic should be pushed upwards into the guidance controller
            if (_phases[phase].Unguided)
            {
                u = _phases[phase].u0;
            }
            else if (_phases[phase].Coast && phase < Segments - 1)
            {
                u = _phases[phase + 1].u0;
            }
            else
            {
                u = x.PV.normalized;
            }

            (double pitch, double heading) = Functions.ECIToPitchHeading(x.R, u);
            return (pitch, heading);
        }

        public (V3 r, V3 v) TerminalStateVectors()
        {
            using DD xraw = Interpolate(tmax);
            using var x = ArrayWrapper.Rent(xraw);
            return (x.R * _lengthScale, x.V * _velocityScale);
        }

        public string PhaseString(double t)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _phases.Count; i++)
            {
                sb.AppendLine(PhaseString(t, i));
            }
            return sb.ToString();
        }

        // FIXME: this ia also probably specific display logic
        public string PhaseString(double t, int n)
        {
            Phase phase = _phases[n];

            if (phase.Coast)
                return $"coast: {Tgo(t, n):F1}s";

            return $"burn: {phase.KSPStage} {Tgo(t, n):F1}s {DV(t, n):F1}m/s ()";
        }

        // FIXME: this is really specific display logic
        public string TerminalString()
        {
            (V3 rf, V3 vf) = TerminalStateVectors();

            (double sma, double ecc, double inc, double lan, double argp, double tanom) = Functions.KeplerianFromStateVectors(_mu,
                rf, vf);

            double peR = Functions.PeriapsisFromKeplerian(sma, ecc);
            double apR = Functions.ApoapsisFromKeplerian(sma, ecc);
            double peA = peR - _rbody;
            double apA = apR <= 0 ? apR : apR - _rbody;
            double fpa = Functions.FlightPathAngle(rf, vf);
            
            var sb = new StringBuilder();
            sb.AppendLine($"{peA.ToSI(0)}m x {apA.ToSI(0)}m FPA: {Rad2Deg(fpa):F1}");
            sb.AppendLine($"sma: {sma.ToSI(0)}m ecc: {ecc:F3}");
            sb.Append($"inc: {Rad2Deg(inc):F1} lan: {Rad2Deg(lan):F1} argp: {Rad2Deg(argp):F1} ta: {Rad2Deg(tanom):F1}");
            return sb.ToString();
        }

        private int PhaseForTbar(double tbar)
        {
            for (int i = 0; i < _tmax.Count; i++)
                if (tbar < _tmax[i])
                    return i;
            return _tmax.Count - 1;
        }

        private DD Interpolate(double tbar)
        {
            return _interpolants[PhaseForTbar(tbar)].Evaluate(tbar);
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
