/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    /// 4 Constraint terminal conditions with fixed attachment for the minimum propellant / maximum mass problem.
    ///
    /// Lu, Ping, Stephen Forbes, and Morgan Baldwin. “A Versatile Powered Guidance Algorithm.”
    /// In AIAA Guidance, Navigation, and Control Conference. Minneapolis, Minnesota: American Institute of Aeronautics
    /// and Astronautics, 2012. https://doi.org/10.2514/6.2012-4843.
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
            
            this._gammaT = gammaT;
            this._rT     = rT;
            this._vT     = vT;
            this._incT   = Math.Abs(ClampPi(incT));
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            var n = new V3(0, 0, 1);
            var rn = V3.Cross(yf.R, n);
            var vn = V3.Cross(yf.V, n);
            var hf = V3.Cross(yf.R, yf.V);

            double con1 = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            double con2 = (yf.V.sqrMagnitude - _vT * _vT) * 0.5;
            double con3 = V3.Dot(n, hf.normalized) - Math.Cos(_incT);
            double con4 = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);
            double tv1 =
                _rT * _rT * (V3.Dot(yf.V, yf.PR) - _vT * Math.Sin(_gammaT) / _rT * V3.Dot(yf.R, yf.PR)) -
                _vT * _vT * (V3.Dot(yf.R, yf.PV) - _rT * Math.Sin(_gammaT) / _vT * V3.Dot(yf.V, yf.PV));
            double tv2 = V3.Dot(hf, yf.PR) * V3.Dot(hf, rn) + V3.Dot(hf, yf.PV) * V3.Dot(hf, vn);
            return (con1, con2, con3, con4, tv1, tv2);
        }
    }
}
