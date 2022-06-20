#nullable enable

using MechJebLib.Primitives;
using MechJebLib.PVG.Terminal;

namespace MechJebLib.PVG
{
    public class Problem
    {
        public readonly Scale       Scale;
        public          IPVGTerminal Terminal;
        public readonly V3          r0;
        public readonly V3          r0_bar;
        public readonly double      m0;
        public readonly double      m0_bar;
        public readonly V3          v0;
        public readonly V3          v0_bar;
        public readonly double      t0;

        public Problem(V3 r0, V3 v0, double m0, double t0, double mu)
        {
            Scale   = new Scale(r0, m0, mu);
            this.r0 = r0;
            r0_bar  = r0 / Scale.lengthScale;
            this.v0 = v0;
            v0_bar  = v0 / Scale.velocityScale;
            this.m0 = m0;
            m0_bar  = m0 / Scale.massScale;

            this.t0 = t0;
        }
    }
}
