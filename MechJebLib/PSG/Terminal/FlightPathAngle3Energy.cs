/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG.Terminal
{
    public readonly struct FlightPathAngle3Energy : ITerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _incT;

        public FlightPathAngle3Energy(double gammaT, double rT, double incT)
        {
            NumConstraints = 3;
            _gammaT        = gammaT;
            _rT            = rT;
            _incT          = Abs(ClampPi(incT));
        }

        public ITerminal Rescale(Scale scale)  => new FlightPathAngle3Energy(_gammaT, _rT / scale.LengthScale, _incT);
        public double    TargetOrbitalEnergy() => double.PositiveInfinity;
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            var    rf   = V3.CopyFromIndices(x, ri);
            var    vf   = V3.CopyFromIndices(x, vi);
            var    hf   = V3.Cross(rf, vf);
            double hfm3 = hf.sqrMagnitude * hf.magnitude;

            f[ci++] = (rf.sqrMagnitude - _rT * _rT) * 0.5;
            f[ci++] = hf.normalized.z - Cos(_incT);
            f[ci++] = V3.Dot(rf, vf) - Sin(_gammaT);

            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, rf.x);
            alglib.sparseappendelement(j, ri.Item2, rf.y);
            alglib.sparseappendelement(j, ri.Item3, rf.z);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, (vf.y * (hf.x * hf.x + hf.y * hf.y) + vf.z * hf.y * hf.z) / hfm3);
            alglib.sparseappendelement(j, ri.Item2, (-vf.x * (hf.x * hf.x + hf.y * hf.y) - vf.z * hf.x * hf.z) / hfm3);
            alglib.sparseappendelement(j, ri.Item3, hf.z * (hf.x * vf.y - hf.y * vf.x) / hfm3);
            alglib.sparseappendelement(j, vi.Item1, (-rf.y * (hf.x * hf.x + hf.y * hf.y) - rf.z * hf.y * hf.z) / hfm3);
            alglib.sparseappendelement(j, vi.Item2, (rf.x * (hf.x * hf.x + hf.y * hf.y) + rf.z * hf.x * hf.z) / hfm3);
            alglib.sparseappendelement(j, vi.Item3, hf.z * (hf.y * rf.x - hf.x * rf.y) / hfm3);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, ri.Item1, vf.x);
            alglib.sparseappendelement(j, ri.Item2, vf.y);
            alglib.sparseappendelement(j, ri.Item3, vf.z);
            alglib.sparseappendelement(j, vi.Item1, rf.x);
            alglib.sparseappendelement(j, vi.Item2, rf.y);
            alglib.sparseappendelement(j, vi.Item3, rf.z);
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
