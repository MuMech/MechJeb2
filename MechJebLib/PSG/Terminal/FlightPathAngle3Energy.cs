/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
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
            double gammaT = _gammaT;
            double rT     = _rT;
            double incT   = _incT;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            ci = ApplyScalarConstraintV3(f, j, ci, FlightPathAngleConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, RadiusConstraint, new[] { rf }, new[] { ri });
            ci = ApplyScalarConstraintV3(f, j, ci, InclinationConstraint, new[] { rf, vf }, new[] { ri, vi });

            return;

            Dual FlightPathAngleConstraint(DualV3[] p)
            {
                return DualV3.Dot(p[0], p[1]) - Sin(gammaT);
            }

            Dual RadiusConstraint(DualV3[] p)
            {
                return DualV3.Dot(p[0], p[0]) - rT * rT;
            }

            Dual InclinationConstraint(DualV3[] p)
            {
                return DualV3.Cross(p[0], p[1]).normalized.z - Cos(incT);
            }
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
