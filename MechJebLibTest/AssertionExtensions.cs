/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections.Generic;
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
                    string.Format(CultureInfo.CurrentCulture, "{0}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0}", actual)
                );
        }

        // A proper relative tolerance comparison comparsion between float values.
        public static void ShouldEqual(this double actual, double expected, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            if (!NearlyEqual(actual, expected, tolerance))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        public static void ShouldEqual(this V3 actual, V3 expected, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            if (!NearlyEqual(actual, expected, tolerance))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        public static void ShouldEqual(this M3 actual, M3 expected, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            if (!NearlyEqual(actual, expected, tolerance))
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", expected),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        // Comparison to zero within a tolerance
        public static void ShouldBeZero(this double actual, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            if (Abs(actual) > tolerance)
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", 0.0),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        // Comparison to zero within a tolerance
        public static void ShouldBeZero(this V3 actual, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            if (Abs(actual.x) > tolerance || Abs(actual.y) > tolerance || Abs(actual.z) > tolerance)
                throw new EqualException(
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", 0.0),
                    string.Format(CultureInfo.CurrentCulture, "{0:G17}", actual)
                );
        }

        public static void ShouldBePositive(this double actual)
        {
            if (!IsFinite(actual))
                throw new XunitException($"{actual} must be finite");

            if (actual <= 0)
                throw new XunitException($"{actual} must be positive");
        }

        public static void ShouldContain(this double[] actual, double expected, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            foreach (double act in actual)
                if (NearlyEqual(act, expected, tolerance))
                    return;

            throw new XunitException($"{DoubleArrayString(actual)} does not contain {expected}");
        }

        public static void ShouldContain(this double[] actual, IEnumerable<double> expected, double tolerance = EPS)
        {
            if (double.IsNaN(tolerance) || double.IsNegativeInfinity(tolerance) || tolerance < 0.0)
                throw new ArgumentException("Epsilon must be greater than or equal to zero", nameof(tolerance));

            foreach (double ex in expected)
                actual.ShouldContain(ex, tolerance);
        }
    }
}
