/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.HoverslamSimulation
{
    public struct HoverslamLayout
    {
        public const int R_INDEX              = 0;
        public const int V_INDEX              = 3;
        public const int M_INDEX              = 6;
        public const int DV_INDEX             = 7;
        public const int HOVERSLAM_LAYOUT_LEN = 8;

        public V3 R;

        public V3 V;

        public double M;

        public double DV;

        public HoverslamLayout(HoverslamLayout other)
        {
            R = other.R;
            V = other.V;
            M = other.M;
            DV = other.DV;
        }

        public void CopyTo(IList<double> other)
        {
            R.CopyTo(other);
            V.CopyTo(other, V_INDEX);
            other[M_INDEX] = M;
            other[DV_INDEX] = DV;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other);
            V.CopyFrom(other, V_INDEX);
            M = other[M_INDEX];
            DV = other[DV_INDEX];
        }

        public static HoverslamLayout CreateFrom(IList<double> other)
        {
            var a = new HoverslamLayout();

            a.CopyFrom(other);

            return a;
        }
    }
}
