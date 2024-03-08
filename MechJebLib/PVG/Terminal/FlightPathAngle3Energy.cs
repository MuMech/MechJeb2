/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     3 constraint terminal conditions with fixed attachment for the fixed-time, maximum-energy problem.
    ///     Lu, Ping, Hongsheng Sun, and Bruce Tsai. “Closed-Loop Endoatmospheric Ascent Guidance.”
    ///     Journal of Guidance, Control, and Dynamics 26, no. 2 (March 2003): 283–94. https://doi.org/10.2514/2.5045.
    /// </summary>
    public readonly struct FlightPathAngle3Energy : IPVGTerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _incT;

        public FlightPathAngle3Energy(double gammaT, double rT, double incT)
        {
            _gammaT = gammaT;
            _rT     = rT;
            _incT   = Math.Abs(ClampPi(incT));
        }

        public IPVGTerminal Rescale(Scale scale) => new FlightPathAngle3Energy(_gammaT, _rT / scale.LengthScale, _incT);

        public int TerminalConstraints(IntegratorRecord yf, double[] zout, int offset)
        {
            var hf = V3.Cross(yf.R, yf.V);

            double k = yf.V.sqrMagnitude / V3.Dot(yf.V, yf.Pv);
            yf.Pv *= k;
            yf.Pr *= k;

            var n = new V3(0, 0, 1);
            var rn = V3.Cross(yf.R, n);
            var vn = V3.Cross(yf.V, n);

            zout[offset]     = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            zout[offset + 1] = V3.Dot(n, hf.normalized) - Math.Cos(_incT);
            zout[offset + 2] = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);

            zout[offset + 3] = V3.Dot(yf.V, yf.Pr) * yf.R.sqrMagnitude - V3.Dot(yf.R, yf.Pv) * yf.V.sqrMagnitude +
                               V3.Dot(yf.R, yf.V) * (yf.V.sqrMagnitude - V3.Dot(yf.R, yf.Pr));
            zout[offset + 4] = V3.Dot(yf.V, yf.Pv) - yf.V.sqrMagnitude;
            zout[offset + 5] = V3.Dot(hf, yf.Pr) * V3.Dot(hf, rn) + V3.Dot(hf, yf.Pv) * V3.Dot(hf, vn);

            return offset + 6;
        }
    }
}
