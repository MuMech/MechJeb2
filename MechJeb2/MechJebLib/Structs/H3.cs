/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using MechJebLib.Maths;
using MechJebLib.Utils;

#nullable enable

namespace MechJebLib.Structs
{
    public class H3 : HBase<Vector3d>
    {
        private static readonly ObjectPool<H3> _pool = new ObjectPool<H3>(New);

        private static H3 New()
        {
            return new H3();
        }

        public static H3 Get()
        {
            H3 h = _pool.Get();
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Return(this);
        }

        protected override Vector3d Allocate()
        {
            return Vector3d.zero;
        }

        protected override Vector3d Allocate(Vector3d value)
        {
            return value;
        }

        protected override void Subtract(Vector3d a, Vector3d b, ref Vector3d result)
        {
            result = a - b;
        }

        protected override void Divide(Vector3d a, double b, ref Vector3d result)
        {
            result = a / b;
        }

        protected override void Multiply(Vector3d a, double b, ref Vector3d result)
        {
            result = a * b;
        }

        protected override void Addition(Vector3d a, Vector3d b, ref Vector3d result)
        {
            result = a + b;
        }

        protected override Vector3d Interpolant(double x1, Vector3d y1, Vector3d yp1, double x2, Vector3d y2, Vector3d yp2, double x)
        {
            return Functions.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x);
        }
    }
}
