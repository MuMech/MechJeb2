/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.ODE
{
    using IVPFunc = Action<IList<double>, double, IList<double>>;

    public class Tsit5 : AbstractRungeKutta
    {
        protected override int Order               => 5;
        protected override int Stages              => 6;
        protected override int ErrorEstimatorOrder => 4;

        #region IntegrationConstants

        private const double A21 = 0.161;
        private const double A31 = -0.008480655492356989;
        private const double A32 = 0.335480655492357;
        private const double A41 = 2.8971530571054935;
        private const double A42 = -6.359448489975075;
        private const double A43 = 4.3622954328695815;
        private const double A51 = 5.325864828439257;
        private const double A52 = -11.748883564062828;
        private const double A53 = 7.4955393428898365;
        private const double A54 = -0.09249506636175525;
        private const double A61 = 5.86145544294642;
        private const double A62 = -12.92096931784711;
        private const double A63 = 8.159367898576159;
        private const double A64 = -0.071584973281401;
        private const double A65 = -0.028269050394068383;
        private const double A71 = 0.09646076681806523;
        private const double A72 = 0.01;
        private const double A73 = 0.4798896504144996;
        private const double A74 = 1.379008574103742;
        private const double A75 = -3.290069515436081;
        private const double A76 = 2.324710524099774;

        private const double C2 = 0.161;
        private const double C3 = 0.327;
        private const double C4 = 0.9;
        private const double C5 = 0.9800255409045097;

        private const double E1 = 0.0017800110522257773;
        private const double E2 = 0.0008164344596567463;
        private const double E3 = -0.007880878010261994;
        private const double E4 = 0.1447110071732629;
        private const double E5 = -0.5823571654525552;
        private const double E6 = 0.45808210592918686;
        private const double E7 = -0.015151515151515152;

        #endregion

        protected override void RKStep(IVPFunc f)
        {
            double h = Habs * Direction;

            K[1].CopyFrom(Dy);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A21 * Dy[i]);
            f(Ynew, T + C2 * h, K[2]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A31 * K[1][i] + A32 * K[2][i]);
            f(Ynew, T + C3 * h, K[3]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A41 * K[1][i] + A42 * K[2][i] + A43 * K[3][i]);
            f(Ynew, T + C4 * h, K[4]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A51 * K[1][i] + A52 * K[2][i] + A53 * K[3][i] + A54 * K[4][i]);
            f(Ynew, T + C5 * h, K[5]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A61 * K[1][i] + A62 * K[2][i] + A63 * K[3][i] + A64 * K[4][i] + A65 * K[5][i]);
            f(Ynew, T + h, K[6]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A71 * K[1][i] + A72 * K[2][i] + A73 * K[3][i] + A74 * K[4][i] + A75 * K[5][i] + A76 * K[6][i]);

            f(Ynew, T + h, K[7]);


            K[7].CopyTo(Dynew);
        }

        protected override double ScaledErrorNorm()
        {
            using var err = Vn.Rent(N);

            for (int i = 0; i < N; i++)
                err[i] = K[1][i] * E1 + K[2][i] * E2 + K[3][i] * E3 + K[4][i] * E4 + K[5][i] * E5 + K[6][i] * E6 + K[7][i] * E7;

            double error = 0.0;

            for (int i = 0; i < N; i++)
            {
                double scale = Atol + Rtol * Max(Abs(Y[i]), Abs(Ynew[i]));
                error += Powi(err[i] / scale, 2);
            }

            return Sqrt(error / N);
        }

        protected override void InitInterpolant()
        {
            // intentionally left blank
        }

        protected override void Interpolate(double x, Vn yout) => throw new NotImplementedException();
    }
}
