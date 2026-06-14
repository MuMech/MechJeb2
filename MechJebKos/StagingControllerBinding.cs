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
    // ADDONS:MECHJEB:STAGINGCONTROLLER - drives MechJebModuleStagingController.
    [KOSNomenclature("MechJebStagingController")]
    public class StagingControllerBinding : ComputerModuleBinding<MechJebModuleStagingController>
    {
        public StagingControllerBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value => SetUsersEnabled(value),
                "Whether continuous autostaging is engaged."));
            AddSuffix("AUTOSTAGEPREDELAY", new SetSuffix<ScalarValue>(() => Module.AutostagePreDelay.Val, value => Module.AutostagePreDelay.Val = value,
                "Seconds to wait before firing a stage."));
            AddSuffix("AUTOSTAGEPOSTDELAY", new SetSuffix<ScalarValue>(() => Module.AutostagePostDelay.Val, value => Module.AutostagePostDelay.Val = value,
                "Seconds to wait after firing a stage."));
            AddSuffix("AUTOSTAGELIMIT", new SetSuffix<ScalarValue>(() => Module.AutostageLimit.Val, value => Module.AutostageLimit.Val = (int)value,
                "Stop autostaging at this stage number."));
            AddSuffix("FAIRINGMAXDYNAMICPRESSURE", new SetSuffix<ScalarValue>(() => Module.FairingMaxDynamicPressure.Val,
                value => Module.FairingMaxDynamicPressure.Val = value,
                "Max dynamic pressure in Pa for fairing deployment."));
            AddSuffix("FAIRINGMINALTITUDE", new SetSuffix<ScalarValue>(() => Module.FairingMinAltitude.Val, value => Module.FairingMinAltitude.Val = value,
                "Min altitude in meters for fairing deployment."));
            AddSuffix("FAIRINGMAXAEROTHERMALFLUX", new SetSuffix<ScalarValue>(() => Module.FairingMaxAerothermalFlux.Val,
                value => Module.FairingMaxAerothermalFlux.Val = value,
                "Max aerothermal flux in W/m^2 for fairing deployment."));
            AddSuffix("CLAMPAUTOSTAGETHRUSTPCT", new SetSuffix<ScalarValue>(() => Module.ClampAutoStageThrustPct.Val,
                value => Module.ClampAutoStageThrustPct.Val = value,
                "Thrust fraction (0-1) required before releasing launch clamps."));
            AddSuffix("HOTSTAGING", new SetSuffix<BooleanValue>(() => Module.HotStaging, value => Module.HotStaging = value,
                "Support hotstaging."));
            AddSuffix("HOTSTAGINGLEADTIME", new SetSuffix<ScalarValue>(() => Module.HotStagingLeadTime.Val, value => Module.HotStagingLeadTime.Val = value,
                "Seconds before burnout to begin hotstaging."));
            AddSuffix("DROPSOLIDS", new SetSuffix<BooleanValue>(() => Module.DropSolids, value => Module.DropSolids = value,
                "Drop spent solid boosters early."));
            AddSuffix("DROPSOLIDSLEADTIME", new SetSuffix<ScalarValue>(() => Module.DropSolidsLeadTime.Val, value => Module.DropSolidsLeadTime.Val = value,
                "Seconds before burnout to drop solids."));
            AddSuffix("AUTOSTAGINGONCE", new Suffix<BooleanValue>(() => Module.AutostagingOnce,
                "True while a one-shot autostage is pending."));
            AddSuffix("AUTOSTAGEONCE", new NoArgsVoidSuffix(() => Module.AutostageOnce(this),
                "Autostage the next stage only."));
            AddSuffix("STAGE", new NoArgsVoidSuffix(() => Module.Stage(),
                "Stage, respecting the pre-delay countdown."));
            AddSuffix("IMMEDIATESTAGE", new NoArgsVoidSuffix(() => Module.ImmediateStage(),
                "Stage immediately, bypassing the pre-delay."));
        }

    }
}
