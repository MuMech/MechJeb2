#nullable enable

using System;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public readonly struct Scale
    {
        private readonly double accelScale;
        private readonly double areaScale;
        private readonly double densityScale;
        public readonly  double forceScale;
        public readonly  double g_bar;
        public readonly  double lengthScale;
        public readonly  double massScale;
        public readonly  double mdotScale;
        private readonly double pressureScale;
        public readonly  double timeScale;
        public readonly  double velocityScale;
        private readonly double volumeScale;

        public Scale(V3 r0, double m0, double mu)
        {
            massScale     = m0;
            lengthScale   = r0.magnitude;
            g_bar         = mu / (lengthScale * lengthScale);
            velocityScale = Math.Sqrt(lengthScale * g_bar);
            timeScale     = lengthScale / velocityScale;
            accelScale    = velocityScale / timeScale;
            forceScale    = massScale * accelScale;
            mdotScale     = massScale / timeScale;
            areaScale     = lengthScale * lengthScale;
            volumeScale   = areaScale * lengthScale;
            densityScale  = massScale / volumeScale;
            pressureScale = forceScale / areaScale;
        }
    }
}
