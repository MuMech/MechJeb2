/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;

namespace MechJebLib.Functions
{
    /// <summary>
    ///     Finds when two objects orbiting the same central body are closest together within a window of time.
    /// </summary>
    public static class MinimumSeparation
    {
        // r/v is period / (2 pi) on a circular orbit, so this is about 20 samples per orbit, and more around a periapsis.
        private const double STEP_FRACTION = 0.3;

        // Bounds the cost when the window spans many orbits of the shorter orbit.
        private const int MAX_SAMPLES = 1000;

        // Bounds each further look around the best result of a march that hit the limit.
        private const int MAX_ZOOM_SAMPLES = 100;

        private const int MAX_ITERATIONS = 50;

        internal readonly struct Sample
        {
            // d(|r|^2 / 2)/dt, which is zero at a minimum or a maximum of the separation
            public readonly double Rdv;
            public readonly double Drdv;
            public readonly double Ddrdv;
            public readonly double Distance;

            // the smaller of r/v for the two objects: how soon the separation can change character
            public readonly double Timescale;

            public Sample(double rdv, double drdv, double ddrdv, double distance, double timescale)
            {
                Rdv = rdv;
                Drdv = drdv;
                Ddrdv = ddrdv;
                Distance = distance;
                Timescale = timescale;
            }
        }

        /// <summary>
        ///     Finds the smallest separation of objects a and b over [0, dtMax], measured from the given state vectors.
        ///     This is the global minimum over the window, NOT the next local minimum: every pass in the window is compared
        ///     and the ones with a larger separation are skipped, however much sooner they come.
        ///     The result can be an end of the window.  It is dt = 0 when nothing ahead is closer, and dt = dtMax when the
        ///     objects are still closing at the end.
        ///     The window is sampled forward looking for rdv going from negative to positive, with each one then refined by a
        ///     bracketed Halley iteration falling back to bisection.  Testing the sign of rdv instead of comparing distances
        ///     means a narrow minimum is only missed if a maximum falls in the same step.
        ///     When the window spans more than about 50 orbits of the shorter orbit the sample limit coarsens the steps.  Those
        ///     steps only compare distances, and the search then looks again around the best one, so a pass can be missed.
        /// </summary>
        /// <param name="mu">Gravitational parameter of the central body</param>
        /// <param name="ra">Position of object a at t = 0</param>
        /// <param name="va">Velocity of object a at t = 0</param>
        /// <param name="rb">Position of object b at t = 0</param>
        /// <param name="vb">Velocity of object b at t = 0</param>
        /// <param name="dtMax">End of the window</param>
        /// <param name="dtTol">Convergence tolerance of the refinement, in the time units of mu</param>
        /// <returns>The time from t = 0 of the smallest separation, and that separation</returns>
        public static (double dt, double distance) Solve(double mu, V3 ra, V3 va, V3 rb, V3 vb, double dtMax, double dtTol = 1e-4)
        {
            Check.PositiveFinite(mu, nameof(mu));
            Check.Finite(ra, nameof(ra));
            Check.Finite(va, nameof(va));
            Check.Finite(rb, nameof(rb));
            Check.Finite(vb, nameof(vb));
            Check.NonNegativeFinite(dtMax, nameof(dtMax));
            Check.PositiveFinite(dtTol, nameof(dtTol));

            double bestDt = 0;
            double bestDistance = double.PositiveInfinity;

            double tStart = 0;
            double tEnd = dtMax;
            double minStep = dtMax / MAX_SAMPLES;

            while (March(mu, ra, va, rb, vb, tStart, tEnd, minStep, dtTol, ref bestDt, ref bestDistance))
            {
                tStart = Math.Max(bestDt - minStep, 0);
                tEnd = Math.Min(bestDt + minStep, dtMax);
                minStep = (tEnd - tStart) / MAX_ZOOM_SAMPLES;
            }

            return (bestDt, bestDistance);
        }

        // Returns true if minStep held back any of the steps, which means the best result needs looking at more closely.
        private static bool March(double mu, V3 ra, V3 va, V3 rb, V3 vb, double tStart, double tEnd, double minStep, double dtTol, ref double bestDt,
            ref double bestDistance)
        {
            bool coarsened = false;

            double t = tStart;
            Sample sample = Evaluate(mu, ra, va, rb, vb, t);

            if (sample.Distance < bestDistance)
            {
                bestDistance = sample.Distance;
                bestDt = t;
            }

            while (t < tEnd)
            {
                double step = STEP_FRACTION * sample.Timescale;
                bool limited = step < minStep;

                if (limited)
                {
                    step = minStep;
                    coarsened = true;
                }

                double tNext = Math.Min(t + step, tEnd);
                Sample next = Evaluate(mu, ra, va, rb, vb, tNext);

                // The opposite sign change is a maximum, which the march just steps over.  A coarsened step can hold several
                // passes, so a sign change there would only refine to an arbitrary one of them.
                if (!limited && sample.Rdv < 0 && next.Rdv >= 0)
                {
                    (double dtMin, double distanceMin) = Refine(mu, ra, va, rb, vb, t, tNext, dtTol);

                    if (distanceMin < bestDistance)
                    {
                        bestDistance = distanceMin;
                        bestDt = dtMin;
                    }
                }

                if (next.Distance < bestDistance)
                {
                    bestDistance = next.Distance;
                    bestDt = tNext;
                }

                t = tNext;
                sample = next;
            }

            return coarsened;
        }

        // Requires rdv(lo) < 0 and rdv(hi) >= 0 with lo < hi, which keeps the iteration on the minimum between them.
        private static (double dt, double distance) Refine(double mu, V3 ra, V3 va, V3 rb, V3 vb, double lo, double hi, double dtTol)
        {
            double t = 0.5 * (lo + hi);
            Sample sample = Evaluate(mu, ra, va, rb, vb, t);

            for (int i = 1; i < MAX_ITERATIONS; i++)
            {
                if (sample.Rdv < 0)
                    lo = t;
                else if (sample.Rdv > 0)
                    hi = t;
                else
                    break;

                double newtonDt = -sample.Rdv / sample.Drdv;
                double halleyDt = -2 * sample.Rdv * sample.Drdv / (2 * sample.Drdv * sample.Drdv - sample.Rdv * sample.Ddrdv);

                // no Math.Sign() here, since it throws on a NaN
                double dt = double.IsNaN(halleyDt) || halleyDt * newtonDt < 0 ? newtonDt : halleyDt;

                double tNext = t + dt;

                if (double.IsNaN(tNext) || tNext <= lo || tNext >= hi)
                    tNext = 0.5 * (lo + hi);

                double step = Math.Abs(tNext - t);

                t = tNext;
                sample = Evaluate(mu, ra, va, rb, vb, t);

                if (step <= dtTol)
                    break;
            }

            return (t, sample.Distance);
        }

        internal static Sample Evaluate(double mu, V3 ra, V3 va, V3 rb, V3 vb, double t)
        {
            (V3 raf, V3 vaf) = Shepperd.Solve(mu, t, ra, va);
            (V3 rbf, V3 vbf) = Shepperd.Solve(mu, t, rb, vb);

            (V3 aa, V3 ja) = AccelerationAndJerk(mu, raf, vaf);
            (V3 ab, V3 jb) = AccelerationAndJerk(mu, rbf, vbf);

            V3 r = raf - rbf;
            V3 v = vaf - vbf;
            V3 a = aa - ab;
            V3 j = ja - jb;

            double rdv = V3.Dot(r, v);
            double drdv = V3.Dot(v, v) + V3.Dot(r, a);
            double ddrdv = 3 * V3.Dot(v, a) + V3.Dot(r, j);
            double timescale = Math.Min(raf.magnitude / vaf.magnitude, rbf.magnitude / vbf.magnitude);

            return new Sample(rdv, drdv, ddrdv, r.magnitude, timescale);
        }

        private static (V3 a, V3 j) AccelerationAndJerk(double mu, V3 r, V3 v)
        {
            double r2 = r.sqrMagnitude;
            double r3 = r2 * r.magnitude;

            V3 a = -mu * r / r3;
            V3 j = -mu * (v - 3 * V3.Dot(r, v) * r / r2) / r3;

            return (a, j);
        }
    }
}
