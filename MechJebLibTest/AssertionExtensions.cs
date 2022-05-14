/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Globalization;
using Xunit.Sdk;
using static MechJebLib.Utils.Statics;

namespace AssertExtensions
{
    /// <summary>
    ///     Xunit Assertion Extensions
    /// </summary>
    public static class AssertionExtensions
    {
        // A proper relative tolerance comparison comparsion between float values.
        public static void ShouldEqual(this double actual,double expected,double epsilon)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero",nameof(epsilon));

            if (!NearlyEqual(actual,expected,epsilon))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture,"{0:G17}",expected),
                    string.Format(CultureInfo.CurrentCulture,"{0:G17}",actual)
                );
        }

        // Comparison to zero within a tolerance
        public static void ShouldBeZero(this double actual,double epsilon)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero",nameof(epsilon));

            if (Math.Abs(actual) > epsilon)
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture,"{0:G17}",0.0),
                    string.Format(CultureInfo.CurrentCulture,"{0:G17}",actual)
                );
        }
    }
}
