/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.FuelFlowSimulation.PartModules
{
    public class SimProceduralFairingDecoupler : SimPartModule
    {
        private static readonly ObjectPool<SimProceduralFairingDecoupler> _pool = new ObjectPool<SimProceduralFairingDecoupler>(New, Clear);

        public bool IsDecoupled;

        public override void Dispose() => _pool.Release(this);

        public static SimProceduralFairingDecoupler Borrow(SimPart part)
        {
            SimProceduralFairingDecoupler decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        private static SimProceduralFairingDecoupler New() => new SimProceduralFairingDecoupler();

        private static void Clear(SimProceduralFairingDecoupler m)
        {
        }
    }
}
