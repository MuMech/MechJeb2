using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    /// 3 constraint terminal conditions with fixed attachment for the fixed-time, maximum-energy problem.
    /// 
    /// Lu, Ping, Hongsheng Sun, and Bruce Tsai. “Closed-Loop Endoatmospheric Ascent Guidance.”
    /// Journal of Guidance, Control, and Dynamics 26, no. 2 (March 2003): 283–94. https://doi.org/10.2514/2.5045.
    /// </summary>
    public readonly struct FlightPathAngle3Energy : IPVGTerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _incT;

        public FlightPathAngle3Energy(double gammaT, double rT, double incT)
        {
            this._gammaT = gammaT;
            this._rT     = rT;
            this._incT   = Math.Abs(ClampPi(incT));
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            var hf = V3.Cross(yf.R, yf.V);

            var n = new V3(0, 0, 1);
            var rn = V3.Cross(yf.R, n);
            var vn = V3.Cross(yf.V, n);

            double con1 = (yf.R.sqrMagnitude - _rT * _rT) * 0.5;
            double con2 = V3.Dot(n, hf.normalized) - Math.Cos(_incT);
            double con3 = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(_gammaT);

            double tv1 = V3.Dot(yf.V, yf.PR) * yf.R.sqrMagnitude - V3.Dot(yf.R, yf.PV) * yf.V.sqrMagnitude +
                         V3.Dot(yf.R, yf.V) * (yf.V.sqrMagnitude - V3.Dot(yf.R, yf.PR));
            double tv2 = V3.Dot(yf.V, yf.PV) - yf.V.sqrMagnitude;
            double tv3 = V3.Dot(hf, yf.PR) * V3.Dot(hf, rn) + V3.Dot(hf, yf.PV) * V3.Dot(hf, vn);

            return (con1, con2, con3, tv1, tv2, tv3);
        }
    }
}
