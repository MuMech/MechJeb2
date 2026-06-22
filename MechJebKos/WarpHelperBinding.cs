/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:WARPHELPER - the high-level "warp to an orbital event" helper (MechJebModuleWarpHelper).
    //
    // Set TARGET (and, for TIME / PHASEANGLE targets, TIMEOFFSET / PHASEANGLE), then WARP. The helper
    // resolves the target time and drives the low-level controller toward (target - LEADTIME), dropping
    // out of warp on arrival. WARPTO(target) is a convenience that sets TARGET and warps in one call.
    // For warping straight to a known UT or holding a fixed rate, use ADDONS:MECHJEB:WARPCONTROLLER.
    [KOSNomenclature("MechJebWarpHelper")]
    public class WarpHelperBinding : ComputerModuleBinding<MechJebModuleWarpHelper>
    {
        public WarpHelperBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("TARGET", new SetSuffix<StringValue>(() => TargetToString(Module.warpTarget), value => Module.warpTarget = StringToTarget(value),
                "The warp target: PERIAPSIS, APOAPSIS, NODE, SOI, TIME, PHASEANGLE, HOVERSLAMBURN, or ATMOSPHERICENTRY."));
            AddSuffix("LEADTIME", new SetSuffix<ScalarValue>(() => Module.leadTime.Val, value => Module.leadTime.Val = value,
                "Seconds before the target event at which to stop warping."));
            AddSuffix("TIMEOFFSET", new SetSuffix<ScalarValue>(() => Module.timeOffset.Val, value => Module.timeOffset.Val = value,
                "Seconds to warp for when TARGET is TIME."));
            AddSuffix("PHASEANGLE", new SetSuffix<ScalarValue>(() => Module.phaseAngle.Val, value => Module.phaseAngle.Val = value,
                "Target phase angle in degrees (relative to the current target) when TARGET is PHASEANGLE."));

            AddSuffix("WARPING", new Suffix<BooleanValue>(() => Module.warping,
                "True while the helper is actively warping toward its target."));

            AddSuffix("WARP", new NoArgsVoidSuffix(() => Module.StartWarp(),
                "Resolve the current TARGET and begin warping toward it."));
            AddSuffix("WARPTO", new OneArgsSuffix<StringValue>(target => { Module.warpTarget = StringToTarget(target); Module.StartWarp(); },
                "Set TARGET to the given event and begin warping toward it in one call."));
            AddSuffix("ABORT", new NoArgsVoidSuffix(() => Module.AbortWarp(),
                "Stop warping and drop back to 1x warp."));
        }

        private static string TargetToString(MechJebModuleWarpHelper.WarpTarget target)
        {
            switch (target)
            {
                case MechJebModuleWarpHelper.WarpTarget.Periapsis:        return "PERIAPSIS";
                case MechJebModuleWarpHelper.WarpTarget.Apoapsis:         return "APOAPSIS";
                case MechJebModuleWarpHelper.WarpTarget.Node:             return "NODE";
                case MechJebModuleWarpHelper.WarpTarget.SoI:              return "SOI";
                case MechJebModuleWarpHelper.WarpTarget.Time:             return "TIME";
                case MechJebModuleWarpHelper.WarpTarget.PhaseAngleT:      return "PHASEANGLE";
                case MechJebModuleWarpHelper.WarpTarget.HoverslamBurn:    return "HOVERSLAMBURN";
                case MechJebModuleWarpHelper.WarpTarget.AtmosphericEntry: return "ATMOSPHERICENTRY";
                default:                                                  return target.ToString().ToUpperInvariant();
            }
        }

        private static MechJebModuleWarpHelper.WarpTarget StringToTarget(string value)
        {
            switch (value.Trim().ToUpperInvariant())
            {
                case "PERIAPSIS":        return MechJebModuleWarpHelper.WarpTarget.Periapsis;
                case "APOAPSIS":         return MechJebModuleWarpHelper.WarpTarget.Apoapsis;
                case "NODE":             return MechJebModuleWarpHelper.WarpTarget.Node;
                case "SOI":              return MechJebModuleWarpHelper.WarpTarget.SoI;
                case "TIME":             return MechJebModuleWarpHelper.WarpTarget.Time;
                case "PHASEANGLE":       return MechJebModuleWarpHelper.WarpTarget.PhaseAngleT;
                case "HOVERSLAMBURN":    return MechJebModuleWarpHelper.WarpTarget.HoverslamBurn;
                case "ATMOSPHERICENTRY": return MechJebModuleWarpHelper.WarpTarget.AtmosphericEntry;
                default:
                    throw new KOSException(
                        $"Unknown warp target '{value}'. Expected one of: PERIAPSIS, APOAPSIS, NODE, SOI, TIME, PHASEANGLE, HOVERSLAMBURN, ATMOSPHERICENTRY.");
            }
        }
    }
}
