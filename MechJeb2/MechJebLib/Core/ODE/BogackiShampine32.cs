/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using MechJebLib.Primitives;

namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<Vn, double, Vn>;
    using IVPEvent = Func<double, Vn, Vn, (double x, bool dir, bool stop)>;

    public class BogackiShampine32 : AbstractRungeKutta
    {
        protected override int Order               => 3;
        protected override int Stages              => 3;
        protected override int ErrorEstimatorOrder => 2;

        #region IntegrationConstants

        private const double A21 = 0.5;
        private const double A32 = 0.75;
        private const double A41 = 2.0 / 9.0;
        private const double A42 = 1.0 / 3.0;
        private const double A43 = 4.0 / 9.0;

        private const double C2 = 0.5;
        private const double C3 = 0.75;

        private const double E1 = -5.0 / 72.0;
        private const double E2 = 1.0 / 12.0;
        private const double E3 = 1.0 / 9.0;
        private const double E4 = -0.125;

        #endregion

        protected override void RKStep(IVPFunc f, Vn err)
        {
            double h = Habs * Direction;

            Dy.CopyTo(K[1]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A21 * Dy[i]);
            f(Ynew, T + C2 * h, K[2]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A32 * K[2][i]);
            f(Ynew, T + C3 * h, K[3]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A41 * K[1][i] + A42 * K[2][i] + A43 * K[3][i]);
            f(Ynew, T + h, K[4]);

            for (int i = 0; i < N; i++)
                err[i] = K[1][i] * E1 + K[2][i] * E2 + K[3][i] * E3 + K[4][i] * E4;

            K[4].CopyTo(Dynew);
        }

        protected override void InitInterpolant()
        {
            // intentionally left blank
        }

        protected override void Interpolate(double x, Vn yout)
        {
            /*
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

            for (int i = 0; i < n; i++)
            {
                yout[i] = y[i] + h * s * (bf1 * data.K1[i] + bf3 * data.K3[i] + bf4 * data.K4[i] + bf5 * data.K5[i] + bf6 * data.K6[i] +
                                          bf7 * data.K7[i]);
            }
            */
        }
    }
}
