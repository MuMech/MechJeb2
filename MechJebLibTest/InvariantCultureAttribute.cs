/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

namespace MechJebLibTest
{
    /// <summary>
    ///     Runs a test in the invariant culture, for tests that compare formatted numbers with fixed strings: otherwise the
    ///     decimal separator and the infinity symbol follow the machine's culture (en-GB on Windows prints infinity as "∞").
    /// </summary>
    public sealed class InvariantCultureAttribute : BeforeAfterTestAttribute
    {
        private CultureInfo? _culture;

        public override void Before(MethodInfo methodUnderTest)
        {
            _culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (_culture != null)
                CultureInfo.CurrentCulture = _culture;
        }
    }
}
