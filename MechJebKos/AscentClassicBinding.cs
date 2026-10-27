/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:ASCENT:CLASSIC - the classic (gravity-turn) ascent autopilot.
    //
    // Shared ascent settings come from the base; this adds the classic-only gravity-turn path and
    // corrective-steering settings. Staging/thrust/node settings live on their own bindings.
    [KOSNomenclature("MechJebAscentClassic")]
    public class AscentClassicBinding : AscentBindingBase<MechJebModuleAscentClassicAutopilot>
    {
        public AscentClassicBinding(Func<MechJebCore?> core) : base(core) { }

        protected override AscentType TargetType => AscentType.CLASSIC;

        protected override EditableDouble InclinationSetting => AscentSettings.DesiredInclination;

        protected override void InitializeTypeSuffixes()
        {
            AddSuffix("DESIREDALTITUDE", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredOrbitAltitude.Val,
                value => AscentSettings.DesiredOrbitAltitude.Val = value, "Target orbit altitude (periapsis) in meters."));
            AddSuffix("INCLINATION", new SetSuffix<ScalarValue>(() => AscentSettings.DesiredInclination.Val,
                value => AscentSettings.DesiredInclination.Val = value, "Target orbital inclination in degrees."));

            AddSuffix("AUTOPATH", new SetSuffix<BooleanValue>(() => AscentSettings.AutoPath,
                value => AscentSettings.AutoPath = value, "Automatically compute the gravity-turn path from the parameters below."));

            AddSuffix("TURNSTARTALTITUDE", new SetSuffix<ScalarValue>(() => AscentSettings.TurnStartAltitude.Val,
                value => AscentSettings.TurnStartAltitude.Val = value, "Altitude (m) at which the gravity turn starts."));
            AddSuffix("TURNSTARTVELOCITY", new SetSuffix<ScalarValue>(() => AscentSettings.TurnStartVelocity.Val,
                value => AscentSettings.TurnStartVelocity.Val = value, "Surface velocity (m/s) at which the gravity turn starts."));
            AddSuffix("TURNENDALTITUDE", new SetSuffix<ScalarValue>(() => AscentSettings.TurnEndAltitude.Val,
                value => AscentSettings.TurnEndAltitude.Val = value, "Altitude (m) at which the gravity turn ends."));
            AddSuffix("TURNENDANGLE", new SetSuffix<ScalarValue>(() => AscentSettings.TurnEndAngle.Val,
                value => AscentSettings.TurnEndAngle.Val = value, "Pitch angle (degrees above horizon) at the end of the gravity turn."));
            AddSuffix("TURNSHAPEEXPONENT", new SetSuffix<ScalarValue>(() => AscentSettings.TurnShapeExponent.Val,
                value => AscentSettings.TurnShapeExponent.Val = value, "Shape exponent of the gravity-turn curve (0-1)."));

            AddSuffix("AUTOTURNPERC", new SetSuffix<ScalarValue>(() => AscentSettings.AutoTurnPerc,
                value => AscentSettings.AutoTurnPerc = (float)value, "Auto-path turn-start altitude as a fraction of the atmosphere depth."));
            AddSuffix("AUTOTURNSPDFACTOR", new SetSuffix<ScalarValue>(() => AscentSettings.AutoTurnSpdFactor,
                value => AscentSettings.AutoTurnSpdFactor = (float)value, "Auto-path turn-start velocity factor."));

            AddSuffix("CORRECTIVESTEERING", new SetSuffix<BooleanValue>(() => AscentSettings.CorrectiveSteering,
                value => AscentSettings.CorrectiveSteering = value, "Enable corrective steering toward the target orbit."));
            AddSuffix("CORRECTIVESTEERINGGAIN", new SetSuffix<ScalarValue>(() => AscentSettings.CorrectiveSteeringGain.Val,
                value => AscentSettings.CorrectiveSteeringGain.Val = value, "Corrective steering gain."));

            AddSuffix("SKIPCIRCULARIZATION", new SetSuffix<BooleanValue>(() => AscentSettings.SkipCircularization,
                value => AscentSettings.SkipCircularization = value, "Skip the circularization burn at apoapsis."));

            AddSuffix("LIMITAOA", new SetSuffix<BooleanValue>(() => AscentSettings.LimitAoA,
                value => AscentSettings.LimitAoA = value, "Enable the classic angle-of-attack limiter."));
            AddSuffix("MAXAOA", new SetSuffix<ScalarValue>(() => AscentSettings.MaxAoA.Val,
                value => AscentSettings.MaxAoA.Val = value, "Maximum angle of attack in degrees (classic limiter)."));
            AddSuffix("AOALIMITFADEOUTPRESSURE", new SetSuffix<ScalarValue>(() => AscentSettings.AOALimitFadeoutPressure.Val,
                value => AscentSettings.AOALimitFadeoutPressure.Val = value, "Dynamic pressure (Pa) at which the classic AoA limit fades out."));
        }
    }
}
