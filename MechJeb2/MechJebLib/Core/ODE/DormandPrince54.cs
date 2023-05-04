/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

#nullable enable

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<Vn, double, Vn>;
    using IVPEvent = Func<double, Vn, Vn, (double x, bool dir, bool stop)>;

    public class DormandPrince54 : AbstractRungeKutta
    {
        protected override int Order               => 5;
        protected override int Stages              => 6;
        protected override int ErrorEstimatorOrder => 4;

        #region IntegrationConstants

        // constants for DP5(4)7FM
        private const double A21 = 1.0 / 5.0;
        private const double A31 = 3.0 / 40.0;
        private const double A32 = 9.0 / 40.0;
        private const double A41 = 44.0 / 45.0;
        private const double A42 = -56.0 / 15.0;
        private const double A43 = 32.0 / 9.0;
        private const double A51 = 19372.0 / 6561.0;
        private const double A52 = -25360.0 / 2187.0;
        private const double A53 = 64448.0 / 6561.0;
        private const double A54 = -212.0 / 729.0;
        private const double A61 = 9017.0 / 3168.0;
        private const double A62 = -355.0 / 33.0;
        private const double A63 = 46732.0 / 5247.0;
        private const double A64 = 49.0 / 176.0;
        private const double A65 = -5103.0 / 18656.0;
        private const double A71 = 35.0 / 384.0;
        private const double A73 = 500.0 / 1113.0;
        private const double A74 = 125.0 / 192.0;
        private const double A75 = -2187.0 / 6784.0;
        private const double A76 = 11.0 / 84.0;

        private const double C2 = 1.0 / 5.0;
        private const double C3 = 3.0 / 10.0;
        private const double C4 = 4.0 / 5.0;
        private const double C5 = 8.0 / 9.0;

        private const double E1 = 71.0 / 57600.0;
        private const double E3 = -71.0 / 16695.0;
        private const double E4 = 71.0 / 1920.0;
        private const double E5 = -17253.0 / 339200.0;
        private const double E6 = 22.0 / 525.0;
        private const double E7 = -1.0 / 40.0;

        #endregion


        protected override void RKStep(IVPFunc f, double t, double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, Vn err)
        {
            double h = habs * direction;

            dy.CopyTo(K[1]);

            for (int i = 0; i < N; i++)
                ynew[i] = y[i] + h * (A21 * dy[i]);
            f(ynew, t + C2 * h, K[2]);

            for (int i = 0; i < N; i++)
                ynew[i] = y[i] + h * (A31 * K[1][i] + A32 * K[2][i]);
            f(ynew, t + C3 * h, K[3]);

            for (int i = 0; i < N; i++)
                ynew[i] = y[i] + h * (A41 * K[1][i] + A42 * K[2][i] + A43 * K[3][i]);
            f(ynew, t + C4 * h, K[4]);

            for (int i = 0; i < N; i++)
                ynew[i] = y[i] + h * (A51 * K[1][i] + A52 * K[2][i] + A53 * K[3][i] + A54 * K[4][i]);
            f(ynew, t + C5 * h, K[5]);

            for (int i = 0; i < N; i++)
                ynew[i] = y[i] + h * (A61 * K[1][i] + A62 * K[2][i] + A63 * K[3][i] + A64 * K[4][i] + A65 * K[5][i]);
            f(ynew, t + h, K[6]);

            for (int i = 0; i < N; i++)
                ynew[i] = y[i] + h * (A71 * K[1][i] + A73 * K[3][i] + A74 * K[4][i] + A75 * K[5][i] + A76 * K[6][i]);

            f(ynew, t + h, K[7]);

            for (int i = 0; i < N; i++)
                err[i] = K[1][i] * E1 + K[3][i] * E3 + K[4][i] * E4 + K[5][i] * E5 + K[6][i] * E6 + K[7][i] * E7;

            K[7].CopyTo(dynew);
        }

        protected override void InitInterpolant(double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew)
        {
            // intentionally left blank for now
        }

        protected override void Interpolate(double x, double t, double h, Vn y, Vn yout)
        {
            double s = (x - t) / h;
            double s2 = s * s;
            double s3 = s * s2;
            double s4 = s2 * s2;

            double bf1 = 1.0 / 11282082432.0 * (157015080.0 * s4 - 13107642775.0 * s3 + 34969693132.0 * s2 - 32272833064.0 * s + 11282082432.0);
            double bf3 = -100.0 / 32700410799.0 * s * (15701508.0 * s3 - 914128567.0 * s2 + 2074956840.0 * s - 1323431896.0);
            double bf4 = 25.0 / 5641041216.0 * s * (94209048.0 * s3 - 1518414297.0 * s2 + 2460397220.0 * s - 889289856.0);
            double bf5 = -2187.0 / 199316789632.0 * s * (52338360.0 * s3 - 451824525.0 * s2 + 687873124.0 * s - 259006536.0);
            double bf6 = 11.0 / 2467955532.0 * s * (106151040.0 * s3 - 661884105.0 * s2 + 946554244.0 * s - 361440756.0);
            double bf7 = 1.0 / 29380423.0 * s * (1.0 - s) * (8293050.0 * s2 - 82437520.0 * s + 44764047.0);

            for (int i = 0; i < N; i++)
            {
                yout[i] = y[i] + h * s * (bf1 * K[1][i] + bf3 * K[3][i] + bf4 * K[4][i] + bf5 * K[5][i] + bf6 * K[6][i] +
                                          bf7 * K[7][i]);
            }
        }
    }
}
