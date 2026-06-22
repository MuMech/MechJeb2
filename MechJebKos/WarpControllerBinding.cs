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
    // ADDONS:MECHJEB:WARPCONTROLLER - the low-level time-warp engine (MechJebModuleWarpController).
    //
    // This is the primitive MechJeb uses internally: warp to a UT, or hold a regular/physics warp at a
    // capped rate. It does not resolve orbital events (periapsis, nodes, SoI, ...) -- use
    // ADDONS:MECHJEB:WARPHELPER for that higher-level targeting. The controller is always enabled, so
    // there is no ENABLE/DISENGAGE here; MINIMUMWARP cancels any in-progress warp.
    [KOSNomenclature("MechJebWarpController")]
    public class WarpControllerBinding : ComputerModuleBinding<MechJebModuleWarpController>
    {
        public WarpControllerBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            // --- status ---
            AddSuffix("TARGETUT", new Suffix<ScalarValue>(() => Module.warpToUT,
                "The universal time currently being warped to (0 when not warping to a UT)."));
            AddSuffix("PAUSED", new Suffix<BooleanValue>(() => Module.WarpPaused,
                "True if MechJeb's warp is paused (the user pauses/resumes from the Warp Helper menu)."));

            // --- options ---
            AddSuffix("QUICKWARP", new SetSuffix<BooleanValue>(() => Module.useQuickWarp, value => Module.useQuickWarp = value,
                "Quick-warp mode: jump straight to a high rate instead of ramping up gradually."));
            AddSuffix("ACTIVATESASONWARP", new SetSuffix<BooleanValue>(() => Module.activateSASOnWarp, value => Module.activateSASOnWarp = value,
                "Turn SAS on during regular warp (for compatibility with PersistentRotation)."));

            // --- actions ---
            AddSuffix("WARPTOUT", new OneArgsSuffix<ScalarValue>(ut => Module.WarpToUT(ut),
                "Warp to the given universal time at the maximum available rate."));
            AddSuffix("WARPTOUTATRATE", new TwoArgsSuffix<ScalarValue, ScalarValue>((ut, maxRate) => Module.WarpToUT(ut, maxRate),
                "Warp to the given universal time, capping the warp rate at maxRate."));
            AddSuffix("WARPREGULARATRATE", new OneArgsSuffix<ScalarValue>(maxRate => Module.WarpRegularAtRate((float)maxRate.GetDoubleValue()),
                "Step on-rails (regular) warp one notch toward the highest rate <= maxRate. Call each tick to hold a rate."));
            AddSuffix("WARPPHYSICSATRATE", new OneArgsSuffix<ScalarValue>(maxRate => Module.WarpPhysicsAtRate((float)maxRate.GetDoubleValue()),
                "Step physics warp one notch toward the highest rate <= maxRate. Call each tick to hold a rate."));
            AddSuffix("MINIMUMWARP", new NoArgsVoidSuffix(() => Module.MinimumWarp(),
                "Cancel any in-progress warp and drop back to 1x (minimum) warp."));
        }
    }
}
