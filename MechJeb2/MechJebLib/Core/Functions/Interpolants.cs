/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.Core.Functions
{
    public class Interpolants
    {
        public static void CubicHermiteInterpolant(double x1, IList<double> y1, IList<double> yp1, double x2, IList<double> y2,
            IList<double> yp2, double x, int n, IList<double> y)
        {
            double t = (x - x1) / (x2 - x1);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;
            for (int i = 0; i < n; i++)
                y[i] = h00 * y1[i] + h10 * (x2 - x1) * yp1[i] + h01 * y2[i] + h11 * (x2 - x1) * yp2[i];
        }

        public static double CubicHermiteInterpolant(double x1, double y1, double yp1, double x2, double y2,
            double yp2, double x)
        {
            double t = (x - x1) / (x2 - x1);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;

            return h00 * y1 + h10 * (x2 - x1) * yp1 + h01 * y2 + h11 * (x2 - x1) * yp2;
        }

        public static V3 CubicHermiteInterpolant(double x1, V3 y1, V3 yp1, double x2, V3 y2,
            V3 yp2, double x)
        {
            double t = (x - x1) / (x2 - x1);
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10 = t3 - 2 * t2 + t;
            double h01 = -2 * t3 + 3 * t2;
            double h11 = t3 - t2;

            return h00 * y1 + h10 * (x2 - x1) * yp1 + h01 * y2 + h11 * (x2 - x1) * yp2;
        }
    }
}
