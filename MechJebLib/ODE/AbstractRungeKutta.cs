/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.ODE
{
    using IVPFunc = Action<IList<double>, double, IList<double>>;

    public abstract class AbstractRungeKutta : AbstractIVP
    {
        private const double MAX_FACTOR = 10;
        private const double MIN_FACTOR = 0.2;
        private const double SAFETY = 0.9;

        protected readonly List<Vn> K = new List<Vn>();

        private double _beta = 0.2;
        private double _lastErrorNorm = 1e-4;

        protected abstract int Order { get; }
        protected abstract int Stages { get; }
        protected abstract int ErrorEstimatorOrder { get; }

        public double Beta
        {
            get => _beta / (ErrorEstimatorOrder + 1.0);
            set => _beta = value * (ErrorEstimatorOrder + 1.0);
        }

        private double _alpha => 1.0 / (ErrorEstimatorOrder + 1.0) - 0.75 * Beta;

        protected override (double, double) Step(IVPFunc f)
        {
            bool previouslyRejected = false;

            while (true)
            {
                CancellationToken.ThrowIfCancellationRequested();

                double minStep = 10 * Abs(NextAfter(T, Direction * double.PositiveInfinity) - T);
                minStep = Max(minStep, MinStep);

                if (Habs > MaxStep)
                    Habs = MaxStep;

                if (Habs < minStep)
                {
                    if (ThrowOnMinStep)
                        throw new InvalidOperationException(
                            $"Step size is smaller than minimum step size at t={T}: {Habs} < {minStep}");
                    Habs = minStep;
                }

                RKStep(f);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (MinStep == MaxStep)
                    return (Habs, Habs);

                double errorNorm = ScaledErrorNorm();

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (errorNorm < 1 || (Habs == minStep && !ThrowOnMinStep))
                {
                    double factor = errorNorm == 0
                        ? MAX_FACTOR
                        : Min(MAX_FACTOR, SAFETY * Pow(errorNorm, -_alpha) * Pow(_lastErrorNorm, Beta));

                    if (previouslyRejected)
                        factor = Min(1.0, factor);

                    Tnew = T + Habs * Direction;

                    _lastErrorNorm = Max(errorNorm, 1e-4);

                    return (Habs, Habs * factor);
                }

                Habs *= Max(MIN_FACTOR, SAFETY * Pow(errorNorm, -_alpha));

                previouslyRejected = true;
            }
        }

        // https://github.com/scipy/scipy/blob/c374ca7fdfa32dd3817cbec0f1863e01640279eb/scipy/integrate/_ivp/common.py#L66-L121
        // E. Hairer, S. P. Norsett G. Wanner, "Solving Ordinary Differential Equations I: Nonstiff Problems", Sec. II.4.
        protected override double SelectInitialStep(IVPFunc f, double t0, IReadOnlyList<double> y0,
            IReadOnlyList<double> dy, int direction)
        {
            if (MaxStep == MinStep)
                return MinStep;

            using Vn y = Vn.Rent(N).CopyFrom(y0);
            using Vn f0 = Vn.Rent(N).CopyFrom(dy);
            using Vn scale = y.Abs().MutTimes(Rtol).MutAdd(Atol);

            double d0 = y.magnitude / scale.magnitude;
            double d1 = f0.magnitude / scale.magnitude;

            double h0;
            if (d0 < 1e-5 || d1 < 1e-5)
                h0 = 1e-6;
            else
                h0 = 0.01 * d0 / d1;

            using Vn y1 = f0.Dup().MutTimes(h0).MutTimes(direction).MutAdd(y0);

            using var f1 = Vn.Rent(N);
            f(f1, t0 + h0 * direction, y1);
            double d2 = f1.MutSub(f0).MutDiv(scale).magnitude / h0;

            double h1;
            if (d1 <= 1e-15 && d2 <= 1e-15)
                h1 = Max(1e-6, h0 * 1e-3);
            else
                h1 = Pow(0.01 / Max(d1, d2), 1.0 / (Order + 1));

            return Min(100 * h0, h1);
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

        protected abstract void RKStep(IVPFunc f);

        protected abstract double ScaledErrorNorm();
    }
}
