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

namespace MechJebLibTest
{
    /// <summary>
    ///     Xunit Assertion Extensions
    /// </summary>
    public static class AssertionExtensions
    {
        public static void ShouldEqual(this int actual, int expected)
        {
            if (actual != expected)
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

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

        public static void ShouldNotBeZero(this double actual)
        {
            if (actual == 0)
                throw new XunitException($"Expected non-zero value, but was {actual}");
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

        // Handles radians wrapping around at 2PI
        public static void ShouldBeZeroRadians(this double actual, double epsilon = EPS)
        {
            if (actual > PI)
                actual.ShouldEqual(TAU, epsilon);
            else
                actual.ShouldBeZero(epsilon);
        }

        public static void ShouldBePositive(this double actual)
        {
            if (!IsFinite(actual))
                throw new XunitException($"{actual} must be finite");

            if (actual <= 0)
                throw new XunitException($"{actual} must be positive");
        }

        public static void ShouldBePositiveInfinity(this double actual)
        {
            if (!double.IsPositiveInfinity(actual))
                throw new XunitException($"Expected positive infinity, but was {actual}");
        }

        public static void ShouldBeNegativeInfinity(this double actual)
        {
            if (!double.IsNegativeInfinity(actual))
                throw new XunitException($"Expected negative infinity, but was {actual}");
        }

        public static void ShouldBeFinite(this double actual)
        {
            if (!IsFinite(actual))
                throw new XunitException($"{actual} must be finite");
        }

        public static void ShouldBeNaN(this double actual)
        {
            if (!double.IsNaN(actual))
                throw new XunitException($"Expected NaN, but was {actual}");
        }

        public static void ShouldBeGreaterThan(this double actual, double expected)
        {
            if (actual <= expected)
                throw new XunitException($"Expected {actual} to be greater than {expected}");
        }

        public static void ShouldBeLessThan(this double actual, double expected)
        {
            if (actual >= expected)
                throw new XunitException($"Expected {actual} to be less than {expected}");
        }

        public static void ShouldBeGreaterThanOrEqualTo(this double actual, double expected)
        {
            if (actual < expected)
                throw new XunitException($"Expected {actual} to be greater than or equal to {expected}");
        }

        public static void ShouldBeLessThanOrEqualTo(this double actual, double expected)
        {
            if (actual > expected)
                throw new XunitException($"Expected {actual} to be less than or equal to {expected}");
        }

        public static void ShouldBeTrue(this bool actual)
        {
            if (!actual)
                throw new XunitException("Expected true, but was false");
        }

        public static void ShouldBeFalse(this bool actual)
        {
            if (actual)
                throw new XunitException("Expected false, but was true");
        }
    }
}
