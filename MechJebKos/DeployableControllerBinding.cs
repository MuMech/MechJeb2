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
    // Shared binding for MechJebModuleDeployableController subclasses (antennas, solar panels).
    // These are always-on services gated by AUTODEPLOY, so there is deliberately no ENABLED suffix.
    [KOSNomenclature("MechJebDeployableController")]
    public abstract class DeployableControllerBinding<T> : ComputerModuleBinding<T> where T : MechJebModuleDeployableController
    {
        protected DeployableControllerBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("AUTODEPLOY", new SetSuffix<BooleanValue>(() => Module.AutoDeploy, value => Module.AutoDeploy = value,
                "Automatically extend/retract the controlled parts based on flight conditions."));
            AddSuffix("EXTENDED", new Suffix<BooleanValue>(() => !Module.AllRetracted(),
                "True if any controlled part is extended or extending."));
            AddSuffix("EXTEND", new NoArgsVoidSuffix(() => Module.ExtendAll(),
                "Extend all controlled parts."));
            AddSuffix("RETRACT", new NoArgsVoidSuffix(() => Module.RetractAll(),
                "Retract all controlled parts."));
        }
    }
}
