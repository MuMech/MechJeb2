/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.ODE
{
    using IVPFunc = Action<Vec, double, Vec>;

    public class Tsit5 : AbstractRungeKutta
    {
        public override int Order               => 5;
        public override int Stages              => 6;
        public override int ErrorEstimatorOrder => 4;

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

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vec _k1 = null!;
        private Vec _k2 = null!;
        private Vec _k3 = null!;
        private Vec _k4 = null!;
        private Vec _k5 = null!;
        private Vec _k6 = null!;
        private Vec _k7 = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        protected override void RKStep(IVPFunc f)
        {
            double h = Habs * Direction;

            _k1.CopyFrom(Dy);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A21 * Dy[i]);
            f(Ynew, T + C2 * h, _k2);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A31 * _k1[i] + A32 * _k2[i]);
            f(Ynew, T + C3 * h, _k3);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A41 * _k1[i] + A42 * _k2[i] + A43 * _k3[i]);
            f(Ynew, T + C4 * h, _k4);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A51 * _k1[i] + A52 * _k2[i] + A53 * _k3[i] + A54 * _k4[i]);
            f(Ynew, T + C5 * h, _k5);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A61 * _k1[i] + A62 * _k2[i] + A63 * _k3[i] + A64 * _k4[i] + A65 * _k5[i]);
            f(Ynew, T + h, _k6);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A71 * _k1[i] + A72 * _k2[i] + A73 * _k3[i] + A74 * _k4[i] + A75 * _k5[i] + A76 * _k6[i]);

            f(Ynew, T + h, _k7);


            _k7.CopyTo(Dynew);
        }

        protected override void Init()
        {
            base.Init();
            _k1 = Vec.Rent(N);
            _k2 = Vec.Rent(N);
            _k3 = Vec.Rent(N);
            _k4 = Vec.Rent(N);
            _k5 = Vec.Rent(N);
            _k6 = Vec.Rent(N);
            _k7 = Vec.Rent(N);
        }

        protected override void Cleanup()
        {
            _k1.Dispose();
            _k2.Dispose();
            _k3.Dispose();
            _k4.Dispose();
            _k5.Dispose();
            _k6.Dispose();
            _k7.Dispose();
        }

        protected override double ScaledErrorNorm()
        {
            using var err = Vec.Rent(N);

            for (int i = 0; i < N; i++)
                err[i] = _k1[i] * E1 + _k2[i] * E2 + _k3[i] * E3 + _k4[i] * E4 + _k5[i] * E5 + _k6[i] * E6 + _k7[i] * E7;

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

        protected override void Interpolate(double x, Vec yout) => throw new NotImplementedException();
    }
}
