/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;

namespace MechJebLib.Control
{
    /// <summary>
    ///     Tells a center of mass that has really moved from one swinging around on a bending vessel.  It averages the
    ///     position and not the size of the error, because the size of an oscillation never averages out.
    /// </summary>
    public class CoMShiftTracker
    {
        /// <summary>
        ///     Samples to average, one per physics tick.  At 50 Hz that's 4 s, which cancels bending at 0.25, 0.5 and 1 Hz
        ///     exactly and leaves a fraction of 1 / (pi * f * 4 s) at most of anything else above 0.25 Hz.  Propellant moves
        ///     the CoM far slower than that.
        /// </summary>
        public const int WINDOW = 200;

        private readonly V3[] _samples = new V3[WINDOW];
        private          int  _count;
        private          int  _next;
        private          V3   _sum;
        private          V3   _reference;

        /// <summary>
        ///     How far the average is from the reference.
        /// </summary>
        public double Error { get; private set; }

        /// <summary>
        ///     Starts over from a new reference, for when the old samples no longer mean anything.
        /// </summary>
        /// <param name="com">the new reference</param>
        public void Reset(V3 com)
        {
            _reference = com;
            _sum       = V3.zero;
            _count     = 0;
            _next      = 0;
            Error      = 0;
        }

        /// <summary>
        ///     Takes one tick's position.  Nothing can trip until the window has filled.
        /// </summary>
        /// <param name="com">the center of mass, in a frame that doesn't move with it</param>
        /// <param name="threshold">how far the average can get from the reference</param>
        /// <returns>true if it went past the threshold, and the average is the new reference</returns>
        public bool Update(V3 com, double threshold)
        {
            if (_count == WINDOW)
                _sum -= _samples[_next];
            else
                _count++;

            _samples[_next] =  com;
            _sum            += com;
            _next           =  (_next + 1) % WINDOW;

            if (_count < WINDOW)
                return false;

            V3 average = _sum / WINDOW;
            Error = (average - _reference).magnitude;

            if (Error <= threshold)
                return false;

            _reference = average;
            return true;
        }
    }
}
