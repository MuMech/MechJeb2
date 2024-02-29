/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public class ResidualLayout
    {
        public const int RESIDUAL_LAYOUT_LEN = 14;

        public  V3     R;
        public  V3     V;
        private V3     _terminal0;
        private V3     _terminal1;
        public  double M;
        public  double Bt;

        public (double a, double b, double c, double d, double e, double f) Terminal
        {
            set
            {
                _terminal0[0] = value.a;
                _terminal0[1] = value.b;
                _terminal0[2] = value.c;
                _terminal1[0] = value.d;
                _terminal1[1] = value.e;
                _terminal1[2] = value.f;
            }
        }

        public void CopyTo(IList<double> other)
        {
            R.CopyTo(other);
            V.CopyTo(other, 3);
            other[6]  = M;
            other[7]  = _terminal0[0];
            other[8]  = _terminal0[1];
            other[9]  = _terminal0[2];
            other[10] = _terminal1[0];
            other[11] = _terminal1[1];
            other[12] = _terminal1[2];
            other[13] = Bt;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other);
            V.CopyFrom(other, 3);
            M          = other[6];
            _terminal0 = new V3(other[7], other[8], other[9]);
            _terminal1 = new V3(other[10], other[11], other[12]);
            Bt         = other[13];
        }

        public static ResidualLayout CreateFrom(IList<double> other)
        {
            var a = new ResidualLayout();

            a.CopyFrom(other);

            return a;
        }
    }
}
