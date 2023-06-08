﻿/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     4 Constraint terminal conditions with fixed attachment for the minimum propellant / maximum mass problem with
    ///     reduced transversality conditions.
    ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public readonly struct FlightPathAngle4Reduced : IPVGTerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _vT;
        private readonly double _incT;

        public FlightPathAngle4Reduced(double gammaT, double rT, double vT, double incT)
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

        public IPVGTerminal Rescale(Scale scale)
        {
            return new FlightPathAngle4Reduced(_gammaT, _rT / scale.LengthScale, _vT / scale.VelocityScale, _incT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(OutputLayout yf)
        {
            var n = new V3(0, 0, 1);
            var hf = V3.Cross(yf.R, yf.V);

            double con1 = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            double con2 = (yf.V.sqrMagnitude - _vT * _vT) * 0.5;
            double con3 = V3.Dot(n, hf.normalized) - Math.Cos(_incT);
            double con4 = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);
            double tv1 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), hf); // free ArgP
            double tv2 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), n);  // free LAN
            return (con1, con2, con3, con4, tv1, tv2);
        }
    }
}
