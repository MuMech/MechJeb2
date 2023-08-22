/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Globalization;
using MechJebLib.Primitives;
using Xunit.Sdk;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace AssertExtensions
{
    /// <summary>
    ///     Xunit Assertion Extensions
    /// </summary>
    public static class AssertionExtensions
    {
        // A proper relative tolerance comparison comparsion between float values.
        public static void ShouldEqual(this double actual, double expected, double epsilon = EPS)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(epsilon));

            if (!NearlyEqual(actual, expected, epsilon))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        public static void ShouldEqual(this V3 actual, V3 expected, double epsilon = EPS)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(epsilon));

            if (!NearlyEqual(actual, expected, epsilon))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        public static void ShouldEqual(this M3 actual, M3 expected, double epsilon = EPS)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(epsilon));

            if (!NearlyEqual(actual, expected, epsilon))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        // Comparison to zero within a tolerance
        public static void ShouldBeZero(this double actual, double epsilon = EPS)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(epsilon));

            if (Abs(actual) > epsilon)
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", 0.0),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        // Comparison to zero within a tolerance
        public static void ShouldBeZero(this V3 actual, double epsilon = EPS)
        {
            if (double.IsNaN(epsilon) || double.IsNegativeInfinity(epsilon) || epsilon < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(epsilon));

            if (Abs(actual.x) > epsilon || Abs(actual.y) > epsilon || Abs(actual.z) > epsilon)
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", 0.0),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        public static void ShouldBePositive(this double actual)
        {
            if (!actual.IsFinite())
                throw new XunitException($"{actual} must be finite");

            if (actual <= 0)
                throw new XunitException($"{actual} must be positive");
        }
    }
}
