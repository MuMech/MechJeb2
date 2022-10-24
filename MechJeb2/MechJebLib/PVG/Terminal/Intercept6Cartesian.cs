/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using MechJebLib.Primitives;

namespace MechJebLib.PVG.Terminal
{
    /// <summary>
    ///     6 constrant match to orbital state vectors.
    ///     This may work to bootstrap a problem, but will not be very useful for closed loop guidance once the exact solution
    ///     becomes impossible.
    /// </summary>
    public readonly struct Intercept6Cartesian : IPVGTerminal
    {
        private readonly V3 _rT;
        private readonly V3 _vT;

        public Intercept6Cartesian(V3 rT, V3 vT)
        {
            _rT = rT;
            _vT = vT;
        }

        public IPVGTerminal Rescale(Scale scale)
        {
            return new Intercept6Cartesian(_rT / scale.lengthScale, _vT / scale.velocityScale);
        }

        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf)
        {
            V3 rmiss = yf.R - _rT;
            V3 vmiss = yf.V - _vT;

            return (rmiss[0], rmiss[1], rmiss[2], vmiss[0], vmiss[1], vmiss[2]);
        }
    }
}
