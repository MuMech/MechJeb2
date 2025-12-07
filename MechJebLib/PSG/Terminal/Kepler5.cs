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
    public readonly struct Kepler5 : ITerminal
    {
        private readonly double _smaT;
        private readonly double _eccT;
        private readonly double _incT;
        private readonly double _lanT;
        private readonly double _argpT;
        private readonly V3     _hT;
        private readonly V3     _eT;

        public Kepler5(double smaT, double eccT, double incT, double lanT, double argpT)
        {
            NumConstraints = 6;
            _smaT          = smaT;
            _eccT          = eccT;
            _incT          = Math.Abs(ClampPi(incT));
            _lanT          = lanT;
            _argpT         = argpT;

            _hT = Astro.HvecFromKeplerian(1.0, _smaT, _eccT, _incT, _lanT);
            _eT = Astro.EvecFromKeplerian(_eccT, _incT, _lanT, _argpT);
        }

        public ITerminal Rescale(Scale scale)  => new Kepler5(_smaT / scale.LengthScale, _eccT, _incT, _lanT, _argpT);
        public double    TargetOrbitalEnergy() => -1.0 / (2.0 * _smaT);
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            var    rf    = V3.CopyFromIndices(x, ri);
            var    vf    = V3.CopyFromIndices(x, vi);
            var    hf    = V3.Cross(rf, vf);
            V3     ef    = V3.Cross(vf, hf) - rf.normalized;
            V3     eMiss = ef - _eT;
            double rfm3  = rf.sqrMagnitude * rf.magnitude;

            f[ci++] = hf.x - _hT.x;
            f[ci++] = hf.y - _hT.y;
            f[ci++] = hf.z - _hT.z;
            f[ci++] = eMiss[0];
            f[ci++] = eMiss[1];
            f[ci++] = eMiss[2];

            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item2, vf.z);
            alglib.sparseappendelement(j, ri.Item3, -vf.y);
            alglib.sparseappendelement(j, vi.Item2, -rf.z);
            alglib.sparseappendelement(j, vi.Item3, rf.y);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, -vf.z);
            alglib.sparseappendelement(j, ri.Item3, vf.x);
            alglib.sparseappendelement(j, vi.Item1, rf.z);
            alglib.sparseappendelement(j, vi.Item3, -rf.x);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, vf.y);
            alglib.sparseappendelement(j, ri.Item2, -vf.x);
            alglib.sparseappendelement(j, vi.Item1, -rf.y);
            alglib.sparseappendelement(j, vi.Item2, rf.x);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, vf.y * vf.y + vf.z * vf.z - (rf.y * rf.y + rf.z * rf.z) / rfm3);
            alglib.sparseappendelement(j, ri.Item2, -vf.x * vf.y + rf.x * rf.y / rfm3);
            alglib.sparseappendelement(j, ri.Item3, -vf.x * vf.z + rf.x * rf.z / rfm3);
            alglib.sparseappendelement(j, vi.Item1, -vf.y * rf.y - vf.z * rf.z);
            alglib.sparseappendelement(j, vi.Item2, 2 * rf.x * vf.y - rf.y * vf.x);
            alglib.sparseappendelement(j, vi.Item3, 2 * rf.x * vf.z - rf.z * vf.x);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, -vf.x * vf.y + rf.x * rf.y / rfm3);
            alglib.sparseappendelement(j, ri.Item2, vf.x * vf.x + vf.z * vf.z - (rf.x * rf.x + rf.z * rf.z) / rfm3);
            alglib.sparseappendelement(j, ri.Item3, -vf.y * vf.z + rf.y * rf.z / rfm3);
            alglib.sparseappendelement(j, vi.Item1, 2 * rf.y * vf.x - rf.x * vf.y);
            alglib.sparseappendelement(j, vi.Item2, -vf.x * rf.x - vf.z * rf.z);
            alglib.sparseappendelement(j, vi.Item3, 2 * rf.y * vf.z - rf.z * vf.y);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, -vf.x * vf.z + rf.x * rf.z / rfm3);
            alglib.sparseappendelement(j, ri.Item2, -vf.y * vf.z + rf.y * rf.z / rfm3);
            alglib.sparseappendelement(j, ri.Item3, vf.x * vf.x + vf.y * vf.y - (rf.x * rf.x + rf.y * rf.y) / rfm3);
            alglib.sparseappendelement(j, vi.Item1, 2 * rf.z * vf.x - rf.x * vf.z);
            alglib.sparseappendelement(j, vi.Item2, 2 * rf.z * vf.y - rf.y * vf.z);
            alglib.sparseappendelement(j, vi.Item3, -vf.x * rf.x - vf.y * rf.y);
        }

        public ITerminal GetFPA()
        {
            double attR = Astro.PeriapsisFromKeplerian(_smaT, _eccT);
            (double vT, double gammaT) = Astro.FPATargetFromKeplerian(_smaT, _eccT, attR, 1.0);
            return new FlightPathAngle5(gammaT, attR, vT, _incT, _lanT);
        }

        public bool IsFPA() => false;
    }
}
