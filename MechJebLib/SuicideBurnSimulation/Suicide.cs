/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿using System;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.SuicideBurnSimulation
{
    public partial class Suicide : BackgroundJob<Suicide.SuicideResult>
    {
        public struct SuicideResult
        {
            public V3     Rf;
            public double Tf;
        }

        public static SuicideBuilder Builder() => new SuicideBuilder();

        protected override SuicideResult Run(object? o) => throw new NotImplementedException();
    }
}
