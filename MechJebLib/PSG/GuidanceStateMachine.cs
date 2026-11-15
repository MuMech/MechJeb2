/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public enum PSGStatus
    {
        // engaged, waiting for a Solution or for the autopilot to let guidance fly
        WAITING,

        // flying a burn segment
        BURNING,

        // flying a coast segment
        COASTING,

        // the last burn's terminal guidance: steering frozen, cutting on the tick the target is reached
        FINAL,

        // the last burn finished with the RCS
        FINAL_RCS,

        FINISHED
    }

    // what guidance does with the main throttle and the RCS this tick
    public enum GuidanceThrottle
    {
        // leave the throttle alone
        NONE,

        // the Solution's throttle
        SOLUTION,

        // throttle off
        OFF,

        // throttle off and RCS translating forward
        RCS_FORWARD
    }

    public enum GuidanceStaging
    {
        NONE,

        // stage with the staging controller's delays (asked every tick while it is wanted)
        STAGE,

        // stage now
        IMMEDIATE
    }

    [Flags]
    public enum GuidanceMessages { NONE = 0, RCS_COAST = 1, RCS_TRIM = 2, RCS_OFF = 4 }

    /// <summary>
    ///     What the vessel looks like to guidance this tick. Thrust and mass are in any units where thrust / mass is an
    ///     acceleration (kN and t in KSP). Positions, velocities and the acceleration are in the Solution's frame.
    /// </summary>
    public struct GuidanceInputs
    {
        // the Solution's clock (MET)
        public double Met;

        // the universal time, for the timers
        public double Time;

        // the length of a physics tick
        public double Dt;

        // the vessel's current KSP stage
        public int KSPStage;

        public bool Grounded;

        // the autopilot let guidance fly (AssertStart)
        public bool AllowExecution;

        public double Mass;

        // the engines' thrust available (with the engines out and the RCS group on, the RCS thrust along the vessel's axis)
        public double ThrustAvailable;

        // the RCS thrust available forward
        public double RCSThrustUp;

        // at least 10 % of the RCS thrust pushes the vessel forward (the RCS group off counts too)
        public bool ForwardRCS;

        // the RCS action group is on
        public bool RCSGroup;

        // the delta-v left in the current KSP stage by the stage stats, NaN while they have nothing for it
        public double StageDeltaV;

        public V3 R;
        public V3 V;
        public V3 A;

        // how long before the end of a coast the RCS settles the propellant
        public double UllageLeadTime;

        // MechJeb's autostaging is on
        public bool Autostaging;
    }

    /// <summary>
    ///     What guidance asks the vessel to do this tick.
    /// </summary>
    public struct GuidanceCommand
    {
        public GuidanceThrottle Throttle;

        // the autostage limit to hold, -1 to leave it
        public int AutoStageLimit;

        public GuidanceStaging Staging;

        // turn the RCS action group on
        public bool RCSGroupOn;

        // drop out of time warp
        public bool MinimumWarp;

        // a coast is behind: the flight plan must mark it done
        public bool MarkCoastDone;

        public GuidanceMessages Messages;

        // guidance is over: disengage
        public bool Finished;

        public static GuidanceCommand None => new GuidanceCommand { AutoStageLimit = -1 };
    }

    /// <summary>
    ///     The decisions of the PSG guidance controller. The KSP side gathers the inputs, calls Step once per physics tick, and
    ///     applies the command.
    ///     The Solution's phases are grouped into segments: a coast on its own, or a run of burns up to a coast, a change between
    ///     optimized and fixed burns, or the last burn (the terminal stage; the pruned stages after it are not flown). Guidance
    ///     holds the segment it flies, and only ever moves on to the next one. Each segment says how it ends:
    ///     - the last burn: terminal guidance when the target is reached (FINAL, with an RCS trim when the RCS is on), or for a
    ///       fixed last burn when it is spent;
    ///     - a burn before a coast, or an optimized burn the plan cuts with propellant left: at its planned end, on the nearest
    ///       tick, once the vessel is in its last stage;
    ///     - a burn the plan burns to depletion: when the vessel stages past it (autostaging, which may go down to the next
    ///       segment's stage);
    ///     - a coast: at its planned end, after the ullage lead (a coast after the last burn is not flown);
    ///     - any burn: when it is spent (no thrust in its last stage for a second, with nothing left in it, or once it has burned
    ///       past its planned end, or on a burn guidance cuts), or when the vessel has staged into the next segment;
    ///     - any segment: when a new Solution no longer has it.
    ///     Guidance drops the stage of a segment it ended itself; the autostage limit keeps autostaging within the segment, and
    ///     autostaging takes the vessel on to the next segment's first stage (guidance does, with the staging delays, when
    ///     autostaging is off).
    /// </summary>
    public class GuidanceStateMachine
    {
        private enum SegmentEnd
        {
            // cut at the planned end
            CUT,

            // the plan burns it out: staging moves on
            DEPLETION,

            // terminal guidance on the target
            TERMINAL,

            // a coast's planned end
            COAST
        }

        private struct Segment
        {
            public int        First;
            public int        Last;
            public SegmentEnd End;
            public bool       IsLast;

            // the highest and lowest KSP stage of its phases
            public int FirstKSPStage;
            public int LastKSPStage;

            public double EndTime;

            public bool Coast => End == SegmentEnd.COAST;

            public override string ToString() => $"phases {First}-{Last} {End}{(IsLast ? " last" : "")} stages {FirstKSPStage}-{LastKSPStage} ends {EndTime:F2}";
        }

        // what the cut leaves within a tick and what stays in a stage the plan burns out, relative to the stage's mass
        private const double PROPELLANT_LEFT = 1e-3;

        // terminal guidance starts this long before the end of the last burn
        private const double FINAL_TIME = 10;

        public PSGStatus Status { get; private set; } = PSGStatus.WAITING;

        public Solution? Solution { get; private set; }

        // a coast has been started since guidance was engaged
        public bool HasCoasted { get; private set; }

        // the universal time the last coast started
        public double StartCoast { get; private set; }

        private readonly List<Segment> _segments = new List<Segment>();

        private int _cursor;

        // the phases of the segments already flown (KSP stage, coast, after a coast): the Solutions solved in flight start
        // from the vessel's state, and one solved before guidance moved on still has the segment it left
        private readonly List<int> _done = new List<int>();

        private double _spentSince = double.NaN;

        // guidance moved on to a segment it has to stage into, from this KSP stage (-1: nothing to stage)
        private int _stageFrom = -1;

        // a Solution moved the cursor on to a later segment: the next Step moves on as Advance does
        private bool _relocated;

        // the current segment's last stage has had thrust since the segment started
        private bool _lit;

        public bool IsTerminal() => Status == PSGStatus.FINAL || Status == PSGStatus.FINAL_RCS;

        public bool IsCoasting() => Status == PSGStatus.COASTING;

        public bool IsRCSTrim() => Status == PSGStatus.FINAL_RCS;

        // flying a burn or a coast, or with a Solution and waiting to fly it: when the optimizer should run
        public bool IsNormal() => (Status == PSGStatus.WAITING && Solution != null) || Status == PSGStatus.BURNING || Status == PSGStatus.COASTING;

        // waiting or flying normally: not in terminal guidance and not finished
        public bool IsReady() => Status == PSGStatus.WAITING || Status == PSGStatus.BURNING || Status == PSGStatus.COASTING;

        // the steering can be followed
        public bool IsStable() => IsNormal() || IsTerminal();

        // the state of an enabled controller with nothing to fly yet
        public void Reset()
        {
            Status = PSGStatus.WAITING;
            Solution = null;
            HasCoasted = false;
            StartCoast = 0;
            _segments.Clear();
            _cursor = 0;
            _done.Clear();
            _spentSince = double.NaN;
            _stageFrom = -1;
            _relocated = false;
            _lit = false;
        }

        public void SetSolution(Solution solution)
        {
            if (Status == PSGStatus.FINISHED)
                return;

            // the segment being flown, by its phases' keys in the Solution it came from
            bool flying = Status == PSGStatus.BURNING || Status == PSGStatus.COASTING;
            _flown.Clear();
            if (flying && Solution != null)
                for (int p = _segments[_cursor].First; p <= _segments[_cursor].Last; p++)
                    _flown.Add(Key(p));

            Solution = solution;
            BuildSegments(solution);

            _cursor = 0;
            while (_cursor < _segments.Count - 1 && IsDone(_segments[_cursor]))
                _cursor++;

            // a Solution that no longer has the segment being flown (its stage was too spent to plan) moves guidance on to the
            // next one, as the segment's own end would
            if (flying && !Contains(_segments[_cursor], _flown))
            {
                foreach (int key in _flown)
                    if (!_done.Contains(key))
                        _done.Add(key);
                _relocated = true;
            }
        }

        private readonly List<int> _flown = new List<int>();

        private bool Contains(in Segment seg, List<int> keys)
        {
            for (int p = seg.First; p <= seg.Last; p++)
                if (keys.Contains(Key(p)))
                    return true;
            return false;
        }

        public void Finish()
        {
            Status = PSGStatus.FINISHED;
            Solution = null;
        }

        public GuidanceCommand Step(in GuidanceInputs inputs)
        {
            GuidanceCommand cmd = GuidanceCommand.None;

            if (Status == PSGStatus.FINISHED)
            {
                cmd.Finished = true;
                return cmd;
            }

            if (Solution == null || !inputs.AllowExecution)
                return cmd;

            if (Status == PSGStatus.WAITING)
                Enter(inputs, ref cmd);

            if (_relocated)
            {
                _relocated = false;
                Print("[GuidanceStateMachine] the new Solution starts with a later segment.");
                MoveOn(inputs, ref cmd, true);
            }

            if (!inputs.Grounded)
                Transitions(inputs, ref cmd);

            if (cmd.Finished)
            {
                Finish();
                return cmd;
            }

            Drive(inputs, ref cmd);

            return cmd;
        }

        private void Transitions(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            // a segment's end can take guidance straight past the next one (a vessel staged two segments ahead)
            for (int n = 0; n <= _segments.Count; n++)
            {
                Segment seg = _segments[_cursor];

                // staged past the segment
                if (inputs.KSPStage < seg.LastKSPStage)
                {
                    if (seg.IsLast)
                    {
                        Print("[GuidanceStateMachine] staged past the last stage.");
                        cmd.Finished = true;
                        return;
                    }

                    if (inputs.KSPStage <= _segments[_cursor + 1].FirstKSPStage)
                    {
                        Advance(inputs, ref cmd, false);
                        continue;
                    }
                }

                if (!seg.Coast && Spent(seg, inputs))
                {
                    if (seg.IsLast)
                    {
                        Print($"[GuidanceStateMachine] the last stage ran dry {seg.EndTime - inputs.Met:F1} s before the end of the burn.");
                        cmd.Finished = true;
                        return;
                    }

                    Print($"[GuidanceStateMachine] the burn in stage {inputs.KSPStage} has no thrust left, {seg.EndTime - inputs.Met:F1} s before its end.");
                    // a burned out stage is autostaging's to drop: only a segment that ends on guidance's cut is staged off here
                    Advance(inputs, ref cmd, seg.End == SegmentEnd.CUT);
                    continue;
                }

                switch (seg.End)
                {
                    case SegmentEnd.COAST when inputs.Met >= seg.EndTime:
                        Print("[GuidanceStateMachine] end of the coast.");
                        if (seg.IsLast)
                        {
                            cmd.Finished = true;
                            return;
                        }

                        Advance(inputs, ref cmd, true);
                        continue;
                    // the nearest tick to the planned end: cutting now stops seg.EndTime - Met short, the next tick a tick less
                    // than that over.  Only in the segment's last stage: a vessel still in an earlier one keeps burning until
                    // staging or the spent rule brings it there.
                    case SegmentEnd.CUT when seg.EndTime - inputs.Met < 0.5 * inputs.Dt && inputs.KSPStage <= seg.LastKSPStage:
                        Print($"[GuidanceStateMachine] cut at the planned end of the burn in stage {inputs.KSPStage}.");
                        Advance(inputs, ref cmd, true);
                        continue;
                    case SegmentEnd.TERMINAL:
                        TerminalGuidance(seg, inputs, ref cmd);
                        return;
                }

                return;
            }
        }

        // the last segment's terminal guidance
        private void TerminalGuidance(in Segment seg, in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            int terminalStage = Solution!.KSPStage(seg.Last);

            if (Status == PSGStatus.BURNING)
            {
                if (seg.EndTime - inputs.Met > FINAL_TIME || inputs.KSPStage > terminalStage)
                    return;

                if (inputs.ForwardRCS && !inputs.RCSGroup)
                    cmd.Messages |= GuidanceMessages.RCS_OFF;
                Status = PSGStatus.FINAL;
            }

            cmd.MinimumWarp = true;

            if (Status == PSGStatus.FINAL_RCS && !inputs.RCSGroup)
            {
                Print("[GuidanceStateMachine] terminating guidance due to manual deactivation of RCS.");
                cmd.Finished = true;
                return;
            }

            bool willTrim = Status == PSGStatus.FINAL && inputs.ForwardRCS && inputs.RCSGroup && inputs.KSPStage == terminalStage;

            // stopping one tick short is more accurate for rockets without RCS, but sometimes we overshoot with only one tick
            double dt = (willTrim ? 2 : 1) * inputs.Dt;
            V3 v1 = inputs.V + inputs.A * dt;
            V3 x1 = inputs.R + inputs.V * dt + 0.5 * inputs.A * dt * dt;

            if (!Solution.TerminalGuidanceSatisfied(x1, v1, inputs.Met, inputs.KSPStage))
                return;

            if (willTrim)
            {
                Print("[GuidanceStateMachine] transition to RCS terminal guidance.");
                cmd.Messages |= GuidanceMessages.RCS_TRIM;
                cmd.RCSGroupOn = true;
                Status = PSGStatus.FINAL_RCS;
                return;
            }

            Print("[GuidanceStateMachine] terminal guidance completed.");
            cmd.Finished = true;
        }

        // the throttle, RCS, warp and staging of the state; no state changes here
        private void Drive(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            Segment seg = _segments[_cursor];

            cmd.AutoStageLimit = AutoStageLimit();

            // staging into a segment guidance moved on to: the stage it ended is dropped now (asked again while KSP refuses
            // it); the stages after that (an interstage, a decoupler before the engines) are left to autostaging, with its
            // checks for ullage, fairings and the rest, or staged with the staging delays when autostaging is off
            bool dropping = false;
            if (_stageFrom >= 0)
            {
                if (inputs.KSPStage <= seg.FirstKSPStage)
                    _stageFrom = -1;
                else if (inputs.KSPStage >= _stageFrom)
                {
                    dropping = true;
                    if (cmd.Staging == GuidanceStaging.NONE)
                        cmd.Staging = GuidanceStaging.IMMEDIATE;
                }
                else if (!inputs.Autostaging && cmd.Staging == GuidanceStaging.NONE)
                    cmd.Staging = GuidanceStaging.STAGE;
            }

            if (HasCoasted && !seg.Coast)
                cmd.MarkCoastDone = true;

            switch (Status)
            {
                case PSGStatus.COASTING:
                    cmd.Throttle = seg.EndTime - inputs.Met < inputs.UllageLeadTime ? GuidanceThrottle.RCS_FORWARD : GuidanceThrottle.OFF;
                    break;
                case PSGStatus.FINAL_RCS:
                    cmd.Throttle = GuidanceThrottle.RCS_FORWARD;
                    break;
                default:
                    // the stage that was cut stays off until it is dropped
                    cmd.Throttle = dropping ? GuidanceThrottle.OFF : GuidanceThrottle.SOLUTION;
                    if (seg.End == SegmentEnd.CUT && seg.EndTime - inputs.Met <= FINAL_TIME)
                        cmd.MinimumWarp = true;
                    break;
            }
        }

        // autostaging stays within the segment, except that a burn the plan burns out may be dropped into the next segment
        private int AutoStageLimit()
        {
            Segment seg = _segments[_cursor];
            if (seg.End == SegmentEnd.DEPLETION && !seg.IsLast)
                return _segments[_cursor + 1].FirstKSPStage;
            return seg.LastKSPStage;
        }

        // start flying the current segment
        private void Enter(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            _spentSince = double.NaN;
            _lit = false;

            if (!_segments[_cursor].Coast)
            {
                Status = PSGStatus.BURNING;
                return;
            }

            Status = PSGStatus.COASTING;
            HasCoasted = true;
            StartCoast = inputs.Time;
            if (!inputs.RCSGroup)
            {
                cmd.RCSGroupOn = true;
                cmd.Messages |= GuidanceMessages.RCS_COAST;
            }
        }

        // move on to the next segment, staging into it if guidance ended this one
        private void Advance(in GuidanceInputs inputs, ref GuidanceCommand cmd, bool stage)
        {
            Segment seg = _segments[_cursor];
            for (int p = seg.First; p <= seg.Last; p++)
            {
                int key = Key(p);
                if (!_done.Contains(key))
                    _done.Add(key);
            }

            _cursor++;
            MoveOn(inputs, ref cmd, stage);
        }

        // start the segment at the cursor, staging into it if guidance ended the one before
        private void MoveOn(in GuidanceInputs inputs, ref GuidanceCommand cmd, bool stage)
        {
            Segment next = _segments[_cursor];

            _stageFrom = stage && inputs.KSPStage > next.FirstKSPStage ? inputs.KSPStage : -1;
            // the cut and the stage on one tick: a hot handover to the next burn, or the coast's separation
            if (_stageFrom >= 0)
                cmd.Staging = GuidanceStaging.IMMEDIATE;

            Enter(inputs, ref cmd);
        }

        // the segment's last stage has had no thrust to give for a second.  A RealFuels engine that has not lit yet has no
        // ThrustAvailable either, but still has its propellant.  In the RCS trim the RCS is what has to be out.
        private bool Spent(in Segment seg, in GuidanceInputs inputs)
        {
            bool inLastStage = inputs.KSPStage <= seg.LastKSPStage;
            if (inLastStage && !NoThrust(inputs.ThrustAvailable, inputs))
                _lit = true;

            // A stage with propellant left counts as spent once it has burned, past the planned end, or at any time on a burn
            // that guidance cuts (a failed engine, which autostaging would drop but the cut's autostage limit holds).  One that
            // never lit may still be waiting to: a vessel behind the clock, an engine held for ullage.
            bool burnedOut = _lit && (inputs.Met >= seg.EndTime || seg.End == SegmentEnd.CUT);
            bool spent = Status != PSGStatus.WAITING && inLastStage &&
                (Status == PSGStatus.FINAL_RCS
                    ? NoThrust(inputs.RCSThrustUp, inputs)
                    : NoThrust(inputs.ThrustAvailable, inputs) && (inputs.StageDeltaV < 1 || burnedOut));

            if (!spent)
            {
                _spentSince = double.NaN;
                return false;
            }

            if (double.IsNaN(_spentSince))
                _spentSince = inputs.Time;

            return inputs.Time - _spentSince >= 1;
        }

        // under 1 mm/s² on this vessel
        private static bool NoThrust(double thrust, in GuidanceInputs inputs) => thrust < 1e-3 * inputs.Mass;

        private bool IsDone(in Segment seg)
        {
            for (int p = seg.First; p <= seg.Last; p++)
                if (!_done.Contains(Key(p)))
                    return false;
            return true;
        }

        // a phase by its KSP stage, whether it is the (only) coast, and for a burn whether it comes after the coast: the two
        // burns of a coast-during coast share a KSP stage.  A Solution solved after the coast doesn't have it any more.
        private int Key(int p)
        {
            Solution solution = Solution!;
            if (solution.CoastPhase(p))
                return solution.KSPStage(p) * 4 + 1;

            bool hasCoast = false, afterCoast = false;
            for (int i = 0; i < solution.Segments; i++)
                if (solution.CoastPhase(i))
                {
                    hasCoast = true;
                    afterCoast = i < p;
                    break;
                }

            if (!hasCoast)
                afterCoast = HasCoasted;

            return solution.KSPStage(p) * 4 + (afterCoast ? 2 : 0);
        }

        private void BuildSegments(Solution solution)
        {
            _segments.Clear();

            int terminal = -1;
            for (int p = 0; p < solution.Segments; p++)
                if (solution.Phases[p].TerminalStage)
                    terminal = p;
            Check.True(terminal >= 0, "Solution has no terminal stage");

            for (int p = 0; p <= terminal; p++)
            {
                Phase phase = solution.Phases[p];
                Phase? prev = p > 0 ? solution.Phases[p - 1] : null;

                bool newSegment = prev == null || phase.Coast || prev.Coast || phase.AllowShutdown != prev.AllowShutdown ||
                    phase.Unguided != prev.Unguided;

                if (newSegment)
                    _segments.Add(new Segment { First = p, Last = p, FirstKSPStage = phase.KSPStage, LastKSPStage = phase.KSPStage });

                Segment seg = _segments[_segments.Count - 1];
                seg.Last = p;
                seg.LastKSPStage = Math.Min(seg.LastKSPStage, phase.KSPStage);
                seg.FirstKSPStage = Math.Max(seg.FirstKSPStage, phase.KSPStage);
                seg.EndTime = solution.EndTime(p);
                _segments[_segments.Count - 1] = seg;
            }

            // a coast after the last burn (every burn after it pruned) is not flown: the burn before it is the last segment
            while (_segments.Count > 1 && solution.Phases[_segments[_segments.Count - 1].First].Coast)
                _segments.RemoveAt(_segments.Count - 1);

            for (int i = 0; i < _segments.Count; i++)
            {
                Segment seg = _segments[i];
                Phase last = solution.Phases[seg.Last];
                seg.IsLast = i == _segments.Count - 1;

                if (last.Coast)
                    seg.End = SegmentEnd.COAST;
                else if (seg.IsLast)
                    seg.End = AnyAllowShutdown(solution, seg) ? SegmentEnd.TERMINAL : SegmentEnd.DEPLETION;
                else if (solution.Phases[_segments[i + 1].First].Coast || (last.AllowShutdown && PropellantLeft(solution, seg.Last)))
                    seg.End = SegmentEnd.CUT;
                else
                    seg.End = SegmentEnd.DEPLETION;

                _segments[i] = seg;
            }
        }

        private static bool AnyAllowShutdown(Solution solution, in Segment seg)
        {
            for (int p = seg.First; p <= seg.Last; p++)
                if (solution.Phases[p].AllowShutdown)
                    return true;
            return false;
        }

        // the plan ends the burn with propellant left in the stage
        private static bool PropellantLeft(Solution solution, int p)
        {
            Phase phase = solution.Phases[p];
            double mEnd = solution.State(p, solution.EndTime(p)).M;
            return mEnd - phase.Mf > PROPELLANT_LEFT * phase.M0;
        }

        public override string ToString() => $"{Status} segment {_cursor}: {(_segments.Count > _cursor ? _segments[_cursor].ToString() : "none")}";
    }
}
