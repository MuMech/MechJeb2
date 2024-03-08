using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public struct SegmentRecord
    {
        public const int R_INDEX         = 0;
        public const int V_INDEX         = 3;
        public const int PV_INDEX        = 6;
        public const int PR_INDEX        = 9;
        public const int M_INDEX         = 12;
        public const int DT_INDEX        = 13;
        public const int SEGMENT_REC_LEN = 14;

        public V3     R;
        public V3     V;
        public V3     Pv;
        public V3     Pr;
        public double M;
        public double Dt;

        public void CopyTo(IList<double> other, int offset = 0)
        {
            R.CopyTo(other, offset + R_INDEX);
            V.CopyTo(other, offset + V_INDEX);
            Pv.CopyTo(other, offset + PV_INDEX);
            Pr.CopyTo(other, offset + PR_INDEX);
            other[M_INDEX]  = M;
            other[DT_INDEX] = Dt;
        }

        public void CopyFrom(IList<double> other, int offset = 0)
        {
            R.CopyFrom(other, offset + R_INDEX);
            V.CopyFrom(other, offset + V_INDEX);
            Pv.CopyFrom(other, offset + PV_INDEX);
            Pr.CopyFrom(other, offset + PR_INDEX);
            M  = other[offset + M_INDEX];
            Dt = other[offset + DT_INDEX];
        }

        public static SegmentRecord CreateFrom(IList<double> other, int offset = 0)
        {
            var a = new SegmentRecord();
            a.CopyFrom(other, offset);

            return a;
        }

        public static SegmentRecord CreateFrom(IntegratorRecord other, double dt)
        {
            var a = new SegmentRecord();
            a.CopyFrom(other);
            a.Dt = dt;

            return a;
        }

        private void CopyFrom(IntegratorRecord other)
        {
            R  = other.R;
            V  = other.V;
            Pv = other.Pv;
            Pr = other.Pr;
            M  = other.M;
            Dt = 0;
        }
    }
}
