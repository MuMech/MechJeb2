using System;
using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public struct InputWrapper
    {
        public const int INPUT_WRAPPER_LEN = 15;

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
            other[12] = M;
            other[13] = Pm;
            other[14] = Bt;
        }

        public void CopyFrom(IList<double> other)
        {
            R.CopyFrom(other, 0);
            V.CopyFrom(other, 3);
            PV.CopyFrom(other, 6);
            PR.CopyFrom(other, 9);
            M  = other[12];
            Pm = other[13];
            Bt = other[14];
        }

        public static InputWrapper CreateFrom(IList<double> other)
        {
            var a = new InputWrapper();

            a.R.CopyFrom(other);
            a.V.CopyFrom(other, 3);
            a.PV.CopyFrom(other, 6);
            a.PR.CopyFrom(other, 9);
            a.M  = other[12];
            a.Pm = other[13];
            a.Bt = other[14];

            return a;
        }
    }
}
