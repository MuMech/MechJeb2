/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

namespace MechJebLib.Utils
{
    /// <summary>
    ///     A solver stopped without meeting its constraints: the request may be infeasible, and the best point it found isn't an
    ///     answer to it.
    /// </summary>
    public class ConvergenceException : MechJebLibException
    {
        public ConvergenceException(string message) : base(message) { }
    }
}
