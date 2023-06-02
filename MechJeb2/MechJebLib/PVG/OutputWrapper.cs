/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.PVG
{
    public struct OutputWrapper
    {
        public const int OUTPUT_WRAPPER_LEN = 15;

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

        public OutputWrapper(InputWrapper other)
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
            R.CopyTo(other, 0);
            V.CopyTo(other, 3);
            PV.CopyTo(other, 6);
            PR.CopyTo(other, 9);
            other[12] = M;
            other[13] = Pm;
            other[14] = DV;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other, 0);
            V.CopyFrom(other, 3);
            PV.CopyFrom(other, 6);
            PR.CopyFrom(other, 9);
            M  = other[12];
            Pm = other[13];
            DV = other[14];
        }

        public static OutputWrapper CreateFrom(IList<double> other)
        {
            var a = new OutputWrapper();

            a.R.CopyFrom(other, 0);
            a.V.CopyFrom(other, 3);
            a.PV.CopyFrom(other, 6);
            a.PR.CopyFrom(other, 9);
            a.M  = other[12];
            a.Pm = other[13];
            a.DV = other[14];

            return a;
        }
    }
}
