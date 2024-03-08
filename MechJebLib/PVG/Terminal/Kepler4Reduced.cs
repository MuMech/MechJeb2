/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     4 Constraint terminal conditions with free attachment for the minimum propellant / maximum mass problem with
    ///     reduced transversality conditions.
    ///     Pan, Binfeng, Zheng Chen, Ping Lu, and Bo Gao. “Reduced Transversality Conditions in Optimal Space Trajectories.”
    ///     Journal of Guidance, Control, and Dynamics 36, no. 5 (September 2013): 1289–1300. https://doi.org/10.2514/1.60181.
    /// </summary>
    public readonly struct Kepler4Reduced : IPVGTerminal
    {
        private readonly double _smaT;
        private readonly double _eccT;
        private readonly double _incT;
        private readonly double _lanT;

        private readonly V3     _hT;
        private readonly double _peRT;

        public Kepler4Reduced(double smaT, double eccT, double incT, double lanT)
        {
            _smaT = smaT;
            _eccT = eccT;
            _incT = Math.Abs(ClampPi(incT));
            _lanT = lanT;

            _hT   = Astro.HvecFromKeplerian(1.0, _smaT, _eccT, _incT, _lanT);
            _peRT = Astro.PeriapsisFromKeplerian(_smaT, _eccT);
        }

        public IPVGTerminal Rescale(Scale scale) => new Kepler4Reduced(_smaT / scale.LengthScale, _eccT, _incT, _lanT);

        public int TerminalConstraints(IntegratorRecord yf, double[] zout, int offset)
        {
            double rfm = yf.R.magnitude;
            double rf3 = rfm * rfm * rfm;

            var hf = V3.Cross(yf.R, yf.V);
            V3 hmiss = hf - _hT;

            zout[offset]     = Astro.PeriapsisFromStateVectors(1.0, yf.R, yf.V) - _peRT; // periapsis
            zout[offset + 1] = hmiss[0];
            zout[offset + 2] = hmiss[1];
            zout[offset + 3] = hmiss[2];
            zout[offset + 4] = V3.Dot(V3.Cross(yf.Pr, yf.R) + V3.Cross(yf.Pv, yf.V), hf); // free Argp
            zout[offset + 5] = V3.Dot(yf.Pr, yf.V) - V3.Dot(yf.Pv, yf.R) / rf3;           // free TA

            return offset + 6;
        }
    }
}
