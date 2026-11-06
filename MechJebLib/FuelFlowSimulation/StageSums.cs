/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;

namespace MechJebLib.FuelFlowSimulation
{
    /// <summary>
    ///     One simulation's figures for a whole KSP stage, from that stage's segments. The simulation starts a segment
    ///     whenever the number of running engines changes, so a KSP stage can have several, and not as many in vacuum as in
    ///     the atmosphere (jets don't run in vacuum).
    /// </summary>
    public sealed class StageSums
    {
        /// <summary>The simulation has at least one segment for this stage.</summary>
        public bool Present;

        public double StartMass, EndMass, StagedMass, ControllableMass, Thrust, Isp, DeltaV, DeltaTime, MaxAccel;
        public double MinRcsDeltaV, MaxRcsDeltaV, RcsISP, RcsThrust, RcsMass, RcsDeltaTime, RcsStartTMR, RcsEndTMR, RcsUllageTime;

        private bool _burned;

        public double ResourceMass => StartMass - EndMass;

        public double StartTWR(double geeASL)    => StartMass > 0 ? Thrust / (9.80665 * geeASL * StartMass) : 0;
        public double MaxTWR(double geeASL)      => MaxAccel / (9.80665 * geeASL);
        public double RcsStartTWR(double geeASL) => RcsStartTMR / (9.80665 * geeASL);
        public double RcsMaxTWR(double geeASL)   => RcsEndTMR / (9.80665 * geeASL);

        public void Clear()
        {
            Present = _burned = false;
            StartMass = EndMass = StagedMass = ControllableMass = Thrust = Isp = DeltaV = DeltaTime = MaxAccel = 0;
            MinRcsDeltaV = MaxRcsDeltaV = RcsISP = RcsThrust = RcsMass = RcsDeltaTime = RcsStartTMR = RcsEndTMR = RcsUllageTime = 0;
        }

        /// <summary>
        ///     Adds the stage's next segment, in the order they burn. Δv, time and staged mass add up. The masses, thrust and Isp
        ///     are those at the start of the burn, the end mass at its end, and the max acceleration is the highest.
        ///     The simulation takes some RCS figures once, when a stage starts (an RCS burn at the stage's full mass: min Δv, ISP,
        ///     thrust, mass, time, the TMR at the end of that burn, and ullage), so only the stage's first segment has them. It
        ///     takes the others at the end of each segment (an RCS burn at the mass left: max Δv, the TMR at the start of that
        ///     burn), so the stage's are its last segment's.
        /// </summary>
        public void Add(FuelStats s)
        {
            if (!Present)
            {
                Present          = true;
                StartMass        = s.StartMass;
                ControllableMass = s.ControllableMass;
                Thrust           = s.Thrust;
                Isp              = s.Isp;
                MinRcsDeltaV     = s.MinRcsDeltaV;
                RcsISP           = s.RcsISP;
                RcsThrust        = s.RcsThrust;
                RcsMass          = s.RcsMass;
                RcsDeltaTime     = s.RcsDeltaTime;
                RcsEndTMR        = s.RcsEndTMR;
                RcsUllageTime    = s.RcsUllageTime;
            }

            // thrust and Isp at the start of the burn: a segment with nothing running can come first
            if (!_burned && s.DeltaTime > 0)
            {
                _burned = true;
                Thrust  = s.Thrust;
                Isp     = s.Isp;
            }

            StagedMass   += s.StagedMass;
            DeltaV       += s.DeltaV;
            DeltaTime    += s.DeltaTime;
            MaxAccel     =  Math.Max(MaxAccel, s.MaxAccel);
            EndMass      =  s.EndMass;
            MaxRcsDeltaV =  s.MaxRcsDeltaV;
            RcsStartTMR  =  s.RcsStartTMR;
        }
    }

    /// <summary>A KSP stage's figures in vacuum and in the atmosphere, each from that simulation's own segments.</summary>
    public sealed class KSPStageSums
    {
        public          int       KSPStage;
        public readonly StageSums Vac  = new StageSums();
        public readonly StageSums Atmo = new StageSums();

        /// <summary>The masses and ullage time: the atmosphere's, or the vacuum's for a stage the atmosphere lacks.</summary>
        public StageSums Masses => Atmo.Present ? Atmo : Vac;

        /// <summary>
        ///     Rebuilds <paramref name="stages" /> from the simulations' segment lists: one entry per KSP stage, in the order of
        ///     the lists (the last stage first, the current stage last). The entries are reused from <paramref name="pool" />,
        ///     so a rebuild allocates nothing once both lists have held as many stages.
        /// </summary>
        public static void Build(List<FuelStats> vac, List<FuelStats> atmo, List<KSPStageSums> stages, List<KSPStageSums> pool)
        {
            // not AddRange: on .NET Framework it copies through a new array
            for (int i = 0; i < stages.Count; i++)
                pool.Add(stages[i]);
            stages.Clear();

            // the simulation reverses its segments at the end, so the lists hold them in the reverse of the order they burn:
            // read them from the end, so that each stage's sums see its segments in the order they burn
            for (int i = atmo.Count - 1; i >= 0; i--)
                Entry(stages, pool, atmo[i].KSPStage).Atmo.Add(atmo[i]);
            for (int i = vac.Count - 1; i >= 0; i--)
                Entry(stages, pool, vac[i].KSPStage).Vac.Add(vac[i]);
        }

        // the stage's entry, added where it keeps the stages in increasing order (as the lists are) if it is new
        private static KSPStageSums Entry(List<KSPStageSums> stages, List<KSPStageSums> pool, int kspStage)
        {
            int at = 0;
            for (; at < stages.Count; at++)
            {
                if (stages[at].KSPStage == kspStage)
                    return stages[at];
                if (stages[at].KSPStage > kspStage)
                    break;
            }

            KSPStageSums entry;
            if (pool.Count > 0)
            {
                entry = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
            }
            else
                entry = new KSPStageSums();

            entry.KSPStage = kspStage;
            entry.Vac.Clear();
            entry.Atmo.Clear();
            stages.Insert(at, entry);
            return entry;
        }
    }
}
