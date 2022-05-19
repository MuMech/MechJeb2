/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using MechJebLib.Maths;

#nullable enable

namespace MechJebLib.Structs
{
    public class H1 : HBase<double>
    {
        protected override double Allocate()
        {
            return 0.0;
        }

        protected override double Allocate(double value)
        {
            return value;
        }

        protected override void Subtract(double a, double b, ref double result)
        {
            result = a - b;
        }

        protected override void Divide(double a, double b, ref double result)
        {
            result = a / b;
        }

        protected override void Multiply(double a, double b, ref double result)
        {
            result = a * b;
        }

        protected override void Addition(double a, double b, ref double result)
        {
            result = a + b;
        }

        protected override double Interpolant(double x1, double y1, double yp1, double x2, double y2, double yp2, double x)
        {
            return Functions.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x);
        }
    }
}
