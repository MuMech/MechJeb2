/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;

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

            K[1].CopyFrom(Dy);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A21 * Dy[i]);
            f(Ynew, T + C2 * h, K[2]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A32 * K[2][i]);
            f(Ynew, T + C3 * h, K[3]);

            for (int i = 0; i < N; i++)
                Ynew[i] = Y[i] + h * (A41 * Dy[i] + A42 * K[2][i] + A43 * K[3][i]);
            f(Ynew, T + h, K[4]);

            for (int i = 0; i < N; i++)
                err[i] = Dy[i] * E1 + K[2][i] * E2 + K[3][i] * E3 + K[4][i] * E4;

            K[4].CopyTo(Dynew);
        }

        protected override void InitInterpolant()
        {
            // intentionally left blank
        }

        protected override void Interpolate(double x, Vn yout) => Interpolants.CubicHermiteInterpolant(T, Y, Dy, Tnew, Ynew, Dynew, x, N, yout);
    }
}
