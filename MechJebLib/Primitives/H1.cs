/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Text;
using MechJebLib.Utils;
using static System.FormattableString;

namespace MechJebLib.Primitives
{
    public class H1 : HBase<double>
    {
        private static readonly ObjectPool<H1> _pool = new ObjectPool<H1>(New, Clear);

        private H1()
        {
        }

        private static H1 New() => new H1();

        public static H1 Get(bool unityCompat = false)
        {
            H1 h = _pool.Borrow();
            h.UnityCompat = unityCompat;
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Release(this);
        }

        protected override double Allocate() => 0.0;

        protected override double Allocate(double value) => value;

        protected override void Subtract(double a, double b, ref double result) => result = a - b;

        protected override void Divide(double a, double b, ref double result) => result = a / b;

        protected override void Multiply(double a, double b, ref double result) => result = a * b;

        protected override void Addition(double a, double b, ref double result) => result = a + b;

        protected override double Interpolant(double x1, double y1, double yp1, double x2, double y2, double yp2, double x) =>
            Functions.Interpolants.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x);

        private static void Clear(H1 h) => h.Clear();

        // Debug dump of the raw keyframes, in a form that mirrors HBase.Add(time, value, inTangent, outTangent) so the
        // curve can be transcribed into a test fixture.  HBase/H3/Hn are effectively deprecated so this lives only on H1.
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(Invariant($"H1(unityCompat={UnityCompat})["));
            for (int i = 0; i < _list.Count; i++)
            {
                HFrame<double> f = _list.Values[i];
                if (i > 0)
                    sb.Append(", ");
                sb.Append(Invariant($"(t={f.Time}, v={f.Value}, in={f.InTangent}, out={f.OutTangent})"));
            }

            sb.Append("]");
            return sb.ToString();
        }
    }
}
