/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.TwoBody;

namespace MechJebLib.PVG.Integrators
{
    public class VacuumCoastAnalytic : IPVGIntegrator
    {
        public void IntegrateInternal(Vn yin, Vn yfout, Phase phase, double t0, double tf)
        {
            var y0 = IntegratorRecord.CreateFrom(yin);
            var yf = new IntegratorRecord();

            (V3 rf, V3 vf, M3 stm00, M3 stm01, M3 stm10, M3 stm11) = Shepperd.Solve2(1.0, tf - t0, y0.R, y0.V);

            yf.R = rf;
            yf.V = vf;

            yf.Pv = stm00 * y0.Pv - stm01 * y0.Pr;
            yf.Pr = -stm10 * y0.Pv + stm11 * y0.Pr;

            yf.M = y0.M;

            yf.DV = y0.DV;

            yf.CopyTo(yfout);
        }

        public Hn Integrate(Vn yin, Vn yfout, Phase phase, double t0, double tf)
        {
            var interpolant = Hn.Get(IntegratorRecord.INTEGRATOR_REC_LEN);
            interpolant.Add(t0, yin);
            for (int i = 1; i < 21; i++)
            {
                double t2 = t0 + (tf - t0) * i / 20.0;
                IntegrateInternal(yin, yfout, phase, t0, t2);
                interpolant.Add(t2, yfout);
            }

            return interpolant;
        }
    }
}
