/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Interpolants;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     Guidance flies a fixed vertical ascent off the ground, at full throttle straight up, before the pitch program hands
    ///     over to the solution: so a problem on the ground is solved from the top of that rise instead of from the ground.
    ///     The rise is integrated ahead of the solve with the optimizer's own dynamics (AscentProblem.FullThrottleAcceleration)
    ///     in the first burn, and the problem starts where it ends.  The rise itself is not a phase: the optimizer, its warm
    ///     start and guidance's phase indices all start at the first phase.  The problem carries it (Problem.Rise) for each
    ///     Solution to keep as the trajectory from the ground to its T0.
    /// </summary>
    internal static class VerticalRise
    {
        /// <summary>
        ///     The problem from the top of the vertical rise from pad, with the first phase starting there.  Without a rise to
        ///     fly (no height, a first phase that coasts, or a rocket that can't lift off or burns out before the top) it is
        ///     pad and the phases are untouched.
        /// </summary>
        /// <param name="pad">the normalized problem on the ground</param>
        /// <param name="phases">the normalized phases, the first of which flies the rise, changed in place</param>
        /// <param name="height">the normalized height of the rise</param>
        public static Problem Presolve(Problem pad, PhaseCollection phases, double height)
        {
            if (!(height > 0) || phases.Count == 0 || phases[0].Coast)
                return pad;

            Phase phase = phases[0];
            CheckDebug.True(phase.Normalized);

            double invDensityR0 = AscentProblem.InvDensityR0(pad);
            double rTop = pad.R0.magnitude + height;

            void Rhs(Vec yin, double t, Vec dyout)
            {
                var y = InterpolantLayout.CreateFrom(yin);
                double rm = y.R.magnitude;
                V3 rhat = y.R / rm;

                var dy = new InterpolantLayout
                {
                    R = y.V,
                    V = AscentProblem.FullThrottleAcceleration(pad, phase, invDensityR0, y.R, y.V, y.M, rhat),
                    M = -phase.Mdot,
                    // the thrust direction is the radial unit vector, which turns with the position
                    U = (y.V - V3.Dot(rhat, y.V) * rhat) / rm,
                    T = 0
                };

                dy.CopyTo(dyout);
            }

            // can't lift off: straight up, the acceleration from rest (on the rotating planet) is not upwards
            V3 r0hat = pad.R0.normalized;
            V3 vh = pad.V0 - V3.Dot(pad.V0, r0hat) * r0hat;
            double climb = V3.Dot(AscentProblem.FullThrottleAcceleration(pad, phase, invDensityR0, pad.R0, pad.V0, pad.M0, r0hat), r0hat) +
                vh.sqrMagnitude / pad.R0.magnitude;
            if (!(climb > 0))
                return pad;

            var y0 = new InterpolantLayout { R = pad.R0, V = pad.V0, M = pad.M0, U = r0hat, T = 1 };
            double tf = pad.T0 + phase.BurnTimeFromMass(pad.M0);

            using var y0Vec = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var yfVec = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            y0.CopyTo(y0Vec);

            var solver = new Tsit5 { Rtol = 1e-10, Atol = 1e-10, MaxEvals = 100000 };
            var top = new Event((y, t, i) => InterpolantLayout.CreateFrom(y).R.magnitude - rTop);
            var rise = VecInterpolant.Rent();

            solver.Solve(Rhs, y0Vec, yfVec, pad.T0, tf, rise, new[] { top });

            // burned out before the top
            if (solver.Status != AbstractIVP.IVPStatus.EventTerminated)
            {
                rise.Dispose();
                return pad;
            }

            var y1 = InterpolantLayout.CreateFrom(yfVec);
            double t1 = solver.T;

            // the first burn starts at the top, with the propellant the rise left it
            phase.M0 = y1.M;
            phase.Bt = phase.BurnTimeFromMass(y1.M);
            phase.MinT = phase.AllowShutdown ? 0 : phase.Bt;
            phase.MaxT = phase.Bt / phase.MinThrottle;

            // The back pressure of every burn is relative to the density at the problem's initial position (InvDensityR0),
            // which is now the top of the rise, so each VexCurrent moves to the exhaust velocity there to keep the thrust the
            // same at every height.
            if (AscentProblem.HasAtmosphere(pad))
            {
                double ratio = AscentProblem.NormalizedDensity(y1.R.magnitude, pad.RBody, pad.H0).M * invDensityR0;
                foreach (Phase p in phases)
                    if (!p.Coast)
                        p.VexCurrent = p.VexVacuum - (p.VexVacuum - p.VexCurrent) * ratio;
            }

            return new Problem(pad.Scale, y1.R, y1.V, y1.R.normalized, y1.M, t1, pad.Mu, pad.RBody, pad.H0, pad.Rho0CdAref,
                pad.Rho0InvQAlphaMax, pad.Rho0InvQMax, pad.W, pad.Terminal, rise);
        }
    }
}
