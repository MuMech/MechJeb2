using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public readonly struct LQRLayout
    {
        public const int LQR_LAYOUT_LEN = 27;

        public readonly M3 Kr;
        public readonly M3 Kv;

        public LQRLayout(M3 kr, M3 kv)
        {
            Kr = kr;
            Kv = kv;
        }

        public void CopyTo(IList<double> other)
        {
            Kr.CopyTo(other, 0);
            Kv.CopyTo(other, 9);
        }

        public static LQRLayout CreateFrom(IList<double> other) =>
            new LQRLayout(
                M3.CopyFrom(other, 0),
                M3.CopyFrom(other, 9)
            );
    }
}
