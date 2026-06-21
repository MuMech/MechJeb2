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

        protected override void InitializeTypeSuffixes()
        {
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
        }
    }
}
