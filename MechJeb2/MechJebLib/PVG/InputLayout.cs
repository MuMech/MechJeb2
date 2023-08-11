/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public struct InputLayout
    {
        public const int RX_INDEX         = 0;
        public const int RY_INDEX         = 1;
        public const int RZ_INDEX         = 2;
        public const int VX_INDEX         = 3;
        public const int VY_INDEX         = 4;
        public const int VZ_INDEX         = 5;
        public const int M_INDEX          = 12;
        public const int BT_INDEX         = 14;
        public const int INPUT_LAYOUT_LEN = 15;

        public V3 R;

        public V3 V;

        public V3 PV;

        public V3 PR;

        public double CostateMagnitude => Math.Sqrt(PR.sqrMagnitude + PV.sqrMagnitude);

        public double M;

        public double Pm;

        public double H0
        {
            get
            {
                double rm = R.magnitude;
                return V3.Dot(PR, V) -
                       V3.Dot(PV, R) / (rm * rm * rm); // FIXME: this changes for linear gravity model??!?!?
            }
        }

        public double Bt;

        public void CopyTo(IList<double> other)
        {
            R.CopyTo(other);
            V.CopyTo(other, 3);
            PV.CopyTo(other, 6);
            PR.CopyTo(other, 9);
            other[M_INDEX]  = M;
            other[13]       = Pm;
            other[BT_INDEX] = Bt;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other);
            V.CopyFrom(other, 3);
            PV.CopyFrom(other, 6);
            PR.CopyFrom(other, 9);
            M  = other[M_INDEX];
            Pm = other[13];
            Bt = other[BT_INDEX];
        }

        public static InputLayout CreateFrom(IList<double> other)
        {
            var a = new InputLayout();

            a.CopyFrom(other);

            return a;
        }
    }
}
