/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    // the cubic Hermite interpolant over [t, t + h], stored in power-basis form in s = (x - t) / h:
    //   p(s) = c0 + c1·s + c2·s² + c3·s³
    internal class CubicHermiteVecNode : InterpolantNode<Vec>
    {
        private static readonly ObjectPool<CubicHermiteVecNode> _pool = new ObjectPool<CubicHermiteVecNode>(New, Clear);

        public static CubicHermiteVecNode Rent(double t, double h, Vec y, Vec dy, Vec ynew, Vec dynew)
        {
            CubicHermiteVecNode node = _pool.Borrow();
            node._t = t;
            node._c0 = y.Dup();

            // a zero-width node (e.g. from a zero-length PSG phase) would interpolate 0/0, so it degenerates to a constant
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (t + h == t)
            {
                node._invH = 0;
                node._c1 = Vec.Rent(y.Length, true);
                node._c2 = Vec.Rent(y.Length, true);
                node._c3 = Vec.Rent(y.Length, true);
                return node;
            }

            node._invH = 1.0 / h;
            node._c1 = dy.Dup().Scal(h);                                            // h·dy
            node._c2 = ynew.Dup().Sub(y).Scal(3).Axpy(-2 * h, dy).Axpy(-h, dynew); // 3(ynew - y) - h(2·dy + dynew)
            node._c3 = y.Dup().Sub(ynew).Scal(2).Axpy(h, dy).Axpy(h, dynew);       // 2(y - ynew) + h(dy + dynew)
            return node;
        }

        private CubicHermiteVecNode() { }

        private static CubicHermiteVecNode New() => new CubicHermiteVecNode();

        private static void Clear(CubicHermiteVecNode o)
        {
            o.LeftT = o.RightT = 0;
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._c0 = o._c1 = o._c2 = o._c3 = null!;
            o._t = 0;
            o._invH = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private double _t;
        private double _invH;
        private Vec _c0 = null!;
        private Vec _c1 = null!;
        private Vec _c2 = null!;
        private Vec _c3 = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        public override Vec Evaluate(double x)
        {
            var yout = Vec.Rent(_c0.Length);
            double s = (x - _t) * _invH;
            for (int i = 0; i < yout.Length; i++)
                yout[i] = _c0[i] + s * (_c1[i] + s * (_c2[i] + s * _c3[i]));
            return yout;
        }

        public override void Dispose()
        {
            _c0.Dispose();
            _c1.Dispose();
            _c2.Dispose();
            _c3.Dispose();
            _pool.Release(this);
        }
    }
}
