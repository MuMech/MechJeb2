/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using MechJebLib.FuelFlowSimulation;

namespace MuMech.MechJebKos
{
    // The per-stage, per-condition (vacuum or atmospheric) fuel-flow stats for one simulated stage,
    // reached via ADDONS:MECHJEB:STAGESTATS:STAGEDELTAV[i]:VAC / :ATMO.
    //
    // FuelStats is a value type recomputed by the background simulation; the snapshot delegate is
    // re-invoked on every suffix read so the values stay live (and re-validate the stage index).
    // The conditions delegate carries the R/V/U/T the simulation that produced these stats was run
    // under (the vacuum or atmospheric snapshot, matching this object's branch of the pair).
    [KOSNomenclature("MechJebFuelStats")]
    public class FuelStatsBinding : Structure
    {
        private readonly Func<FuelStats> _snapshot;
        private readonly Func<SimSnapshot> _conditions;

        internal FuelStatsBinding(Func<FuelStats> snapshot, Func<SimSnapshot> conditions)
        {
            _snapshot = snapshot;
            _conditions = conditions;
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            // --- simulation snapshot conditions ---
            AddSuffix("T", new Suffix<ScalarValue>(() => _conditions().T,
                "Universal time the simulation that produced these stats was run."));
            AddSuffix("R", new Suffix<Vector>(() => new Vector(_conditions().R),
                "Body-centric position the simulation that produced these stats used."));
            AddSuffix("V", new Suffix<Vector>(() => new Vector(_conditions().V),
                "Velocity the simulation that produced these stats used."));
            AddSuffix("U", new Suffix<Vector>(() => new Vector(_conditions().U),
                "Forward direction the simulation that produced these stats used."));

            AddSuffix("KSPSTAGE", new Suffix<ScalarValue>(() => _snapshot().KSPStage,
                "KSP stage number this segment corresponds to."));
            AddSuffix("STARTTIME", new Suffix<ScalarValue>(() => _snapshot().StartTime,
                "Burn time elapsed before this stage starts, in seconds."));
            AddSuffix("DELTATIME", new Suffix<ScalarValue>(() => _snapshot().DeltaTime,
                "Burn time of this stage in seconds."));
            AddSuffix("DELTAV", new Suffix<ScalarValue>(() => _snapshot().DeltaV,
                "Delta-V of this stage in m/s."));
            AddSuffix("ISP", new Suffix<ScalarValue>(() => _snapshot().Isp,
                "Specific impulse of this stage in seconds."));
            AddSuffix("STARTMASS", new Suffix<ScalarValue>(() => _snapshot().StartMass,
                "Vessel mass at the start of this stage in tonnes."));
            AddSuffix("ENDMASS", new Suffix<ScalarValue>(() => _snapshot().EndMass,
                "Vessel mass at the end of this stage in tonnes."));
            AddSuffix("STAGEDMASS", new Suffix<ScalarValue>(() => _snapshot().StagedMass,
                "Mass jettisoned when this stage is staged, in tonnes."));
            AddSuffix("RESOURCEMASS", new Suffix<ScalarValue>(() => _snapshot().ResourceMass,
                "Propellant mass consumed during this stage in tonnes."));
            AddSuffix("CONTROLLABLEMASS", new Suffix<ScalarValue>(() => _snapshot().ControllableMass,
                "Controllable (RP-1 avionics) mass for this stage in tonnes."));

            AddSuffix("THRUST", new Suffix<ScalarValue>(() => _snapshot().Thrust,
                "Thrust of this stage in kN."));
            AddSuffix("MINTHRUST", new Suffix<ScalarValue>(() => _snapshot().MinThrust,
                "Minimum thrust of this stage in kN."));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => _snapshot().MaxThrust,
                "Maximum thrust of this stage in kN."));
            AddSuffix("MAXACCEL", new Suffix<ScalarValue>(() => _snapshot().MaxAccel,
                "Maximum acceleration of this stage in m/s^2."));
            AddSuffix("SPOOLUPTIME", new Suffix<ScalarValue>(() => _snapshot().SpoolUpTime,
                "Engine spool-up time for this stage in seconds."));

            AddSuffix("MAXRCSDELTAV", new Suffix<ScalarValue>(() => _snapshot().MaxRcsDeltaV,
                "Maximum RCS delta-V for this stage in m/s."));
            AddSuffix("MINRCSDELTAV", new Suffix<ScalarValue>(() => _snapshot().MinRcsDeltaV,
                "Minimum RCS delta-V for this stage in m/s."));
            AddSuffix("RCSISP", new Suffix<ScalarValue>(() => _snapshot().RcsISP,
                "RCS specific impulse for this stage in seconds."));
            AddSuffix("RCSDELTATIME", new Suffix<ScalarValue>(() => _snapshot().RcsDeltaTime,
                "RCS burn time for this stage in seconds."));
            AddSuffix("RCSTHRUST", new Suffix<ScalarValue>(() => _snapshot().RcsThrust,
                "RCS thrust for this stage in kN."));
            AddSuffix("RCSMASS", new Suffix<ScalarValue>(() => _snapshot().RcsMass,
                "RCS propellant mass for this stage in tonnes."));
            AddSuffix("RCSSTARTTMR", new Suffix<ScalarValue>(() => _snapshot().RcsStartTMR,
                "RCS thrust-to-mass ratio at the start of this stage."));
            AddSuffix("RCSENDTMR", new Suffix<ScalarValue>(() => _snapshot().RcsEndTMR,
                "RCS thrust-to-mass ratio at the end of this stage."));
            AddSuffix("RCSULLAGETIME", new Suffix<ScalarValue>(() => _snapshot().RcsUllageTime,
                "RCS ullage burn time for this stage in seconds."));
        }
    }
}
