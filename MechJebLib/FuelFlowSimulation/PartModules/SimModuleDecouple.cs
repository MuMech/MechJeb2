/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Utils;
using static System.FormattableString;

namespace MechJebLib.FuelFlowSimulation.PartModules
{
    public class SimModuleDecouple : SimPartModule
    {
        private static readonly ObjectPool<SimModuleDecouple> _pool = new ObjectPool<SimModuleDecouple>(New, m => m.Clear());

        public bool IsDecoupled = false;
        public bool IsOmniDecoupler = false;
        public bool Staged = false;
        public SimPart? AttachedPart;

        public override void Dispose() => _pool.Release(this);

        public static SimModuleDecouple Borrow(SimPart part)
        {
            SimModuleDecouple decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        protected override void Clear()
        {
            base.Clear();
            AttachedPart = null;
        }

        private static SimModuleDecouple New() => new SimModuleDecouple();

        public override string ToString()
        {
            List<string> fields = CommonFieldList();
            AddField(fields, "IsDecoupled", IsDecoupled, false);
            AddField(fields, "IsOmniDecoupler", IsOmniDecoupler, false);
            AddField(fields, "Staged", Staged, false);
            if (AttachedPart != null)
                fields.Add(Invariant($"AttachedPart={AttachedPart.Ident}"));
            return ModuleLine("SimModuleDecouple", fields);
        }
    }
}
