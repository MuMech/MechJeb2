/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     4 Constraint terminal conditions with fixed attachment for the minimum propellant / maximum mass problem.
    ///     Lu, Ping, Stephen Forbes, and Morgan Baldwin. “A Versatile Powered Guidance Algorithm.”
    ///     In AIAA Guidance, Navigation, and Control Conference. Minneapolis, Minnesota: American Institute of Aeronautics
    ///     and Astronautics, 2012. https://doi.org/10.2514/6.2012-4843.
    /// </summary>
    public readonly struct FlightPathAngle4Propellant : IPVGTerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _vT;
        private readonly double _incT;

        public FlightPathAngle4Propellant(double gammaT, double rT, double vT, double incT)
        {
            Check.Finite(gammaT);
            Check.PositiveFinite(rT);
            Check.PositiveFinite(vT);
            Check.Finite(incT);

            _gammaT = gammaT;
            _rT     = rT;
            _vT     = vT;
            _incT   = Math.Abs(ClampPi(incT));
        }

        public IPVGTerminal Rescale(Scale scale) => new FlightPathAngle4Propellant(_gammaT, _rT, _vT, _incT);

        public int TerminalConstraints(IntegratorRecord yf, double[] zout, int offset)
        {
            var n = new V3(0, 0, 1);
            var rn = V3.Cross(yf.R, n);
            var vn = V3.Cross(yf.V, n);
            var hf = V3.Cross(yf.R, yf.V);

            zout[offset]     = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            zout[offset + 1] = (yf.V.sqrMagnitude - _vT * _vT) * 0.5;
            zout[offset + 2] = V3.Dot(n, hf.normalized) - Math.Cos(_incT);
            zout[offset + 3] = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);
            zout[offset + 4] =
                _rT * _rT * (V3.Dot(yf.V, yf.Pr) - _vT * Math.Sin(_gammaT) / _rT * V3.Dot(yf.R, yf.Pr)) -
                _vT * _vT * (V3.Dot(yf.R, yf.Pv) - _rT * Math.Sin(_gammaT) / _vT * V3.Dot(yf.V, yf.Pv));
            zout[offset + 5] = V3.Dot(hf, yf.Pr) * V3.Dot(hf, rn) + V3.Dot(hf, yf.Pv) * V3.Dot(hf, vn);
            return offset + 6;
        }
    }
}
