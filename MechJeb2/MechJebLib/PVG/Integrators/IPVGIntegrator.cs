/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using MechJebLib.Primitives;

#nullable enable

namespace MechJebLib.PVG.Integrators
{
    public interface IPVGIntegrator
    {
        void Integrate(DD y0, DD yf, Phase phase, double t0, double tf);
        void Integrate(DD y0, DD yf, Phase phase, double t0, double tf, Solution solution);
    }
}
