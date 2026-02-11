/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("MechJebLibTest.TestInitialization", "MechJebLibTest")]

namespace MechJebLibTest
{
    public class TestInitialization : XunitTestFramework
    {
        public TestInitialization(IMessageSink messageSink) : base(messageSink)
        {
            // use per-thread object pools instead of global object pools, in order to isolate them per-test.
            ObjectPoolBase.UseGlobal = false;
        }
    }
}
