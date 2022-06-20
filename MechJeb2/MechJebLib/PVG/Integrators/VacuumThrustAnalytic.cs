#nullable enable

using System;
using MechJebLib.Primitives;

namespace MechJebLib.PVG.Integrators
{
    public class VacuumThrustAnalytic : IPVGIntegrator
    {
        public void Integrate(DD yin, DD yfout, Phase phase, double t0, double tf)
        {
            using var y0 = ArrayWrapper.Rent(yin);
            using var yf = ArrayWrapper.Rent(yfout);

            double rm = y0.R.magnitude;

            // precompute some values
            double omega = Math.Sqrt(rm * rm * rm);
            double delta = (tf - t0) / 4;
            double comega1 = Math.Cos(omega * delta);
            double somega1 = Math.Sin(omega * delta);
            double comega2 = Math.Cos(omega * 2 * delta);
            double somega2 = Math.Sin(omega * 2 * delta);
            double comega3 = Math.Cos(omega * 3 * delta);
            double somega3 = Math.Sin(omega * 3 * delta);
            double comega4 = Math.Cos(omega * 4 * delta);
            double somega4 = Math.Sin(omega * 4 * delta);

            // vehicle mass
            double m0 = phase.m0_bar;
            double m1 = phase.m0_bar - phase.mdot_bar * delta;
            double m2 = phase.m0_bar - phase.mdot_bar * 2 * delta;
            double m3 = phase.m0_bar - phase.mdot_bar * 3 * delta;
            double m4 = phase.m0_bar - phase.mdot_bar * 4 * delta;

            // vehicle thrust acceleration
            double at0 = phase.thrust_bar / m0;
            double at1 = phase.thrust_bar / m1;
            double at2 = phase.thrust_bar / m2;
            double at3 = phase.thrust_bar / m3;
            double at4 = phase.thrust_bar / m4;

            // equation 14 (pv for the quadrature points, and terminal pr)
            V3 pv0 = y0.PV;
            V3 pv1 = comega1 * y0.PV - somega1 * y0.PR / omega;
            V3 pv2 = comega2 * y0.PV - somega2 * y0.PR / omega;
            V3 pv3 = comega3 * y0.PV - somega3 * y0.PR / omega;
            V3 pv4 = comega4 * y0.PV - somega4 * y0.PR / omega;
            V3 pr4 = -(-somega4 * y0.PV - comega4 * y0.PR / omega) * omega;

            // equations 15 + 16 (for the quadrature points)
            V3 ic0 = pv0.normalized * at0;
            V3 is0 = V3.zero;
            V3 ic1 = pv1.normalized * Math.Cos(omega * 1 * delta) * at1;
            V3 is1 = pv1.normalized * Math.Sin(omega * 1 * delta) * at1;
            V3 ic2 = pv2.normalized * Math.Cos(omega * 2 * delta) * at2;
            V3 is2 = pv2.normalized * Math.Sin(omega * 2 * delta) * at2;
            V3 ic3 = pv3.normalized * Math.Cos(omega * 3 * delta) * at3;
            V3 is3 = pv3.normalized * Math.Sin(omega * 3 * delta) * at3;
            V3 ic4 = pv4.normalized * Math.Cos(omega * 4 * delta) * at4;
            V3 is4 = pv4.normalized * Math.Sin(omega * 4 * delta) * at4;

            // equation 20 (milne's rule)
            V3 Ic = 4 * delta / 90 * (7 * ic0 + 32 * ic1 + 12 * ic2 + 32 * ic3 + 7 * ic4);
            V3 Is = 4 * delta / 90 * (7 * is0 + 32 * is1 + 12 * is2 + 32 * is3 + 7 * is4);

            // equation 18
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

            yf.Pm = y0.Pm;
        }

        public void Integrate(DD y0in, DD yfout, Phase phase, double t0, double tf, Solution solution)
        {
            var interpolant = Hn.Get(ArrayWrapper.ARRAY_WRAPPER_LEN);
            interpolant.Add(t0, y0in);
            for (int i = 1; i < 21; i++)
            {
                double t2 = t0 + (tf - t0) * i / 20.0;
                Integrate(y0in, yfout, phase, t0, t2);
                
                // mildly janky fixup of the dv burned
                ArrayWrapper y0 = ArrayWrapper.Rent(y0in);
                ArrayWrapper yf = ArrayWrapper.Rent(yfout);
                double dt = t2 - t0;
                yf.DV = y0.DV + phase.ve_bar * Math.Log(phase.m0_bar / (phase.m0_bar - dt * phase.mdot_bar));
                
                interpolant.Add(t2, yfout);
            }

            solution.AddSegment(t0, tf, interpolant);
        }
    }
}
