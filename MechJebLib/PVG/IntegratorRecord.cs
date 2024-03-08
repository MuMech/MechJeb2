/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;
using static System.Math;

namespace MechJebLib.PVG
{
    public struct IntegratorRecord
    {
        public const int R_INDEX            = 0;
        public const int V_INDEX            = 3;
        public const int PV_INDEX           = 6;
        public const int PR_INDEX           = 9;
        public const int M_INDEX            = 12;
        public const int DV_INDEX           = 13;
        public const int INTEGRATOR_REC_LEN = 14;

        public V3     R;
        public V3     V;
        public V3     Pv;
        public V3     Pr;
        public double CostateMagnitude => Sqrt(Pr.sqrMagnitude + Pv.sqrMagnitude);
        public double M;
        public double DV;

        public void CopyTo(IList<double> other, int offset = 0)
        {
            R.CopyTo(other, offset + R_INDEX);
            V.CopyTo(other, offset + V_INDEX);
            Pv.CopyTo(other, offset + PV_INDEX);
            Pr.CopyTo(other, offset + PR_INDEX);
            other[M_INDEX]  = M;
            other[DV_INDEX] = DV;
        }

        public void CopyFrom(IList<double> other, int offset = 0)
        {
            R.CopyFrom(other, offset + R_INDEX);
            V.CopyFrom(other, offset + V_INDEX);
            Pv.CopyFrom(other, offset + PV_INDEX);
            Pr.CopyFrom(other, offset + PR_INDEX);
            M  = other[offset + M_INDEX];
            DV = other[offset + DV_INDEX];
        }

        public static IntegratorRecord CreateFrom(IList<double> other, int offset = 0)
        {
            var a = new IntegratorRecord();
            a.CopyFrom(other, offset);

            return a;
        }

        public void CopyFrom(SegmentRecord other, double dv)
        {
            R  = other.R;
            V  = other.V;
            Pv = other.Pv;
            Pr = other.Pr;
            M  = other.M;
            DV = dv;
        }
    }
}
