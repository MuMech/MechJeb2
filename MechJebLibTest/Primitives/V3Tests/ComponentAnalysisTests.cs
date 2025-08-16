/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class ComponentAnalysisTests
    {
        [Fact]
        private void MaxMagnitudePositiveComponents()
        {
            new V3(1, 2, 3).max_magnitude.ShouldEqual(3.0);
            new V3(5, 4, 3).max_magnitude.ShouldEqual(5.0);
            new V3(2, 7, 3).max_magnitude.ShouldEqual(7.0);
            new V3(1, 1, 1).max_magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void MaxMagnitudeNegativeComponents()
        {
            new V3(-1, -2, -3).max_magnitude.ShouldEqual(3.0);
            new V3(-5, -4, -3).max_magnitude.ShouldEqual(5.0);
            new V3(-2, -7, -3).max_magnitude.ShouldEqual(7.0);
            new V3(-10, 5, 3).max_magnitude.ShouldEqual(10.0);
        }

        [Fact]
        private void MaxMagnitudeMixedComponents()
        {
            new V3(1, -5, 3).max_magnitude.ShouldEqual(5.0);
            new V3(-8, 2, 6).max_magnitude.ShouldEqual(8.0);
            new V3(4, 3, -9).max_magnitude.ShouldEqual(9.0);
            new V3(-1, -1, 1).max_magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void MaxMagnitudeWithZeros()
        {
            new V3(0, 0, 5).max_magnitude.ShouldEqual(5.0);
            new V3(3, 0, 0).max_magnitude.ShouldEqual(3.0);
            new V3(0, -7, 0).max_magnitude.ShouldEqual(7.0);
            new V3(0, 0, 0).max_magnitude.ShouldEqual(0.0);
        }

        [Fact]
        private void MinMagnitudePositiveComponents()
        {
            new V3(1, 2, 3).min_magnitude.ShouldEqual(1.0);
            new V3(5, 4, 3).min_magnitude.ShouldEqual(3.0);
            new V3(2, 7, 3).min_magnitude.ShouldEqual(2.0);
            new V3(8, 8, 8).min_magnitude.ShouldEqual(8.0);
        }

        [Fact]
        private void MinMagnitudeNegativeComponents()
        {
            new V3(-1, -2, -3).min_magnitude.ShouldEqual(1.0);
            new V3(-5, -4, -3).min_magnitude.ShouldEqual(3.0);
            new V3(-2, -7, -3).min_magnitude.ShouldEqual(2.0);
            new V3(-10, -5, 3).min_magnitude.ShouldEqual(3.0);
        }

        [Fact]
        private void MinMagnitudeMixedComponents()
        {
            new V3(1, -5, 3).min_magnitude.ShouldEqual(1.0);
            new V3(-8, 2, 6).min_magnitude.ShouldEqual(2.0);
            new V3(4, 3, -9).min_magnitude.ShouldEqual(3.0);
            new V3(-7, -7, 7).min_magnitude.ShouldEqual(7.0);
        }

        [Fact]
        private void MinMagnitudeWithZeros()
        {
            new V3(0, 0, 5).min_magnitude.ShouldEqual(0.0);
            new V3(3, 0, 0).min_magnitude.ShouldEqual(0.0);
            new V3(0, -7, 0).min_magnitude.ShouldEqual(0.0);
            new V3(0, 0, 0).min_magnitude.ShouldEqual(0.0);
            new V3(0, 1, 2).min_magnitude.ShouldEqual(0.0);
        }

        [Fact]
        private void MaxMagnitudeIndexPositiveComponents()
        {
            new V3(1, 2, 3).max_magnitude_index.ShouldEqual(2);
            new V3(5, 4, 3).max_magnitude_index.ShouldEqual(0);
            new V3(2, 7, 3).max_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void MaxMagnitudeIndexNegativeComponents()
        {
            new V3(-1, -2, -3).max_magnitude_index.ShouldEqual(2);
            new V3(-5, -4, -3).max_magnitude_index.ShouldEqual(0);
            new V3(-2, -7, -3).max_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void MaxMagnitudeIndexMixedComponents()
        {
            new V3(1, -5, 3).max_magnitude_index.ShouldEqual(1);
            new V3(-8, 2, 6).max_magnitude_index.ShouldEqual(0);
            new V3(4, 3, -9).max_magnitude_index.ShouldEqual(2);
        }

        [Fact]
        private void MaxMagnitudeIndexTieBreaking()
        {
            new V3(5, 5, 3).max_magnitude_index.ShouldEqual(0);
            new V3(3, 5, 5).max_magnitude_index.ShouldEqual(1);
            new V3(5, 3, 5).max_magnitude_index.ShouldEqual(0);
            new V3(-5, 5, 3).max_magnitude_index.ShouldEqual(0);
            new V3(3, -5, 5).max_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void MinMagnitudeIndexPositiveComponents()
        {
            new V3(1, 2, 3).min_magnitude_index.ShouldEqual(0);
            new V3(5, 4, 3).min_magnitude_index.ShouldEqual(2);
            new V3(2, 7, 3).min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void MinMagnitudeIndexNegativeComponents()
        {
            new V3(-1, -2, -3).min_magnitude_index.ShouldEqual(0);
            new V3(-5, -4, -3).min_magnitude_index.ShouldEqual(2);
            new V3(-2, -7, -3).min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void MinMagnitudeIndexMixedComponents()
        {
            new V3(1, -5, 3).min_magnitude_index.ShouldEqual(0);
            new V3(-8, 2, 6).min_magnitude_index.ShouldEqual(1);
            new V3(4, 3, -9).min_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void MinMagnitudeIndexWithZeros()
        {
            new V3(0, 2, 3).min_magnitude_index.ShouldEqual(0);
            new V3(1, 0, 3).min_magnitude_index.ShouldEqual(1);
            new V3(1, 2, 0).min_magnitude_index.ShouldEqual(2);
            new V3(0, 0, 3).min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void MinMagnitudeIndexTieBreaking()
        {
            new V3(3, 3, 5).min_magnitude_index.ShouldEqual(0);
            new V3(5, 3, 3).min_magnitude_index.ShouldEqual(1);
            new V3(3, 5, 3).min_magnitude_index.ShouldEqual(0);
            new V3(-3, 3, 5).min_magnitude_index.ShouldEqual(0);
            new V3(5, -3, 3).min_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void ComponentAnalysisAllEqual()
        {
            var v = new V3(4, 4, 4);

            v.max_magnitude.ShouldEqual(4.0);
            v.min_magnitude.ShouldEqual(4.0);
            v.max_magnitude_index.ShouldEqual(0);
            v.min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void ComponentAnalysisAllEqualNegative()
        {
            var v = new V3(-7, -7, -7);

            v.max_magnitude.ShouldEqual(7.0);
            v.min_magnitude.ShouldEqual(7.0);
            v.max_magnitude_index.ShouldEqual(0);
            v.min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void ComponentAnalysisLargeValues()
        {
            var v = new V3(1e100, 1e200, 1e150);

            v.max_magnitude.ShouldEqual(1e200);
            v.min_magnitude.ShouldEqual(1e100);
            v.max_magnitude_index.ShouldEqual(1);
            v.min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void ComponentAnalysisSmallValues()
        {
            var v = new V3(1e-100, 1e-200, 1e-150);

            v.max_magnitude.ShouldEqual(1e-100);
            v.min_magnitude.ShouldEqual(1e-200);
            v.max_magnitude_index.ShouldEqual(0);
            v.min_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void ComponentAnalysisInfinityValues()
        {
            var v = new V3(double.PositiveInfinity, 1, 2);

            v.max_magnitude.ShouldEqual(double.PositiveInfinity);
            v.min_magnitude.ShouldEqual(1.0);

            var v2 = new V3(double.NegativeInfinity, 1, 2);

            v2.max_magnitude.ShouldEqual(double.PositiveInfinity);
            v2.min_magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void ComponentAnalysisNaNValues()
        {
            var v = new V3(double.NaN, 1, 2);

            Assert.True(double.IsNaN(v.max_magnitude));
            Assert.True(double.IsNaN(v.min_magnitude));
        }

        [Fact]
        private void ComponentAnalysisUnitVectors()
        {
            V3.xaxis.max_magnitude.ShouldEqual(1.0);
            V3.xaxis.min_magnitude.ShouldEqual(0.0);
            V3.xaxis.max_magnitude_index.ShouldEqual(0);
            V3.xaxis.min_magnitude_index.ShouldEqual(1);

            V3.yaxis.max_magnitude_index.ShouldEqual(1);
            V3.yaxis.min_magnitude_index.ShouldEqual(0);

            V3.zaxis.max_magnitude_index.ShouldEqual(2);
            V3.zaxis.min_magnitude_index.ShouldEqual(0);
        }
    }
}
