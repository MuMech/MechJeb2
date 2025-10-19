/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG.Terminal
{
    public readonly struct Kepler3 : ITerminal
    {
        private readonly double _smaT;
        private readonly double _eccT;
        private readonly double _incT;
        private readonly double _hTm;
        private readonly double _energyT;

        public Kepler3(double smaT, double eccT, double incT)
        {
            NumConstraints = 3;
            _smaT          = smaT;
            _eccT          = eccT;
            _incT          = Math.Abs(ClampPi(incT));
            _hTm           = Astro.HmagFromKeplerian(1.0, _smaT, _eccT);
            _energyT       = -1.0 / (2.0 * _smaT);
        }

        public ITerminal Rescale(Scale scale)  => new Kepler3(_smaT / scale.LengthScale, _eccT, _incT);
        public double    TargetOrbitalEnergy() => -1.0 / (2.0 * _smaT);
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            var    rf      = V3.CopyFromIndices(x, ri);
            var    vf      = V3.CopyFromIndices(x, vi);
            var    hf      = V3.Cross(rf, vf);
            V3     ef      = V3.Cross(vf, hf) - rf.normalized;
            double energyf = 0.5 * V3.Dot(vf, vf) - 1.0 / rf.magnitude;
            double hfm3    = hf.sqrMagnitude * hf.magnitude;
            double rfm3    = rf.sqrMagnitude * rf.magnitude;

            // angular momentum / semiparameter
            // energy / sma (problematic because you can't square it + infinite sma for parabolic)
            // periapsis and/or apoapsis
            // eccvec magnitude / eccentricity
            f[ci++] = V3.Dot(hf, hf) * 0.5 - _hTm * _hTm * 0.5;
            f[ci++] = energyf - _energyT;
            f[ci++] = hf.normalized.z - Math.Cos(_incT);

            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, hf.z * vf.y - hf.y * vf.z);
            alglib.sparseappendelement(j, ri.Item2, hf.x * vf.z - hf.z * vf.x);
            alglib.sparseappendelement(j, ri.Item3, hf.y * vf.x - hf.x * vf.y);
            alglib.sparseappendelement(j, vi.Item1, hf.y * rf.z - hf.z * rf.y);
            alglib.sparseappendelement(j, vi.Item2, hf.z * rf.x - hf.x * rf.z);
            alglib.sparseappendelement(j, vi.Item3, hf.x * rf.y - hf.y * rf.x);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, rf.x / rfm3);
            alglib.sparseappendelement(j, ri.Item2, rf.y / rfm3);
            alglib.sparseappendelement(j, ri.Item3, rf.z / rfm3);
            alglib.sparseappendelement(j, vi.Item1, vf.x);
            alglib.sparseappendelement(j, vi.Item2, vf.y);
            alglib.sparseappendelement(j, vi.Item3, vf.z);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, (vf.y * (hf.x * hf.x + hf.y * hf.y) + vf.z * hf.y * hf.z) / hfm3);
            alglib.sparseappendelement(j, ri.Item2, (-vf.x * (hf.x * hf.x + hf.y * hf.y) - vf.z * hf.x * hf.z) / hfm3);
            alglib.sparseappendelement(j, ri.Item3, hf.z * (hf.x * vf.y - hf.y * vf.x) / hfm3);
            alglib.sparseappendelement(j, vi.Item1, (-rf.y * (hf.x * hf.x + hf.y * hf.y) - rf.z * hf.y * hf.z) / hfm3);
            alglib.sparseappendelement(j, vi.Item2, (rf.x * (hf.x * hf.x + hf.y * hf.y) + rf.z * hf.x * hf.z) / hfm3);
            alglib.sparseappendelement(j, vi.Item3, hf.z * (hf.y * rf.x - hf.x * rf.y) / hfm3);
        }

        public ITerminal GetFPA()
        {
            double attR = Astro.PeriapsisFromKeplerian(_smaT, _eccT);
            (double vT, double gammaT) = Astro.FPATargetFromKeplerian(_smaT, _eccT, attR, 1.0);
            return new FlightPathAngle4(gammaT, attR, vT, _incT);
        }

        public bool IsFPA() => false;
    }
}
