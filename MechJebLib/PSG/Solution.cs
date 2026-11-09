/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.Functions;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    // Evaluating a Solution updates its interpolants' lookup caches, so one Solution must never be used from two threads:
    // give each thread its own DeepCopy().
    //
    // FlightPlan is the snapshot of the plan this solution was built from (see AscentBuilder.FlightPlan), not the caller's
    // live plan, and nothing may change it.  Each phase's FlightPlanIndex indexes its Stages, and since the stage list is
    // frozen in flight the same index finds the stage in the live plan too.
    //
    // A Solution stays in one system of units for its whole life, and every accessor takes and returns that system's units.
    // SolutionBuilder and AscentGuesser build it Normalized, in the units of its Problem's Scale, which the optimizer and its
    // warm start work in.  DeNormalized() makes the SI copy (meters, m/s, kg and seconds) which guidance flies.
    //
    // The clock is MET, the time since liftoff, which is zero on the ground: normalized MET in a normalized Solution, and MET
    // in seconds in an SI one.  A solution computed on the ground starts at MET zero, so guidance waiting on the pad always
    // gets its first point, and one computed in flight starts at the MET of its initial state.
    public class Solution : IDisposable
    {
        public readonly bool Normalized;

        // the Scale of the Problem the solution was built for, which an SI copy keeps
        public readonly Scale Scale;

        public readonly List<Phase> Phases = new List<Phase>();
        public readonly FlightPlan? FlightPlan;

        private readonly List<double>         _tmin         = new List<double>();
        private readonly List<double>         _tmax         = new List<double>();
        private readonly List<double>         _dvstart      = new List<double>();
        private readonly List<double>         _dvend        = new List<double>();
        private readonly List<VecInterpolant> _interpolants = new List<VecInterpolant>();

        // in SI units, for TerminalString
        private readonly double _mu;
        private readonly double _rbody;

        public int Segments => Phases.Count;

        // the MET of the start and the end of the trajectory
        public double T0 => _tmin[0];
        public double Tf => _tmax[Segments - 1];

        // a normalized solution for the problem
        public Solution(Problem problem, FlightPlan? flightPlan)
            : this(problem.Scale, true, problem.Mu, problem.RBody * problem.Scale.LengthScale, flightPlan)
        {
        }

        private Solution(Scale scale, bool normalized, double mu, double rbody, FlightPlan? flightPlan)
        {
            Scale = scale;
            Normalized = normalized;
            _mu = mu;
            _rbody = rbody;
            FlightPlan = flightPlan;
        }

        public void AddSegment(VecInterpolant interpolant, Phase phase)
        {
            CheckDebug.True(phase.Normalized == Normalized);

            _tmin.Add(interpolant.MinT);
            _tmax.Add(interpolant.MaxT);
            Phases.Add(phase.DeepCopy());
            _interpolants.Add(interpolant);
            int idx = _interpolants.Count - 1;
            double dvstart = _dvend.Count > 0 ? _dvend[_dvend.Count - 1] : 0;
            double dvend = dvstart + phase.DeltaVFromMass(M(idx, interpolant.MinT), M(idx, interpolant.MaxT));
            _dvstart.Add(dvstart);
            _dvend.Add(dvend);
        }

        public Solution DeepCopy()
        {
            var dup = new Solution(Scale, Normalized, _mu, _rbody, FlightPlan?.DeepCopy());
            for (int i = 0; i < _interpolants.Count; i++)
                dup.AddSegment(_interpolants[i].DeepCopy(), Phases[i]);
            return dup;
        }

        /// <summary>
        ///     A copy of this normalized solution in SI units, for guidance to fly.  Like DeepCopy() it shares nothing with this
        ///     one, including its own copy of the FlightPlan.
        /// </summary>
        public Solution DeNormalized()
        {
            Check.True(Normalized, "the Solution is already in SI units");

            var dup = new Solution(Scale, false, _mu, _rbody, FlightPlan?.DeepCopy());

            using var yScale = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            new InterpolantLayout
            {
                R = Scale.LengthScale * V3.one,
                V = Scale.VelocityScale * V3.one,
                M = Scale.MassScale,
                U = V3.one,
                T = 1
            }.CopyTo(yScale);

            for (int i = 0; i < _interpolants.Count; i++)
            {
                VecInterpolant interpolant = _interpolants[i].DeepCopy();
                interpolant.Rescale(Scale.TimeScale, yScale);
                dup.AddSegment(interpolant, Phases[i].DeNormalize(Scale));
            }

            return dup;
        }

        public double StartTime(int p) => _tmin[p];

        public double EndTime(int p) => _tmax[p];

        public V3 R(double t)
        {
            using Vec xRaw = Interpolate(t);
            return InterpolantLayout.CreateFrom(xRaw).R;
        }

        public V3 V(double t)
        {
            using Vec xRaw = Interpolate(t);
            return InterpolantLayout.CreateFrom(xRaw).V;
        }

        public V3 U(double t)
        {
            using Vec xRaw = Interpolate(t);
            return InterpolantLayout.CreateFrom(xRaw).U;
        }

        public double M(double t)
        {
            using Vec xRaw = Interpolate(t);
            return InterpolantLayout.CreateFrom(xRaw).M;
        }

        // the mass on the given segment's own interpolant, which picks the right side of the drop in mass at a phase boundary,
        // particularly for the terminal mass at the end of a phase
        // XXX: maybe should throw if t is outside the segment?
        private double M(int segment, double t)
        {
            using Vec xRaw = Interpolate(segment, t);
            return InterpolantLayout.CreateFrom(xRaw).M;
        }

        // the delta-v used since the start of the trajectory
        public double DV(double t)
        {
            int idx = IndexFor(t);
            double dv = Phases[idx].DeltaVFromMass(M(idx, _tmin[idx]), M(idx, t));
            return _dvstart[idx] + dv;
        }

        // the delta-v left in phase n
        public double DV(double t, int n)
        {
            t = Clamp(t, _tmin[n], _tmax[n]);
            return Phases[n].DeltaVFromMass(M(n, t), M(n, _tmax[n]));
        }

        // the time left in the burn before the coast, in the coast, and in the burn after it (each may be multi-stage); without
        // a coast every burn counts as the burn after it
        public (double burn1, double coast, double burn2) TgoSplit(double t)
        {
            double burn1 = 0, coast = 0, burn2 = 0;

            int i;

            for (i = IndexFor(t); i < Phases.Count; i++)
            {
                if (Phases[i].Coast)
                    break;
                burn1 += Tgo(t, i);
            }

            // never found a coast
            if (i == Phases.Count)
                return (0, 0, burn1);

            coast = Tgo(t, i);
            i++;

            while (i < Phases.Count)
            {
                burn2 += Tgo(t, i);
                i++;
            }

            return (burn1, coast, burn2);
        }

        // runs negative past Tf
        public double Tgo(double t) => Tf - t;

        // the time left in phase n: all of it before the phase starts, and none of it once it is over
        public double Tgo(double t, int n)
        {
            if (t > _tmin[n])
                return Max(_tmax[n] - t, 0);
            return _tmax[n] - _tmin[n];
        }

        // The burn time left in a KSP stage, summed over all of its burn phases.  This does not start from the phase the clock
        // is in: a vessel can stage ahead of the clock (a fixed booster burning out before its predicted end), and starting
        // from the booster phase would find no phases for the new stage and return zero.  Tgo gives zero for phases in the
        // past, so a stage burned past its predicted end (residuals) still returns zero.
        public double TgoForKSPStage(double t, int kspStage)
        {
            double sum = 0;
            for (int i = 0; i < Phases.Count; i++)
            {
                if (Phases[i].Coast || Phases[i].KSPStage != kspStage)
                    continue;
                sum += Tgo(t, i);
            }

            return sum;
        }

        public double Vgo(double t) => DV(Tf) - DV(t);

        public bool Coast(double t) => Phases[IndexFor(t)].Coast;

        // Specialized API to determine if we still have the coast in our future or not
        // (or if we're in a coast right now)
        public bool WillCoast(double t)
        {
            for (int i = IndexFor(t); i < Phases.Count; i++)
                if (Phases[i].Coast)
                    return true;

            return false;
        }

        // true if we're in a coast, or the phase after the current one is a coast.  Unlike WillCoast this is false for a burn
        // which is followed by other burns before the coast.
        public bool CoastFollows(double t)
        {
            int idx = IndexFor(t);

            return Phases[idx].Coast || (idx + 1 < Phases.Count && Phases[idx + 1].Coast);
        }

        public int CoastKSPStage()
        {
            for (int i = 0; i < Phases.Count; i++)
                if (Phases[i].Coast)
                    return Phases[i].KSPStage;

            return -1;
        }

        public int TerminalKSPStage() => Phases[TerminalIndex()].KSPStage;

        public int TerminalMJPhase() => Phases[TerminalIndex()].MJPhase;

        // SolutionBuilder.AnalyzeStages always flags a terminal stage, so a Solution without one is a bug
        // (AscentGuesser solutions skip AnalyzeStages and must not be asked)
        private int TerminalIndex()
        {
            int idx = Phases.FindIndex(p => p.TerminalStage);
            Check.True(idx >= 0, "Solution has no terminal stage");
            return idx;
        }

        // -1 if no stage needs a precise shutdown (e.g. all fixed burn time stages with no coast)
        public int OptimizeKSPStage()
        {
            for (int i = 0; i < Phases.Count; i++)
                if (Phases[i].PreciseShutdown)
                    return Phases[i].KSPStage;

            return -1;
        }

        public bool Unguided(double t) => Phases[IndexFor(t)].Unguided;

        public bool CoastPhase(int phase) => Phases[phase].Coast;

        public int KSPStage(int phase) => Phases[phase].KSPStage;

        public int MJPhase(int phase) => Phases[phase].MJPhase;

        // -1 without a FlightPlan
        public int FlightPlanIndex(int phase) => Phases[phase].FlightPlanIndex;

        // the stage of this solution's FlightPlan snapshot that the phase was built from, null without a FlightPlan
        public FlightPlanStage? FlightPlanStage(int phase) => FlightPlan?.Stages[Phases[phase].FlightPlanIndex];

        // the time left until the end of the phase the clock is in
        public double StageTimeLeft(double t) => _tmax[IndexFor(t)] - t;

        // engineMinThrottle is the fraction of available thrust the engines currently produce at zero KSP throttle
        public (V3, double) InertialGuidance(double t, double engineMinThrottle)
        {
            using Vec xRaw = Interpolate(t);
            var x = InterpolantLayout.CreateFrom(xRaw);
            V3 u0 = x.U.normalized;

            int phase = IndexFor(t);
            // don't command less than the optimizer's throttle floor if the interpolant dips between collocation points
            double thrustPct = Clamp(x.T, Phases[phase].MinThrottle, 1.0);
            double kspThrottle = engineMinThrottle < 1.0 ? (thrustPct - engineMinThrottle) / (1.0 - engineMinThrottle) : 1.0;

            return (u0, Clamp(kspThrottle, 0.01, 1.0));
        }

        public (V3 r, V3 v) TerminalStateVectors() => StateVectors(Tf);

        public (V3 r, V3 v) StateVectors(double t)
        {
            using Vec xRaw = Interpolate(t);
            var x = InterpolantLayout.CreateFrom(xRaw);
            return (x.R, x.V);
        }

        // FIXME: this is really specific display logic
        public string TerminalString()
        {
            Check.False(Normalized, "TerminalString prints SI units, use DeNormalized()");

            (V3 rf, V3 vf) = TerminalStateVectors();

            (double sma, double ecc, double inc, double lan, double argp, double tanom, _) = Astro.KeplerianFromStateVectors(_mu,
                rf, vf);

            double peR = Astro.PeriapsisFromKeplerian(sma, ecc);
            double apR = Astro.ApoapsisFromKeplerian(sma, ecc);
            double peA = peR - _rbody;
            double apA = apR <= 0 ? apR : apR - _rbody;
            double fpa = Astro.FlightPathAngle(rf, vf);
            double rT = rf.magnitude - _rbody;
            double vT = vf.magnitude;

            var sb = new StringBuilder();
            sb.AppendLine($"Orbit: {peA.ToSI()}m x {apA.ToSI()}m");
            sb.AppendLine($"rT: {rT.ToSI()}m vT: {vT.ToSI()}m/s FPA: {Rad2Deg(fpa):F1}°");
            sb.AppendLine($"sma: {sma.ToSI()}m ecc: {ecc:F3} inc: {Rad2Deg(inc):F1}°");
            sb.Append($"lan: {Rad2Deg(lan):F1}° argp: {Rad2Deg(argp):F1}° ta: {Rad2Deg(tanom):F1}°");
            return sb.ToString();
        }

        private int IndexFor(double t)
        {
            for (int i = 0; i < _tmax.Count; i++)
                if (t < _tmax[i])
                    return i;
            return _tmax.Count - 1;
        }

        public int IndexForKSPStage(int kspStage, bool coasting)
        {
            // phases are in chronological order, which is descending KSPStage order, so walking backwards and
            // keeping the last match lands on the earliest (currently active) phase rather than the final one.
            int idx = -1;

            for (int i = Phases.Count - 1; i >= 0; i--)
            {
                // only ever one coast phase so this test is good enough
                if (Phases[i].Coast && coasting)
                    return i;
                // the kspstage may not match (fairing jettison), hence the inequality test
                if (!Phases[i].Coast && !coasting && Phases[i].KSPStage <= kspStage)
                    idx = i;
            }

            return idx;
        }

        private Vec Interpolate(double t) => _interpolants[IndexFor(t)].Evaluate(t);

        private Vec Interpolate(int segment, double t) => _interpolants[segment].Evaluate(t);

        /// <summary>
        ///     This handles the precise termination of a burn segment.  Our trajectory only supports one
        ///     coast in a multiburnphase-coast-multiburnphase layout.  The multiburn phase may be a combination
        ///     of fixed burntime and precise burning stages.
        ///     We need to support:
        ///     - precise shutdown at the termination of the ascent
        ///     - precise shutdown before a coast
        ///     - precise shutdown before fixed burntime upper stages
        ///     - some timing fuzz (particularly when we got over Tf or burn slightly into the coast segment)
        ///     - staging during the 10-second terminal phase
        ///     So based on the time:
        ///     - if we're over Tf, just return the terminal conditions at the end of the trajectory
        ///     - if we're in a coast phase, back up to find the burn phase
        ///     - if we're in a fixed phase, back up to find the previous allow shutdown phase
        ///     - if we're in an allow shutdown phase, and the next phase is also allow shutdown (and not a coast), go forward
        ///     The result should be the right trajectory segment+phase to check the terminal conditions against.
        /// </summary>
        /// <param name="pos">Current BCI position</param>
        /// <param name="vel">Current BCI velocity</param>
        /// <param name="t">Current MET</param>
        /// <param name="kspStage">The vessel's current KSP stage, which the checked phase must not come before</param>
        /// <returns></returns>
        public bool TerminalGuidanceSatisfied(V3 pos, V3 vel, double t, int kspStage)
        {
            /*
             NOTE: This algorithm suggests that there's a higher level abstraction above a phase/physical rocket stage which fuses
             Phases into Multi-Phase segments based on them being adjacent and either all AllowShutdown or not (or I guess
             adjacent coasts, but I expect the optimizer to remove those as adjacent coasts are trivially fuse-able).

             XXX: This tests for the angular momentum exceeding the target, so it is only applicable for orbit raising / ascents.
            */
            var hf = V3.Cross(pos, vel);

            (V3 rT, V3 vT) = StateVectors(Tf);

            if (t < Tf)
            {
                // the vessel may have staged ahead of the clock (a fixed booster burning out before its predicted end), so
                // never check against a phase before the current KSP stage, such as the end of that booster
                int first = Max(IndexForKSPStage(kspStage, false), 0);
                int idx = Max(IndexFor(t), first);
                // back up to find an AllowShutdown stage if we've gone past the terminal time and
                // hit a coast or a fixed burn time stage (the first phase may be a coast in a solution
                // computed during the coast, so don't walk off the front)
                while (idx > first && (!Phases[idx].AllowShutdown || Phases[idx].Coast))
                    idx--;
                // go forward to the end of this multi-stage burn segment until we hit a coast or fixed stages
                while (idx < Segments - 1 && Phases[idx].AllowShutdown && Phases[idx + 1].AllowShutdown && !Phases[idx + 1].Coast)
                    idx++;
                (rT, vT) = StateVectors(_tmax[idx]);
            }

            var hT = V3.Cross(rT, vT);

            if (hf.magnitude > hT.magnitude)
                Print($"Terminal conditions met -- hf: {hf.magnitude} hT: {hT.magnitude}");

            return hf.magnitude > hT.magnitude;
        }

        public void Dispose()
        {
            foreach (VecInterpolant i in _interpolants)
                i.Dispose();
            _interpolants.Clear();
        }
    }
}
