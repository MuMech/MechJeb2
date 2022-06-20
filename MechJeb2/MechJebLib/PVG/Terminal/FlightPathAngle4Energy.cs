using System;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    /// 4 constraint terminal conditions with fixed attachment for the fixed-time, maximum-energy problem.
    ///
    /// The flight path angle gammaT is fixed at zero.  I don't know of a transversality condition (tv1) that
    /// would work with a user-specified gammaT and the authors explictly used the zero constriant in gammaT to
    /// eliminate the \nu vectors in the full transversality equations.
    /// 
    /// Lu, Ping, Hongsheng Sun, and Bruce Tsai. “Closed-Loop Endoatmospheric Ascent Guidance.”
    /// Journal of Guidance, Control, and Dynamics 26, no. 2 (March 2003): 283–94. https://doi.org/10.2514/2.5045.
    /// </summary>
    public class FlightPathAngle4Energy : IPVGTerminal
    {
        private V3     i_hT;
        private double rT;

        public FlightPathAngle4Energy(double rT, double incT, double lanT)
        {
            this.rT = rT;
            incT    = Math.Abs(ClampPi(incT));
            i_hT    = Functions.HunitFromKeplerian(incT, lanT);
        }
        
        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            double con1 = (yf.R.sqrMagnitude - rT * rT) * 0.5;
            double con2 = V3.Dot(yf.R.normalized, yf.V.normalized);
            double con3 = V3.Dot(yf.R, i_hT);
            double con4 = V3.Dot(yf.V, i_hT);

            double tv1 = V3.Dot(yf.V, yf.PR) * yf.R.sqrMagnitude - V3.Dot(yf.R, yf.PV) * yf.V.sqrMagnitude;
            double tv2 = V3.Dot(yf.V, yf.PV) - yf.V.sqrMagnitude;

            return (con1, con2, con3, con4, tv1, tv2);
        }
    }
}
