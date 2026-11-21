/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;

namespace MechJebLib.HoverslamSimulation
{
    /// <summary>
    ///     The hoverslam's initial state and target in normalized units, as PSG's Problem: the target radius is the unit of
    ///     length, the circular speed there the unit of velocity and the starting mass the unit of mass, so mu is 1.
    /// </summary>
    public readonly struct HoverslamProblem
    {
        public readonly Scale Scale;
        public readonly V3 R0;
        public readonly V3 V0;
        public readonly V3 W;
        public readonly double Height; // the target radius
        public readonly double Vfm;    // the final vertical speed, negative for a descent

        // The UT of r0/v0 in seconds.  The simulation's clock counts from it, so it never enters the dynamics, and left in SI
        // an ignition now reports exactly this.
        public readonly double T0;

        public HoverslamProblem(V3 r0, V3 v0, double t0, double mu, V3 w, double height, double vfm, double m0)
        {
            Scale = Scale.Create(mu, height, m0);
            R0 = r0 / Scale.LengthScale;
            V0 = v0 / Scale.VelocityScale;
            W = w * Scale.TimeScale;
            Height = height / Scale.LengthScale;
            Vfm = vfm / Scale.VelocityScale;
            T0 = t0;
        }
    }
}
