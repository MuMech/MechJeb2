/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class EqualityHashingTests
    {
        [Fact]
        private void EqualsWithIdenticalVectors()
        {
            var v1 = new V3(1.5, 2.7, -3.2);
            var v2 = new V3(1.5, 2.7, -3.2);

            v1.Equals(v2).ShouldBeTrue();
            v2.Equals(v1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithDifferentVectors()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(1, 2, 4);

            v1.Equals(v2).ShouldBeFalse();
            v2.Equals(v1).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithSelf()
        {
            var v = new V3(1.5, 2.7, -3.2);

            v.Equals(v).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithZeroVectors()
        {
            var v1 = new V3(0, 0, 0);
            V3  v2 = V3.zero;

            v1.Equals(v2).ShouldBeTrue();
            v2.Equals(v1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithNaN()
        {
            var v1 = new V3(double.NaN, 2, 3);
            var v2 = new V3(double.NaN, 2, 3);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithMixedNaN()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(1, 2, double.NaN);

            v1.Equals(v2).ShouldBeFalse();
            v2.Equals(v1).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithInfinity()
        {
            var v1 = new V3(double.PositiveInfinity, 2, 3);
            var v2 = new V3(double.PositiveInfinity, 2, 3);
            var v3 = new V3(double.NegativeInfinity, 2, 3);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v3).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithVerySmallDifferences()
        {
            var v1 = new V3(1.0, 2.0, 3.0);
            var v2 = new V3(1.0 + EPS, 2.0, 3.0);

            v1.Equals(v2).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithV3()
        {
            var    v1 = new V3(1, 2, 3);
            object v2 = new V3(1, 2, 3);

            v1.Equals(v2).ShouldBeTrue();
        }

        [Fact]
        private void EqualsObjectOverloadWithDifferentV3()
        {
            var    v1 = new V3(1, 2, 3);
            object v2 = new V3(4, 5, 6);

            v1.Equals(v2).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithNull()
        {
            var v = new V3(1, 2, 3);

            v.Equals(null).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithWrongType()
        {
            var    v          = new V3(1, 2, 3);
            object notAVector = "not a vector";

            v.Equals(notAVector).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithNumber()
        {
            var    v      = new V3(1, 2, 3);
            object number = 42.0;

            v.Equals(number).ShouldBeFalse();
        }

        [Fact]
        private void GetHashCodeIdenticalVectors()
        {
            var v1 = new V3(1.5, 2.7, -3.2);
            var v2 = new V3(1.5, 2.7, -3.2);

            v1.GetHashCode().ShouldEqual(v2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeDifferentVectors()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(3, 2, 1);

            Assert.NotEqual(v1.GetHashCode(), v2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeConsistency()
        {
            var v     = new V3(1.5, 2.7, -3.2);
            int hash1 = v.GetHashCode();
            int hash2 = v.GetHashCode();

            hash1.ShouldEqual(hash2);
        }

        [Fact]
        private void GetHashCodeZeroVector()
        {
            var v1 = new V3(0, 0, 0);
            V3  v2 = V3.zero;

            v1.GetHashCode().ShouldEqual(v2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeWithNaN()
        {
            var v1 = new V3(double.NaN, 2, 3);
            var v2 = new V3(double.NaN, 2, 3);

            v1.GetHashCode().ShouldEqual(v2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeWithInfinity()
        {
            var v1 = new V3(double.PositiveInfinity, 2, 3);
            var v2 = new V3(double.PositiveInfinity, 2, 3);
            var v3 = new V3(double.NegativeInfinity, 2, 3);

            v1.GetHashCode().ShouldEqual(v2.GetHashCode());
            Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
        }

        [Fact]
        private void GetHashCodeDistribution()
        {
            var hashes = new HashSet<int>
            {
                new V3(0, 0, 0).GetHashCode(),
                new V3(1, 0, 0).GetHashCode(),
                new V3(0, 1, 0).GetHashCode(),
                new V3(0, 0, 1).GetHashCode(),
                new V3(1, 1, 1).GetHashCode(),
                new V3(-1, -1, -1).GetHashCode(),
                new V3(1.5, 2.5, 3.5).GetHashCode()
            };

            Assert.True(hashes.Count >= 6);
        }

        [Fact]
        private void HashCodeAndEqualityContract()
        {
            var v1 = new V3(1.5, 2.7, -3.2);
            var v2 = new V3(1.5, 2.7, -3.2);
            var v3 = new V3(1.5, 2.7, -3.1);

            if (v1.Equals(v2))
                v1.GetHashCode().ShouldEqual(v2.GetHashCode());

            if (!v1.Equals(v3))
                Assert.True(true);
        }

        [Fact]
        private void EqualsTransitivity()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(1, 2, 3);
            var v3 = new V3(1, 2, 3);

            v1.Equals(v2).ShouldBeTrue();
            v2.Equals(v3).ShouldBeTrue();
            v1.Equals(v3).ShouldBeTrue();
        }

        [Fact]
        private void EqualsSymmetry()
        {
            var v1 = new V3(1.5, 2.7, -3.2);
            var v2 = new V3(1.5, 2.7, -3.2);

            (v1.Equals(v2) == v2.Equals(v1)).ShouldBeTrue();

            var v3 = new V3(1, 2, 3);
            var v4 = new V3(4, 5, 6);

            (v3.Equals(v4) == v4.Equals(v3)).ShouldBeTrue();
        }

        [Fact]
        private void GetHashCodeStabilityAfterModification()
        {
            var v            = new V3(1, 2, 3);
            int originalHash = v.GetHashCode();

            v.x = 4;
            int modifiedHash = v.GetHashCode();

            Assert.NotEqual(originalHash, modifiedHash);
        }

        [Fact]
        private void EqualsWithLargeValues()
        {
            var v1 = new V3(1e100, 1e200, 1e150);
            var v2 = new V3(1e100, 1e200, 1e150);
            var v3 = new V3(1e100, 1e200, 1e151);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v3).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithSmallValues()
        {
            var v1 = new V3(1e-100, 1e-200, 1e-150);
            var v2 = new V3(1e-100, 1e-200, 1e-150);
            var v3 = new V3(1e-100, 1e-200, 1e-151);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v3).ShouldBeFalse();
        }

        [Fact]
        private void GetHashCodeWithSpecialValues()
        {
            V3[] vectors = { V3.zero, V3.one, V3.positiveinfinity, V3.negativeinfinity, V3.nan, V3.xaxis, V3.yaxis, V3.zaxis };

            foreach (V3 v in vectors)
            {
                int hash1 = v.GetHashCode();
                int hash2 = v.GetHashCode();
                hash1.ShouldEqual(hash2);
            }
        }

        [Fact]
        private void EqualsPerformanceCharacteristics()
        {
            var v1 = new V3(1.123456789012345, 2.234567890123456, 3.345678901234567);
            var v2 = new V3(1.123456789012345, 2.234567890123456, 3.345678901234567);
            var v3 = new V3(1.123456789012346, 2.234567890123456, 3.345678901234567);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v3).ShouldBeFalse();
        }

        [Fact]
        private void DictionaryUsage()
        {
            var dict = new Dictionary<V3, string>();
            var v1   = new V3(1, 2, 3);
            var v2   = new V3(1, 2, 3);
            var v3   = new V3(4, 5, 6);

            dict[v1] = "first";
            dict[v3] = "second";

            dict.ContainsKey(v2).ShouldBeTrue();
            dict[v2].ShouldEqual("first");
            dict.ContainsKey(v3).ShouldBeTrue();
        }

        [Fact]
        private void HashSetUsage()
        {
            var set = new HashSet<V3>();
            var v1  = new V3(1, 2, 3);
            var v2  = new V3(1, 2, 3);
            var v3  = new V3(4, 5, 6);

            set.Add(v1).ShouldBeTrue();
            set.Add(v2).ShouldBeFalse();
            set.Add(v3).ShouldBeTrue();

            set.Count.ShouldEqual(2);
            set.Contains(v2).ShouldBeTrue();
        }
    }
}
