/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using MechJebLib.Primitives;
using MechJebLib.PVG.Terminal;

namespace MechJebLib.PVG
{
    public class Problem
    {
        public readonly Scale               Scale;
        public          IPVGTerminal?       Terminal;
        public readonly V3                  R0;
        public readonly V3                  R0Bar;
        public readonly double              M0;
        public readonly double              M0Bar;
        public readonly V3                  V0;
        public readonly V3                  V0Bar;
        public readonly double              T0;
        public readonly V3                  U0;
        public readonly double              Mu;
        public readonly double              Rbody;

        public Problem(V3 r0, V3 v0, V3 u0, double m0, double t0, double mu, double rbody)
        {
            Scale = new Scale(r0, m0, mu);
            Mu    = mu;
            R0    = r0;
            R0Bar = r0 / Scale.lengthScale;
            V0    = v0;
            V0Bar = v0 / Scale.velocityScale;
            M0    = m0;
            M0Bar = m0 / Scale.massScale;
            U0    = u0;
            Rbody = rbody;
            
            T0    = t0;
        }
    }
}
