/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿using System;

namespace MechJebLib.Utils
{
    public class MechJebLibException : Exception
    {
        public MechJebLibException() { }
        public MechJebLibException(string message) : base(message) { }
        public MechJebLibException(string message, Exception inner) : base(message, inner) { }
    }
}
