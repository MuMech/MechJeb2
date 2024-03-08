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
    ///     5 Constraint terminal conditions with fixed attachment for the minimum propellant / maximum mass problem with
    ///     reduced transversality conditions.
    ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public readonly struct FlightPathAngle5Reduced : IPVGTerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _vT;
        private readonly double _incT;
        private readonly double _lanT;

        private readonly V3 _hT;

        public FlightPathAngle5Reduced(double gammaT, double rT, double vT, double incT, double lanT)
        {
            _gammaT = gammaT;
            _rT     = rT;
            _vT     = vT;
            _lanT   = Clamp2Pi(lanT);
            _incT   = Math.Abs(ClampPi(incT));

            _hT = Astro.HvecFromFlightPathAngle(_rT, _vT, _gammaT, _incT, _lanT);
        }

        public IPVGTerminal Rescale(Scale scale) =>
            new FlightPathAngle5Reduced(_gammaT, _rT / scale.LengthScale, _vT / scale.VelocityScale, _incT, _lanT);

        public int TerminalConstraints(IntegratorRecord yf, double[] zout, int offset)
        {
            var hf = V3.Cross(yf.R, yf.V);
            V3 hmiss = hf - _hT;

            zout[offset]     = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            zout[offset + 1] = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);
            zout[offset + 2] = hmiss[0];
            zout[offset + 3] = hmiss[1];
            zout[offset + 4] = hmiss[2];
            zout[offset + 5] = V3.Dot(V3.Cross(yf.Pr, yf.R) + V3.Cross(yf.Pv, yf.V), hf); // free argP

            return offset + 6;
        }
    }
}
