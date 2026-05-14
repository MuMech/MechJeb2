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

    public abstract class AbstractRungeKutta : AbstractIVP
    {
        private const double MAX_FACTOR = 10;
        private const double MIN_FACTOR = 0.2;
        private const double SAFETY = 0.9;

        private double _beta = 0.2;
        private double _lastErrorNorm = 1e-4;

        public abstract int Order               { get; }
        public abstract int Stages              { get; }
        public abstract int ErrorEstimatorOrder { get; }

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

                // FIXME? the way we ignore minStep when we're snapping to tf might cause numerical issues in
                // edge cases.  It might be better to let the solver take a full step and not actually enforce
                // tf at all, and then backtrack with the interpolant in order to hit tf precisely.  I believe
                // this is how DifferentialEquations.jl solves this problem a bit more robustly.
                if (Habs < minStep && !Snapping)
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
        protected override double SelectInitialStep(IVPFunc f, double t0, Vec y0,
            Vec dy, int direction)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (MaxStep == MinStep)
                return MinStep;

            using Vec y     = Vec.Rent(N).CopyFrom(y0);
            using Vec f0    = Vec.Rent(N).CopyFrom(dy);
            using Vec scale = y.Dup().Abs().Scal(Rtol).Shift(Atol);

            double d0 = y.Nrm2() / scale.Nrm2();
            double d1 = f0.Nrm2() / scale.Nrm2();

            double h0;
            if (d0 < 1e-5 || d1 < 1e-5)
                h0 = 1e-6;
            else
                h0 = 0.01 * d0 / d1;

            using Vec y1 = f0.Dup().Scal(h0 * direction).Add(y0);

            using var f1 = Vec.Rent(N);
            f(y1, t0 + h0 * direction, f1);
            double d2 = f1.Sub(f0).Div(scale).Nrm2() / h0;

            double h1;
            if (d1 <= 1e-15 && d2 <= 1e-15)
                h1 = Max(1e-6, h0 * 1e-3);
            else
                h1 = Pow(0.01 / Max(d1, d2), 1.0 / (Order + 1));

            return Min(100 * h0, h1);
        }

        protected override void Init() => _lastErrorNorm = 1e-4;

        protected abstract void RKStep(IVPFunc f);

        protected abstract double ScaledErrorNorm();
    }
}
