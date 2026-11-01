/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using static System.Math;

namespace MechJebLib.Control
{
    public static class StoppingSpeed
    {
        /// <summary>
        ///     The speed to close a distance at and still stop on it, with the gain limited near zero.  sqrt(2 * a * d) has
        ///     no gain limit there, so noise in the distance goes straight to the speed.  Below accel / gain^2 this is
        ///     gain * d instead, and the two meet there with the same value and the same slope.
        /// </summary>
        /// <param name="distance">distance left, only its size counts</param>
        /// <param name="accel">deceleration to plan on</param>
        /// <param name="gain">speed per unit of distance near zero, must be positive</param>
        /// <returns>the speed, never negative</returns>
        public static double GainLimited(double distance, double accel, double gain)
        {
            double d = Abs(distance);
            double linearRange = accel / (gain * gain);

            return d <= linearRange ? gain * d : Sqrt(2 * accel * (d - linearRange / 2));
        }
    }
}
