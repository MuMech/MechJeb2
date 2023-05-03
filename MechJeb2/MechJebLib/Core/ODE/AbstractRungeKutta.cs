/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<Vn, double, Vn>;
    using IVPEvent = Func<double, Vn, Vn, (double x, bool dir, bool stop)>;

    public abstract class AbstractRungeKutta : AbstractIVP
    {
        private const double MAX_FACTOR = 10;
        private const double MIN_FACTOR = 0.2;
        private const double SAFETY     = 0.9;

        protected abstract int Order               { get; }
        protected abstract int Stages              { get; }
        protected abstract int ErrorEstimatorOrder { get; }

        private double _beta = 0.2;

        public double Beta
        {
            get => _beta / (ErrorEstimatorOrder + 1.0);
            set => _beta = value * (ErrorEstimatorOrder + 1.0);
        }

        private double _alpha => 1.0 / (ErrorEstimatorOrder + 1.0) - 0.75 * Beta;
        private double _lastErrorNorm = 1e-4;

        protected override double Step(IVPFunc f, double t, double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, object data)
        {
            int n = y.Count;
            using var err = Vn.Rent(n);

            bool previouslyRejected = false;

            while (true)
            {
                RKStep(f, t, habs, direction, y, dy, ynew, dynew, err, data);

                double errorNorm = ScaledErrorNorm(y, ynew, err);

                if (errorNorm < 1)
                {
                    double factor;
                    if (errorNorm == 0)
                        factor = MAX_FACTOR;
                    else
                        factor = Math.Min(MAX_FACTOR,SAFETY*Math.Pow(errorNorm, -_alpha)*Math.Pow(_lastErrorNorm, Beta));

                    if (previouslyRejected)
                        factor = Math.Min(1.0, factor);

                    habs *= factor;

                    _lastErrorNorm = Math.Max(errorNorm, 1e-4);

                    break;
                }

                habs *= Math.Max(MIN_FACTOR, SAFETY * Math.Pow(errorNorm, -_alpha));

                previouslyRejected = true;
            }

            return habs;
        }

        protected override double SelectInitialStep(double t0, double tf)
        {
            if (Hstart > 0)
                return Hstart;

            double v = Math.Abs(tf - t0);
            return 0.001 * v;
        }

        protected override void Initialize()
        {
            _lastErrorNorm = 1e-4;
        }

        protected abstract void   RKStep(IVPFunc f, double t, double habs, int direction, Vn y, Vn dy, Vn ynew, Vn dynew, Vn err, object data);

        private double ScaledErrorNorm(Vn y, Vn ynew, Vn err)
        {
            int n = err.Count;

            double error = 0.0;

            for (int i = 0; i < n; i++)
            {
                double scale = Atol + Rtol * Math.Max(Math.Abs(y[i]), Math.Abs(ynew[i]));
                error += Powi(err[i]/scale, 2);
            }

            return Math.Sqrt(error / n);
        }
    }
}
