/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Utils;

namespace MechJebLib.FuelFlowSimulation.PartModules
{
    // this handles ControllableMass for both ModuleAvionics and ModuleProceduralAvionics
    public class SimModuleAvionics : SimPartModule
    {
        private static readonly ObjectPool<SimModuleAvionics> _pool = new ObjectPool<SimModuleAvionics>(New, Clear);

        public double ControllableMass;

        public override void Dispose() => _pool.Release(this);

        public static SimModuleAvionics Borrow(SimPart part)
        {
            SimModuleAvionics avionics = _pool.Borrow();
            avionics.Part = part;
            return avionics;
        }

        private static SimModuleAvionics New() => new SimModuleAvionics();

        private static void Clear(SimModuleAvionics m)
        {
        }
    }
}
