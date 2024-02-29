using System;
using System.Collections.Generic;
using System.Reflection;

namespace MechJebLibBindings
{
    public static class PartExtensions
    {
        private static readonly FieldInfo? _rfIgnited;
        private static readonly FieldInfo? _rfIgnitions;

        static PartExtensions()
        {
            if (!ReflectionUtils.IsAssemblyLoaded("RealFuels"))
                return;

            _rfIgnited = ReflectionUtils.GetFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "ignited",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _rfIgnitions = ReflectionUtils.GetFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "ignitions");
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
            if (_rfIgnited is null || _rfIgnitions is null) // stock doesn't have this concept
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
            if (_rfIgnited is null || _rfIgnitions is null) // stock doesn't have this concept
                return false;
            if (e.finalThrust > 0)
                return false;

            try
            {
                if (_rfIgnited.GetValue(e) is bool ignited && ignited)
                    return false;
                if (_rfIgnitions.GetValue(e) is int ignitions)
                    return ignitions == 0;
            }
            catch (ArgumentException) { }

            return false;
        }
    }
}
