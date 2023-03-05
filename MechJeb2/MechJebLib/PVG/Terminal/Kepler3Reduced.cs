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
    public readonly struct Kepler3Reduced : IPVGTerminal
    {
        private readonly double _smaT;
        private readonly double _eccT;
        private readonly double _incT;

        private readonly double _hTm;
        private readonly double _peRT;

        /// <summary>
        ///     3 Constraint terminal conditions with free attachment for the minimum propellant / maximum mass problem with
        ///     reduced transversality conditions.
        ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
        ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
        /// </summary>
        public Kepler3Reduced(double smaT, double eccT, double incT)
        {
            _smaT = smaT;
            _eccT = eccT;
            _incT = Math.Abs(ClampPi(incT));

            _hTm  = Functions.HmagFromKeplerian(1.0, _smaT, _eccT);
            _peRT = Functions.PeriapsisFromKeplerian(_smaT, _eccT);
        }

        public IPVGTerminal Rescale(Scale scale)
        {
            return new Kepler3Reduced(_smaT / scale.lengthScale, _eccT, _incT);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            var hf = V3.Cross(yf.R, yf.V);
            var n = new V3(0, 0, 1);

            double rfm = yf.R.magnitude;
            double rf3 = rfm * rfm * rfm;

            // empirically found this combination worked better and tolerates ecc > 1e-4
            // the use of energy, eccentricity and sma did not converge as well
            double con1 = V3.Dot(hf, hf) * 0.5 - _hTm * _hTm * 0.5;                     // angular momentum
            double con2 = Functions.PeriapsisFromStateVectors(1.0, yf.R, yf.V) - _peRT; // periapsis
            double con3 = V3.Dot(n, hf.normalized) - Math.Cos(_incT);                   // inclination
            double tv1 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), hf);     // free Argp
            double tv2 = V3.Dot(V3.Cross(yf.PR, yf.R) + V3.Cross(yf.PV, yf.V), n);      // free LAN
            double tv3 = V3.Dot(yf.PR, yf.V) - V3.Dot(yf.PV, yf.R) / rf3;               // free TA

            return (con1, con2, con3, tv1, tv2, tv3);
        }
    }
}
