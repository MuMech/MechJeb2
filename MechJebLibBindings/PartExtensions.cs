/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Reflection;
using static MechJebLibBindings.ReflectionUtils;

namespace MechJebLibBindings
{
    public static class PartExtensions
    {
        private static readonly ClassContext _rfModuleEnginesRf = Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF");
        private static readonly FieldContext _rfIgnited = Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF").Field("ignited", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldContext _rfIgnitions = Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF").Field("ignitions");

        private static readonly bool _isRealFuelsLoadedCorrectly;

        static PartExtensions()
        {
            _isRealFuelsLoadedCorrectly = IsLoadedRealFuels && _rfModuleEnginesRf.IsValid && _rfIgnited.IsValid && _rfIgnitions.IsValid;
        }

        /// <summary>
        ///     Checks that all engine modules on the part are unrestartable and dead.
        ///     Note that the caller must ensure that the vessel does not have any launch clamps, which always allows the engine
        ///     to start.
        ///     When called on a part with no engine modules, this method returns false.
        /// </summary>
        /// <param name="p">the Part</param>
        /// <returns>if the Part's engine modules are all unrestable dead engines</returns>
        public static bool IsUnrestartableDeadEngine(this Part p)
        {
            if (CheatOptions.InfinitePropellant)
                return false;
            if (!_isRealFuelsLoadedCorrectly) // stock doesn't have this concept
                return false;

            List<ModuleEngines> enginelist = p.FindModulesImplementing<ModuleEngines>();

            if (enginelist.Count == 0)
                return false;

            foreach (ModuleEngines e in enginelist)
            {
                if (!e.IsUnrestartableDeadEngine())
                    return false;
            }

            return true;
        }

        /// <summary>
        ///     Checks that the engine module is unrestartable and dead.
        ///     Note that the caller must ensure that the vessel does not have any launch clamps, which always allows the engine
        ///     to start.
        /// </summary>
        /// <param name="e">the ModulEngines</param>
        /// <returns>if the engine module is an unrestartable dead engine</returns>
        public static bool IsUnrestartableDeadEngine(this ModuleEngines e)
        {
            if (CheatOptions.InfinitePropellant)
                return false;
            if (!_isRealFuelsLoadedCorrectly) // stock doesn't have this concept
                return false;
            if (e.finalThrust > 0)
                return false;
            if (!_rfModuleEnginesRf.IsInstance(e))
                return false;

            if (_rfIgnited.GetValue<bool>(e))
                return false;

            return _rfIgnitions.GetValue<int>(e) == 0;
        }
    }
}
