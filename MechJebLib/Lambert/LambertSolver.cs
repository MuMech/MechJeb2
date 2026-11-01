/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Lambert
{
    /// <summary>
    ///     Zero revolution Lambert solutions for any two distinct positions.  Russell refuses a transfer angle below about
    ///     1 mrad and very short times of flight, which a close approach on a long period orbit needs: 100 s of an orbit of
    ///     a million seconds is 0.6 mrad.  Gooding has neither limit, and solves those.
    /// </summary>
    public static class LambertSolver
    {
        /// <summary>
        ///     Solves Lambert's problem with Russell, or with Gooding when Russell refuses the geometry.
        /// </summary>
        /// <param name="mu">Gravitational parameter (mu)</param>
        /// <param name="r1">Initial position vector</param>
        /// <param name="r2">Final position vector</param>
        /// <param name="tof">Time of flight between both positions, must be positive</param>
        /// <param name="direction">Which of the two Lambert arcs to solve for (see <see cref="TransferGeometry" />)</param>
        /// <param name="h">Axis to use for prograde/retrograde (does not need to be normalized)</param>
        /// <returns>The initial (v1) and final (v2) velocity vectors</returns>
        public static (V3 v1, V3 v2) Solve(double mu, V3 r1, V3 r2, double tof, TransferGeometry direction, V3 h)
        {
            // Gooding spins forever on these, so they must not reach it
            Check.PositiveFinite(mu);
            Check.PositiveFinite(tof);
            Check.NonZeroFinite(r1);
            Check.NonZeroFinite(r2);
            Check.Finite(h);

            try
            {
                return Russell.Solve(mu, r1, r2, tof, direction, 0, h);
            }
            catch (ArgumentException)
            {
                return Gooding.Solve(mu, r1, r2, tof, direction, 0, h);
            }
        }
    }
}
