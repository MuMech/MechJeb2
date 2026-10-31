/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest.Utils
{
    // Check is always compiled, so these must pass in both Debug and Release.
    public class CheckTests
    {
        // Assert.Throws is exact-type, so this also pins that Check throws ArgumentException and not a subclass
        private static void Fails(Action action, string name) =>
            Assert.StartsWith(name + " must ", Assert.Throws<ArgumentException>(action).Message);

        [Fact]
        public void Booleans()
        {
            Check.True(true);
            Check.False(false);

            ArgumentException e = Assert.Throws<ArgumentException>(() => Check.True(false, "some message"));
            Assert.Equal("some message", e.Message);
            Assert.Throws<ArgumentException>(() => Check.False(true));
        }

        [Fact]
        public void Nulls()
        {
            Check.NotNull(new object());
            Check.NotNull((int?)1);

            Fails(() => Check.NotNull((object?)null, "obj"), "obj");
            Fails(() => Check.NotNull((int?)null, "val"), "val");
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonFiniteFailsEveryFiniteCheck(double d)
        {
            Fails(() => Check.Finite(d, "x"), "x");
            Fails(() => Check.PositiveFinite(d, "x"), "x");
            Fails(() => Check.NonNegativeFinite(d, "x"), "x");
            Fails(() => Check.NegativeFinite(d, "x"), "x");
            Fails(() => Check.NonPositiveFinite(d, "x"), "x");
            Fails(() => Check.NonZeroFinite(d, "x"), "x");
        }

        [Fact]
        public void NaNFailsEverySignCheck()
        {
            Fails(() => Check.Positive(double.NaN, "x"), "x");
            Fails(() => Check.NonNegative(double.NaN, "x"), "x");
            Fails(() => Check.Negative(double.NaN, "x"), "x");
            Fails(() => Check.NonPositive(double.NaN, "x"), "x");
            Fails(() => Check.Zero(double.NaN, "x"), "x");
        }

        [Fact]
        public void Signs()
        {
            Check.Finite(-1.0);
            Check.Positive(double.PositiveInfinity);
            Check.PositiveFinite(1.0);
            Check.NonNegative(0.0);
            Check.NonNegativeFinite(0.0);
            Check.Negative(double.NegativeInfinity);
            Check.NegativeFinite(-1.0);
            Check.NonPositive(0.0);
            Check.NonPositiveFinite(0.0);
            Check.Zero(0.0);
            Check.NonZero(double.NaN);
            Check.NonZeroFinite(-1.0);

            Fails(() => Check.Positive(0, "x"), "x");
            Fails(() => Check.PositiveFinite(0, "x"), "x");
            Fails(() => Check.NonNegative(-1, "x"), "x");
            Fails(() => Check.NonNegativeFinite(-1, "x"), "x");
            Fails(() => Check.Negative(0, "x"), "x");
            Fails(() => Check.NegativeFinite(0, "x"), "x");
            Fails(() => Check.NonPositive(1, "x"), "x");
            Fails(() => Check.NonPositiveFinite(1, "x"), "x");
            Fails(() => Check.Zero(1, "x"), "x");
            Fails(() => Check.NonZero(0, "x"), "x");
            Fails(() => Check.NonZeroFinite(0, "x"), "x");
        }

        [Fact]
        public void MessageIncludesValue()
        {
            // single line, since this is what the PSG shows the user as LastFailureMessage
            Assert.Equal("vT must be positive and finite, got NaN", Assert.Throws<ArgumentException>(() => Check.PositiveFinite(double.NaN, "vT")).Message);
            Assert.Equal("argument must be positive, got -1", Assert.Throws<ArgumentException>(() => Check.Positive(-1)).Message);
            Assert.Equal("r must be non-zero, got [0, 0, 0]", Assert.Throws<ArgumentException>(() => Check.NonZero(V3.zero, "r")).Message);
        }

        [Fact]
        public void Vectors()
        {
            Check.Finite(V3.zero);
            Check.NonZero(new V3(double.NaN, 0, 0));
            Check.NonZeroFinite(new V3(1, 0, 0));

            Fails(() => Check.Finite(new V3(0, double.NaN, 0), "r"), "r");
            Fails(() => Check.NonZero(V3.zero, "r"), "r");
            Fails(() => Check.NonZeroFinite(V3.zero, "r"), "r");
            Fails(() => Check.NonZeroFinite(new V3(0, 0, double.PositiveInfinity), "r"), "r");
        }

        [Fact]
        public void Arrays()
        {
            Check.CanContain(new double[3], 3);
            Check.CanContain(new List<double> { 1, 2, 3 }, 3);

            Fails(() => Check.CanContain(new double[2], 3, "a"), "a");
            Fails(() => Check.CanContain(new List<double> { 1, 2 }, 3, "a"), "a");
        }
    }
}
