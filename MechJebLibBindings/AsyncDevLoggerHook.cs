/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using MechJebLib.Utils;
using UnityEngine;

namespace MechJebLibBindings.Logger
{
    public static class AsyncDevLoggerHook
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public static void EnsureInitialized()
        {
            // Triggering this empty method forces the static constructor below to fire!
        }

        static AsyncDevLoggerHook()
        {
#if DEBUG
            // We use this to grab ksp's path and feed it to the logger
            // Quick and dirty static class hook.
            string kspRoot = KSPUtil.ApplicationRootPath; // Cleared to touch KSP APIs
            string logDir = Path.Combine(kspRoot, "GameData", "MechJeb2", "Plugins", "PluginData");
            // Hand the directory path across the fence to initialize the .NET math logger
            AsyncDevLogger.Initialize(logDir);
#endif
        }



    }
}
