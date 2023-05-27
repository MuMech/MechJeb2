/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;

namespace MechJebLib.Primitives
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

        public Scale(double lengthScale, double velocityScale, double massScale)
        {
            LengthScale   = lengthScale;
            MassScale     = massScale;
            VelocityScale = velocityScale;
        }

        public static Scale Create(double mu, double r0, double m0=1.0)
        {
            double massScale     = m0;
            double lengthScale   = r0;
            double velocityScale = Math.Sqrt(mu / lengthScale);
            return new Scale(lengthScale, velocityScale, massScale);
        }

        public Scale ConvertTo(Scale other)
        {
            return new Scale(
                 other.LengthScale / LengthScale,
                 other.VelocityScale / VelocityScale,
                 other.MassScale / MassScale
                );
        }
    }
}
