/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

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

        public IPVGTerminal Rescale(Scale scale) => new Intercept6Cartesian(_rT / scale.LengthScale, _vT / scale.VelocityScale);

        public int TerminalConstraints(IntegratorRecord yf, double[] zout, int offset)
        {
            V3 rmiss = yf.R - _rT;
            V3 vmiss = yf.V - _vT;

            zout[offset]     = rmiss[0];
            zout[offset + 1] = rmiss[1];
            zout[offset + 2] = rmiss[2];
            zout[offset + 3] = vmiss[0];
            zout[offset + 4] = vmiss[1];
            zout[offset + 5] = vmiss[2];

            return offset + 6;
        }
    }
}
