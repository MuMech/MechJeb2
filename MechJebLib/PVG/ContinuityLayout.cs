/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public class ContinuityLayout
    {
        public const int CONTINUITY_LAYOUT_LEN = ResidualLayout.RESIDUAL_LAYOUT_LEN;

        public V3     R;
        public V3     V;
        public V3     Pv;
        public V3     Pr;
        public double M;
        public double Bt;

        public void CopyTo(IList<double> other)
        {
            R.CopyTo(other);
            V.CopyTo(other, 3);
            Pv.CopyTo(other, 6);
            Pr.CopyTo(other, 9);
            other[12] = M;
            other[13] = Bt;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other);
            V.CopyFrom(other, 3);
            Pv.CopyFrom(other, 6);
            Pr.CopyFrom(other, 9);
            M  = other[12];
            Bt = other[13];
        }

        public static ContinuityLayout CreateFrom(IList<double> other)
        {
            var a = new ContinuityLayout();

            a.CopyFrom(other);

            return a;
        }
    }
}
