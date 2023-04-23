/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using MechJebLib.Primitives;

namespace MechJebLib.PVG
{
    public readonly struct Scale
    {
        public readonly double LengthScale;
        public readonly double MassScale;
        public readonly double VelocityScale;

        public double TimeScale     => LengthScale / VelocityScale;
        public double AccelScale    => VelocityScale / TimeScale;
        public double ForceScale    => MassScale * AccelScale;
        public double MdotScale     => MassScale / TimeScale;
        public double AreaScale     => LengthScale * LengthScale;
        public double VolumeScale   => AreaScale * LengthScale;
        public double DensityScale  => MassScale / VolumeScale;
        public double PressureScale => ForceScale / AreaScale;

        public Scale(V3 r0, double m0, double mu)
        {
            MassScale     = m0;
            LengthScale   = r0.magnitude;
            VelocityScale = Math.Sqrt(mu / LengthScale);
        }
    }
}
