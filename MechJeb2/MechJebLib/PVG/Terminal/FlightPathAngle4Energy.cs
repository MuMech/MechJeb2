/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

#nullable enable

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
            _iHT  = Functions.HunitFromKeplerian(_incT, _lanT);
        }

        public IPVGTerminal Rescale(Scale scale)
        {
            return new FlightPathAngle4Energy(_rT / scale.lengthScale, _incT, _lanT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            double con1 = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            double con2 = V3.Dot(yf.R.normalized, yf.V.normalized);
            double con3 = V3.Dot(yf.R, _iHT);
            double con4 = V3.Dot(yf.V, _iHT);

            double tv1 = V3.Dot(yf.V, yf.PR) * yf.R.sqrMagnitude - V3.Dot(yf.R, yf.PV) * yf.V.sqrMagnitude;
            double tv2 = V3.Dot(yf.V, yf.PV) - yf.V.sqrMagnitude;

            return (con1, con2, con3, con4, tv1, tv2);
        }
    }
}
