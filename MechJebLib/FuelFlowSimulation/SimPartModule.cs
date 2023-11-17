/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;

namespace MechJebLib.FuelFlowSimulation
{
    public abstract class SimPartModule : IDisposable
    {
        public bool    IsEnabled;
        public SimPart Part = null!;
        public bool    ModuleIsEnabled;
        public bool    StagingEnabled;

        public abstract void Dispose();
    }
}
