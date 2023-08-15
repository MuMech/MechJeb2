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
    public struct OutputLayout
    {
        public const int M_INDEX           = 12;
        public const int PM_INDEX          = 13;
        public const int DV_INDEX          = 14;
        public const int OUTPUT_LAYOUT_LEN = 15;

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

        public double DV;

        public OutputLayout(InputLayout other)
        {
            R  = other.R;
            V  = other.V;
            PV = other.PV;
            PR = other.PR;
            M  = other.M;
            Pm = other.Pm;
            DV = 0;
        }

        public void CopyTo(IList<double> other)
        {
            R.CopyTo(other);
            V.CopyTo(other, 3);
            PV.CopyTo(other, 6);
            PR.CopyTo(other, 9);
            other[M_INDEX]  = M;
            other[PM_INDEX] = Pm;
            other[DV_INDEX] = DV;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other);
            V.CopyFrom(other, 3);
            PV.CopyFrom(other, 6);
            PR.CopyFrom(other, 9);
            M  = other[M_INDEX];
            Pm = other[PM_INDEX];
            DV = other[DV_INDEX];
        }

        public static OutputLayout CreateFrom(IList<double> other)
        {
            var a = new OutputLayout();

            a.CopyFrom(other);

            return a;
        }
    }
}
