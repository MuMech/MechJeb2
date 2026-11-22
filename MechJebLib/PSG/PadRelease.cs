/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using static System.Math;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     When an aerodynamics model that only knows the whole vessel can be read after the pad's structure has left it. Such
    ///     a model rebuilds the vessel's shape in the background and answers with the old one until it is done, which its
    ///     reference area shows, unless the structure was too narrow to change that area.
    /// </summary>
    public class PadRelease
    {
        // a rebuild of the same shape moves the reference area by a few tenths of a percent
        private const double SAME_AREA = 0.01;

        // seconds, several times what a rebuild takes
        private const double SETTLED = 5;

        private double _attachedArea;
        private double _releasedAt = double.NaN;

        /// <summary>From the time structure is seen on the vessel until the model has been read without it.</summary>
        public bool Pending { get; private set; }

        public void Reset()
        {
            Pending     = false;
            _releasedAt = double.NaN;
        }

        public void Attached(double refArea)
        {
            Pending       = true;
            _attachedArea = refArea;
            _releasedAt   = double.NaN;
        }

        /// <summary>The structure is off the vessel. Only the first call after Attached sets the time.</summary>
        public void Released(double time)
        {
            if (double.IsNaN(_releasedAt))
                _releasedAt = time;
        }

        public bool CanRead(double time, double refArea)
        {
            if (!Pending)
                return true;
            if (double.IsNaN(_releasedAt) || !(refArea > 0))
                return false;

            return time - _releasedAt >= SETTLED || Abs(refArea - _attachedArea) > SAME_AREA * _attachedArea;
        }
    }
}
