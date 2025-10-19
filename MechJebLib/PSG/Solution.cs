/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class Solution : IDisposable
    {
        public           double       T0;
        public           double       Tf => T0 + Tmax * _timeScale;
        private readonly Scale        _scale;
        private readonly List<double> _tmin         = new List<double>();
        private readonly List<double> _tmax         = new List<double>();
        private readonly List<Hn>     _interpolants = new List<Hn>();
        public readonly  List<Phase>  Phases        = new List<Phase>();
        private readonly double       _mu;
        private readonly double       _rbody;

        public  int    Segments       => Phases.Count;
        private double _timeScale     => _scale.TimeScale;
        private double _lengthScale   => _scale.LengthScale;
        private double _velocityScale => _scale.VelocityScale;
        private double _massScale     => _scale.MassScale;

        public double Tmax => _tmax[Segments - 1];
        public double Tmin => _tmin[0];

        public Solution(Problem problem)
        {
            _scale = problem.Scale;
            _mu    = problem.Mu;
            _rbody = problem.Rbody * _scale.LengthScale;
            // t0 is a public API that can be updated while we're landed waiting for takeoff.
            T0 = problem.T0;
        }

        public void AddSegment(double t0, double tf, Hn interpolant, Phase phase)
        {
            _tmin.Add(t0);
            _tmax.Add(tf);
            Phases.Add(phase.DeepCopy());
            _interpolants.Add(interpolant);
        }

        // convert kerbal time to normalized time
        public double Tbar(double t)
        {
            double tbar = (t - T0) / _timeScale;
            if (tbar < Tmin)
                return Tmin;

            if (tbar > Tmax)
                return Tmax;

            return tbar;
        }

        public V3 R(double t)
        {
            double tBar = (t - T0) / _timeScale;
            return RBar(tBar) * _lengthScale;
        }

        public V3 RBar(double tBar)
        {
            using Vn xRaw = Interpolate(tBar);
            var      x    = InterpolantLayout.CreateFrom(xRaw);
            return x.R;
        }

        public V3 V(double t)
        {
            double tBar = (t - T0) / _timeScale;
            return VBar(tBar) * _velocityScale;
        }

        public V3 VBar(double tBar)
        {
            using Vn xRaw = Interpolate(tBar);
            var      x    = InterpolantLayout.CreateFrom(xRaw);
            return x.V;
        }

        public V3 U(double t)
        {
            double tBar = (t - T0) / _timeScale;
            return UBar(tBar);
        }

        public V3 UBar(double tBar)
        {
            using Vn xRaw = Interpolate(tBar);
            var      x    = InterpolantLayout.CreateFrom(xRaw);
            return x.U;
        }

        public double M(double t)
        {
            double tBar = (t - T0) / _timeScale;
            return MBar(tBar) * _massScale;
        }

        public double MBar(double tBar)
        {
            using Vn xRaw = Interpolate(tBar);
            var      x    = InterpolantLayout.CreateFrom(xRaw);
            return x.M;
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
            double lo = Min(Max(_tmin[segment], tbar), hi);

            return hi - lo;
        }

        public double DV(double t)
        {
            double   tbar = (t - T0) / _timeScale;
            using Vn xraw = Interpolate(tbar);
            var      x    = InterpolantLayout.CreateFrom(xraw);
            return x.Dv * _velocityScale;
        }

        public double DV(double t, int n)
        {
            double   tbar  = (t - T0) / _timeScale;
            double   min   = tbar > _tmin[n] ? tbar : _tmin[n];
            double   max   = _tmax[n];
            using Vn ddmin = Interpolate(n, min);
            var      xmin  = InterpolantLayout.CreateFrom(ddmin);
            using Vn ddmax = Interpolate(n, max);
            var      xmax  = InterpolantLayout.CreateFrom(ddmax);
            return Max(xmax.Dv - xmin.Dv, 0) * _velocityScale;
        }

        public (double burn1, double coast, double burn2) TgoBarSplit(double tBar)
        {
            double burn1 = 0, coast = 0, burn2 = 0;

            int i;

            for (i = IndexForTbar(tBar); i < Phases.Count; i++)
            {
                if (Phases[i].Coast)
                    break;
                burn1 += TgoBar(tBar, i);
            }

            // never found a coast
            if (i == Phases.Count)
                return (0, 0, burn1);

            coast = TgoBar(tBar, i);
            i++;

            while (i < Phases.Count)
            {
                burn2 += TgoBar(tBar, i);
                i++;
            }

            return (burn1, coast, burn2);
        }

        public double Tgo(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return (Tmax - tbar) * _timeScale;
        }

        public double Tgo(double t, int n)
        {
            double tbar = (t - T0) / _timeScale;
            return TgoBar(tbar, n) * _timeScale;
        }

        public double TgoBar(double tbar, int n)
        {
            if (tbar > _tmin[n])
                return Max(_tmax[n] - tbar, 0);
            return _tmax[n] - _tmin[n];
        }

        public double Vgo(double t) => DV(Tf) - DV(t);

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
                if (Phases[i].Coast)
                    return true;

            return false;
        }

        public int CoastKSPStage()
        {
            for (int i = 0; i < Phases.Count; i++)
                if (Phases[i].Coast)
                    return Phases[i].KSPStage;

            return -1;
        }

        public int TerminalKSPStage() => Phases[Phases.FindIndex(p => p.TerminalStage)].KSPStage;

        public int OptimizeKSPStage() => Phases[Phases.FindIndex(p => p.PreciseShutdown)].KSPStage;

        public bool Unguided(double t)
        {
            double tbar = (t - T0) / _timeScale;
            return Phases[IndexForTbar(tbar)].Unguided;
        }

        public bool CoastPhase(int phase) => Phases[phase].Coast;

        public int KSPStage(int phase) => Phases[phase].KSPStage;

        public int MJPhase(int phase) => Phases[phase].MJPhase;

        public double StageTimeLeft(double t)
        {
            double tbar  = (t - T0) / _timeScale;
            int    phase = IndexForTbar(tbar);
            return (_tmax[phase] - tbar) * _timeScale;
        }

        public (double pitch, double heading, V3 inertial) PitchAndHeading(double t)
        {
            double tbar = (t - T0) / _timeScale;

            using Vn xraw = Interpolate(tbar);
            var      x    = InterpolantLayout.CreateFrom(xraw);
            V3       u    = x.U.normalized;

            int phase = IndexForTbar(tbar);

            if (Phases[phase].Coast && phase < Segments - 1)
            {
                double   tbar2 = _tmin[phase + 1];
                using Vn xraw2 = Interpolate(tbar2);
                var      x2    = InterpolantLayout.CreateFrom(xraw2);
                u = x2.U.normalized;
            }

            (double pitch, double heading) = Astro.ECIToPitchHeading(x.R, u);
            return (pitch, heading, u);
        }

        public (V3 r, V3 v) TerminalStateVectors() => StateVectors(Tmax);

        public (V3 r, V3 v) StateVectors(double tbar)
        {
            using Vn xraw = Interpolate(tbar);
            var      x    = InterpolantLayout.CreateFrom(xraw);
            return (x.R * _lengthScale, x.V * _velocityScale);
        }

        // FIXME: this is really specific display logic
        public string TerminalString()
        {
            (V3 rf, V3 vf) = TerminalStateVectors();

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) = Astro.KeplerianFromStateVectors(_mu,
                rf, vf);

            double peR = Astro.PeriapsisFromKeplerian(sma, ecc);
            double apR = Astro.ApoapsisFromKeplerian(sma, ecc);
            double peA = peR - _rbody;
            double apA = apR <= 0 ? apR : apR - _rbody;
            double fpa = Astro.FlightPathAngle(rf, vf);
            double rT  = rf.magnitude - _rbody;
            double vT  = vf.magnitude;

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

        private Vn Interpolate(double tbar) => _interpolants[IndexForTbar(tbar)].Evaluate(tbar);

        private Vn Interpolate(int segment, double tbar) => _interpolants[segment].Evaluate(tbar);

        /// <summary>
        ///     This handles the precise termination of a burn segment.  Our trajectory only supports one
        ///     coast in a multiburnphase-coast-multiburnphase layout.  The multiburn phase may be a combination
        ///     of fixed burntime and precise burning stages.
        ///     We need to support:
        ///     - precise shutdown at the termination of the ascent
        ///     - precise shutdown before a coast
        ///     - precise shutdown before fixed burntime upper stages
        ///     - some timing fuzz (particularly when we got over Tmax or burn slightly into the coast segment)
        ///     - staging during the 10-second terminal phase
        ///     So based on the time:
        ///     - if we're over Tmax, just return the terminal conditions at the end of the trajectory
        ///     - if we're in a coast phase, back up to find the burn phase
        ///     - if we're in a fixed phase, back up to find the previous allow shutdown phase
        ///     - if we're in an allow shutdown phase, and the next phase is also allow shutdown (and not a coast), go forward
        ///     The result should be the right trajectory segment+phase to check the terminal conditions against.
        /// </summary>
        /// <param name="pos">Current BCI position</param>
        /// <param name="vel">Current BCI velocity</param>
        /// <param name="t">Current World time</param>
        /// <returns></returns>
        public bool TerminalGuidanceSatisfied(V3 pos, V3 vel, double t)
        {
            /*
             NOTE: This algorithm suggests that there's a higher level abstraction above a phase/physical rocket stage which fuses
             Phases into Multi-Phase segments based on them being adjacent and either all AllowShutdown or not (or I guess
             adjacent coasts, but I expect the optimizer to remove those as adjacent coasts are trivially fuse-able).

             XXX: This tests for the angular momentum exceeding the target, so it is only applicable for orbit raising / ascents.
            */
            var    hf   = V3.Cross(pos, vel);
            double tbar = (t - T0) / _timeScale;

            (V3 rT, V3 vT) = StateVectors(Tmax);

            if (tbar < Tmax)
            {
                int idx = IndexForTbar(tbar);
                // back up to find an AllowShutdown stage if we've gone past the terminal time and
                // hit a coast or a fixed burn time stage
                while ((idx > 0 && !Phases[idx].AllowShutdown) || Phases[idx].Coast)
                    idx--;
                // go forward to the end of this multi-stage burn segment until we hit a coast or fixed stages
                while (idx < Segments - 1 && Phases[idx].AllowShutdown && Phases[idx + 1].AllowShutdown && !Phases[idx + 1].Coast)
                    idx++;
                double end = _tmax[idx];
                (rT, vT) = StateVectors(end);
            }

            var hT = V3.Cross(rT, vT);

            if (hf.magnitude > hT.magnitude)
                Print($"Terminal conditions met -- hf: {hf.magnitude} hT: {hT.magnitude}");

            return hf.magnitude > hT.magnitude;
        }

        public void Dispose()
        {
            // FIXME: need to dispose properly
        }
    }
}
