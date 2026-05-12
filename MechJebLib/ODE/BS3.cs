/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.ODE
{
    using IVPFunc = Action<IList<double>, double, IList<double>>;

    /*
     * BS3 requires a smaller Rtol+Atol of 1e-8 and higher MaxIterations of 20,000 (10x DP5) or so.
     * - Without fixing the minstepsize to always take at least a minimum floating point step, BS3 may hang.
     */
    public class BS3 : AbstractRungeKutta
    {
        protected override int Order               => 3;
        protected override int Stages              => 3;
        protected override int ErrorEstimatorOrder => 2;

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vn _k1 = null!;
        private Vn _k2 = null!;
        private Vn _k3 = null!;
        private Vn _k4 = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        protected override void RKStep(IVPFunc f)
        {
            double h = Habs * Direction;

            _k1.CopyFrom(Dy);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A21 * Dy[i]);
            f(Ynew, T + C2 * h, _k2);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A32 * _k2[i]);
            f(Ynew, T + C3 * h, _k3);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A41 * Dy[i] + A42 * _k2[i] + A43 * _k3[i]);
            f(Ynew, T + h, _k4);

            _k4.CopyTo(Dynew);
        }

        protected override double ScaledErrorNorm()
        {
            using var err = Vn.Rent(N);

            for (int i = 0; i < N; i++)
                err[i] = Dy[i] * E1 + _k2[i] * E2 + _k3[i] * E3 + _k4[i] * E4;

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
            _k1 = Vn.Rent(N);
            _k2 = Vn.Rent(N);
            _k3 = Vn.Rent(N);
            _k4 = Vn.Rent(N);
        }

        protected override void Cleanup()
        {
            _k1.Dispose();
            _k2.Dispose();
            _k3.Dispose();
            _k4.Dispose();
        }

        protected override void Interpolate(double x, Vn yout) =>
            Interpolants.CubicHermiteInterpolant(T, Y, Dy, Tnew, Ynew, Dynew, x, N, yout);

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
    }
}
