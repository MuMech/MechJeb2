/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;

namespace MechJebLib.PSG
{
    public readonly struct Problem
    {
        public readonly Scale     Scale;
        public readonly ITerminal Terminal;
        public readonly V3        R0;
        public readonly V3        V0;
        public readonly double    M0;
        public readonly double    T0;
        public readonly V3        U0;
        public readonly double    Mu;
        public readonly double    RBody;
        public readonly double    Rho0CdAref;
        public readonly double    Rho0InvQAlphaMax;
        public readonly double    Rho0InvQMax;
        public readonly double    H0;
        public readonly V3        W;

        public Problem(V3 r0, V3 v0, V3 u0, double m0, double t0, double mu, double rbody, double h0, double rho0CdAref, double rho0InvQAlphaMax, double rho0InvQMax, V3 w, ITerminal terminal)
        {
            Scale            = Scale.Create(mu, r0.magnitude, m0);
            Mu               = mu;
            R0               = r0 / Scale.LengthScale;
            V0               = v0 / Scale.VelocityScale;
            M0               = m0 / Scale.MassScale;
            U0               = u0;
            RBody            = rbody / Scale.LengthScale;
            H0               = h0 / Scale.LengthScale;
            Rho0CdAref       = rho0CdAref / Scale.MassScale * Scale.LengthScale;
            Rho0InvQAlphaMax = rho0InvQAlphaMax * (Scale.VelocityScale * Scale.VelocityScale);
            Rho0InvQMax      = rho0InvQMax * (Scale.VelocityScale * Scale.VelocityScale);
            Terminal         = terminal.Rescale(Scale);
            T0               = t0;
            W                = w * Scale.TimeScale;
        }

        public Problem(Scale scale, V3 r0, V3 v0, V3 u0, double m0, double t0, double mu, double rbody, double h0, double rho0CdAref, double rho0InvQAlphaMax, double rho0InvQMax, V3 w, ITerminal terminal)
        {
            Scale            = scale;
            Mu               = mu;
            R0               = r0;
            V0               = v0;
            M0               = m0;
            U0               = u0;
            RBody            = rbody;
            H0               = h0;
            Rho0CdAref       = rho0CdAref;
            Rho0InvQAlphaMax = rho0InvQAlphaMax;
            Rho0InvQMax      = rho0InvQMax;
            Terminal         = terminal;
            T0               = t0;
            W                = w;
        }

        public Problem WithoutDynamicPressure()
        {
            return new Problem(
                Scale, R0, V0, U0, M0, T0, Mu, RBody, H0, Rho0CdAref, 0, 0, W, Terminal
            );
        }
    }
}
