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
        private readonly V3     _ehat1;
        private readonly double _e1;
        private readonly V3     _ehat2;
        private readonly double _e2;
        private readonly V3     _eT;

        public Kepler5(double smaT, double eccT, double incT, double lanT, double argpT)
        {
            NumConstraints = 5;
            _smaT          = smaT;
            _eccT          = eccT;
            _incT          = Abs(ClampPi(incT));
            _lanT          = lanT;
            _argpT         = argpT;

            _hT = Astro.HvecFromKeplerian(1.0, _smaT, _eccT, _incT, _lanT);

            // r guaranteed not to be collinear with hT
            V3 r = V3.zero;
            r[_hT.min_magnitude_index] = 1.0;

            // basis vectors orthonormal to hT
            _ehat1 = V3.Cross(_hT, r).normalized;
            _ehat2 = V3.Cross(_hT, _ehat1).normalized;

            // projection of eT onto ehat1/ehat2
            _eT = Astro.EvecFromKeplerian(_eccT, _incT, _lanT, _argpT);
            _e1 = V3.Dot(_eT, _ehat1);
            _e2 = V3.Dot(_eT, _ehat2);
        }

        public ITerminal Rescale(Scale scale)  => new Kepler5(_smaT / scale.LengthScale, _eccT, _incT, _lanT, _argpT);
        public double    TargetOrbitalEnergy() => -1.0 / (2.0 * _smaT);
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            V3     hT    = _hT;
            V3     ehat1 = _ehat1;
            V3     ehat2 = _ehat2;
            double e1    = _e1;
            double e2    = _e2;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            ci = ApplyVectorConstraintV3(f, j, ci, AngularMomentumConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, EccVecConstraint1, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, EccVecConstraint2, new[] { rf, vf }, new[] { ri, vi });

            return;

            DualV3 AngularMomentumConstraint(DualV3[] p)
            {
                return DualV3.Cross(p[0], p[1]) - hT;
            }

            Dual EccVecConstraint1(DualV3[] p)
            {
                var    hf = DualV3.Cross(p[0], p[1]);
                DualV3 ef = DualV3.Cross(p[1], hf) - p[0].normalized;

                return e1 - DualV3.Dot(ef, ehat1);
            }

            Dual EccVecConstraint2(DualV3[] p)
            {
                var    hf = DualV3.Cross(p[0], p[1]);
                DualV3 ef = DualV3.Cross(p[1], hf) - p[0].normalized;

                return e2 - DualV3.Dot(ef, ehat2);
            }
        }

        /* doing FPA attachment on Kepler5 doesn't work since you wind up with a very different argp */
        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
