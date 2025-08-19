/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Globalization;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class StringRepresentationTests
    {
        [Fact]
        private void ToStringDefaultFormat()
        {
            var v = new V3(1.5, 2.5, 3.5);
            v.ToString().ShouldEqual("[1.5, 2.5, 3.5]");
        }

        [Fact]
        private void ToStringWithZeroVector()
        {
            V3.zero.ToString().ShouldEqual("[0, 0, 0]");
        }

        [Fact]
        private void ToStringWithNegativeValues()
        {
            var v = new V3(-1.5, -2.5, -3.5);
            v.ToString().ShouldEqual("[-1.5, -2.5, -3.5]");
        }

        [Fact]
        private void ToStringWithMixedSigns()
        {
            var v = new V3(1, -2, 3);
            v.ToString().ShouldEqual("[1, -2, 3]");
        }

        [Fact]
        private void ToStringWithLargeValues()
        {
            var v = new V3(1.23456789e100, 2.3456789e150, 3.456789e200);
            v.ToString().ShouldEqual("[1.23456789E+100, 2.3456789E+150, 3.456789E+200]");
        }

        [Fact]
        private void ToStringWithSmallValues()
        {
            var v = new V3(1.23456789e-100, 2.3456789e-150, 3.456789e-200);
            v.ToString().ShouldEqual("[1.23456789E-100, 2.3456789E-150, 3.456789E-200]");
        }

        [Fact]
        private void ToStringWithSpecialValues()
        {
            new V3(double.PositiveInfinity, double.NegativeInfinity, double.NaN)
                .ToString().ShouldEqual("[Infinity, -Infinity, NaN]");
        }

        [Fact]
        private void ToStringWithFixedPointFormat()
        {
            var v = new V3(1.23456789, 2.3456789, 3.456789);
            v.ToString("F2").ShouldEqual("[1.23, 2.35, 3.46]");
            v.ToString("F4").ShouldEqual("[1.2346, 2.3457, 3.4568]");
        }

        [Fact]
        private void ToStringWithFixedPointZeroPrecision()
        {
            var v = new V3(1.5, 2.6, 3.4);
            v.ToString("F0").ShouldEqual("[2, 3, 3]");
        }

        [Fact]
        private void ToStringWithExponentialFormat()
        {
            var v = new V3(123.456, 0.00123456, 1234560);
            v.ToString("E2").ShouldEqual("[1.23E+002, 1.23E-003, 1.23E+006]");
        }

        [Fact]
        private void ToStringWithGeneralFormat()
        {
            var v = new V3(123.456, 0.00123456, 1234560);
            v.ToString("G3").ShouldEqual("[123, 0.00123, 1.23E+06]");
        }

        [Fact]
        private void ToStringWithRoundTripFormat()
        {
            var v = new V3(1.0 / 3.0, 2.0 / 3.0, 4.0 / 3.0);
            string result = v.ToString("R");

            result.ShouldContain("0.33333333333333331");
            result.ShouldContain("0.66666666666666663");
            result.ShouldContain("1.3333333333333333");
        }

        [Fact]
        private void ToStringWithPercentFormat()
        {
            var v = new V3(0.1, 0.25, 0.5);
            v.ToString("P1").ShouldEqual("[10.0 %, 25.0 %, 50.0 %]");
        }

        [Fact]
        private void ToStringWithNumberFormat()
        {
            var v = new V3(1234.5, 6789.25, 10000);
            v.ToString("N1").ShouldEqual("[1,234.5, 6,789.3, 10,000.0]");
        }

        [Fact]
        private void ToStringWithNullFormat()
        {
            var v = new V3(1.5, 2.5, 3.5);
            v.ToString(null).ShouldEqual("[1.5, 2.5, 3.5]");
        }

        [Fact]
        private void ToStringWithEmptyFormat()
        {
            var v = new V3(1.5, 2.5, 3.5);
            v.ToString("").ShouldEqual("[1.5, 2.5, 3.5]");
        }

        [Fact]
        private void ToStringWithCustomFormatProvider()
        {
            var v = new V3(1234.5, 6789.25, 10000.125);
            var germanCulture = new CultureInfo("de-DE");

            string result = v.ToString("F2", germanCulture);
            result.ShouldEqual("[1234,50, 6789,25, 10000,13]");
        }

        [Fact]
        private void ToStringWithFrenchCulture()
        {
            var v = new V3(1234.5, 6789.25, 10000.125);
            var frenchCulture = new CultureInfo("fr-FR");

            string result = v.ToString("F2", frenchCulture);
            result.ShouldEqual("[1234,50, 6789,25, 10000,13]");
        }

        [Fact]
        private void ToStringPreservesInvariantCultureByDefault()
        {
            var v = new V3(1234.5, 6789.25, 10000.125);

            string result1 = v.ToString();
            string result2 = v.ToString("G");

            result1.ShouldEqual("[1234.5, 6789.25, 10000.125]");
            result2.ShouldEqual("[1234.5, 6789.25, 10000.125]");
        }

        [Fact]
        private void ToStringWithUnitVectors()
        {
            V3.xaxis.ToString().ShouldEqual("[1, 0, 0]");
            V3.yaxis.ToString().ShouldEqual("[0, 1, 0]");
            V3.zaxis.ToString().ShouldEqual("[0, 0, 1]");
        }

        [Fact]
        private void ToStringWithVeryHighPrecision()
        {
            var v = new V3(1.0 / 3.0, 2.0 / 7.0, 5.0 / 11.0);

            v.ToString("F10").ShouldEqual("[0.3333333333, 0.2857142857, 0.4545454545]");
            v.ToString("F15").ShouldEqual("[0.333333333333333, 0.285714285714286, 0.454545454545455]");
        }

        [Fact]
        private void ToStringRoundingBehavior()
        {
            var v = new V3(1.234, 1.235, 1.236);
            v.ToString("F2").ShouldEqual("[1.23, 1.24, 1.24]");

            var v2 = new V3(1.2345, 1.2355, 1.2365);
            v2.ToString("F3").ShouldEqual("[1.235, 1.236, 1.237]");
        }

        [Fact]
        private void ToStringConsistentBracketFormat()
        {
            var v = new V3(1, 2, 3);
            string result = v.ToString();

            result.ShouldStartWith("[");
            result.ShouldEndWith("]");
            result.ShouldContain(", ");
            result.ShouldEqual("[1, 2, 3]");
        }

        [Fact]
        private void ToStringWithScientificNotation()
        {
            var v = new V3(1.23e-10, 4.56e20, 7.89e5);
            string result = v.ToString();

            result.ShouldContain("1.23E-10");
            result.ShouldContain("4.56E+20");
            result.ShouldContain("789000");
        }

        [Fact]
        private void ToStringAlwaysUsesCommaSeparator()
        {
            var v = new V3(1, 2, 3);
            var germanCulture = new CultureInfo("de-DE");

            string result = v.ToString("G", germanCulture);
            result.ShouldEqual("[1, 2, 3]");
        }

        [Fact]
        private void ToStringHandlesMaxAndMinValues()
        {
            V3.maxvalue.ToString("E2").ShouldContain("1.80E+308");
            V3.minvalue.ToString("E2").ShouldContain("-1.80E+308");
        }

        [Fact]
        private void ToStringNaNConsistency()
        {
            var v1 = new V3(double.NaN, 0, 0);
            var v2 = new V3(0, double.NaN, 0);
            var v3 = new V3(0, 0, double.NaN);

            v1.ToString().ShouldContain("NaN");
            v2.ToString().ShouldContain("NaN");
            v3.ToString().ShouldContain("NaN");
        }

        [Fact]
        private void ToStringInfinityConsistency()
        {
            var v1 = new V3(double.PositiveInfinity, 0, 0);
            var v2 = new V3(0, double.NegativeInfinity, 0);

            v1.ToString().ShouldContain("Infinity");
            v2.ToString().ShouldContain("-Infinity");
        }

        [Fact]
        private void ToStringFormatCaseInsensitive()
        {
            var v = new V3(1.234, 2.345, 3.456);

            v.ToString("f2").ShouldEqual(v.ToString("F2"));
            v.ToString("g3").ShouldEqual(v.ToString("G3"));
        }
    }
}
