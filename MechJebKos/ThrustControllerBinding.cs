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
    // ADDONS:MECHJEB:THRUSTCONTROLLER - the throttle limiters / safety settings on MechJebModuleThrustController.
    //
    // Translatron support is omitted in favor of a future Binding around the TranslatronController.
    // Differential throttle suport is omitted for now.
    [KOSNomenclature("MechJebThrustController")]
    public class ThrustControllerBinding : ComputerModuleBinding<MechJebModuleThrustController>
    {
        public ThrustControllerBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            // NO ENABLED SUFFIX: this one is always-enabled.
            AddSuffix("LIMITDYNAMICPRESSURE", new SetSuffix<BooleanValue>(() => Module.LimitDynamicPressure, value => Module.LimitDynamicPressure = value,
                "Toggle to limit throttle to keep dynamic pressure under MAXDYNAMICPRESSURE."));
            AddSuffix("MAXDYNAMICPRESSURE", new SetSuffix<ScalarValue>(() => Module.MaxDynamicPressure.Val, value => Module.MaxDynamicPressure.Val = value,
                "Maximum dynamic pressure in Pa."));
            AddSuffix("LIMITTOPREVENTOVERHEATS", new SetSuffix<BooleanValue>(() => Module.LimitToPreventOverheats, value => Module.LimitToPreventOverheats = value,
                "Toggle to limit throttle to prevent engine overheats."));
            AddSuffix("LIMITACCELERATION", new SetSuffix<BooleanValue>(() => Module.LimitAcceleration, value => Module.LimitAcceleration = value,
                "Toggle to limit throttle to keep acceleration under MAXACCELERATION."));
            AddSuffix("MAXACCELERATION", new SetSuffix<ScalarValue>(() => Module.MaxAcceleration.Val, value => Module.MaxAcceleration.Val = value,
                "Maximum acceleration in m/s^2."));
            AddSuffix("LIMITTHROTTLE", new SetSuffix<BooleanValue>(() => Module.LimitThrottle, value => Module.LimitThrottle = value,
                "Toggle to limit throttle to at most MAXTHROTTLE."));
            AddSuffix("MAXTHROTTLE", new SetSuffix<ScalarValue>(() => Module.MaxThrottle.Val, value => Module.MaxThrottle.Val = value,
                "Maximum throttle as a fraction (0-1)."));
            AddSuffix("LIMITERMINTHROTTLE", new SetSuffix<BooleanValue>(() => Module.LimiterMinThrottle, value => Module.LimiterMinThrottle = value,
                "Toggle to keep a limited throttle at or above MINTHROTTLE."));
            AddSuffix("MINTHROTTLE", new SetSuffix<ScalarValue>(() => Module.MinThrottle.Val, value => Module.MinThrottle.Val = value,
                "Minimum throttle as a fraction (0-1)."));
            AddSuffix("LIMITTOPREVENTFLAMEOUT", new SetSuffix<BooleanValue>(() => Module.LimitToPreventFlameout, value => Module.LimitToPreventFlameout = value,
                "Toggle to limit throttle to prevent air-breathing flameout."));
            AddSuffix("FLAMEOUTSAFETYPCT", new SetSuffix<ScalarValue>(() => Module.FlameoutSafetyPct.Val, value => Module.FlameoutSafetyPct.Val = value,
                "Flameout intake-air safety margin, in percent."));
            AddSuffix("MANAGEINTAKES", new SetSuffix<BooleanValue>(() => Module.ManageIntakes, value => Module.ManageIntakes = value,
                "Toggle to open and close air intakes automatically."));
            AddSuffix("LIMITTOPREVENTUNSTABLEIGNITION", new SetSuffix<BooleanValue>(() => Module.LimitToPreventUnstableIgnition,
                value => Module.LimitToPreventUnstableIgnition = value,
                "Toggle to kill throttle to prevent unstable (RealFuels) ignitions."));
            AddSuffix("ELECTRICTHROTTLE", new SetSuffix<BooleanValue>(() => Module.ElectricThrottle, value => Module.ElectricThrottle = value,
                "Toggle to limit electric-engine throttle by stored charge."));
            AddSuffix("ELECTRICTHROTTLELO", new SetSuffix<ScalarValue>(() => Module.ElectricThrottleLo.Val, value => Module.ElectricThrottleLo.Val = value,
                "Charge fraction (0-1) at which electric throttle reaches zero."));
            AddSuffix("ELECTRICTHROTTLEHI", new SetSuffix<ScalarValue>(() => Module.ElectricThrottleHi.Val, value => Module.ElectricThrottleHi.Val = value,
                "Charge fraction (0-1) at which electric throttle reaches full."));
            AddSuffix("AUTORCSULLAGING", new SetSuffix<BooleanValue>(() => Module.AutoRCSUllaging, value => Module.AutoRCSUllaging = value,
                "Toggle to use RCS to settle propellant (ullage) before ignition."));
            AddSuffix("SMOOTHTHROTTLE", new SetSuffix<BooleanValue>(() => Module.SmoothThrottle, value => Module.SmoothThrottle = value,
                "Toggle to smooth throttle changes over time."));
            AddSuffix("THROTTLESMOOTHINGTIME", new SetSuffix<ScalarValue>(() => Module.ThrottleSmoothingTime, value => Module.ThrottleSmoothingTime = value,
                "Throttle smoothing time constant in seconds."));
            AddSuffix("LIMITER", new Suffix<StringValue>(() => Module.Limiter.ToString(),
                "The currently active throttle limiter (NONE, THROTTLE, DYNAMIC_PRESSURE, ...)."));
            AddSuffix("THROTTLELIMIT", new Suffix<ScalarValue>(() => Module.ThrottleLimit,
                "The current applied throttle limit as a fraction (0-1), including transient limits."));
            AddSuffix("THROTTLEFIXEDLIMIT", new Suffix<ScalarValue>(() => Module.ThrottleFixedLimit,
                "The current non-transient throttle limit as a fraction (0-1)."));
            AddSuffix("LASTTHROTTLE", new Suffix<ScalarValue>(() => Module.LastThrottle,
                "The throttle actually applied last tick, as a fraction (0-1)."));
        }
    }
}
