/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Utils;
using static System.FormattableString;

namespace MechJebLib.Interpolants
{
    // the cubic Hermite interpolant over [t, t + h], stored in power-basis form in s = (x - t) / h:
    //   p(s) = c0 + c1·s + c2·s² + c3·s³
    internal class CubicHermiteDoubleNode : InterpolantNode<double>
    {
        private static readonly ObjectPool<CubicHermiteDoubleNode> _pool = new ObjectPool<CubicHermiteDoubleNode>(New, Clear);

        public static CubicHermiteDoubleNode Rent(double t, double h, double y, double dy, double ynew, double dynew)
        {
            // a zero-width node would interpolate 0/0, so it degenerates to a constant
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (t + h == t)
                return RentPolynomial(t, 0, y, 0, 0, 0);

            double delta = ynew - y;
            return RentPolynomial(t, 1.0 / h, y, h * dy, 3 * delta - h * (2 * dy + dynew), h * (dy + dynew) - 2 * delta);
        }

        // rebuilds a node from the polynomial form, taking the arguments in the order ToString() prints them
        public static CubicHermiteDoubleNode RentPolynomial(double t, double invH, double c0, double c1, double c2, double c3)
        {
            CubicHermiteDoubleNode node = _pool.Borrow();
            node._t = t;
            node._invH = invH;
            node._c0 = c0;
            node._c1 = c1;
            node._c2 = c2;
            node._c3 = c3;
            return node;
        }

        private CubicHermiteDoubleNode() { }

        private static CubicHermiteDoubleNode New() => new CubicHermiteDoubleNode();

        private static void Clear(CubicHermiteDoubleNode o)
        {
            o.LeftT = o.RightT = 0;
            o._t = o._invH = 0;
            o._c0 = o._c1 = o._c2 = o._c3 = 0;
        }

        private double _t;
        private double _invH;
        private double _c0;
        private double _c1;
        private double _c2;
        private double _c3;

        public override double Evaluate(double x)
        {
            double s = (x - _t) * _invH;
            return _c0 + s * (_c1 + s * (_c2 + s * _c3));
        }

        public override InterpolantNode<double> DeepCopy() => CopyBoundsTo(RentPolynomial(_t, _invH, _c0, _c1, _c2, _c3));

        public override void Dispose() => _pool.Release(this);

        public override string ToString() =>
            Invariant(
                $"[CubicHermiteDoubleNode leftT={LeftT:G17} rightT={RightT:G17} t={_t:G17} invH={_invH:G17} c0={_c0:G17} c1={_c1:G17} c2={_c2:G17} c3={_c3:G17}]");
    }
}
