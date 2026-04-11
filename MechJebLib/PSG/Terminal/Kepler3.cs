/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static MechJebLib.Utils.AutoDiff;
using static System.Math;

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
            _incT          = incT;
            _hTm           = Astro.HmagFromKeplerian(1.0, _smaT, _eccT);
            _energyT       = -1.0 / (2.0 * _smaT);
        }

        public ITerminal Rescale(Scale scale)  => new Kepler3(_smaT / scale.LengthScale, _eccT, _incT);
        public double    TargetOrbitalEnergy() => -1.0 / (2.0 * _smaT);
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            double hTm     = _hTm;
            double incT    = _incT;
            double energyT = _energyT;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            // angular momentum / semiparameter
            // energy / sma (problematic because you can't square it + infinite sma for parabolic)
            // periapsis and/or apoapsis
            // eccvec magnitude / eccentricity

            ci = ApplyScalarConstraintV3(f, j, ci, AngularVelocityMagnitudeConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, OrbitalEnergyConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, InclinationConstraint, new[] { rf, vf }, new[] { ri, vi });

            return;

            Dual AngularVelocityMagnitudeConstraint(DualV3[] p)
            {
                return 0.5 * DualV3.Cross(p[0], p[1]).sqrMagnitude - 0.5 * hTm * hTm;
            }

            Dual OrbitalEnergyConstraint(DualV3[] p)
            {
                return 0.5 * DualV3.Dot(p[1], p[1]) - 1.0 / p[0].magnitude - energyT;
            }

            Dual InclinationConstraint(DualV3[] p)
            {
                return DualV3.Cross(p[0], p[1]).normalized.z - Cos(incT);
            }
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
