/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;

namespace MechJebLib.Utils
{
    /// <summary>
    ///     Thread-safe logger with per-thread override capability.
    ///     Use <see cref="GlobalRegister" /> to set a thread-safe logger for all threads.
    ///     Use <see cref="Register" /> to override the logger for the current thread, and <see cref="Unregister" />
    ///     to drop the override.  The unit test framework does this around every test to route output to that test.
    /// </summary>
    public class Logger
    {
        private Logger() { }

        private static readonly ThreadLocal<Logger> _instance = new ThreadLocal<Logger>(() => new Logger());

        private static Action<object> _globalLogger = o => { };

        private Action<object>? _logger;

        private void PrintImpl(string message) => (_logger ?? _globalLogger)(message);

        public static void GlobalRegister(Action<object> logger) => _globalLogger = logger;

        public static void Register(Action<object> logger) => _instance.Value._logger = logger;

        public static void Unregister() => _instance.Value._logger = null;

        public static void Print(string message) => _instance.Value.PrintImpl(message);
    }
}
