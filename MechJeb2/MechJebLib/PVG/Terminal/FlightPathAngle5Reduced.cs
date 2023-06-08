/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Core;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

#nullable enable

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

            _hT = MechJebLib.Core.Maths.HvecFromFlightPathAngle(_rT, _vT, _gammaT, _incT, _lanT);
        }

        public IPVGTerminal Rescale(Scale scale)
        {
            return new FlightPathAngle5Reduced(_gammaT, _rT / scale.LengthScale, _vT / scale.VelocityScale, _incT, _lanT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(OutputLayout yf)
        {
            var hf = V3.Cross(yf.R, yf.V);
            V3 hmiss = hf - _hT;

            double con1 = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            double con2 = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);
            double con3 = hmiss[0];
            double con4 = hmiss[1];
            double con5 = hmiss[2];
            double tv1 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), hf); // free argP
            return (con1, con2, con3, con4, con5, tv1);
        }
    }
}
