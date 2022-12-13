/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.PVG.Integrators
{
    // from: https://www.researchgate.net/publication/245433631_Rapid_Optimal_Multi-Burn_Ascent_Planning_and_Guidance/link/54cbc8ea0cf29ca810f43929/download
    public class VacuumThrustAnalytic : IPVGIntegrator
    {
        public void Integrate(DD yin, DD yfout, Phase phase, double t0, double tf)
        {
            Check.True(phase.Normalized);
            
            using var y0 = ArrayWrapper.Rent(yin);
            using var yf = ArrayWrapper.Rent(yfout);

            double rm = y0.R.magnitude;

            double thrust = phase.thrust;
            double mdot = phase.mdot;

            if (phase.Infinite)
            {
                thrust *= 2;
                mdot   =  0;
            }
            
            // precompute some values
            double omega = 1.0/Math.Sqrt(rm * rm * rm);
            double dt = tf - t0;
            double delta = dt / 4;
            double comega1 = Math.Cos(omega * delta);
            double somega1 = Math.Sin(omega * delta);
            double comega2 = Math.Cos(omega * 2 * delta);
            double somega2 = Math.Sin(omega * 2 * delta);
            double comega3 = Math.Cos(omega * 3 * delta);
            double somega3 = Math.Sin(omega * 3 * delta);
            double comega4 = Math.Cos(omega * 4 * delta);
            double somega4 = Math.Sin(omega * 4 * delta);

            // vehicle mass at the quadrature points
            double m0 = phase.m0;
            double m1 = m0 - mdot * delta;
            double m2 = m0 - mdot * 2 * delta;
            double m3 = m0 - mdot * 3 * delta;
            double m4 = m0 - mdot * 4 * delta;

            // vehicle thrust acceleration at the quadrature points
            double at0 = thrust / m0;
            double at1 = thrust / m1;
            double at2 = thrust / m2;
            double at3 = thrust / m3;
            double at4 = thrust / m4;

            // equation 14 (pv for the quadrature points, and terminal pr)
            V3 pv1 = comega1 * y0.PV - somega1 * y0.PR / omega;
            V3 pv2 = comega2 * y0.PV - somega2 * y0.PR / omega;
            V3 pv3 = comega3 * y0.PV - somega3 * y0.PR / omega;
            V3 pv4 = comega4 * y0.PV - somega4 * y0.PR / omega;
            V3 pr4 = somega4 * y0.PV * omega + comega4 * y0.PR;

            bool unguided = phase.Unguided;
            V3 u0 = phase.u0.normalized;

            // equations 15 + 16 (for the quadrature points)
            V3 ic0 = (unguided ? u0 : y0.PV.normalized) * at0;
            V3 is0 = V3.zero;
            V3 ic1 = (unguided ? u0 : pv1.normalized) * Math.Cos(omega * 1 * delta) * at1;
            V3 is1 = (unguided ? u0 : pv1.normalized) * Math.Sin(omega * 1 * delta) * at1;
            V3 ic2 = (unguided ? u0 : pv2.normalized) * Math.Cos(omega * 2 * delta) * at2;
            V3 is2 = (unguided ? u0 : pv2.normalized) * Math.Sin(omega * 2 * delta) * at2;
            V3 ic3 = (unguided ? u0 : pv3.normalized) * Math.Cos(omega * 3 * delta) * at3;
            V3 is3 = (unguided ? u0 : pv3.normalized) * Math.Sin(omega * 3 * delta) * at3;
            V3 ic4 = (unguided ? u0 : pv4.normalized) * Math.Cos(omega * 4 * delta) * at4;
            V3 is4 = (unguided ? u0 : pv4.normalized) * Math.Sin(omega * 4 * delta) * at4;

            // equation 20 (milne's rule)
            V3 Ic = 4 * delta / 90 * (7 * ic0 + 32 * ic1 + 12 * ic2 + 32 * ic3 + 7 * ic4);
            V3 Is = 4 * delta / 90 * (7 * is0 + 32 * is1 + 12 * is2 + 32 * is3 + 7 * is4);

            // equation 18 (state equations)
            V3 rf = (comega4 * y0.R * omega + somega4 * y0.V + somega4 * Ic - comega4 * Is) / omega;
            V3 vf = -somega4 * y0.R * omega + comega4 * y0.V + comega4 * Ic + somega4 * Is;

            yf.R  = rf;
            yf.V  = vf;
            yf.PV = pv4;
            yf.PR = pr4;
            yf.M  = m4;

            /*
            if (Phase.FinalMassProblem)
                throw new ArgumentException("linearized thrust integrals do not support mass costate");
                */

            if (!phase.Infinite)
                yf.DV = y0.DV + phase.ve * Math.Log(phase.m0 / (phase.m0 - dt * phase.mdot));
            else
                yf.DV = y0.DV + phase.thrust / phase.m0 * dt;
            
            yf.Pm = y0.Pm;  // FIXME: this is certainly wrong
        }

        public void Integrate(DD y0in, DD yfout, Phase phase, double t0, double tf, Solution solution)
        {
            // kinda janky way to compute interpolants with intermediate points
            var interpolant = Hn.Get(ArrayWrapper.ARRAY_WRAPPER_LEN);
            interpolant.Add(t0, y0in);
            const int SEGMENTS = 20;
            using var y0 = ArrayWrapper.Rent(y0in);

            for (int i = 1; i < 21; i++)
            {
                double dt = (tf - t0) / SEGMENTS * i;
                double t = t0 + dt;
                Integrate(y0in, yfout, phase, t0, t);

                interpolant.Add(t, yfout);
            }

            solution.AddSegment(t0, tf, interpolant, phase);
        }
    }
}
