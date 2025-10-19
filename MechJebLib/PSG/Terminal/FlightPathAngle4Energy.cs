/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG.Terminal
{
    public readonly struct FlightPathAngle4Energy : ITerminal
    {
        private readonly double _rT;
        private readonly double _incT;
        private readonly double _lanT;

        private readonly V3 _iHT;

        public FlightPathAngle4Energy(double rT, double incT, double lanT)
        {
            NumConstraints = 4;
            _rT            = rT;
            _incT          = Abs(ClampPi(incT));
            _lanT          = lanT;
            _iHT           = Astro.HunitFromKeplerian(_incT, _lanT);
        }

        public ITerminal Rescale(Scale scale)  => new FlightPathAngle4Energy(_rT / scale.LengthScale, _incT, _lanT);
        public double    TargetOrbitalEnergy() => double.PositiveInfinity;
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            f[ci++] = (rf.sqrMagnitude - _rT * _rT) * 0.5;
            f[ci++] = V3.Dot(rf, vf); // should be able to support gammaT here now?
            f[ci++] = V3.Dot(rf, _iHT);
            f[ci++] = V3.Dot(vf, _iHT);

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
            alglib.sparseappendelement(j, ri.Item1, _iHT.x);
            alglib.sparseappendelement(j, ri.Item2, _iHT.y);
            alglib.sparseappendelement(j, ri.Item3, _iHT.z);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, vi.Item1, _iHT.x);
            alglib.sparseappendelement(j, vi.Item2, _iHT.y);
            alglib.sparseappendelement(j, vi.Item3, _iHT.z);
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
