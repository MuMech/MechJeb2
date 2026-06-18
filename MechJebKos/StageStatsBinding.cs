/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using MechJebLib.FuelFlowSimulation;
using MechJebLibBindings;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:STAGESTATS - MechJeb's per-stage fuel-flow simulation (MechJebModuleStageStats).
    //
    // The simulation runs as a throttled background job; every read here calls RequestUpdate() to
    // pull the latest completed results and nudge the next run. Vacuum and atmospheric solutions are
    // computed separately, so each stage exposes both (see STAGEDELTAV[i]:VAC / :ATMO).
    //
    // The R/V/U/T snapshot conditions live on each per-stage FuelStats (STAGEDELTAV[i]:VAC:R etc.);
    // the R/V/U vectors are converted from MechJeb's internal simulation frame back to the raw
    // ship-world frame (the same one kOS uses), matching MechJeb's own V3ToWorldRotated.
    [KOSNomenclature("MechJebStageStats")]
    public class StageStatsBinding : ComputerModuleBinding<MechJebModuleStageStats>
    {
        public StageStatsBinding(Func<MechJebCore?> core) : base(core) { }

        // resolve the module and pull/refresh the latest simulation results
        private MechJebModuleStageStats Updated()
        {
            MechJebModuleStageStats module = Module;
            module.RequestUpdate();
            return module;
        }

        // the stage-stats display toggles live on the InfoItems module; re-resolved each call
        private MechJebModuleInfoItems InfoItems => Core.GetComputerModule<MechJebModuleInfoItems>();

        protected override void InitializeSuffixes()
        {
            AddSuffix("STAGEDELTAV", new OneArgsSuffix<FuelStatsPairBinding, ScalarValue>(
                idx => new FuelStatsPairBinding(this, (int)idx),
                "Vacuum/atmosphere fuel-flow stats for the given stage index (0-based, see STAGECOUNT)."));

            AddSuffix("STAGECOUNT", new Suffix<ScalarValue>(() =>
                {
                    MechJebModuleStageStats module = Updated();
                    return Math.Min(module.VacStats.Count, module.AtmoStats.Count);
                },
                "Number of stages valid for STAGEDELTAV indexing (min of vacuum/atmosphere segment counts)."));

            // --- stage-stats display toggles (stored on the InfoItems module) ---
            AddSuffix("SHOWSTAGEDMASS", new SetSuffix<BooleanValue>(() => InfoItems.showStagedMass, v => InfoItems.showStagedMass = v,
                "Show the staged (jettisoned) mass column."));
            AddSuffix("SHOWBURNEDMASS", new SetSuffix<BooleanValue>(() => InfoItems.showBurnedMass, v => InfoItems.showBurnedMass = v,
                "Show the burned (propellant) mass column."));
            AddSuffix("SHOWINITIALMASS", new SetSuffix<BooleanValue>(() => InfoItems.showInitialMass, v => InfoItems.showInitialMass = v,
                "Show the start-mass column."));
            AddSuffix("SHOWFINALMASS", new SetSuffix<BooleanValue>(() => InfoItems.showFinalMass, v => InfoItems.showFinalMass = v,
                "Show the end-mass column."));
            AddSuffix("SHOWTHRUST", new SetSuffix<BooleanValue>(() => InfoItems.showThrust, v => InfoItems.showThrust = v,
                "Show the thrust column."));
            AddSuffix("SHOWRCS", new SetSuffix<BooleanValue>(() => InfoItems.showRcs, v => InfoItems.showRcs = v,
                "Show the RCS columns."));
            AddSuffix("SHOWVACINITIALTWR", new SetSuffix<BooleanValue>(() => InfoItems.showVacInitialTWR, v => InfoItems.showVacInitialTWR = v,
                "Show the vacuum initial-TWR column."));
            AddSuffix("SHOWATMOINITIALTWR", new SetSuffix<BooleanValue>(() => InfoItems.showAtmoInitialTWR, v => InfoItems.showAtmoInitialTWR = v,
                "Show the atmospheric initial-TWR column."));
            AddSuffix("SHOWATMOMAXTWR", new SetSuffix<BooleanValue>(() => InfoItems.showAtmoMaxTWR, v => InfoItems.showAtmoMaxTWR = v,
                "Show the atmospheric max-TWR column."));
            AddSuffix("SHOWVACMAXTWR", new SetSuffix<BooleanValue>(() => InfoItems.showVacMaxTWR, v => InfoItems.showVacMaxTWR = v,
                "Show the vacuum max-TWR column."));
            AddSuffix("SHOWVACDELTAV", new SetSuffix<BooleanValue>(() => InfoItems.showVacDeltaV, v => InfoItems.showVacDeltaV = v,
                "Show the vacuum delta-V column."));
            AddSuffix("SHOWATMODELTAV", new SetSuffix<BooleanValue>(() => InfoItems.showAtmoDeltaV, v => InfoItems.showAtmoDeltaV = v,
                "Show the atmospheric delta-V column."));
            AddSuffix("SHOWVACCUMULATIVEDELTAV", new SetSuffix<BooleanValue>(() => InfoItems.showVacCumulativeDeltaV, v => InfoItems.showVacCumulativeDeltaV = v,
                "Show the vacuum cumulative delta-V column."));
            AddSuffix("SHOWATMOCUMULATIVEDELTAV", new SetSuffix<BooleanValue>(() => InfoItems.showAtmoCumulativeDeltaV, v => InfoItems.showAtmoCumulativeDeltaV = v,
                "Show the atmospheric cumulative delta-V column."));
            AddSuffix("SHOWISP", new SetSuffix<BooleanValue>(() => InfoItems.showISP, v => InfoItems.showISP = v,
                "Show the Isp column."));
            AddSuffix("SHOWTIME", new SetSuffix<BooleanValue>(() => InfoItems.showTime, v => InfoItems.showTime = v,
                "Show the burn-time column."));
            AddSuffix("SHOWCONTROLLABLEMASS", new SetSuffix<BooleanValue>(() => InfoItems.showControllableMass, v => InfoItems.showControllableMass = v,
                "Show the controllable-mass column."));
            AddSuffix("SHOWRCSULLAGETIME", new SetSuffix<BooleanValue>(() => InfoItems.showRcsUllageTime, v => InfoItems.showRcsUllageTime = v,
                "Show the RCS ullage-time column."));
            AddSuffix("SHOWEMPTY", new SetSuffix<BooleanValue>(() => InfoItems.showEmpty, v => InfoItems.showEmpty = v,
                "Show empty stages."));
            AddSuffix("TIMESECONDS", new SetSuffix<BooleanValue>(() => InfoItems.timeSeconds, v => InfoItems.timeSeconds = v,
                "Show burn times in raw seconds rather than formatted time."));
            AddSuffix("LIVESLT", new SetSuffix<BooleanValue>(() => InfoItems.liveSLT, v => InfoItems.liveSLT = v,
                "Compute atmospheric stats at the current conditions (live) rather than at sea level."));
        }

        internal FuelStats GetVacFuelStats(int stage)
        {
            MechJebModuleStageStats module = Updated();
            return IndexOrThrow(module.VacStats, stage, "VacStats");
        }

        internal FuelStats GetAtmoFuelStats(int stage)
        {
            MechJebModuleStageStats module = Updated();
            return IndexOrThrow(module.AtmoStats, stage, "AtmoStats");
        }

        internal SimSnapshot VacConditions()
        {
            MechJebModuleStageStats m = Updated();
            return new SimSnapshot(m.VacR.V3ToWorldRotated(), m.VacV.V3ToWorldRotated(), m.VacU.V3ToWorldRotated(), m.VacT);
        }

        internal SimSnapshot AtmoConditions()
        {
            MechJebModuleStageStats m = Updated();
            return new SimSnapshot(m.AtmoR.V3ToWorldRotated(), m.AtmoV.V3ToWorldRotated(), m.AtmoU.V3ToWorldRotated(), m.AtmoT);
        }

        private static FuelStats IndexOrThrow(System.Collections.Generic.List<FuelStats> list, int stage, string label)
        {
            if (stage < 0 || stage >= list.Count)
                throw new KOSException($"StageStats {label} index {stage} out of range (count={list.Count}).");
            return list[stage];
        }
    }

    // The simulation snapshot conditions (already converted to the ship-world frame) under which a
    // vacuum or atmospheric stage-stats run was computed.
    internal readonly struct SimSnapshot
    {
        public readonly Vector3d R;
        public readonly Vector3d V;
        public readonly Vector3d U;
        public readonly double T;

        public SimSnapshot(Vector3d r, Vector3d v, Vector3d u, double t)
        {
            R = r;
            V = v;
            U = u;
            T = t;
        }
    }
}
