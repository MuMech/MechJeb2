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
    ///     5 Constraint terminal conditions with free attachment for the minimum propellant / maximum mass problem.
    ///     Many references including:
    ///     Brown, K., and G. Johnson. “Real-Time Optimal Guidance.” IEEE Transactions on Automatic Control 12, no. 5 (October
    ///     1967): 501–6. https://doi.org/10.1109/TAC.1967.1098718.
    ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public readonly struct Kepler5Reduced : IPVGTerminal
    {
        private readonly double _smaT;
        private readonly double _eccT;
        private readonly double _incT;
        private readonly double _lanT;
        private readonly double _argpT;
        private readonly V3     _hT;
        private readonly V3     _ehat1;
        private readonly V3     _ehat2;
        private readonly double _e1;
        private readonly double _e2;

        public Kepler5Reduced(double smaT, double eccT, double incT, double lanT, double argpT)
        {
            _smaT  = smaT;
            _eccT  = eccT;
            _incT  = Math.Abs(ClampPi(incT));
            _lanT  = lanT;
            _argpT = argpT;

            _hT = Functions.HvecFromKeplerian(1.0, _smaT, _eccT, _incT, _lanT);

            // r guaranteed not to be colinear with hT
            V3 r = V3.zero;
            r[_hT.min_magnitude_index] = 1.0;

            // basis vectors orthonormal to hT
            _ehat1 = V3.Cross(_hT, r).normalized;
            _ehat2 = V3.Cross(_hT, _ehat1).normalized;

            // projection of eT onto ehat1/ehat2
            V3 eT = Functions.EvecFromKeplerian(_eccT, _incT, _lanT, _argpT);
            _e1 = V3.Dot(eT, _ehat1);
            _e2 = V3.Dot(eT, _ehat2);
        }

        public IPVGTerminal Rescale(Scale scale)
        {
            return new Kepler5Reduced(_smaT / scale.lengthScale, _eccT, _incT, _lanT, _argpT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            double rfm = yf.R.magnitude;
            double rf3 = rfm * rfm * rfm;

            var hf = V3.Cross(yf.R, yf.V);
            V3 ef = V3.Cross(yf.V, hf) - yf.R.normalized;

            V3 hmiss = hf - _hT;
            double emiss1 = _e1 - V3.Dot(ef, _ehat1);
            double emiss2 = _e2 - V3.Dot(ef, _ehat2);

            double t1 = V3.Dot(yf.PR, yf.V) - V3.Dot(yf.PV, yf.R) / rf3;

            return (hmiss[0], hmiss[1], hmiss[2], emiss1, emiss2, t1);
        }
    }
}
