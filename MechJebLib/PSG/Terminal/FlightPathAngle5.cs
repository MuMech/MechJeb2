/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static MechJebLib.Utils.AutoDiff;
using static System.Math;

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
            _incT          = Abs(ClampPi(incT));

            _hT = Astro.HvecFromFlightPathAngle(_rT, _vT, _gammaT, _incT, _lanT);
        }

        public ITerminal Rescale(Scale scale) =>
            new FlightPathAngle5(_gammaT, _rT / scale.LengthScale, _vT / scale.VelocityScale, _incT, _lanT);

        public double TargetOrbitalEnergy() => 0.5 * _vT * _vT - 1.0 / _rT;
        public double IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            V3 hT = _hT;
            double gammaT = _gammaT;
            double rT = _rT;

            var    rf = V3.CopyFromIndices(x, ri);
            var    vf = V3.CopyFromIndices(x, vi);

            ci = ApplyVectorConstraintV3(f, j, ci, AngularMomentumConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, FlightPathAngleConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, RadiusConstraint, new[] { rf }, new[] { ri });

            return;

            DualV3 AngularMomentumConstraint(DualV3[] p)
            {
                return DualV3.Cross(p[0], p[1]) - hT;
            }

            Dual FlightPathAngleConstraint(DualV3[] p)
            {
                return DualV3.Dot(p[0], p[1]) - Sin(gammaT);
            }

            Dual RadiusConstraint(DualV3[] p)
            {
                return DualV3.Dot(p[0], p[0]) - rT * rT;
            }
        }

        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
