/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<Vn, double, Vn>;
    using IVPEvent = Func<double, Vn, Vn, (double x, bool dir, bool stop)>;

    /// <summary>
    ///     https://doi.org/10.1016/S0168-9274(96)00025-6
    ///     https://github.com/PyNumAl/Python-Numerical-Analysis/blob/main/Initial-Value%20Problems/RK%20tableaus/DP54.txt
    ///     https://github.com/blackstonep/Numerical-Recipes/blob/master/stepperdopr5.h
    ///     https://github.com/SciML/OrdinaryDiffEq.jl/blob/master/src/tableaus/low_order_rk_tableaus.jl
    ///     https://github.com/scipy/scipy/blob/main/scipy/integrate/_ivp/rk.py
    ///     https://github.com/zachjweiner/scipy/blob/8ba609c313f09bf2a333849ca3ac2bd24d7655e7/scipy/integrate/_ivp/rk.py
    /// </summary>
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

        private class RKData : IDisposable
        {
            private static readonly ObjectPool<RKData> _pool = new ObjectPool<RKData>(() => new RKData());

            public Vn K1, K2, K3, K4;

            private RKData()
            {
                K1 = K2 = K3 = K4 = null!;
            }

            public void Dispose()
            {
                K1.Dispose();
                K2.Dispose();
                K3.Dispose();
                K4.Dispose();
                _pool.Return(this);
            }

            public static RKData Rent(int n)
            {
                RKData data = _pool.Get();
                data.K1 = Vn.Rent(n);
                data.K2 = Vn.Rent(n);
                data.K3 = Vn.Rent(n);
                data.K4 = Vn.Rent(n);
                return data;
            }
        }

        protected override void RKStep(IVPFunc f, double t, double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, Vn err, object o)
        {
            var data = (RKData)o;

            int n = y.Count;
            double h = habs * direction;

            dy.CopyTo(data.K1);

            for (int i = 0; i < n; i++)
                ynew[i] = y[i] + h * (A21 * dy[i]);
            f(ynew, t + C2 * h, data.K2);

            for (int i = 0; i < n; i++)
                ynew[i] = y[i] + h * (A32 * data.K2[i]);
            f(ynew, t + C3 * h, data.K3);

            for (int i = 0; i < n; i++)
                ynew[i] = y[i] + h * (A41 * data.K1[i] + A42 * data.K2[i] + A43 * data.K3[i]);
            f(ynew, t + h, data.K4);

            for (int i = 0; i < n; i++)
                err[i] = data.K1[i] * E1 + data.K2[i] * E2 + data.K3[i] * E3 + data.K4[i] * E4;

            data.K4.CopyTo(dynew);
        }

        protected override void PrepareInterpolant(double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, object data)
        {
            // intentionally left blank for now
        }

        protected override void Interpolate(double x, double t, double h, Vn y, Vn yout, object o)
        {
            /*
            var data = (RKData)o;

            int n = y.Count;
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

        protected override IDisposable SetupData(int n)
        {
            return RKData.Rent(n);
        }
    }
}
