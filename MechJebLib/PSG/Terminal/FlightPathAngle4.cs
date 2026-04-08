/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static MechJebLib.Utils.AutoDiff;
using static System.Math;

namespace MechJebLib.PSG.Terminal
{
    public readonly struct FlightPathAngle4 : ITerminal
    {
        private readonly double _gammaT;
        private readonly double _rT;
        private readonly double _vT;
        private readonly double _incT;

        public FlightPathAngle4(double gammaT, double rT, double vT, double incT)
        {
            Check.Finite(gammaT);
            Check.PositiveFinite(rT);
            Check.PositiveFinite(vT);
            Check.Finite(incT);

            NumConstraints = 4;
            _gammaT        = gammaT;
            _rT            = rT;
            _vT            = vT;
            _incT          = incT;
        }

        public ITerminal Rescale(Scale scale)  => new FlightPathAngle4(_gammaT, _rT / scale.LengthScale, _vT / scale.VelocityScale, _incT);
        public double    TargetOrbitalEnergy() => 0.5 * _vT * _vT - 1.0 / _rT;
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            double gammaT = _gammaT;
            double rT     = _rT;
            double incT   = _incT;
            double vT     = _vT;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            ci = ApplyScalarConstraintV3(f, j, ci, FlightPathAngleConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, RadiusConstraint, new[] { rf }, new[] { ri });
            ci = ApplyScalarConstraintV3(f, j, ci, VelocityConstraint, new[] { vf }, new[] { vi });
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

            Dual VelocityConstraint(DualV3[] p)
            {
                return DualV3.Dot(p[0], p[0]) - vT * vT;
            }
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
