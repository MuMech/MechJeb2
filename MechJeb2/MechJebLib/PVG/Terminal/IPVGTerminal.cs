/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using MechJebLib.Primitives;

namespace MechJebLib.PVG.Terminal
{
    public interface IPVGTerminal
    {
        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(OutputLayout yf);

        public IPVGTerminal Rescale(Scale scale);
    }
}
