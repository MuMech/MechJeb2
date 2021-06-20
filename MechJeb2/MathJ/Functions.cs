using System;

namespace MuMech.MathJ
{
    public class Functions
    {
               public static void CubicHermiteInterpolant(double x1, double[] y1, double[] yp1, double x2, double[] y2,
            double[] yp2, double x, int n, double[] y)
        {
            double t = (x - x1) / (x2 - x1);
            double h00 = 2 * t * t * t - 3 * t * t + 1;
            double h10 = t * t * t - 2 * t * t + t;
            double h01 = -2 * t * t * t + 3 * t * t;
            double h11 = t * t * t - t * t;
            for (int i = 0; i < n; i++)
                y[i] = h00 * y1[i] + h10 * (x2 - x1) * yp1[i] + h01 * y2[i] + h11 * (x2 - x1) * yp2[i];
        }

        public static double CubicHermiteInterpolant(double x1, double y1, double yp1, double x2, double y2,
            double yp2, double x)
        {
            double t = (x - x1) / (x2 - x1);
            double h00 = 2 * t * t * t - 3 * t * t + 1;
            double h10 = t * t * t - 2 * t * t + t;
            double h01 = -2 * t * t * t + 3 * t * t;
            double h11 = t * t * t - t * t;

            return h00 * y1 + h10 * (x2 - x1) * yp1 + h01 * y2 + h11 * (x2 - x1) * yp2;
        }

        private static double InterpolantRootfinder(double wt1, double wt2, double x)
        {
            double t = 0.5;

            if (wt1 == 1 / 3.0d && wt2 == 1 / 3.0d) return x;

            double wt2S = 1 - wt2;

            int i = 10;

            // it is about 2.5x faster to do this inline that it is to call an abstract function with callbacks
            while (i-- > 0)
            {
                double t2 = 1 - t;
                double fg = 3 * t2 * t2 * t * wt1 + 3 * t2 * t * t * wt2S + t * t * t - x;
                if (Math.Abs(fg) < 2 * Utils.EPS)
                    break;

                // third order householder method
                double fpg = 3 * t2 * t2 * wt1 + 6 * t2 * t * (wt2S - wt1) + 3 * t * t * (1 - wt2S);
                double fppg = 6 * t2 * (wt2S - 2 * wt1) + 6 * t * (1 - 2 * wt2S + wt1);
                double fpppg = 18 * wt1 - 18 * wt2S + 6;

                t -= (6 * fg * fpg * fpg - 3 * fg * fg * fppg) / (6 * fpg * fpg * fpg - 6 * fg * fpg * fppg + fg * fg * fpppg);
            }

            return t;
        }

        public static double AnimationCurveInterpolant(double x1, double y1, double yp1, double wt1, double x2, double y2, double yp2, double wt2,
            double x)
        {
            double dx = x2 - x1;
            x = (x - x1) / dx;
            double dy = y2 - y1;

            double t = InterpolantRootfinder(wt1, wt2, x);
            double t2 = 1 - t;

            double y = 3 * t2 * t2 * t * wt1 * yp1 * dx + 3 * t2 * t * t * (dy - wt2 * yp2 * dx) + t * t * t * dy;

            return y + y1;
        }

        public static Vector3d AnimationCurveInterpolant(double x1, Vector3d y1, Vector3d yp1, double wt1, double x2, Vector3d y2, Vector3d yp2, double wt2, double x)
        {
            double dx = x2 - x1;
            x = (x - x1) / dx;
            Vector3d dy = y2 - y1;

            double t = InterpolantRootfinder(wt1, wt2, x);
            double t2 = 1 - t;

            Vector3d y = 3 * t2 * t2 * t * wt1 * yp1 * dx + 3 * t2 * t * t * (dy - wt2 * yp2 * dx) + t * t * t * dy;

            return y + y1;
        }

        public static void AnimationCurveInterpolant(int n, double x1, double[] y1, double[] yp1, double wt1, double x2, double[] y2, double[] yp2,
            double wt2, double x, double[] y)
        {
            double dx = x2 - x1;
            double xs = (x - x1) / dx;

            double t = InterpolantRootfinder(wt1, wt2, xs);
            double t2 = 1 - t;

            for (int i = 0; i < n; i++)
            {
                double dy = y2[i] - y1[i];
                y[i] = 3 * t2 * t2 * t * wt1 * yp1[i] * dx + 3 * t2 * t * t * (dy - wt2 * yp2[i] * dx) + t * t * t * dy;
                y[i] = y[i] + y1[i];
            }
        }

        public static double[] AnimationCurveInterpolant(int n, double x1, double[] y1, double[] yp1, double wt1, double x2, double[] y2,
            double[] yp2, double wt2, double xs)
        {
            double[] y = Utils.DoublePool.Rent(n);

            AnimationCurveInterpolant(n, x1, y1, yp1, wt1, x2, y2, yp2, wt2, xs, y);

            return y;
        }
    }
}
