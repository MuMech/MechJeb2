/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest
{
    public class NumberTextTests
    {
        [Theory]
        [InlineData("0,5", 0.5)]
        [InlineData("1,5", 1.5)]
        [InlineData("1.5", 1.5)]
        [InlineData("1,000.5", 1000.5)]
        [InlineData("1,000,000", 1000000)]
        [InlineData("-2", -2)]
        [InlineData("1e6", 1e6)]
        public void ParsesDouble(string text, double expected)
        {
            Assert.True(NumberText.TryParseDouble(text, out double val));
            Assert.Equal(expected, val);
        }

        [Theory]
        [InlineData("")]
        [InlineData("-")]
        [InlineData("1.2.3")]
        public void RejectsDouble(string text) => Assert.False(NumberText.TryParseDouble(text, out _));

        [Fact]
        public void ParsesIntList()
        {
            var list = new List<int>();
            NumberText.ParseIntList("1,3-5,9", list);
            Assert.Equal(new[] { 1, 3, 4, 5, 9 }, list);
        }

        [Theory]
        [InlineData("0-2147483647")]
        [InlineData("1-99999999")]
        [InlineData("-5-2147483647")]
        public void IgnoresHugeRange(string text)
        {
            var list = new List<int>();
            NumberText.ParseIntList(text, list);
            Assert.Empty(list);
        }
    }
}
