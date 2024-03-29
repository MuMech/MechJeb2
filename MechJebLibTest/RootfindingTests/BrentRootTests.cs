﻿/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using MechJebLib.Rootfinding;
using MechJebLib.Utils;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.RootfindingTests
{
    public class BrentRootTests
    {
        [Theory]
        [InlineData(2.0)]
        [InlineData(2.1)]
        [InlineData(2.2)]
        [InlineData(2.3)]
        [InlineData(2.4)]
        [InlineData(2.5)]
        [InlineData(2.6)]
        [InlineData(2.7)]
        [InlineData(2.8)]
        [InlineData(2.9)]
        public void Test(double a0)
        {
            double ans = BrentRoot.Solve((t, o) => t * t * t - a0, 0.0, 2.0, null);
            ans.ShouldEqual(Pow(a0, 1.0 / 3.0), 10 * EPS);
        }

        [Fact]
        public void TestBracket()
        {
            // ensure we can find solution at xmin
            double ans = BrentRoot.Solve((t, o) => t * t * t, 0.0, 2.0, null);
            ans.ShouldBeZero();

            // ensure we can find solution at xmax
            ans = BrentRoot.Solve((t, o) => t * t * t, -2.0, 0.0, null);
            ans.ShouldBeZero();

            // this should throw
            Exception ex = Assert.Throws<ArgumentException>(() =>
                BrentRoot.Solve((t, o) => t * t * t, -2.0, -0.0001, null)
            );
            Assert.Contains("guess does not bracket the root", ex.Message);

            // this should throw
            ex = Assert.Throws<ArgumentException>(() =>
                BrentRoot.Solve((t, o) => t * t * t, 0.0001, 2, null)
            );
            Assert.Contains("guess does not bracket the root", ex.Message);
        }

        [Fact]
        public void TestGuess()
        {
            // this works normally
            double ans = BrentRoot.Solve((t, o) => Sin(t), 3, null);
            ans.ShouldEqual(PI, EPS2);

            // this throws due to not being able to precisely find the root
            Exception ex = Assert.Throws<Check.FailedCheck>(() =>
                BrentRoot.Solve((t, o) => t * t, 1, null)
            );
            Assert.Contains("check failed", ex.Message);

            // no root at all
            ex = Assert.Throws<Check.FailedCheck>(() =>
                BrentRoot.Solve((t, o) => Abs(t) + 1, 1, null)
            );
            Assert.Contains("check failed", ex.Message);

            // expansion of region fails to bracket the root
            ex = Assert.Throws<Check.FailedCheck>(() =>
                BrentRoot.Solve((t, o) => t * t - 0.01, 2, null)
            );
            Assert.Contains("check failed", ex.Message);

            // finds a root even though it shouldn't
            ans = BrentRoot.Solve((t, o) => Tan(t), 2, null);
            ans.ShouldEqual(PI / 2, 21 * EPS);
        }
    }
}
