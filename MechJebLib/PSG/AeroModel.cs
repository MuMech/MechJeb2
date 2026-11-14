/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using static System.Math;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     The atmosphere and the aerodynamic limits a Problem was solved with: the exponential atmosphere of
    ///     AscentProblem.NormalizedDensity, the planet's rotation and the Qα and Q limits as their AscentProblem constraints use
    ///     them.  QAlphaFraction and QFraction are those constraints' values without their scaling, so 1 is the limit, at the
    ///     collocation points where the optimizer applies them and between them alike.  A Solution keeps the model in its own
    ///     units.
    /// </summary>
    public readonly struct AeroModel
    {
        public readonly double RBody;

        // the scale height, zero without an atmosphere
        public readonly double H0;

        // the density at the surface over the Qα limit, zero without a limit
        public readonly double Rho0InvQAlphaMax;

        // the density at the surface over the Q limit, zero without a limit
        public readonly double Rho0InvQMax;

        // the planet's angular velocity
        public readonly V3 W;

        public AeroModel(double rBody, double h0, double rho0InvQAlphaMax, double rho0InvQMax, V3 w)
        {
            RBody = rBody;
            H0 = h0;
            Rho0InvQAlphaMax = rho0InvQAlphaMax;
            Rho0InvQMax = rho0InvQMax;
            W = w;
        }

        public static AeroModel From(Problem problem) =>
            new AeroModel(problem.RBody, problem.H0, problem.Rho0InvQAlphaMax, problem.Rho0InvQMax, problem.W);

        // the same model in SI units, for a model in the units of scale
        public AeroModel DeNormalize(Scale scale)
        {
            double v2 = scale.VelocityScale * scale.VelocityScale;
            return new AeroModel(RBody * scale.LengthScale, H0 * scale.LengthScale, Rho0InvQAlphaMax / v2, Rho0InvQMax / v2,
                W / scale.TimeScale);
        }

        public bool Atmosphere => H0 > 0;

        public bool HasQAlphaLimit => Atmosphere && Rho0InvQAlphaMax > 0;

        public bool HasQLimit => Atmosphere && Rho0InvQMax > 0;

        // the velocity relative to the planet's surface
        public V3 SurfaceVelocity(V3 r, V3 v) => v - V3.Cross(W, r);

        // the density over the density at the surface, zero without an atmosphere
        public double RelativeDensity(V3 r) => Atmosphere ? AscentProblem.NormalizedDensity(r.magnitude, RBody, H0).M : 0;

        // the angle between the thrust direction u and the velocity relative to the surface, in radians, zero at rest
        public double AngleOfAttack(V3 r, V3 v, V3 u)
        {
            V3 vr = SurfaceVelocity(r, v);
            if (vr.sqrMagnitude <= 0 || u.sqrMagnitude <= 0)
                return 0;
            return Atan2(V3.Cross(vr, u).magnitude, V3.Dot(vr, u));
        }

        // Q α over the Qα limit (1 at the limit), zero without a limit
        public double QAlphaFraction(V3 r, V3 v, V3 u)
        {
            if (!HasQAlphaLimit)
                return 0;
            return 0.5 * Rho0InvQAlphaMax * RelativeDensity(r) * SurfaceVelocity(r, v).sqrMagnitude * AngleOfAttack(r, v, u);
        }

        // Q over the Q limit (1 at the limit), zero without a limit
        public double QFraction(V3 r, V3 v)
        {
            if (!HasQLimit)
                return 0;
            return 0.5 * Rho0InvQMax * RelativeDensity(r) * SurfaceVelocity(r, v).sqrMagnitude;
        }
    }
}
