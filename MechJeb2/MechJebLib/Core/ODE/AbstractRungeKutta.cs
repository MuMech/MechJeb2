/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<IList<double>, double, IList<double>>;

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

        protected readonly List<Vn> K = new List<Vn>();

        protected override (double, double) Step(IVPFunc f)
        {
            using var err = Vn.Rent(N);

            bool previouslyRejected = false;

            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (Habs > MaxStep)
                    Habs = MaxStep;
                else if (Habs < MinStep)
                    Habs = MinStep;

                RKStep(f, err);

                double errorNorm = ScaledErrorNorm(err);

                if (errorNorm < 1)
                {
                    double factor;
                    if (errorNorm == 0)
                        factor = MAX_FACTOR;
                    else
                        factor = Math.Min(MAX_FACTOR, SAFETY * Math.Pow(errorNorm, -_alpha) * Math.Pow(_lastErrorNorm, Beta));

                    if (previouslyRejected)
                        factor = Math.Min(1.0, factor);

                    Tnew = T + Habs * Direction;

                    _lastErrorNorm = Math.Max(errorNorm, 1e-4);

                    return (Habs, Habs * factor);
                }

                Habs *= Math.Max(MIN_FACTOR, SAFETY * Math.Pow(errorNorm, -_alpha));

                previouslyRejected = true;
            }
        }

        protected override double SelectInitialStep(double t0, double tf)
        {
            if (Hstart > 0)
                return Hstart;

            double v = Math.Abs(tf - t0);
            return 0.001 * v;
        }

        protected override void Init()
        {
            _lastErrorNorm = 1e-4;

            K.Clear();
            // we create an extra K[0] which we do not use, because the literature uses 1-indexed K's
            for (int i = 0; i <= Stages + 1; i++)
                K.Add(Vn.Rent(N));
        }

        protected override void Cleanup()
        {
            for (int i = 0; i <= Stages + 1; i++)
                K[i].Dispose();
            K.Clear();
        }

        protected abstract void RKStep(IVPFunc f, Vn err);

        [UsedImplicitly]
        protected virtual double ScaledErrorNorm(Vn err)
        {
            int n = err.Count;

            double error = 0.0;

            for (int i = 0; i < n; i++)
            {
                double scale = Atol + Rtol * Math.Max(Math.Abs(Y[i]), Math.Abs(Ynew[i]));
                error += Powi(err[i] / scale, 2);
            }

            return Math.Sqrt(error / n);
        }
    }
}
