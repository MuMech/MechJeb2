/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Globalization;
using KodeKit.Headless;
using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    public class CharBufferTests
    {
        [Theory]
        [InlineData(0.0, 0)]
        [InlineData(1234.5, 0)]
        [InlineData(1.005, 2)]
        [InlineData(-0.001, 2)]
        [InlineData(-12.3456, 3)]
        [InlineData(99999.9999, 2)]
        [InlineData(0.5, 0)]
        [InlineData(2.5, 0)]
        public void FixedPointIsToStringFInTheInvariantCulture(double v, int decimals)
        {
            string expected = v.ToString("F" + decimals, CultureInfo.InvariantCulture);
            Assert.Equal(expected, new CharBuffer().Append(v, decimals).ToString());
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        [InlineData(999.96)]
        [InlineData(1234.5)]
        [InlineData(2.15e6)]
        [InlineData(0.00123)]
        [InlineData(-4.56e4)]
        public void SiIsMechJebLibsToSI(double v) => Assert.Equal(v.ToSI(), new CharBuffer().AppendSI(v).ToString());

        [Fact]
        public void DhmsRoundsBeforeSplittingIntoUnits() =>
            Assert.Equal("4m 00.0s", new CharBuffer().AppendDHMS(239.97, 1, 21600, 9201600).ToString());

        [Fact]
        public void ClearKeepsTheBufferAndTheTextStartsAgain()
        {
            CharBuffer b = new CharBuffer(4).Append("abcdefgh");
            char[] chars = b.Chars;
            b.Clear().Append('x');
            Assert.Same(chars, b.Chars);
            Assert.Equal("x", b.ToString());
        }

        [Fact]
        public void SameAsComparesTheTextWithAnArrayPrefix()
        {
            CharBuffer b = new CharBuffer().Append("12");
            Assert.True(b.SameAs(new[] { '1', '2', 'z' }, 2));
            Assert.False(b.SameAs(new[] { '1', '3' }, 2));
            Assert.False(b.SameAs(new[] { '1', '2' }, -1));
        }

        [Fact]
        public void FormattingAllocatesNothing()
        {
            var b = new CharBuffer(64);
            b.Append(1234.5678, 2).AppendSI(2.5e6).AppendInt(-42);
            b.Clear().AppendDHMS(3725.5, 1, 21600, 9201600);
            AssertionExtensions.ShouldNotAllocate(() =>
            {
                for (int i = 0; i < 1000; i++) b.Clear().Append(1234.5678, 2).Append(" m/s").AppendSI(2.5e6).AppendInt(-42).AppendDHMS(3725.5, 1, 21600, 9201600);
            });
        }
    }
}
