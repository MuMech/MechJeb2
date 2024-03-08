/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     4 constraint terminal conditions with fixed attachment for the fixed-time, maximum-energy problem.
    ///     The flight path angle gammaT is fixed at zero.  I don't know of a transversality condition (tv1) that
    ///     would work with a user-specified gammaT and the authors explictly used the zero constriant in gammaT to
    ///     eliminate the \nu vectors in the full transversality equations.
    ///     Lu, Ping, Hongsheng Sun, and Bruce Tsai. “Closed-Loop Endoatmospheric Ascent Guidance.”
    ///     Journal of Guidance, Control, and Dynamics 26, no. 2 (March 2003): 283–94. https://doi.org/10.2514/2.5045.
    /// </summary>
    public readonly struct FlightPathAngle4Energy : IPVGTerminal
    {
        private readonly double _rT;
        private readonly double _incT;
        private readonly double _lanT;

        private readonly V3 _iHT;

        public FlightPathAngle4Energy(double rT, double incT, double lanT)
        {
            _rT   = rT;
            _incT = Math.Abs(ClampPi(incT));
            _lanT = lanT;
            _iHT  = Astro.HunitFromKeplerian(_incT, _lanT);
        }

        public IPVGTerminal Rescale(Scale scale) => new FlightPathAngle4Energy(_rT / scale.LengthScale, _incT, _lanT);

        public int TerminalConstraints(IntegratorRecord yf, double[] zout, int offset)
        {
            zout[offset]     = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            zout[offset + 1] = V3.Dot(yf.R.normalized, yf.V.normalized);
            zout[offset + 2] = V3.Dot(yf.R, _iHT);
            zout[offset + 3] = V3.Dot(yf.V, _iHT);

            zout[offset + 4] = V3.Dot(yf.V, yf.Pr) * yf.R.sqrMagnitude - V3.Dot(yf.R, yf.Pv) * yf.V.sqrMagnitude;
            zout[offset + 5] = V3.Dot(yf.V, yf.Pv) - yf.V.sqrMagnitude;

            return offset + 6;
        }
    }
}
