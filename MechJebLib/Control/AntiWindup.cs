/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

namespace MechJebLib.Control
{
    public static class AntiWindup
    {
        /// <summary>
        ///     One step of the integral term of a loop with a limited output.  The step is dropped when it would leave the
        ///     output past a limit, which also keeps the integral itself inside the limits for non-negative gains.  Limiting
        ///     only the integral to the output range isn't enough: with a high proportional gain, a large error can stay
        ///     saturated while the integral grows, and all of it still has to unwind after the error has crossed zero.
        /// </summary>
        /// <param name="integral">integral term so far</param>
        /// <param name="proportional">proportional term of this step</param>
        /// <param name="step">what this step adds to the integral term</param>
        /// <param name="min">lower limit of the output</param>
        /// <param name="max">upper limit of the output</param>
        /// <returns>the new integral term</returns>
        public static double Integrate(double integral, double proportional, double step, double min, double max)
        {
            double next = integral + step;
            double output = proportional + next;

            return output < min || output > max ? integral : next;
        }
    }
}
