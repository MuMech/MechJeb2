using System;
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

        // Note that if the vessel has launch clamps then the engine will always start but
        // this check is not done here and needs to be handled by the caller.
        public static bool UnrestartableDeadEngine(this ModuleEngines eng)
        {
            if (CheatOptions.InfinitePropellant)
                return false;
            if (_rfIgnited is null || _rfIgnitions is null) // stock doesn't have this concept
                return false;
            if (eng.finalThrust > 0)
                return false;

            try
            {
                if (_rfIgnited.GetValue(eng) is bool ignited && ignited)
                    return false;
                if (_rfIgnitions.GetValue(eng) is int ignitions)
                    return ignitions == 0;
            }
            catch (ArgumentException) { }

            return false;
        }
    }
}
