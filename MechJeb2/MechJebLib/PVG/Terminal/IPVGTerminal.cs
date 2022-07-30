/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

namespace MechJebLib.PVG.Terminal
{
    public interface IPVGTerminal
    {
        public (double a, double b, double c, double d, double e, double f) TerminalConstraints(ArrayWrapper yf);
    }
}
