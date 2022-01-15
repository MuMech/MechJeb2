using System;

namespace MuMech.MathJ
{
    public class Functions
    {
               public static void CubicHermiteInterpolant(double x1, double[] y1, double[] yp1, double x2, double[] y2,
            double[] yp2, double x, int n, double[] y)
        {
            var t = (x - x1) / (x2 - x1);
            var h00 = (2 * t * t * t) - (3 * t * t) + 1;
            var h10 = (t * t * t) - (2 * t * t) + t;
            var h01 = (-2 * t * t * t) + (3 * t * t);
            var h11 = (t * t * t) - (t * t);
            for (var i = 0; i < n; i++)
            {
                y[i] = (h00 * y1[i]) + (h10 * (x2 - x1) * yp1[i]) + (h01 * y2[i]) + (h11 * (x2 - x1) * yp2[i]);
            }
        }

        public static double CubicHermiteInterpolant(double x1, double y1, double yp1, double x2, double y2,
            double yp2, double x)
        {
            var t = (x - x1) / (x2 - x1);
            var h00 = (2 * t * t * t) - (3 * t * t) + 1;
            var h10 = (t * t * t) - (2 * t * t) + t;
            var h01 = (-2 * t * t * t) + (3 * t * t);
            var h11 = (t * t * t) - (t * t);

            return (h00 * y1) + (h10 * (x2 - x1) * yp1) + (h01 * y2) + (h11 * (x2 - x1) * yp2);
        }

        private static double InterpolantRootfinder(double wt1, double wt2, double x)
        {
            var t = 0.5;

            if (wt1 == 1 / 3.0d && wt2 == 1 / 3.0d)
            {
                return x;
            }

            var wt2S = 1 - wt2;

            var i = 10;

            // it is about 2.5x faster to do this inline that it is to call an abstract function with callbacks
            while (i-- > 0)
            {
                var t2 = 1 - t;
                var fg = (3 * t2 * t2 * t * wt1) + (3 * t2 * t * t * wt2S) + (t * t * t) - x;
                if (Math.Abs(fg) < 2 * Utils.EPS)
                {
                    break;
                }

                // third order householder method
                var fpg = (3 * t2 * t2 * wt1) + (6 * t2 * t * (wt2S - wt1)) + (3 * t * t * (1 - wt2S));
                var fppg = (6 * t2 * (wt2S - (2 * wt1))) + (6 * t * (1 - (2 * wt2S) + wt1));
                var fpppg = (18 * wt1) - (18 * wt2S) + 6;

                t -= ((6 * fg * fpg * fpg) - (3 * fg * fg * fppg)) / ((6 * fpg * fpg * fpg) - (6 * fg * fpg * fppg) + (fg * fg * fpppg));
            }

            return t;
        }

        public static double AnimationCurveInterpolant(double x1, double y1, double yp1, double wt1, double x2, double y2, double yp2, double wt2,
            double x)
        {
            // gracefully handle interpolants with a single node
            if (x1 == x2)
            {
                // this looks backwards but it isn't
                if (x <= x1)
                {
                    return y2 + ((x - x2) * yp2);
                }

                return y1 + ((x - x2) * yp1);
            }

            var dx = x2 - x1;
            x = (x - x1) / dx;
            var dy = y2 - y1;

            var t = InterpolantRootfinder(wt1, wt2, x);
            var t2 = 1 - t;

            var y = (3 * t2 * t2 * t * wt1 * yp1 * dx) + (3 * t2 * t * t * (dy - (wt2 * yp2 * dx))) + (t * t * t * dy);

            return y + y1;
        }

        public static Vector3d AnimationCurveInterpolant(double x1, Vector3d y1, Vector3d yp1, double wt1, double x2, Vector3d y2, Vector3d yp2, double wt2, double x)
        {
            // gracefully handle interpolants with a single node
            if (x1 == x2)
            {
                // this looks backwards but it isn't
                if (x <= x1)
                {
                    return y2 + ((x - x2) * yp2);
                }

                return y1 + ((x - x2) * yp1);
            }

            var dx = x2 - x1;
            x = (x - x1) / dx;
            var dy = y2 - y1;

            var t = InterpolantRootfinder(wt1, wt2, x);
            var t2 = 1 - t;

            var y = (3 * t2 * t2 * t * wt1 * yp1 * dx) + (3 * t2 * t * t * (dy - (wt2 * yp2 * dx))) + (t * t * t * dy);

            return y + y1;
        }

        public static void AnimationCurveInterpolant(int n, double x1, double[] y1, double[] yp1, double wt1, double x2, double[] y2, double[] yp2,
            double wt2, double x, double[] y)
        {
            // gracefully handle interpolants with a single node
            if (x1 == x2)
            {
                // this looks backwards but it isn't
                if (x <= x1)
                {
                    for (var i = 0; i < n; i++)
                    {
                        y[i] = y2[i] + ((x - x2) * yp2[i]);
                    }
                    return;
                }

                for (var i = 0; i < n; i++)
                {
                    y[i] = y1[i] + ((x - x2) * yp1[i]);
                }
                return;
            }

            var dx = x2 - x1;
            var xs = (x - x1) / dx;

            var t = InterpolantRootfinder(wt1, wt2, xs);
            var t2 = 1 - t;

            for (var i = 0; i < n; i++)
            {
                var dy = y2[i] - y1[i];
                y[i] = (3 * t2 * t2 * t * wt1 * yp1[i] * dx) + (3 * t2 * t * t * (dy - (wt2 * yp2[i] * dx))) + (t * t * t * dy);
                y[i] = y[i] + y1[i];
            }
        }

        public static double[] AnimationCurveInterpolant(int n, double x1, double[] y1, double[] yp1, double wt1, double x2, double[] y2,
            double[] yp2, double wt2, double xs)
        {
            var y = Utils.DoublePool.Rent(n);

            AnimationCurveInterpolant(n, x1, y1, yp1, wt1, x2, y2, yp2, wt2, xs, y);

            return y;
        }
    }
}
