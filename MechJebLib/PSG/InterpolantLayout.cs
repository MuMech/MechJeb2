/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public struct InterpolantLayout
    {
        public const int INTERPOLANT_LAYOUT_LEN = 11;

        public V3     R;
        public V3     V;
        public double M;
        public V3     U;
        public double Dv;


        public void CopyTo(IList<double> other)
        {
            R.CopyTo(other);
            V.CopyTo(other, 3);
            other[6] = M;
            U.CopyTo(other, 7);
            other[10] = Dv;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other);
            V.CopyFrom(other, 3);
            M = other[6];
            U.CopyFrom(other, 7);
            Dv = other[10];
        }

        public static InterpolantLayout CreateFrom(IList<double> other)
        {
            var a = new InterpolantLayout();

            a.CopyFrom(other);

            return a;
        }
    }
}
