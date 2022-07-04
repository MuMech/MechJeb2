using System;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     4 Constraint terminal conditions with free attachment for the minimum propellant / maximum mass problem with
    ///     reduced transversality conditions.
    /// 
    ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public readonly struct Kepler4Reduced : IPVGTerminal
    {
        private readonly V3     _hT;
        private readonly double _peRT;

        public Kepler4Reduced(double smaT, double eccT, double incT, double lanT)
        {
            incT = Math.Abs(ClampPi(incT));
            _hT   = Functions.HvecFromKeplerian(1.0, smaT, eccT, incT, lanT);
            _peRT = Functions.PeriapsisFromKeplerian(smaT, eccT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            double rfm = yf.R.magnitude;
            double rf3 = rfm * rfm * rfm;

            var hf = V3.Cross(yf.R, yf.V);
            V3 hmiss = hf - _hT;

            double con1 = Functions.PeriapsisFromStateVectors(1.0, yf.R, yf.V) - _peRT; // periapsis
            double con2 = hmiss[0];
            double con3 = hmiss[1];
            double con4 = hmiss[2];
            double tv1 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), hf); // free Argp
            double tv2 = V3.Dot(yf.PR, yf.V) - V3.Dot(yf.PV, yf.R) / rf3;           // free TA

            return (con1, con2, con3, con4, tv1, tv2);
        }
    }
}
