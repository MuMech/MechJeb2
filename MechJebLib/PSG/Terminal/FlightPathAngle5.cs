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
    public readonly struct FlightPathAngle5 : ITerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _vT;
        private readonly double _incT;
        private readonly double _lanT;

        private readonly V3 _hT;

        public FlightPathAngle5(double gammaT, double rT, double vT, double incT, double lanT)
        {
            NumConstraints = 5;
            _gammaT        = gammaT;
            _rT            = rT;
            _vT            = vT;
            _lanT          = Clamp2Pi(lanT);
            _incT          = Math.Abs(ClampPi(incT));

            _hT = Astro.HvecFromFlightPathAngle(_rT, _vT, _gammaT, _incT, _lanT);
        }

        public ITerminal Rescale(Scale scale) =>
            new FlightPathAngle5(_gammaT, _rT / scale.LengthScale, _vT / scale.VelocityScale, _incT, _lanT);

        public double TargetOrbitalEnergy() => 0.5 * _vT * _vT - 1.0 / _rT;
        public double IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);
            var hf = V3.Cross(rf, vf);

            f[ci++] = (rf.sqrMagnitude - _rT * _rT) * 0.5;
            f[ci++] = V3.Dot(rf, vf) - Math.Sin(_gammaT);
            f[ci++] = hf.x - _hT.x;
            f[ci++] = hf.y - _hT.y;
            f[ci++] = hf.z - _hT.z;

            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, rf.x);
            alglib.sparseappendelement(j, ri.Item2, rf.y);
            alglib.sparseappendelement(j, ri.Item3, rf.z);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, vf.x);
            alglib.sparseappendelement(j, ri.Item2, vf.y);
            alglib.sparseappendelement(j, ri.Item3, vf.z);
            alglib.sparseappendelement(j, vi.Item1, rf.x);
            alglib.sparseappendelement(j, vi.Item2, rf.y);
            alglib.sparseappendelement(j, vi.Item3, rf.z);
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
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
