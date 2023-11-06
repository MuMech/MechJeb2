/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using MechJebLib.Simulations.PartModules;

namespace MechJebLib.Simulations
{
    public readonly struct SimPropellant
    {
        public readonly int         id;
        public readonly bool        ignoreForIsp;
        public readonly double      ratio;
        public readonly SimFlowMode FlowMode;
        public readonly double      density;

        public SimPropellant(int id, bool ignoreForIsp, double ratio, SimFlowMode flowMode, double density)
        {
            this.id           = id;
            this.ignoreForIsp = ignoreForIsp;
            this.ratio        = ratio;
            FlowMode          = flowMode;
            this.density      = density;
        }
    }
}
