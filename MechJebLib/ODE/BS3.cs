/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.ODE
{
    using IVPFunc = Action<Vec, double, Vec>;

    /*
     * BS3 requires a smaller Rtol+Atol of 1e-8 and higher MaxIterations of 20,000 (10x DP5) or so.
     * - Without fixing the minstepsize to always take at least a minimum floating point step, BS3 may hang.
     */
    public class BS3 : AbstractRungeKutta
    {
        public override int Order               => 3;
        public override int Stages              => 3;
        public override int ErrorEstimatorOrder => 2;

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vec _k1 = null!;
        private Vec _k2 = null!;
        private Vec _k3 = null!;
        private Vec _k4 = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        protected override void RKStep(IVPFunc f)
        {
            double h = Habs * Direction;

            _k1.CopyFrom(Dy);

            Ynew.LinComb1(Y, h * A21, Dy);
            f(Ynew, T + C2 * h, _k2);

            Ynew.LinComb1(Y, h * A32, _k2);
            f(Ynew, T + C3 * h, _k3);

            Ynew.LinComb3(Y, h * A41, Dy, h * A42, _k2, h * A43, _k3);
            f(Ynew, T + h, _k4);

            _k4.CopyTo(Dynew);
        }

        protected override double ScaledErrorNorm()
        {
            using var err = Vec.Rent(N);
            err.CopyFrom(Dy).Scal(E1);
            err.LinComb3(err, E2, _k2, E3, _k3, E4, _k4);

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
        }

        protected override void Cleanup()
        {
            _k1.Dispose();
            _k2.Dispose();
            _k3.Dispose();
            _k4.Dispose();
        }

        protected override void Interpolate(double x, Vec yout) =>
            yout.CubicHermiteInterpolant(T, Y, Dy, Tnew, Ynew, Dynew, x);

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
