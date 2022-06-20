using System;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    /// 5 Constraint terminal conditions with fixed attachment for the minimum propellant / maximum mass problem with
    /// reduced transversality conditions.
    ///
    /// Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    /// Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public class FlightPathAngle5Reduced : IPVGTerminal
    {
        private readonly double gammaT;
        private readonly V3     hT;
        private readonly double rT;

        public FlightPathAngle5Reduced(double rT, double vT, double gammaT, double incT, double lanT)
        {
            this.gammaT = gammaT;
            this.rT     = rT;
            lanT        = Clamp2Pi(lanT);
            incT        = Math.Abs(ClampPi(incT));
            hT          = Functions.HvecFromFlightPathAngle(rT, vT, gammaT, incT, lanT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            var hf = V3.Cross(yf.R, yf.V);
            V3 hmiss = hf - hT;

            double con1 = (yf.R.sqrMagnitude - rT * rT) * 0.5;
            double con2 = V3.Dot(yf.R.normalized, yf.V.normalized) - Math.Sin(gammaT);
            double con3 = hmiss[0];
            double con4 = hmiss[1];
            double con5 = hmiss[2];
            double tv1 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), hf); // free argP
            return (con1, con2, con3, con4, con5, tv1);
        }
    }
}
