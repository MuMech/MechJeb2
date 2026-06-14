/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    [KOSNomenclature("MechJebComputerModule")]
    public abstract class ComputerModuleBinding<T> : Structure where T : ComputerModule
    {
        private readonly Func<MechJebCore?> _core;

        protected ComputerModuleBinding(Func<MechJebCore?> core)
        {
            _core = core;
            RegisterInitializer(InitSuffixes);
        }

        // the Module deliberately re-evaluates on every call since the core can update dynamically
        protected T Module
        {
            get
            {
                MechJebCore core = _core() ?? throw new KOSException("MechJeb is not available on this vessel.");
                return core.GetComputerModule<T>();
            }
        }

        private void InitSuffixes()
        {
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value => SetEnabled(value),
                "Whether the module is enabled."));
            InitializeSuffixes();
        }

        protected abstract void InitializeSuffixes();

        protected virtual void SetEnabled(bool enabled)
        {
            if (enabled)
                Module.Enable();
            else
                Module.Disable();
        }
    }
}
