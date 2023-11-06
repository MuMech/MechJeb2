/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using MechJebLib.Utils;

namespace MechJebLib.Simulations.PartModules
{
    public class SimLaunchClamp : SimPartModule
    {
        private static readonly ObjectPool<SimLaunchClamp> _pool = new ObjectPool<SimLaunchClamp>(New, Clear);

        public override void Dispose() => _pool.Release(this);

        public static SimLaunchClamp Borrow(SimPart part)
        {
            SimLaunchClamp clamp = _pool.Borrow();
            clamp.Part = part;
            return clamp;
        }

        private static SimLaunchClamp New() => new SimLaunchClamp();

        private static void Clear(SimLaunchClamp m)
        {
        }
    }
}
