/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;

#nullable enable

namespace MechJebLib.PVG.Integrators
{
    public interface IPVGIntegrator
    {
        void Integrate(Vn y0, Vn yf, Phase phase, double t0, double tf);
        void Integrate(Vn y0, Vn yf, Phase phase, double t0, double tf, Solution solution);
    }
}
