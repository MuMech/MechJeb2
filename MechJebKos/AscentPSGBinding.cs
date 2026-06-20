/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using MechJebLib.Functions;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:ASCENT:PSG - the PSG (powered-explicit-guidance) ascent autopilot.
    //
    // Shared ascent settings come from the base; this adds the PSG-only targeting (apoapsis / attach
    // altitude / flight-path-angle / argument-of-periapsis), guidance tuning, and coast/spinup
    // staging, plus the LAN-targeted timed launches (which MechJeb only offers for PSG).
    //
    // NOTE: the PSG unguided/fixed stage *lists* (EditableIntList) are not yet exposed -- they need a
    // kOS list wrapper. The scalar coast/spinup stage settings are here.
    [KOSNomenclature("MechJebAscentPSG")]
    public class AscentPSGBinding : AscentBindingBase<MechJebModuleAscentPSGAutopilot>
    {
        private const double DEG2RAD = Math.PI / 180.0;
        private const double RAD2DEG = 180.0 / Math.PI;

        public AscentPSGBinding(Func<MechJebCore?> core) : base(core) { }

        protected override AscentType TargetType => AscentType.PSG;

        protected override void InitializeTypeSuffixes()
        {
            // --- target orbit (PSG) ---
            AddSuffix("DESIREDAPOAPSIS", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredApoapsis.Val,
                value => AscentSettings.DesiredApoapsis.Val = value, "Target apoapsis altitude in meters."));
            AddSuffix("ATTACHALT", new SetSuffix<BooleanValue>(() => AscentSettings.AttachAltFlag,
                value => AscentSettings.AttachAltFlag = value, "Attach the guidance solution at a specified altitude."));
            AddSuffix("ATTACHALTITUDE", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredAttachAlt.Val,
                value => AscentSettings.DesiredAttachAlt.Val = value, "Altitude (m) at which to attach the guidance solution (optimized stages)."));
            AddSuffix("ATTACHALTITUDEFIXED", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredAttachAltFixed.Val,
                value => AscentSettings.DesiredAttachAltFixed.Val = value, "Altitude (m) at which to attach the guidance solution (fixed stages)."));
            AddSuffix("FPA", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredFPA.Val * RAD2DEG,
                value => AscentSettings.DesiredFPA.Val = value * DEG2RAD, "Target flight path angle at attach altitude in degrees."));
            AddSuffix("ARGP", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredArgP.Val * RAD2DEG,
                value => AscentSettings.DesiredArgP.Val = value * DEG2RAD, "Target argument of periapsis in degrees."));
            AddSuffix("ARGPFLAG", new SetSuffix<BooleanValue>(() => AscentSettings.DesiredArgPFlag,
                value => AscentSettings.DesiredArgPFlag = value, "Target a specific argument of periapsis."));
            AddSuffix("OPTIMIZESTAGE", new SetSuffix<BooleanValue>(() => AscentSettings.OptimizeStageFlag,
                value => AscentSettings.OptimizeStageFlag = value, "Let PSG optimize the final stage burn (vs. fixed attach altitude)."));

            // --- guidance tuning ---
            AddSuffix("PITCHSTARTHEIGHT", new SetSuffix<ScalarValue>(() => AscentSettings.PitchStartHeight.Val,
                value => AscentSettings.PitchStartHeight.Val = value, "Height (m) at which the initial pitch-over starts."));
            AddSuffix("PITCHRATE", new SetSuffix<ScalarValue>(() => AscentSettings.PitchRate.Val,
                value => AscentSettings.PitchRate.Val = value, "Initial pitch-over rate in degrees/second."));
            AddSuffix("CD", new SetSuffix<ScalarValue>(() => AscentSettings.Cd.Val,
                value => AscentSettings.Cd.Val = value, "Drag coefficient used by the guidance model."));
            AddSuffix("AREF", new SetSuffix<ScalarValue>(() => AscentSettings.Aref.Val,
                value => AscentSettings.Aref.Val = value, "Reference area (m^2) used by the guidance model (0 = auto)."));

            // --- coast ---
            AddSuffix("MINDELTAV", new SetSuffix<ScalarValue>(() => AscentSettings.MinDeltaV.Val,
                value => AscentSettings.MinDeltaV.Val = value, "Minimum stage delta-V (m/s) before PSG will coast."));
            AddSuffix("MAXCOAST", new SetSuffix<ScalarValue>(() => AscentSettings.MaxCoast.Val,
                value => AscentSettings.MaxCoast.Val = value, "Maximum coast duration in seconds."));
            AddSuffix("MINCOAST", new SetSuffix<ScalarValue>(() => AscentSettings.MinCoast.Val,
                value => AscentSettings.MinCoast.Val = value, "Minimum coast duration in seconds."));
            AddSuffix("COASTSTAGE", new SetSuffix<ScalarValue>(() => AscentSettings.CoastStageInternal.Val,
                value => AscentSettings.CoastStageInternal.Val = (int)value, "KSP stage index at which to insert the coast."));
            AddSuffix("COASTSTAGEFLAG", new SetSuffix<BooleanValue>(() => AscentSettings.CoastStageFlag,
                value => AscentSettings.CoastStageFlag = value, "Enable the fixed coast stage (else PSG picks automatically)."));

            // --- spinup ---
            AddSuffix("SPINUPSTAGE", new SetSuffix<ScalarValue>(() => AscentSettings.SpinupStageInternal.Val,
                value => AscentSettings.SpinupStageInternal.Val = (int)value, "KSP stage index to spin up (spin-stabilized upper stages)."));
            AddSuffix("SPINUPSTAGEFLAG", new SetSuffix<BooleanValue>(() => AscentSettings.SpinupStageFlag,
                value => AscentSettings.SpinupStageFlag = value, "Enable the spinup stage."));
            AddSuffix("SPINUPLEADTIME", new SetSuffix<ScalarValue>(() => AscentSettings.SpinupLeadTime.Val,
                value => AscentSettings.SpinupLeadTime.Val = value, "Lead time (s) before staging to begin spinup."));
            AddSuffix("SPINUPANGULARVELOCITY", new SetSuffix<ScalarValue>(() => AscentSettings.SpinupAngularVelocity.Val,
                value => AscentSettings.SpinupAngularVelocity.Val = value, "Target spin rate in radians/second."));

            // --- PSG-only timed launches ---
            AddSuffix("LAUNCHTOLAN", new OneArgsSuffix<ScalarValue, ScalarValue>(LaunchToLan,
                "Engage and start a timed launch to the given LAN (degrees). Returns the scheduled launch UT."));
            AddSuffix("LAUNCHTOTARGETLAN", new NoArgsSuffix<ScalarValue>(LaunchToTargetLan,
                "Engage and start a timed launch matching the current target's LAN (requires a target in the same SoI). Returns the scheduled launch UT."));
        }

        private ScalarValue LaunchToLan(ScalarValue degrees)
        {
            Engage();
            AscentSettings.LaunchingToLan = true;
            AscentSettings.DesiredLan.Val = degrees;

            VesselState vs = Core.VesselState;
            double time = Astro.TimeToPlane(
                Core.vessel.mainBody.rotationPeriod,
                vs.Latitude,
                vs.CelestialLongitude,
                AscentSettings.DesiredLan,
                AscentSettings.DesiredInclination);

            double launchUT = vs.Time + time;
            Module.StartCountdown(launchUT);
            return launchUT;
        }

        private ScalarValue LaunchToTargetLan()
        {
            Orbit targetOrbit = Core.Target.TargetOrbit;
            if (!Core.Target.NormalTargetExists || targetOrbit == null || targetOrbit.referenceBody != Core.vessel.mainBody)
                throw new KOSException("Launch to target LAN requires a target in the same sphere of influence.");

            Engage();
            AscentSettings.LaunchingToMatchLan = true;

            VesselState vs = Core.VesselState;
            double time = Astro.TimeToPlane(
                Core.vessel.mainBody.rotationPeriod,
                vs.Latitude,
                vs.CelestialLongitude,
                targetOrbit.LAN - AscentSettings.LaunchLANDifference,
                AscentSettings.DesiredInclination);

            double launchUT = vs.Time + time;
            Module.StartCountdown(launchUT);
            return launchUT;
        }
    }
}
