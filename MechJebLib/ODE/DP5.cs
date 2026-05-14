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

    /// <summary>
    ///     https://doi.org/10.1016/0771-050X(80)90013-3
    ///     https://github.com/PyNumAl/Python-Numerical-Analysis/blob/main/Initial-Value%20Problems/RK%20tableaus/DP54.txt
    ///     https://github.com/blackstonep/Numerical-Recipes/blob/master/stepperdopr5.h
    ///     https://github.com/SciML/OrdinaryDiffEq.jl/blob/master/src/tableaus/low_order_rk_tableaus.jl
    ///     https://github.com/scipy/scipy/blob/main/scipy/integrate/_ivp/rk.py
    ///     https://github.com/zachjweiner/scipy/blob/8ba609c313f09bf2a333849ca3ac2bd24d7655e7/scipy/integrate/_ivp/rk.py
    /// </summary>
    public class DP5 : AbstractRungeKutta
    {
        public override int Order               => 5;
        public override int Stages              => 6;
        public override int ErrorEstimatorOrder => 4;

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

        private const double B10 = 11282082432.0 / 11282082432.0;
        private const double B11 = -32272833064.0 / 11282082432.0;
        private const double B12 = 34969693132.0 / 11282082432.0;
        private const double B13 = -13107642775.0 / 11282082432.0;
        private const double B14 = 157015080.0 / 11282082432.0;

        private const double B31 = -100 * -1323431896.0 / 32700410799.0;
        private const double B32 = -100 * 2074956840.0 / 32700410799.0;
        private const double B33 = -100 * -914128567.0 / 32700410799.0;
        private const double B34 = -100 * 15701508.0 / 32700410799.0;

        private const double B41 = 25.0 * -889289856.0 / 5641041216.0;
        private const double B42 = 25.0 * 2460397220.0 / 5641041216.0;
        private const double B43 = 25.0 * -1518414297.0 / 5641041216.0;
        private const double B44 = 25.0 * 94209048.0 / 5641041216.0;

        private const double B51 = -2187.0 * -259006536.0 / 199316789632.0;
        private const double B52 = -2187.0 * 687873124.0 / 199316789632.0;
        private const double B53 = -2187.0 * -451824525.0 / 199316789632.0;
        private const double B54 = -2187.0 * 52338360.0 / 199316789632.0;

        private const double B61 = 11.0 * -361440756.0 / 2467955532.0;
        private const double B62 = 11.0 * 946554244.0 / 2467955532.0;
        private const double B63 = 11.0 * -661884105.0 / 2467955532.0;
        private const double B64 = 11.0 * 106151040.0 / 2467955532.0;

        private const double B71 = 44764047.0 / 29380423.0;
        private const double B72 = -82437520.0 / 29380423.0;
        private const double B73 = 8293050.0 / 29380423.0;

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

            Ynew.LinComb1(Y, h * A21, Dy);
            f(Ynew, T + C2 * h, _k2);

            Ynew.LinComb2(Y, h * A31, _k1, h * A32, _k2);
            f(Ynew, T + C3 * h, _k3);

            Ynew.LinComb3(Y, h * A41, _k1, h * A42, _k2, h * A43, _k3);
            f(Ynew, T + C4 * h, _k4);

            Ynew.LinComb4(Y, h * A51, _k1, h * A52, _k2, h * A53, _k3, h * A54, _k4);
            f(Ynew, T + C5 * h, _k5);

            Ynew.LinComb5(Y, h * A61, _k1, h * A62, _k2, h * A63, _k3, h * A64, _k4, h * A65, _k5);
            f(Ynew, T + h, _k6);

            Ynew.LinComb5(Y, h * A71, _k1, h * A73, _k3, h * A74, _k4, h * A75, _k5, h * A76, _k6);
            f(Ynew, T + h, _k7);

            _k7.CopyTo(Dynew);
        }

        protected override double ScaledErrorNorm()
        {
            using var err = Vec.Rent(N);
            err.CopyFrom(_k1).Scal(E1);
            err.LinComb5(err, E3, _k3, E4, _k4, E5, _k5, E6, _k6, E7, _k7);

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

        // https://doi.org/10.1016/0898-1221(86)90025-8
        protected override void Interpolate(double x, Vec yout)
        {
            double h  = Habs * Direction;
            double s  = (x - T) / h;
            double s2 = s * s;
            double s3 = s * s2;
            double s4 = s2 * s2;

            double hs  = h * s;
            double bs1 = B10 + B11 * s + B12 * s2 + B13 * s3 + B14 * s4;
            double bs3 = s * (B31 + B32 * s + B33 * s2 + B34 * s3);
            double bs4 = s * (B41 + B42 * s + B43 * s2 + B44 * s3);
            double bs5 = s * (B51 + B52 * s + B53 * s2 + B54 * s3);
            double bs6 = s * (B61 + B62 * s + B63 * s2 + B64 * s3);
            double bs7 = (1.0 - s) * s * (B71 + B72 * s + B73 * s2);

            yout.LinComb6(Y, hs * bs1, _k1, hs * bs3, _k3, hs * bs4, _k4, hs * bs5, _k5, hs * bs6, _k6, hs * bs7, _k7);
        }
    }
}
