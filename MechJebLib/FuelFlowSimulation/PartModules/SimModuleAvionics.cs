/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Utils;

namespace MechJebLib.FuelFlowSimulation.PartModules
{
    // this handles ControllableMass for both ModuleAvionics and ModuleProceduralAvionics
    public class SimModuleAvionics : SimPartModule
    {
        private static readonly ObjectPool<SimModuleAvionics> _pool = new ObjectPool<SimModuleAvionics>(New, m => m.Clear());

        public double ControllableMass;

        public override void Dispose() => _pool.Release(this);

        public static SimModuleAvionics Borrow(SimPart part)
        {
            SimModuleAvionics avionics = _pool.Borrow();
            avionics.Part = part;
            return avionics;
        }

        protected override void Clear()
        {
            base.Clear();
            // the builder only sets this when RP-0 is loaded correctly
            ControllableMass = 0;
        }

        private static SimModuleAvionics New() => new SimModuleAvionics();

        public override string ToString()
        {
            List<string> fields = CommonFieldList();
            AddField(fields, "ControllableMass", ControllableMass, 0);
            return ModuleLine("SimModuleAvionics", fields);
        }
    }
}
