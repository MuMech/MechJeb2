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
    // Shared base for the classic and PSG ascent bindings. Holds the settings common to both ascent
    // styles plus the engage orchestration: engaging first flips MechJeb (and the ascent window) to
    // this binding's ascent type, then adds to the autopilot's Users pool exactly like the GUI's
    // Engage button. Staging/thrust/node settings are intentionally NOT exposed here -- use
    // ADDONS:MECHJEB:STAGINGCONTROLLER / :THRUSTCONTROLLER / :NODEEXECUTOR for those.
    [KOSNomenclature("MechJebAscentBase")]
    public abstract class AscentBindingBase<TAutopilot> : ComputerModuleBinding<TAutopilot>
        where TAutopilot : MechJebModuleAscentBaseAutopilot
    {
        protected AscentBindingBase(Func<MechJebCore?> core) : base(core) { }

        protected abstract AscentType TargetType { get; }

        protected MechJebModuleAscentSettings AscentSettings => Core.AscentSettings;

        // Set the ascent type first (this disables the other autopilot via AscentSettings'
        // DisableAscentModules) and line the ascent window up to match, then engage this type.
        internal void Engage()
        {
            AscentSettings.AscentType = TargetType;
            Core.GetComputerModule<MechJebModuleAscentMenu>()._lastPSGSettingsEnabled = TargetType == AscentType.PSG;
            SetUsersEnabled(true);
        }

        internal void Disengage() => SetUsersEnabled(false);

        protected override void InitializeSuffixes()
        {
            // --- engage / status ---
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value => { if (value) Engage(); else Disengage(); },
                "Whether this ascent autopilot is engaged. Setting true flips MechJeb to this ascent type and engages it."));
            AddSuffix("ENGAGE", new NoArgsVoidSuffix(Engage,
                "Flip MechJeb to this ascent type and engage the autopilot immediately."));
            AddSuffix("DISENGAGE", new NoArgsVoidSuffix(Disengage,
                "Disengage this ascent autopilot."));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Module.Status,
                "Current ascent autopilot status text."));

            // --- target orbit ---
            AddSuffix("DESIREDALTITUDE", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredOrbitAltitude.Val,
                value => AscentSettings.DesiredOrbitAltitude.Val = value, "Target orbit altitude (periapsis) in meters."));
            AddSuffix("INCLINATION", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredInclination.Val,
                value => AscentSettings.DesiredInclination.Val = value, "Target orbital inclination in degrees."));
            AddSuffix("LAN", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredLan.Val,
                value => AscentSettings.DesiredLan.Val = value, "Target longitude of the ascending node in degrees."));
            AddSuffix("RELATIVELAN", new SetSuffix<BooleanValue>(() => AscentSettings.RelativeLAN,
                value => AscentSettings.RelativeLAN = value, "Interpret LAN relative to the launch site."));

            // --- staging coordination (the autostage toggle the ascent owns) ---
            AddSuffix("AUTOSTAGE", new SetSuffix<BooleanValue>(() => AscentSettings.Autostage,
                value => AscentSettings.Autostage = value, "Auto-stage during ascent (see STAGINGCONTROLLER for staging details)."));

            // --- deployables / circularization ---
            AddSuffix("AUTODEPLOYSOLARPANELS", new SetSuffix<BooleanValue>(() => AscentSettings.AutoDeploySolarPanels,
                value => AscentSettings.AutoDeploySolarPanels = value, "Automatically deploy solar panels after leaving the atmosphere."));
            AddSuffix("AUTODEPLOYANTENNAS", new SetSuffix<BooleanValue>(() => AscentSettings.AutoDeployAntennas,
                value => AscentSettings.AutoDeployAntennas = value, "Automatically deploy antennas after leaving the atmosphere."));
            AddSuffix("SKIPCIRCULARIZATION", new SetSuffix<BooleanValue>(() => AscentSettings.SkipCircularization,
                value => AscentSettings.SkipCircularization = value, "Skip the circularization burn at apoapsis."));

            // --- roll profile ---
            AddSuffix("FORCEROLL", new SetSuffix<BooleanValue>(() => AscentSettings.ForceRoll,
                value => AscentSettings.ForceRoll = value, "Control roll during ascent."));
            AddSuffix("VERTICALROLL", new SetSuffix<ScalarValue>(() => AscentSettings.VerticalRoll.Val,
                value => AscentSettings.VerticalRoll.Val = value, "Roll angle during the vertical climb in degrees."));
            AddSuffix("TURNROLL", new SetSuffix<ScalarValue>(() => AscentSettings.TurnRoll.Val,
                value => AscentSettings.TurnRoll.Val = value, "Roll angle during the gravity turn in degrees."));
            AddSuffix("ROLLALTITUDE", new SetSuffix<ScalarValue>(() => AscentSettings.RollAltitude.Val,
                value => AscentSettings.RollAltitude.Val = value, "Altitude (m) at which the roll program begins."));

            // --- AoA limiters ---
            AddSuffix("LIMITAOA", new SetSuffix<BooleanValue>(() => AscentSettings.LimitAoA,
                value => AscentSettings.LimitAoA = value, "Enable the classic angle-of-attack limiter."));
            AddSuffix("MAXAOA", new SetSuffix<ScalarValue>(() => AscentSettings.MaxAoA.Val,
                value => AscentSettings.MaxAoA.Val = value, "Maximum angle of attack in degrees (classic limiter)."));
            AddSuffix("AOALIMITFADEOUTPRESSURE", new SetSuffix<ScalarValue>(() => AscentSettings.AOALimitFadeoutPressure.Val,
                value => AscentSettings.AOALimitFadeoutPressure.Val = value, "Dynamic pressure (Pa) at which the classic AoA limit fades out."));
            AddSuffix("LIMITQA", new SetSuffix<ScalarValue>(() => AscentSettings.LimitQa.Val,
                value => AscentSettings.LimitQa.Val = value, "Q-alpha (dynamic-pressure x AoA) limit for the ascent."));
            AddSuffix("LIMITQAENABLED", new SetSuffix<BooleanValue>(() => AscentSettings.LimitQaEnabled,
                value => AscentSettings.LimitQaEnabled = value, "Enable the q-alpha limiter."));

            // --- timed launch ---
            AddSuffix("WARPCOUNTDOWN", new SetSuffix<ScalarValue>(() => AscentSettings.WarpCountDown.Val,
                value => AscentSettings.WarpCountDown.Val = (int)value, "Seconds before a timed launch to stop warping."));
            AddSuffix("LAUNCHLANDIFFERENCE", new SetSuffix<ScalarValue>(() => AscentSettings.LaunchLANDifference.Val,
                value => AscentSettings.LaunchLANDifference.Val = value, "LAN offset (degrees) applied to launch-into-plane timing."));
            AddSuffix("OVERRIDEWARPTOPLANE", new SetSuffix<BooleanValue>(() => AscentSettings.OverrideWarpToPlane,
                value => AscentSettings.OverrideWarpToPlane = value, "Launch immediately instead of warping to the target plane."));
            AddSuffix("LAUNCHINTOPLANE", new NoArgsSuffix<ScalarValue>(LaunchIntoPlane,
                "Engage and start a timed launch into the plane of the current target (requires a target in the same SoI). Returns the scheduled launch UT."));

            InitializeTypeSuffixes();
        }

        protected abstract void InitializeTypeSuffixes();

        private ScalarValue LaunchIntoPlane()
        {
            Orbit targetOrbit = Core.Target.TargetOrbit;
            if (!Core.Target.NormalTargetExists || targetOrbit == null || targetOrbit.referenceBody != Core.vessel.mainBody)
                throw new KOSException("Launch into plane requires a target in the same sphere of influence.");

            Engage();
            AscentSettings.LaunchingToPlane = true;

            VesselState vs = Core.VesselState;
            (double time, double inclination) = Astro.MinimumTimeToPlane(
                Core.vessel.mainBody.rotationPeriod,
                vs.Latitude,
                vs.CelestialLongitude,
                targetOrbit.LAN - AscentSettings.LaunchLANDifference,
                targetOrbit.inclination);

            AscentSettings.DesiredInclination.Val = inclination;
            double launchUT = vs.Time + time;
            Module.StartCountdown(launchUT);
            return launchUT;
        }
    }
}
