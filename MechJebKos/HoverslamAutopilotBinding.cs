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
    // ADDONS:MECHJEB:HOVERSLAMAUTOPILOT - drives MechJebModuleHoverslamAutopilot.
    //
    // This one engages through its own Enabled flag (not the Users pool), and can be enabled before
    // the simulation has a solution - it idles in its Init state and starts flying once one appears.
    [KOSNomenclature("MechJebHoverslamAutopilot")]
    public class HoverslamAutopilotBinding : ComputerModuleBinding<MechJebModuleHoverslamAutopilot>
    {
        public HoverslamAutopilotBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value => Module.Enabled = value,
                "Engage the hoverslam autopilot. May be enabled before a solution exists; it starts flying once one appears."));
            AddSuffix("STATE", new Suffix<StringValue>(() => Module.HoverslamState,
                "Autopilot state: Disabled, Init, Align, Coast, Burn, Vertical, or Finished."));
            AddSuffix("IGNITIONLEAD", new SetSuffix<ScalarValue>(() => Module.IgnitionLead.Val, value => Module.IgnitionLead.Val = value,
                "Seconds before the computed ignition time to start the burn."));
            AddSuffix("TOUCHDOWNSPEED", new SetSuffix<ScalarValue>(() => Module.TouchdownSpeed.Val, value => Module.TouchdownSpeed.Val = value,
                "Target vertical speed at touchdown in m/s."));
            AddSuffix("AUTOWARP", new SetSuffix<BooleanValue>(() => Module.AutoWarp, value => Module.AutoWarp = value,
                "Automatically time-warp to the ignition time."));
            AddSuffix("HOLDUPRIGHT", new SetSuffix<BooleanValue>(() => Module.HoldUpright, value => Module.HoldUpright = value,
                "Hold the vessel upright (via SmartASS) after touchdown."));
            AddSuffix("PWMPULSEWIDTH", new SetSuffix<ScalarValue>(() => Module.PWMPulseWidth.Val, value => Module.PWMPulseWidth.Val = value,
                "Minimum PWM on-time in seconds for throttle pulsing during final descent."));
        }
    }
}
