/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using MechJebLib.Primitives;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     6 constrant match to orbital state vectors.
    ///     This may work to bootstrap a problem, but will not be very useful for closed loop guidance once the exact solution
    ///     becomes impossible.
    /// </summary>
    public readonly struct Intercept6Cartesian : IPVGTerminal
    {
        private readonly V3 _rT;
        private readonly V3 _vT;

        public Intercept6Cartesian(V3 rT, V3 vT)
        {
            _rT = rT;
            _vT = vT;
        }

        public IPVGTerminal Rescale(Scale scale)
        {
            return new Intercept6Cartesian(_rT / scale.LengthScale, _vT / scale.VelocityScale);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            V3 rmiss = yf.R - _rT;
            V3 vmiss = yf.V - _vT;

            return (rmiss[0], rmiss[1], rmiss[2], vmiss[0], vmiss[1], vmiss[2]);
        }
    }
}
