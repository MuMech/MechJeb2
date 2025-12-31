using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public readonly struct RiccatiLayout
    {
        public const int RICCATI_LAYOUT_LEN = 27;

        public readonly M3 Prr;
        public readonly M3 Prv;
        public readonly M3 Pvv;

        public RiccatiLayout(M3 prr, M3 prv, M3 pvv)
        {
            Prr = prr;
            Prv = prv;
            Pvv = pvv;
        }

        public void CopyTo(IList<double> other)
        {
            Prr.CopyTo(other, 0);
            Prv.CopyTo(other, 9);
            Pvv.CopyTo(other, 18);
        }

        public static RiccatiLayout CreateFrom(IList<double> other) =>
            new RiccatiLayout(
                M3.CopyFrom(other, 0),
                M3.CopyFrom(other, 9),
                M3.CopyFrom(other, 18)
            );
    }
}
