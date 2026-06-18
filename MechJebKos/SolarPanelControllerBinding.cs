/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:SOLARPANELCONTROLLER - drives MechJebModuleSolarPanelController.
    [KOSNomenclature("MechJebSolarPanelController")]
    public class SolarPanelControllerBinding : DeployableControllerBinding<MechJebModuleSolarPanelController>
    {
        public SolarPanelControllerBinding(Func<MechJebCore?> core) : base(core) { }
    }
}
