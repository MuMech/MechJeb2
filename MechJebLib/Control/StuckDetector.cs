/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;

namespace MechJebLib.Control
{
    /// <summary>
    ///     Notices a velocity loop at its limit with nothing to show for it, which is what a vessel held against something
    ///     looks like.  It watches how the velocity moves along the push, and not the error, since the command can step while
    ///     the loop is at its limit.  That is compared with what the thrust should have done, so a heavy vessel that's slow to
    ///     answer doesn't count, however long its error takes to close.  A vessel pushing another along counts as long as
    ///     the velocity is relative to it, since the two then move together.
    /// </summary>
    public class StuckDetector
    {
        // a push in a new direction, over 45 degrees from the first, is a new push
        private const double SAME_PUSH = 0.7;

        private readonly double _time;
        private readonly double _minResponse;
        private double _pushing;
        private V3 _push;
        private V3 _firstVelocity;

        /// <param name="time">how long it has to last, in seconds</param>
        /// <param name="minResponse">share of the thrust's acceleration that counts as answering</param>
        public StuckDetector(double time, double minResponse)
        {
            _time        = time;
            _minResponse = minResponse;
        }

        public void Reset() => _pushing = 0;

        /// <summary>One tick of the loop.</summary>
        /// <param name="error">velocity error, the way the loop is pushing</param>
        /// <param name="velocity">velocity relative to the target</param>
        /// <param name="accel">what the thrust available along the error should do</param>
        /// <param name="dt">length of the tick</param>
        /// <param name="saturated">the loop is at its limit</param>
        /// <returns>true when it has been stuck for the whole time</returns>
        public bool Update(V3 error, V3 velocity, double accel, double dt, bool saturated)
        {
            if (!saturated)
            {
                Reset();
                return false;
            }

            V3 push = error.normalized;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_pushing == 0 || V3.Dot(push, _push) < SAME_PUSH)
            {
                _pushing       = 0;
                _push          = push;
                _firstVelocity = velocity;
            }

            _pushing += dt;

            if (V3.Dot(velocity - _firstVelocity, _push) > _minResponse * accel * _pushing)
            {
                Reset();
                return false;
            }

            return _pushing >= _time;
        }
    }
}
