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
            _incT          = Abs(ClampPi(incT));
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
            V3 hT = _hT;
            V3 eT = _eT;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            ci = ApplyVectorConstraintV3(f, j, ci, AngularMomentumConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyVectorConstraintV3(f, j, ci, EccVecConstraint, new[] { rf, vf }, new[] { ri, vi });

            return;

            DualV3 AngularMomentumConstraint(DualV3[] p)
            {
                return DualV3.Cross(p[0], p[1]) - hT;
            }

            DualV3 EccVecConstraint(DualV3[] p)
            {
                return DualV3.Cross(p[1], DualV3.Cross(p[0], p[1])) - p[0].normalized - eT;
            }
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
