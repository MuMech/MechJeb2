/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;

namespace MechJebLibTest
{
    /// <summary>
    ///     Runs the test methods of a class side by side, as xunit runs test classes, instead of one after another. xunit
    ///     waits for a class's slowest tests in a row, so a few classes of long tests set the length of the whole run.
    ///     Only for classes whose tests share nothing (no static state, no class fixture): each test still gets its own
    ///     instance of the class. See LoggingClassRunner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ParallelTestMethodsAttribute : Attribute
    {
    }
}
