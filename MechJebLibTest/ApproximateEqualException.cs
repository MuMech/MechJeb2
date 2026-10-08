/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Globalization;
using Xunit.Sdk;

namespace MechJebLibTest
{
    public class ApproximateEqualException : XunitException
    {
        public ApproximateEqualException(string expected, string actual, double epsilon)
            : base(string.Format(
                CultureInfo.CurrentCulture,
                "NearlyEquals() Failure: Values are not approximately equal\n" +
                "Expected: {0}\n" +
                "Actual:   {1}\n" +
                "Epsilon:  {2:G}",
                expected, actual, epsilon))
        {
        }
    }
}
