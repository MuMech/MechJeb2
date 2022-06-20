using System;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     5 Constraint terminal conditions with free attachment for the minimum propellant / maximum mass problem.
    ///     Many references including:
    ///     Brown, K., and G. Johnson. “Real-Time Optimal Guidance.” IEEE Transactions on Automatic Control 12, no. 5 (October
    ///     1967): 501–6. https://doi.org/10.1109/TAC.1967.1098718.
    ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public class Kepler5Reduced : IPVGTerminal
    {
        private readonly V3     hT;
        private readonly V3     ehat1;
        private readonly V3     ehat2;
        private readonly double e1;
        private readonly double e2;

        public Kepler5Reduced(double smaT, double eccT, double incT, double lanT, double argpT)
        {
            incT = Math.Abs(ClampPi(incT));
            
            hT   = Functions.HvecFromKeplerian(1.0, smaT, eccT, incT, lanT);

            // r guaranteed not to be colinear with hT
            V3 r = V3.zero;
            r[hT.min_magnitude_index] = 1.0;

            // basis vectors orthonormal to hT
            ehat1 = V3.Cross(hT, r).normalized;
            ehat2 = V3.Cross(hT, ehat1).normalized;

            // projection of eT onto ehat1/ehat2
            V3 eT = Functions.EvecFromKeplerian(eccT, incT, lanT, argpT);
            e1 = V3.Dot(eT, ehat1);
            e2 = V3.Dot(eT, ehat2);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            double rfm = yf.R.magnitude;
            double rf3 = rfm * rfm * rfm;

            var hf = V3.Cross(yf.R, yf.V);
            V3 ef = V3.Cross(yf.V, hf) - yf.R.normalized;

            V3 hmiss = hf - hT;
            double emiss1 = e1 - V3.Dot(ef, ehat1);
            double emiss2 = e2 - V3.Dot(ef, ehat2);

            double t1 = V3.Dot(yf.PR, yf.V) - V3.Dot(yf.PV, yf.R) / rf3;

            return (hmiss[0], hmiss[1], hmiss[2], emiss1, emiss2, t1);
        }
    }
}
