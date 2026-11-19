/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;
using static MechJebLibTest.PSGTests.SolutionTests;

namespace MechJebLibTest.PSGTests
{
    /// <summary>
    ///     A stand-in rocket for the AscentStateMachine to fly, one physics tick at a time: it gathers the inputs, steps the
    ///     machine, applies the command as MechJebModulePSGAutopilot does, runs a model of MechJeb's autostaging, and burns the
    ///     propellant.
    ///     The rocket is a list of engines, each lit by one KSP stage and dropped by another, with its propellant in seconds of
    ///     burn at full throttle, and of other parts (fairings) dropped by a stage.  Every engine gives a unit of thrust on a
    ///     vessel of unit mass.
    ///     The stage stats are a model of the FuelFlowSimulation run on the vessel as it is, one row per KSP stage from the top
    ///     (KSP stage 0) down to the current stage, refreshed every StatsPeriod, so they are stale right after staging.
    ///     The autostaging won't drop an engine with propellant left (InverseStageDecouplesActiveOrIdleEngineOrTank), which is
    ///     what makes a booster cut with propellant left a relight hazard, and stops at the autopilot's autostage limit but for
    ///     fairings.
    ///     The vessel rides the synthetic trajectory of SolutionTests (r x v grows strictly with time) while it thrusts or
    ///     coasts, so terminal guidance meets the target's angular momentum at the end of the Solution.  The flight starts at
    ///     MET 4096 s, where those Solutions start, in a vacuum and high above the pad, so the first tick takes PRELAUNCH to
    ///     PITCH_PROGRAM and the second hands over to guidance.
    /// </summary>
    internal sealed class AscentFlight
    {
        // a physics tick
        public const double DT = 0.02;

        // the delta-v of a second of burn, in the stage stats
        public const double DV_PER_SECOND = 100;

        // PSGSimulation's minimum delta-v of a stage the plan burns
        public const double MIN_DV = 40;

        // the flight starts where the synthetic Solutions start
        public const double MET0 = T0;

        private const double UT0 = 100000;

        // the thrust acceleration along the synthetic trajectory
        private static readonly V3 _accel = VELOCITY_SCALE / 4 / TIME_SCALE * _yHat;

        public enum StagedBy { AUTOSTAGING, STAGE, IMMEDIATE, HAND }

        public sealed class Engine
        {
            public readonly uint Id;

            // the KSP stage that lights it
            public readonly int Ignite;

            // the KSP stage whose firing drops it, -1 if none does
            public readonly int Drop;

            // its propellant, in seconds of burn at full throttle
            public double Fuel;

            // the MET it fails (no thrust, with its propellant left), and the MET before which it won't light (waiting for the
            // propellant to settle); the stage stats see neither
            public double FailAt  = double.PositiveInfinity;
            public double LightAt = double.NegativeInfinity;

            // how many times it lit: a cut engine that lit again was relit
            public int Burns;

            public bool Thrusting;

            public Engine(uint id, int ignite, int drop, double fuel)
            {
                Id = id;
                Ignite = ignite;
                Drop = drop;
                Fuel = fuel;
            }
        }

        private readonly struct Part
        {
            public readonly uint Id;
            public readonly int  Drop;
            public readonly bool Fairing;

            public Part(uint id, int drop, bool fairing)
            {
                Id = id;
                Drop = drop;
                Fairing = fairing;
            }
        }

        public readonly struct Tick
        {
            public readonly double        Met;
            public readonly PSGState      From;
            public readonly PSGState      State;
            public readonly int           Stage;
            public readonly AscentCommand Command;

            public Tick(double met, PSGState from, PSGState state, int stage, AscentCommand command)
            {
                Met = met;
                From = from;
                State = state;
                Stage = stage;
                Command = command;
            }
        }

        public readonly struct Staging
        {
            public readonly double   Met;
            public readonly int      From;
            public readonly StagedBy By;

            public Staging(double met, int from, StagedBy by)
            {
                Met = met;
                From = from;
                By = by;
            }

            public override string ToString() => $"{By} {From}->{From - 1} at {Met:F2}";
        }

        public readonly AscentStateMachine Machine = new AscentStateMachine();

        public readonly List<Engine> Engines = new List<Engine>();
        private readonly List<Part>  _parts  = new List<Part>();

        public int Stage;

        // the vessel's controls
        public double Throttle;
        public bool   RCSGroup;

        // the RCS: forward thrust (ForwardRCS when there is some), its propellant in seconds, and how fast it moves the vessel
        // along the trajectory, relative to the engines
        public double RcsThrust;
        public double RcsFuel           = double.PositiveInfinity;
        public double RcsTrajectoryRate = 1;

        // the autostaging: the user's limit, the autopilot's last request, hot staging (a stage that only lights engines waits
        // for the current ones to burn out), fairings held by their conditions (the atmosphere), the pre-delay (Staging.Stage()
        // asked for that long first), and KSP's staging cooldown (the post delay too)
        public bool   Autostage = true;
        public int    UserAutostageLimit;
        public int    AutostageLimit = -1;
        public bool   HotStaging;
        public bool   HoldFairings;
        public double AutostagePreDelay;
        public double StagingCooldown    = 0.5;
        public double RefuseStagingUntil = double.NegativeInfinity;

        // the guidance requests KSP refused
        public int StagingRefusals;

        public double StatsPeriod = 0.1;

        // the rest of the inputs
        public bool   Grounded;
        public bool   LiftedOff = true;
        public bool   HasAtmosphere;
        public double AltitudeBottom   = 100000;
        public bool   PrelaunchDone    = true;
        public bool   AllowThrottle    = true;
        public double PitchStartHeight = 100;
        public double PitchRate        = 1;
        public double UllageLeadTime   = 2;
        public int    SpinupStage      = -1;

        public int    Ticks;
        public double Met  => MET0 + Ticks * DT;
        public double Time => UT0 + Ticks * DT;

        // where the vessel is on the synthetic trajectory (an MET of it), and whether it moved along it in the last tick
        public  double TrajectoryT = MET0;
        private bool   _moving;

        public readonly List<Tick>    Log      = new List<Tick>();
        public readonly List<Staging> Stagings = new List<Staging>();

        private List<FuelStats> _stats = new List<FuelStats>();
        private double          _statsTime = double.NaN;
        private double          _lastStageTime = double.NegativeInfinity;
        private bool            _countingDown;
        private double          _countdownStart;
        private double          _lastStageRequest;
        private bool            _rcsForward;

        public AscentFlight(int stage) => Stage = stage;

        public Engine AddEngine(uint id, int ignite, int drop, double fuel)
        {
            var engine = new Engine(id, ignite, drop, fuel);
            Engines.Add(engine);
            return engine;
        }

        public void AddPart(uint id, int drop, bool fairing = false) => _parts.Add(new Part(id, drop, fairing));

        public Engine EngineOf(uint id) => Engines.Single(e => e.Id == id);

        /*
         * The vessel
         */

        private bool Attached(int drop) => Stage > drop;

        private bool Ignited(Engine e) => Stage <= e.Ignite;

        // gives thrust when throttled up
        private bool Available(Engine e) => Attached(e.Drop) && Ignited(e) && e.Fuel > 0 && Met < e.FailAt && Met >= e.LightAt;

        private bool Decouples(int stage) => Engines.Any(e => e.Drop == stage) || _parts.Any(p => p.Drop == stage);

        private bool HasFairing(int stage) => _parts.Any(p => p.Drop == stage && p.Fairing && Attached(p.Drop));

        // autostaging never drops an engine with propellant (a failed one is dead)
        private bool DropsFueledEngine(int stage) => Engines.Any(e => e.Drop == stage && Attached(e.Drop) && e.Fuel > 0 && Met < e.FailAt);

        private bool LightsEngines(int stage) => Engines.Any(e => e.Ignite == stage);

        // InverseStageHasActiveEngines: lit, with propellant
        private bool HasActiveEngines() => Engines.Any(e => Attached(e.Drop) && Ignited(e) && e.Fuel > 0);

        private HashSet<uint> PartIds()
        {
            var ids = new HashSet<uint>();
            foreach (Engine e in Engines)
                if (Attached(e.Drop))
                    ids.Add(e.Id);
            foreach (Part p in _parts)
                if (Attached(p.Drop))
                    ids.Add(p.Id);
            return ids;
        }

        /// <summary>
        ///     The stage stats of the vessel now, top-down.  Like the FuelFlowSimulation, a stage burns until the engines the next
        ///     stage drops are out, or (when the next stage drops nothing) until all of its engines are out, and is staged at once
        ///     when the next stage only drops other parts.  A row lists the engines with propellant that burn in its stage.
        /// </summary>
        public List<FuelStats> Stats()
        {
            var fuel = Engines.ToDictionary(e => e, e => e.Fuel);
            var rows = new FuelStats[Stage + 1];

            for (int s = Stage; s >= 0; s--)
            {
                List<Engine> burning = Engines.Where(e => e.Ignite >= s && e.Drop < s && fuel[e] > 0).ToList();

                IEnumerable<Engine> waitFor = s == 0 || !Decouples(s - 1) ? burning : burning.Where(e => e.Drop == s - 1);
                double duration = waitFor.Select(e => fuel[e]).DefaultIfEmpty(0).Max();

                var row = new FuelStats
                {
                    KSPStage = s,
                    DeltaV = DV_PER_SECOND * duration,
                    DeltaTime = duration,
                    Isp = 300,
                    StartMass = 1,
                    EndMass = 1,
                    Thrust = burning.Count,
                    MaxThrust = burning.Count,
                    MinThrust = burning.Count
                };

                foreach (Engine e in burning)
                {
                    row.AddEngineSignature(new EngineSignature(e.Id, 0));
                    fuel[e] = Math.Max(fuel[e] - duration, 0);
                }

                row.SortEngineSignatures();
                rows[s] = row;
            }

            return rows.ToList();
        }

        private void RefreshStats()
        {
            _stats = Stats();
            _statsTime = Time;
        }

        /*
         * Plans and Solutions
         */

        /// <summary>
        ///     The stage editor's plan over the stage stats now: the bottom planned burn rows (all of them by default), and the
        ///     coast in the slot of its kind at the stats row coastRow.
        /// </summary>
        public FlightPlan Plan(PlanEditor.CoastKind? coast = null, int coastRow = -1, int planned = 0)
        {
            List<FuelStats> rows = Stats();
            var editor = new PlanEditor(new LogicalPlan(), rows, MIN_DV);
            if (planned > 0)
                editor.EndAt(planned - 1);
            if (coast.HasValue)
                Assert.True(editor.SetCoast(new PlanEditor.CoastSlot(coast.Value, coastRow)));

            FlightPlan? plan = FlightPlan.FromLogicalPlan(editor.Plan(), rows, rows, MIN_DV);
            Assert.NotNull(plan);
            return plan!;
        }

        public readonly struct Leg
        {
            public readonly double Seconds;
            public readonly bool   Coast;
            public readonly bool   AllowShutdown;
            public readonly bool   PropellantLeft;

            public Leg(double seconds, bool coast, bool allowShutdown, bool propellantLeft)
            {
                Seconds = seconds;
                Coast = coast;
                AllowShutdown = allowShutdown;
                PropellantLeft = propellantLeft;
            }
        }

        // a fixed burn time stage
        public static Leg Fixed(double seconds) => new Leg(seconds, false, false, false);

        // an optimized burn the optimizer ends with propellant left
        public static Leg Cut(double seconds) => new Leg(seconds, false, true, true);

        // an optimized burn that burns all of its propellant
        public static Leg Optimized(double seconds) => new Leg(seconds, false, true, false);

        public static Leg Coast(double seconds) => new Leg(seconds, true, true, false);

        /// <summary>
        ///     A Solution over the flight plan's stages that aren't done, from startIndex (the bottom by default) up, one leg each,
        ///     starting at MET start.  As SolutionBuilder.AnalyzeStages does, the last leg is the terminal stage, the last
        ///     optimized burn has the precise shutdown, and the legs before it too short to fly (a zero-length coast or burn) are
        ///     skipped.  Guidance flies its own copy of the flight plan.
        /// </summary>
        public static Solution Solve(FlightPlan plan, double start, int startIndex, params Leg[] legs)
        {
            var indices = new List<int>();
            for (int i = startIndex; i >= 0; i--)
                if (!plan.Stages[i].Done)
                    indices.Add(i);
            Assert.Equal(indices.Count, legs.Length);

            int precise = Array.FindLastIndex(legs, l => l.AllowShutdown && !l.Coast);

            var segs = new Seg[legs.Length];
            double tbar = (start - T0) / TIME_SCALE;
            double m = 64 * Math.Pow(4, legs.Length);

            for (int k = 0; k < legs.Length; k++)
            {
                FlightPlanStage stage = plan.Stages[indices[k]];
                Leg leg = legs[k];
                double tbar1 = tbar + leg.Seconds / TIME_SCALE;

                Phase phase;
                if (leg.Coast)
                {
                    phase = NewCoast(stage.KSPStage, stage.MJStage, m);
                    segs[k] = new Seg(phase, tbar, tbar1, m, m, _yHat, _yHat, 0, 0);
                }
                else
                {
                    // a zero-length burn burns nothing, though its stage's thrust needs a burn time
                    phase = NewBurn(stage.KSPStage, stage.MJStage, m, m / 2, 300, leg.Seconds > 0 ? leg.Seconds : 1, leg.AllowShutdown);
                    phase.PropellantLeft = leg.PropellantLeft;
                    segs[k] = new Seg(phase, tbar, tbar1, m, leg.Seconds > 0 ? m / 2 : m, _yHat, _yHat, 1, 1);
                    m /= 4;
                }

                phase.FlightPlanIndex = indices[k];
                phase.PreciseShutdown = k == precise;
                phase.TerminalStage = k == legs.Length - 1;
                phase.Skip = !phase.TerminalStage && leg.Seconds / TIME_SCALE < SolutionBuilder.SKIP_TIME;
                tbar = tbar1;
            }

            return Build(plan, segs);
        }

        public static Solution Solve(FlightPlan plan, double start, params Leg[] legs) => Solve(plan, start, plan.Stages.Count - 1, legs);

        // the flight plan guidance flies, as it has reconciled it
        public FlightPlan FlownPlan => Machine.Solution!.FlightPlan!;

        /*
         * Flying
         */

        public void Engage(Solution solution)
        {
            RefreshStats();
            Machine.Start();
            Assert.True(Machine.SetSolution(solution));
        }

        public void Step()
        {
            if (double.IsNaN(_statsTime))
                RefreshStats();

            PSGState from = Machine.State;
            AscentCommand cmd = Machine.Step(Inputs());
            Log.Add(new Tick(Met, from, Machine.State, Stage, cmd));

            Apply(cmd);
            Autostaging();
            Physics();
        }

        public void Run(double seconds)
        {
            int n = (int)Math.Round(seconds / DT);
            for (int i = 0; i < n; i++)
                Step();
        }

        // steps until done or until maxSeconds have passed, and says whether it got done
        public bool RunUntil(Func<AscentFlight, bool> done, double maxSeconds = 1000)
        {
            int n = (int)Math.Round(maxSeconds / DT);
            for (int i = 0; i < n; i++)
            {
                Step();
                if (done(this))
                    return true;
            }

            return false;
        }

        public bool RunUntil(PSGState state, double maxSeconds = 1000) => RunUntil(f => f.Machine.State == state, maxSeconds);

        // flies to FINISHED
        public void Fly(double maxSeconds = 1000) => Assert.True(RunUntil(PSGState.FINISHED, maxSeconds), $"not finished: {Sequence()}");

        private AscentInputs Inputs() =>
            new AscentInputs
            {
                Met = Met,
                Time = Time,
                Dt = DT,
                CurrentStage = Stage,
                Grounded = Grounded,
                LiftedOff = LiftedOff,
                AltitudeBottom = AltitudeBottom,
                HasAtmosphere = HasAtmosphere,
                Mass = 1,
                ThrustAvailable = Engines.Count(Available),
                MinThrottle = 0,
                RCSThrustUp = RCSGroup && RcsFuel > 0 ? RcsThrust : 0,
                ForwardRCS = RcsThrust > 0,
                RCSGroup = RCSGroup,
                R = ExpectedR(TrajectoryT),
                V = ExpectedV(TrajectoryT),
                A = _moving ? _accel : V3.zero,
                VacStats = _stats,
                AtmoStats = _stats,
                VacT = _statsTime,
                PartIds = PartIds(),
                PrelaunchDone = PrelaunchDone,
                AllowThrottle = AllowThrottle,
                PitchStartHeight = PitchStartHeight,
                PitchRate = PitchRate,
                UllageLeadTime = UllageLeadTime,
                SpinupStage = SpinupStage
            };

        // as MechJebModulePSGAutopilot.Apply, with no countdown holding for liftoff
        private void Apply(AscentCommand cmd)
        {
            if ((cmd.Events & (AscentEvents.COAST_STARTED | AscentEvents.FINISHED)) != 0)
                Throttle = 0;

            if (cmd.RCSGroupOn)
                RCSGroup = true;

            if (cmd.AutostageLimit >= 0)
                AutostageLimit = cmd.AutostageLimit;

            switch (cmd.Throttle)
            {
                case AscentThrottle.GUIDED:
                    Throttle = cmd.ThrottleValue;
                    break;
                case AscentThrottle.OFF:
                case AscentThrottle.RCS_FORWARD:
                    Throttle = 0;
                    break;
            }

            _rcsForward = cmd.Throttle == AscentThrottle.RCS_FORWARD;

            switch (cmd.Staging)
            {
                case AscentStaging.STAGE:
                    StageWithDelays(StagedBy.STAGE);
                    break;
                case AscentStaging.IMMEDIATE:
                    if (Stage > 0)
                        TryStage(StagedBy.IMMEDIATE);
                    break;
            }
        }

        // MechJebModuleStagingController.OnFixedUpdate, with no clamps, chutes or ullage
        private void Autostaging()
        {
            if (!Autostage || Stage <= 0 || Stage <= UserAutostageLimit)
                return;

            int next = Stage - 1;

            // at the autopilot's limit, only a fairing
            if (Stage <= AutostageLimit)
            {
                if (HasFairing(next) && !HoldFairings)
                    StageWithDelays(StagedBy.AUTOSTAGING);
                return;
            }

            if (DropsFueledEngine(next))
                return;

            if (!HasActiveEngines())
            {
                StageWithDelays(StagedBy.AUTOSTAGING);
                return;
            }

            if (HotStaging && LightsEngines(next) && !Decouples(next))
                return;

            if (HoldFairings && HasFairing(next))
                return;

            StageWithDelays(StagedBy.AUTOSTAGING);
        }

        // Staging.Stage(): the user's limit and the post delay, then the pre-delay, which counts down while it is asked every tick
        private void StageWithDelays(StagedBy by)
        {
            if (Stage <= 0 || Stage <= UserAutostageLimit || Time - _lastStageTime < StagingCooldown)
                return;

            if (!_countingDown || Time - _lastStageRequest > 1.5 * DT)
            {
                _countingDown = true;
                _countdownStart = Time;
            }

            _lastStageRequest = Time;

            if (Time - _countdownStart >= AutostagePreDelay - 1e-9)
                TryStage(by);
        }

        // Staging.ImmediateStage(): only KSP's staging cooldown
        private void TryStage(StagedBy by)
        {
            if (Met < RefuseStagingUntil || Time - _lastStageTime < StagingCooldown)
            {
                if (by != StagedBy.AUTOSTAGING)
                    StagingRefusals++;
                return;
            }

            Stagings.Add(new Staging(Met, Stage, by));
            Stage--;
            _lastStageTime = Time;
            _countingDown = false;
        }

        // staged by the pilot
        public void StageByHand()
        {
            if (Stage > 0)
                TryStage(StagedBy.HAND);
        }

        private void Physics()
        {
            bool thrusting = false;
            foreach (Engine e in Engines)
            {
                bool on = Available(e) && Throttle > 0;
                if (on && !e.Thrusting)
                    e.Burns++;
                e.Thrusting = on;
                if (!on)
                    continue;

                e.Fuel = Math.Max(e.Fuel - DT * Throttle, 0);
                thrusting = true;
            }

            bool rcs = _rcsForward && RCSGroup && RcsThrust > 0 && RcsFuel > 0;
            if (rcs)
                RcsFuel = Math.Max(RcsFuel - DT, 0);

            double rate = thrusting || Machine.IsCoasting ? 1 : rcs ? RcsTrajectoryRate : 0;
            TrajectoryT += rate * DT;
            _moving = rate > 0;

            Ticks++;

            if (Time - _statsTime >= StatsPeriod - 1e-9)
                RefreshStats();
        }

        /*
         * What happened
         */

        // the states it went through, e.g. "PRELAUNCH PITCH_PROGRAM BURNING FINISHED"
        public string Sequence()
        {
            var states = new List<PSGState> { PSGState.PRELAUNCH };
            foreach (Tick t in Log)
                if (t.State != states[states.Count - 1])
                    states.Add(t.State);
            return string.Join(" ", states);
        }

        // the MET of the first tick that entered the state, NaN if none did
        public double EnteredAt(PSGState state)
        {
            foreach (Tick t in Log)
                if (t.State == state && t.From != state)
                    return t.Met;
            return double.NaN;
        }

        public IEnumerable<Tick> In(PSGState state) => Log.Where(t => t.State == state);

        public int Count(AscentEvents e) => Log.Count(t => (t.Command.Events & e) != 0);

        public int Count(AscentMessages m) => Log.Count(t => (t.Command.Messages & m) != 0);

        public double At(AscentEvents e) => Log.Where(t => (t.Command.Events & e) != 0).Select(t => t.Met).DefaultIfEmpty(double.NaN).First();

        public string StagingLog() => string.Join(", ", Stagings);
    }
}
