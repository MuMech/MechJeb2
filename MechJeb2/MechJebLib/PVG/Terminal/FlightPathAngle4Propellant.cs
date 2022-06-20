using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    /// 4 Constraint terminal conditions with fixed attachment for the minimum propellant / maximum mass problem.
    ///
    /// Lu, Ping, Stephen Forbes, and Morgan Baldwin. “A Versatile Powered Guidance Algorithm.”
    /// In AIAA Guidance, Navigation, and Control Conference. Minneapolis, Minnesota: American Institute of Aeronautics
    /// and Astronautics, 2012. https://doi.org/10.2514/6.2012-4843.
    /// </summary>
    public class FlightPathAngle4Propellant : IPVGTerminal
    {
        private readonly double gammaT;
        private readonly double rT;
        private readonly double vT;
        private readonly double incT;

        public FlightPathAngle4Propellant(double gammaT, double rT, double vT, double incT)
        {
            Check.Finite(gammaT);
            Check.PositiveFinite(rT);
            Check.PositiveFinite(vT);
            Check.Finite(incT);
            
            this.gammaT = gammaT;
            this.rT     = rT;
            this.vT     = vT;
            this.incT   = Math.Abs(ClampPi(incT));
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            var n = new V3(0, 0, 1);
            var rn = V3.Cross(yf.R, n);
            var vn = V3.Cross(yf.V, n);
            var hf = V3.Cross(yf.R, yf.V);

            double con1 = (yf.R.sqrMagnitude - rT * rT) * 0.5;
            double con2 = (yf.V.sqrMagnitude - vT * vT) * 0.5;
            double con3 = V3.Dot(n, hf.normalized) - Math.Cos(incT);
            double con4 = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(gammaT);
            double tv1 =
                rT * rT * (V3.Dot(yf.V, yf.PR) - vT * Math.Sin(gammaT) / rT * V3.Dot(yf.R, yf.PR)) -
                vT * vT * (V3.Dot(yf.R, yf.PV) - rT * Math.Sin(gammaT) / vT * V3.Dot(yf.V, yf.PV));
            double tv2 = V3.Dot(hf, yf.PR) * V3.Dot(hf, rn) + V3.Dot(hf, yf.PV) * V3.Dot(hf, vn);
            return (con1, con2, con3, con4, tv1, tv2);
        }
    }
}
