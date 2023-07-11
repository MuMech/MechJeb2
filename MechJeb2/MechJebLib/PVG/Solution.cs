/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.Core;
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
        private readonly List<Phase>  Phases        = new List<Phase>();
        private readonly double       _mu;
        private readonly double       _rbody;

        public  int    Segments       => Phases.Count;
        private double _timeScale     => _scale.TimeScale;
        private double _lengthScale   => _scale.LengthScale;
        private double _velocityScale => _scale.VelocityScale;
        private double _massScale     => _scale.MassScale;

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
            Phases.Add(phase); // FIXME: probably need to dup() the phase, and will need to return the phase after pooling is implemented
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
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return x.R * _lengthScale;
        }

        // constant integral of the motion, zero for no yaw steering.
        public V3 Constant(double tbar)
        {
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return V3.Cross(x.PR, x.R) + V3.Cross(x.PV, x.V);
        }

        public V3 V(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return x.V * _velocityScale;
        }

        public V3 Pv(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return PvBar(tbar);
        }

        public V3 PvBar(double tbar)
        {
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return x.PV;
        }

        public V3 Pr(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return PrBar(tbar);
        }

        public V3 PrBar(double tbar)
        {
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return x.PR;
        }

        public double M(double t)
        {
            double tbar = (t - T0) / _timeScale;
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
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
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return x.DV * _velocityScale;
        }

        public double DV(double t, int n)
        {
            double tbar = (t - T0) / _timeScale;
            double min = tbar > _tmin[n] ? tbar : _tmin[n];
            double max = _tmax[n];
            using Vn ddmin = Interpolate(n, min);
            var xmin = OutputLayout.CreateFrom(ddmin);
            using Vn ddmax = Interpolate(n, max);
            var xmax = OutputLayout.CreateFrom(ddmax);
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
            return Phases[IndexForTbar(tbar)].Coast;
        }

        // Specialized API to determine if we still have the coast in our future or not
        public bool WillCoast(double t)
        {
            double tbar = (t - T0) / _timeScale;

            for (int i = IndexForTbar(tbar); i < Phases.Count; i++)
            {
                if (Phases[i].Coast) return true;
            }

            return false;
        }

        public int CoastStage()
        {
            for (int i = 0; i < Phases.Count; i++)
            {
                if (Phases[i].Coast) return Phases[i].KSPStage;
            }

            return -1;
        }

        public int TerminalStage()
        {
            return Phases[Phases.Count - 1].KSPStage;
        }

        public bool Unguided(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return Phases[IndexForTbar(tbar)].Unguided;
        }

        public bool CoastPhase(int phase)
        {
            return Phases[phase].Coast;
        }

        public bool OptimizeTime(int phase)
        {
            return Phases[phase].OptimizeTime;
        }

        public V3 U0(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return Phases[IndexForTbar(tbar)].u0;
        }

        public int KSPStage(int phase)
        {
            return Phases[phase].KSPStage;
        }

        public int MJPhase(int phase)
        {
            return Phases[phase].MJPhase;
        }

        public double StageTimeLeft(double t)
        {
            double tbar = (t - T0) / _timeScale;
            int phase = IndexForTbar(tbar);
            return (_tmax[phase] - tbar) * _timeScale;
        }

        public (double pitch, double heading) PitchAndHeading(double t)
        {
            double tbar = (t - T0) / _timeScale;
            V3 u;

            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);

            int phase = IndexForTbar(tbar);

            // FIXME: this logic should be pushed upwards into the guidance controller
            if (Phases[phase].Unguided)
            {
                u = Phases[phase].u0;
            }
            else if (Phases[phase].Coast && phase < Segments - 1)
            {
                u = Phases[phase + 1].u0;
            }
            else
            {
                u = x.PV.normalized;
            }

            (double pitch, double heading) = Maths.ECIToPitchHeading(x.R, u);
            return (pitch, heading);
        }

        public (V3 r, V3 v) TerminalStateVectors()
        {
            return StateVectors(tmax);
        }

        public (V3 r, V3 v) StateVectors(double tbar)
        {
            using Vn xraw = Interpolate(tbar);
            var x = OutputLayout.CreateFrom(xraw);
            return (x.R * _lengthScale, x.V * _velocityScale);
        }

        // FIXME: this is really specific display logic
        public string TerminalString()
        {
            (V3 rf, V3 vf) = TerminalStateVectors();

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) = Maths.KeplerianFromStateVectors(_mu,
                rf, vf);

            double peR = Maths.PeriapsisFromKeplerian(sma, ecc);
            double apR = Maths.ApoapsisFromKeplerian(sma, ecc);
            double peA = peR - _rbody;
            double apA = apR <= 0 ? apR : apR - _rbody;
            double fpa = Maths.FlightPathAngle(rf, vf);
            double rT = rf.magnitude - _rbody;
            double vT = vf.magnitude;

            var sb = new StringBuilder();
            sb.AppendLine($"Orbit: {peA.ToSI()}m x {apA.ToSI()}m");
            sb.AppendLine($"rT: {rT.ToSI()}m vT: {vT.ToSI()}m/s FPA: {Rad2Deg(fpa):F1}°");
            sb.AppendLine($"sma: {sma.ToSI()}m ecc: {ecc:F3} inc: {Rad2Deg(inc):F1}°");
            sb.Append($"lan: {Rad2Deg(lan):F1}° argp: {Rad2Deg(argp):F1}° ta: {Rad2Deg(tanom):F1}°");
            return sb.ToString();
        }

        private int IndexForTbar(double tbar)
        {
            for (int i = 0; i < _tmax.Count; i++)
                if (tbar < _tmax[i])
                    return i;
            return _tmax.Count - 1;
        }

        public int IndexForKSPStage(int kspStage, bool coasting)
        {
            for (int i = 0; i < Phases.Count; i++)
            {
                if (Phases[i].KSPStage != kspStage)
                    continue;

                if (Phases[i].Coast != coasting)
                    continue;

                return i;
            }

            return -1;
        }

        private Vn Interpolate(double tbar)
        {
            return _interpolants[IndexForTbar(tbar)].Evaluate(tbar);
        }

        private Vn Interpolate(int segment, double tbar)
        {
            return _interpolants[segment].Evaluate(tbar);
        }

        // this is for terminal guidance.
        public bool TerminalGuidanceSatisfied(V3 pos, V3 vel, int index)
        {
            var hf = V3.Cross(pos, vel);
            double end = _tmax[index];

            (V3 rT, V3 vT) = StateVectors(end);

            var hT = V3.Cross(rT, vT);

            Log($"hf: {hf.magnitude} hT: {hT.magnitude} check: {hf.magnitude > hT.magnitude}");

            return hf.magnitude > hT.magnitude;
        }

        public void Dispose()
        {
            // FIXME: need to dispose properly
        }
    }
}
