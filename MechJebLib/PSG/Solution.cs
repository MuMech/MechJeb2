/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
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
    // live plan.  Each phase's FlightPlanIndex indexes its Stages, and since the stage list is frozen in flight the same index
    // finds the stage in the live plan too.  The solver's own copy never changes it; the DeNormalized() copy guidance flies
    // has its own FlightPlan, which guidance reconciles in place with the stage stats (FlightPlan.ReconcileStages) to follow
    // which of its stages are gone.
    //
    // A Solution stays in one system of units for its whole life, and every accessor takes and returns that system's units.
    // SolutionBuilder and AscentGuesser build it Normalized, in the units of its Problem's Scale, which the optimizer and its
    // warm start work in.  DeNormalized() makes the SI copy (meters, m/s, kg and seconds) which guidance flies.
    //
    // The clock is MET, the time since liftoff, which is zero on the ground: normalized MET in a normalized Solution, and MET
    // in seconds in an SI one.  A solution computed on the ground starts at MET zero, so guidance waiting on the pad always
    // gets its first point, and one computed in flight starts at the MET of its initial state.  While it waits on the ground
    // the launch site turns with the planet, which RotateToLaunchSite turns the solution to follow.
    //
    // A solution computed on the ground may start with the vertical rise guidance flies before it pitches over, which was
    // presolved ahead of the optimizer (VerticalRise): the trajectory then starts at TStart on the ground, and its phases at T0
    // at the top of the rise.  The rise is not a phase, so the phases, their times and indices, and the state on a phase's own
    // interpolant are the optimizer's.  Only the queries by time see it: the state before T0 (R, V, U, M, StateVectors), the
    // delta-v used and left (DV, Vgo), and the time and delta-v left in the first phase, which burns the rise.
    //
    // A phase SolutionBuilder.AnalyzeStages marks Skip (one the optimizer left too short to fly, or a pruned stage) is never
    // flown, but it stays in the Solution: its phases line up with the optimizer's for the warm start of the next solve, which
    // reads the old Solution phase by phase.  The queries about what guidance flies (CoastIndex, CoastKSPStage, NextPhase,
    // IndexForKSPStage) pass over skipped phases.  The trajectory is still the optimizer's, skipped phases and all: the state at
    // a time (IndexFor), the times of the phases, TgoSplit, and what each phase is (CoastPhase).
    public class Solution : IDisposable
    {
        public readonly bool Normalized;

        // the Scale of the Problem the solution was built for, which an SI copy keeps
        public readonly Scale Scale;

        public readonly List<Phase> Phases = new List<Phase>();
        public readonly FlightPlan? FlightPlan;

        // the atmosphere and aerodynamic limits of the Problem, in this solution's units
        public readonly AeroModel Aero;

        private readonly List<double>         _tmin         = new List<double>();
        private readonly List<double>         _tmax         = new List<double>();
        private readonly List<double>         _dvstart      = new List<double>();
        private readonly List<double>         _dvend        = new List<double>();
        private readonly List<VecInterpolant> _interpolants = new List<VecInterpolant>();

        // the vertical rise from TStart to T0, or null (see Problem.Rise)
        private VecInterpolant? _rise;

        // in SI units, for TerminalLines
        private readonly double _mu;
        private readonly double _rbody;

        // the launch site the solution was solved at (the start of the rise, or the initial position), in its units
        private readonly V3 _r0;

        // the turn RotateToLaunchSite applies to every vector the solution returns
        private Q3 _rotation = Q3.identity;

        public int Segments => Phases.Count;

        // the MET of the start of the phases and the end of the trajectory
        public double T0 => _tmin[0];
        public double Tf => _tmax[Segments - 1];

        // the trajectory starts with the vertical rise to T0
        public bool HasRise => _rise != null;

        // the MET of the start of the trajectory: of the rise, or of the phases
        public double TStart => _rise?.MinT ?? T0;

        // computed on the ground at liftoff, so RotateToLaunchSite turns it to follow the launch site
        public bool FromPad => TStart <= 0;

        // a normalized solution for the problem
        public Solution(Problem problem, FlightPlan? flightPlan)
            : this(problem.Scale, true, problem.Mu, problem.RBody * problem.Scale.LengthScale, problem.R0, AeroModel.From(problem), flightPlan,
                problem.Rise?.DeepCopy())
        {
        }

        // takes ownership of rise
        private Solution(Scale scale, bool normalized, double mu, double rbody, V3 r0, AeroModel aero, FlightPlan? flightPlan,
            VecInterpolant? rise)
        {
            Aero = aero;
            Scale = scale;
            Normalized = normalized;
            _mu = mu;
            _rbody = rbody;
            _rise = rise;
            _r0 = rise != null ? RiseRaw(rise.MinT).R : r0;
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
            // the first phase burns the rise
            double dvstart = _dvend.Count > 0 ? _dvend[_dvend.Count - 1] : RiseDV(phase, T0);
            double dvend = dvstart + phase.DeltaVFromMass(M(idx, interpolant.MinT), M(idx, interpolant.MaxT));
            _dvstart.Add(dvstart);
            _dvend.Add(dvend);
        }

        public Solution DeepCopy()
        {
            var dup = new Solution(Scale, Normalized, _mu, _rbody, _r0, Aero, FlightPlan?.DeepCopy(), _rise?.DeepCopy()) { _rotation = _rotation };
            for (int i = 0; i < _interpolants.Count; i++)
                dup.AddSegment(_interpolants[i].DeepCopy(), Phases[i]);
            return dup;
        }

        /// <summary>
        ///     A copy of this normalized solution in SI units, for guidance to fly.  Like DeepCopy() it shares nothing with this
        ///     one, including its own copy of the FlightPlan, and it keeps the rotation to the launch site.
        /// </summary>
        public Solution DeNormalized()
        {
            Check.True(Normalized, "the Solution is already in SI units");

            using var yScale = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            new InterpolantLayout
            {
                R = Scale.LengthScale * V3.one,
                V = Scale.VelocityScale * V3.one,
                M = Scale.MassScale,
                U = V3.one,
                T = 1
            }.CopyTo(yScale);

            VecInterpolant? rise = _rise?.DeepCopy();
            rise?.Rescale(Scale.TimeScale, yScale);

            var dup = new Solution(Scale, false, _mu, _rbody, _r0 * Scale.LengthScale, Aero.DeNormalize(Scale),
                FlightPlan?.DeepCopy(), rise) { _rotation = _rotation };

            for (int i = 0; i < _interpolants.Count; i++)
            {
                VecInterpolant interpolant = _interpolants[i].DeepCopy();
                interpolant.Rescale(Scale.TimeScale, yScale);
                dup.AddSegment(interpolant, Phases[i].DeNormalize(Scale));
            }

            return dup;
        }

        /// <summary>
        ///     The launch site turns with the planet while the vessel waits on the ground, so this turns a solution computed on
        ///     the ground about the planet's pole (V3.northpole), from the launch site it was solved at to r0, the launch site
        ///     now.  Every vector the solution returns is turned, which carries its initial position and velocity exactly onto the
        ///     moved launch site and keeps its inclination, and the vertical rise along with the rest.  A solution which doesn't
        ///     start at liftoff (FromPad) is never turned.  Once the vessel has lifted off a solution computed on the ground
        ///     keeps following the vessel, which is close enough while it climbs over the launch site, until the first solution
        ///     computed in flight replaces it.  Only the direction of r0 matters, so it can be in meters or normalized units.
        ///     Each call replaces the last rotation, so it can be called every tick.
        /// </summary>
        public void RotateToLaunchSite(V3 r0)
        {
            if (!FromPad)
                return;

            // the angle about the pole between the two launch sites' projections onto the equator
            double angle = Atan2(_r0.x * r0.y - _r0.y * r0.x, _r0.x * r0.x + _r0.y * r0.y);
            _rotation = Q3.AngleAxis(angle, V3.northpole);
        }

        public double StartTime(int p) => _tmin[p];

        /// <summary>
        ///     The state on phase p's own interpolant at t, turned like every other accessor.  At a phase boundary it gives that
        ///     phase's side of it, and it extrapolates outside the phase.
        /// </summary>
        public InterpolantLayout State(int p, double t)
        {
            using Vec xRaw = Interpolate(p, t);
            var x = InterpolantLayout.CreateFrom(xRaw);
            x.R = _rotation * x.R;
            x.V = _rotation * x.V;
            x.U = _rotation * x.U;
            return x;
        }

        /// <summary>The state on the vertical rise at t, turned like every other accessor.  It extrapolates outside the rise.</summary>
        public InterpolantLayout RiseState(double t)
        {
            Check.True(HasRise, "the Solution has no vertical rise");

            InterpolantLayout x = RiseRaw(t);
            x.R = _rotation * x.R;
            x.V = _rotation * x.V;
            x.U = _rotation * x.U;
            return x;
        }

        // adds the bounds between the rise's interpolant nodes, its start and end included
        public void RiseNodeBoundaries(List<double> into)
        {
            Check.True(HasRise, "the Solution has no vertical rise");

            AddNodeBoundaries(_rise!, into);
        }

        // adds the bounds between phase p's interpolant nodes, its start and end included
        public void NodeBoundaries(int p, List<double> into) => AddNodeBoundaries(_interpolants[p], into);

        private static void AddNodeBoundaries(VecInterpolant interpolant, List<double> into)
        {
            for (int i = 0; i < interpolant.NodeCount; i++)
                into.Add(interpolant.NodeLeftT(i));
            if (interpolant.NodeCount > 0)
                into.Add(interpolant.NodeRightT(interpolant.NodeCount - 1));
        }

        // adds the times phase p's interpolant passes through, which for a SolutionBuilder solution are its collocation points
        public void Knots(int p, List<double> into) => _interpolants[p].Knots(into);

        public double EndTime(int p) => _tmax[p];

        public V3 R(double t)
        {
            using Vec xRaw = Interpolate(t);
            return _rotation * InterpolantLayout.CreateFrom(xRaw).R;
        }

        public V3 V(double t)
        {
            using Vec xRaw = Interpolate(t);
            return _rotation * InterpolantLayout.CreateFrom(xRaw).V;
        }

        public V3 U(double t)
        {
            using Vec xRaw = Interpolate(t);
            return _rotation * InterpolantLayout.CreateFrom(xRaw).U;
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

        // the delta-v used since the start of the trajectory, the rise included
        public double DV(double t)
        {
            if (InRise(t))
                return RiseDV(Phases[0], t);

            int idx = IndexFor(t);
            double dv = Phases[idx].DeltaVFromMass(M(idx, _tmin[idx]), M(idx, t));
            return _dvstart[idx] + dv;
        }

        // the delta-v left in phase n, the rest of the rise included in the first phase
        public double DV(double t, int n)
        {
            double mf = M(n, _tmax[n]);

            if (n == 0 && InRise(t))
                return Phases[0].DeltaVFromMass(RiseRaw(Max(t, TStart)).M, mf);

            t = Clamp(t, _tmin[n], _tmax[n]);
            return Phases[n].DeltaVFromMass(M(n, t), mf);
        }

        // the time is before T0 on a solution with a rise
        private bool InRise(double t) => _rise != null && t < T0;

        // the state on the rise, unturned
        private InterpolantLayout RiseRaw(double t)
        {
            using Vec xRaw = _rise!.Evaluate(t);
            return InterpolantLayout.CreateFrom(xRaw);
        }

        // the delta-v the first phase burns on the rise from its start to t (none without a rise)
        private double RiseDV(Phase first, double t)
        {
            if (_rise == null)
                return 0;

            t = Clamp(t, TStart, _rise.MaxT);
            return first.DeltaVFromMass(RiseRaw(TStart).M, RiseRaw(t).M);
        }

        /// <summary>
        ///     How much delta-v the trajectory burns beyond the propellant the rocket has, in this solution's velocity units, or zero
        ///     when the rocket can reach the target.  The optimizer lets the last stage it may shut down burn past its propellant
        ///     (PhaseCollection.FixLastShutdownStage), so a rocket without enough delta-v still converges, on a trajectory whose
        ///     last burn ends below that stage's empty mass.  A burn-coast-burn of one stage counts once, from its second burn.
        /// </summary>
        public double DeltaVShortfall()
        {
            double shortfall = 0;

            for (int p = 0; p < Segments; p++)
            {
                Phase phase = Phases[p];
                if (phase.Coast)
                    continue;

                double mEnd = M(p, _tmax[p]);
                if (mEnd >= phase.Mf)
                    continue;

                // the mass can't reach zero (MinM is Sqrt(EPS)), but keep the log finite whatever the interpolant gives
                shortfall = Max(shortfall, phase.DeltaVFromMass(phase.Mf, Max(mEnd, phase.Mf * 1e-9)));
            }

            return shortfall;
        }

        // The time left in the burn before the coast, in the coast, and in the burn after it (each may be multi-stage); without
        // a coast every burn counts as the burn after it.  This is the warm start's split (Optimizer.TranscribePreviousSolution),
        // which counts skipped phases on purpose: the next problem has the coast the optimizer shrank to nothing too, and the
        // burns before it are mapped onto the burns before this coast.
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

        // the time left in phase n: all of it before the phase starts, and none of it once it is over.  The first phase
        // starts with the rise.
        public double Tgo(double t, int n)
        {
            double start = n == 0 ? TStart : _tmin[n];
            if (t > start)
                return Max(_tmax[n] - t, 0);
            return _tmax[n] - start;
        }

        public double Vgo(double t) => DV(Tf) - DV(t);

        // the KSP stage of the coast guidance flies, or -1 without one
        public int CoastKSPStage()
        {
            int coast = CoastIndex();
            return coast >= 0 ? Phases[coast].KSPStage : -1;
        }

        public int TerminalKSPStage() => Phases[TerminalIndex()].KSPStage;

        public int TerminalMJPhase() => Phases[TerminalIndex()].MJPhase;

        // The phase that ends the ascent: the phases after it are pruned stages the optimizer gives no burn time, which are
        // skipped.  SolutionBuilder.AnalyzeStages always flags a terminal stage, and never skips it, so a Solution without one
        // is a bug (AscentGuesser solutions skip AnalyzeStages and must not be asked).
        public int TerminalIndex()
        {
            int idx = Phases.FindIndex(p => p.TerminalStage);
            Check.True(idx >= 0, "Solution has no terminal stage");
            CheckDebug.False(Phases[idx].Skip);
            return idx;
        }

        // The phase of the coast guidance flies, or -1 without one (a Solution has at most one).  A coast the optimizer shrank to
        // nothing is skipped, and so is a coast after the terminal stage.
        public int CoastIndex() => Phases.FindIndex(p => p.Coast && !p.Skip);

        // -1 if no stage needs a precise shutdown (e.g. all fixed burn time stages with no coast)
        public int OptimizeKSPStage()
        {
            for (int i = 0; i < Phases.Count; i++)
                if (Phases[i].PreciseShutdown)
                    return Phases[i].KSPStage;

            return -1;
        }

        public bool Unguided(double t) => Phases[IndexFor(t)].Unguided;

        // the phase's kind: a skipped coast is still a coast
        public bool CoastPhase(int phase) => Phases[phase].Coast;

        // guidance never flies the phase (Phase.Skip)
        public bool Skip(int phase) => Phases[phase].Skip;

        // the phase guidance flies after this one: the next one that isn't skipped, or -1 if none is left
        public int NextPhase(int phase)
        {
            for (int i = phase + 1; i < Phases.Count; i++)
                if (!Phases[i].Skip)
                    return i;

            return -1;
        }

        public int KSPStage(int phase) => Phases[phase].KSPStage;

        /// <summary>The KSP stage of the phase flown at <paramref name="t" />.</summary>
        public int KSPStageAt(double t) => Phases[IndexFor(t)].KSPStage;

        public int MJPhase(int phase) => Phases[phase].MJPhase;

        // -1 without a FlightPlan
        public int FlightPlanIndex(int phase) => Phases[phase].FlightPlanIndex;

        // the stage of this solution's FlightPlan snapshot that the phase was built from, null without a FlightPlan
        public FlightPlanStage? FlightPlanStage(int phase) => FlightPlan?.Stages[Phases[phase].FlightPlanIndex];

        /// <summary>
        ///     The thrust direction and KSP throttle of phase p, on its own interpolant at t clamped to the phase.  Guidance flies
        ///     the phase the vessel is in, which residuals put ahead of or behind the clock: a stage burning past its predicted end
        ///     holds the end of its phase, and one that burned out early starts the next phase from its start.
        /// </summary>
        /// <param name="phase">The phase the vessel is flying</param>
        /// <param name="t">The current MET</param>
        /// <param name="engineMinThrottle">The fraction of available thrust the engines currently produce at zero KSP throttle</param>
        public (V3, double) InertialGuidance(int phase, double t, double engineMinThrottle)
        {
            t = Clamp(t, _tmin[phase], _tmax[phase]);

            using Vec xRaw = Interpolate(phase, t);
            var x = InterpolantLayout.CreateFrom(xRaw);
            V3 u0 = (_rotation * x.U).normalized;

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
            return (_rotation * x.R, _rotation * x.V);
        }

        // FIXME: this is really specific display logic
        public string[] TerminalLines()
        {
            Check.False(Normalized, "TerminalLines prints SI units, use DeNormalized()");

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

            return new[]
            {
                $"Orbit: {peA.ToSI()}m x {apA.ToSI()}m",
                $"rT: {rT.ToSI()}m vT: {vT.ToSI()}m/s FPA: {Rad2Deg(fpa):F1}°",
                $"sma: {sma.ToSI()}m ecc: {ecc:F3} inc: {Rad2Deg(inc):F1}°",
                $"lan: {Rad2Deg(lan):F1}° argp: {Rad2Deg(argp):F1}° ta: {Rad2Deg(tanom):F1}°"
            };
        }

        private int IndexFor(double t)
        {
            for (int i = 0; i < _tmax.Count; i++)
                if (t < _tmax[i])
                    return i;
            return _tmax.Count - 1;
        }

        // the phase guidance flies in KSP stage kspStage, or the coast when coasting, or -1: skipped phases are never flown
        public int IndexForKSPStage(int kspStage, bool coasting)
        {
            // phases are in chronological order, which is descending KSPStage order, so walking backwards and
            // keeping the last match lands on the earliest (currently active) phase rather than the final one.
            int idx = -1;

            for (int i = Phases.Count - 1; i >= 0; i--)
            {
                if (Phases[i].Skip)
                    continue;
                // only ever one coast phase so this test is good enough
                if (Phases[i].Coast && coasting)
                    return i;
                // the kspstage may not match (fairing jettison), hence the inequality test
                if (!Phases[i].Coast && !coasting && Phases[i].KSPStage <= kspStage)
                    idx = i;
            }

            return idx;
        }

        // the rise before T0
        private Vec Interpolate(double t) => InRise(t) ? _rise!.Evaluate(t) : _interpolants[IndexFor(t)].Evaluate(t);

        private Vec Interpolate(int segment, double t) => _interpolants[segment].Evaluate(t);

        public void Dispose()
        {
            foreach (VecInterpolant i in _interpolants)
                i.Dispose();
            _interpolants.Clear();
            _rise?.Dispose();
            _rise = null;
        }
    }
}
