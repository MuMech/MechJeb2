/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Functions;
using MechJebLib.Primitives;
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
        private readonly V3 _hT;
        private readonly V3 _pT;
        private readonly V3 _qT;
        private readonly double _epT;
        private readonly double _eqT;

        public Kepler5(double smaT, double eccT, double incT, double lanT, double argpT)
        {
            NumConstraints = 5;
            _smaT = smaT;
            _eccT = eccT;
            _incT = incT;
            _lanT = lanT;
            _argpT = argpT;

            _hT = Astro.HvecFromKeplerian(1.0, _smaT, _eccT, Abs(_incT), _lanT);
            V3 eT = Astro.EvecFromKeplerian(_eccT, Abs(_incT), _lanT, _argpT);

            (_pT, _qT) = _hT.nullspace;
            _epT = V3.Dot(eT, _pT);
            _eqT = V3.Dot(eT, _qT);
        }

        public ITerminal Rescale(Scale scale)  => new Kepler5(_smaT / scale.LengthScale, _eccT, _incT, _lanT, _argpT);
        public double    TargetOrbitalEnergy() => -1.0 / (2.0 * _smaT);
        public double    IncT()                => _incT;

        public int NumConstraints { get; }

        public void Constraints(double[] x, (int, int, int) ri, (int, int, int) vi, double[] f, alglib.sparsematrix j, ref int ci)
        {
            V3 hT = _hT;
            V3 pT = _pT;
            V3 qT = _qT;
            double epT = _epT;
            double eqT = _eqT;

            var rf = V3.CopyFromIndices(x, ri);
            var vf = V3.CopyFromIndices(x, vi);

            ci = ApplyVectorConstraintV3(f, j, ci, AngularMomentumConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, EccVecPConstraint, new[] { rf, vf }, new[] { ri, vi });
            ci = ApplyScalarConstraintV3(f, j, ci, EccVecQConstraint, new[] { rf, vf }, new[] { ri, vi });

            return;

            DualV3 AngularMomentumConstraint(DualV3[] p) => DualV3.Cross(p[0], p[1]) - hT;

            Dual EccVecPConstraint(DualV3[] p) => DualV3.Dot(EccVec(p), pT) - epT;

            Dual EccVecQConstraint(DualV3[] p) => DualV3.Dot(EccVec(p), qT) - eqT;

            static DualV3 EccVec(DualV3[] p) => DualV3.Cross(p[1], DualV3.Cross(p[0], p[1])) - p[0].normalized;
        }

        /* doing FPA attachment on Kepler5 doesn't work since you wind up with a very different argp */
        public ITerminal GetFPA() => this;

        public bool IsFPA() => true;
    }
}
