#nullable enable

using MechJebLib.Maths;
using MechJebLib.Primitives;

namespace MechJebLib.PVG.Integrators
{
    public class VacuumCoastIntegrator : IPVGIntegrator
    {
        public void Integrate(DD yin, DD yfout, Phase phase, double t0, double tf)
        {
            using var y0 = ArrayWrapper.Rent(yin);
            using var yf = ArrayWrapper.Rent(yfout);

            Shepperd.Solve2(1.0, tf - t0, y0.R, yf.V, out V3 rf, out V3 vf, out M3 stm00, out M3 stm01, out M3 stm10,
                out M3 stm11);

            yf.R = rf;
            yf.V = vf;

            yf.PV = stm00 * yf.PV + stm01 * yf.PR;
            yf.PR = stm10 * yf.PV + stm11 * yf.PR;

            yf.Pm = y0.Pm;
        }

        public void Integrate(DD yin, DD yfout, Phase phase, double t0, double tf, Solution solution)
        {
            var interpolant = Hn.Get(ArrayWrapper.ARRAY_WRAPPER_LEN);
            interpolant.Add(t0, yin);
            for (int i = 1; i < 21; i++)
            {
                double t2 = t0 + (tf - t0) * i / 20.0;
                Integrate(yin, yfout, phase, t0, t2);
                interpolant.Add(t2, yfout);
            }

            solution.AddSegment(t0, tf, interpolant);
        }
    }
}
