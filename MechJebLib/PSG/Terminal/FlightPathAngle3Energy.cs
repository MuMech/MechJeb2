/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.AutoDiff;

namespace MechJebLib.PSG.Terminal
{
    public readonly struct FlightPathAngle3Energy : ITerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _incT;

        public FlightPathAngle3Energy(double gammaT, double rT, double incT)
        {
            // rT is the user's burnout altitude, which is not clamped for fixed burntime rockets
            Check.Finite(gammaT, nameof(gammaT));
            Check.PositiveFinite(rT, nameof(rT));
            Check.Finite(incT, nameof(incT));

            NumConstraints = 2 + InclinationConstraints.NumConstraints(incT);
            _gammaT = gammaT;
            _rT = rT;
            _incT = incT;
        }

        public ITerminal Rescale(Scale scale)  => new FlightPathAngle3Energy(_gammaT, _rT / scale.LengthScale, _incT);
        public double    TargetOrbitalEnergy() => double.PositiveInfinity;
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            double gammaT = _gammaT;
            double rT = _rT;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            ci = ApplyScalarConstraintV3(f, j, ci, FlightPathAngleConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, RadiusConstraint, new[] { rf }, new[] { ri });
            ci = InclinationConstraints.Apply(_incT, rf, vf, ri, vi, f, j, ci);

            return;

            Dual FlightPathAngleConstraint(DualV3[] p) => DualV3.Dot(p[0], p[1]) / (rT * p[1].magnitude) - Sin(gammaT);

            Dual RadiusConstraint(DualV3[] p) => DualV3.Dot(p[0], p[0]) / (rT * rT) - 1.0;
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
