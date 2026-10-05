/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Primitives.M2Tests
{
    public class EqualityHashingTests
    {
        private static readonly M2 _a = new M2(
            1, 2,
            3, 4);

        private static readonly M2 _b = new M2(
            2, 0,
            1, 3);

        private static double[] RowMajor(M2 m)
        {
            double[] array = new double[4];
            m.CopyTo(array, 0);
            return array;
        }

        [Fact]
        private void EqualityOperatorTest()
        {
            M2 copy = M2.CopyFrom(RowMajor(_a), 0);

            Assert.True(_a == copy);
            Assert.False(_a != copy);
            Assert.False(_a == _b);
            Assert.True(_a != _b);
        }

        [Fact]
        private void EveryElementParticipatesInEqualityTest()
        {
            for (int k = 0; k < 4; k++)
            {
                double[] array = RowMajor(_a);
                array[k] += 1;
                M2 other = M2.CopyFrom(array, 0);

                Assert.False(_a == other);
                Assert.True(_a != other);
                Assert.False(_a.Equals(other));
            }
        }

        [Fact]
        private void NaNEqualityTest()
        {
            M2 m = M2.identity + new M2(0, double.NaN, 0, 0);
            M2 copy = M2.CopyFrom(RowMajor(m), 0);

            // strict operators follow IEEE-754
            Assert.False(m == copy);
            Assert.True(m != copy);

            // Equals follows double.Equals
            Assert.True(m.Equals(copy));
            Assert.True(m.Equals((object)copy));
            Assert.Equal(m.GetHashCode(), copy.GetHashCode());
        }

        [Fact]
        private void EqualsObjectTest()
        {
            Assert.True(_a.Equals((object)_a));
            Assert.False(_a.Equals((object)_b));
            Assert.False(_a.Equals(null));
            Assert.False(M2.identity.Equals((object)M3.identity));
            Assert.False(_a.Equals("not a matrix"));
        }

        [Fact]
        private void HashCodeTest()
        {
            M2 copy = M2.CopyFrom(RowMajor(_a), 0);

            Assert.Equal(_a.GetHashCode(), copy.GetHashCode());
            Assert.NotEqual(_a.GetHashCode(), _b.GetHashCode());
        }

        [Fact]
        private void NearlyEqualTest()
        {
            Assert.True(NearlyEqual(_a, _a * (1 + 1e-10), 1e-9));
            Assert.False(NearlyEqual(_a, _a * (1 + 1e-8), 1e-9));

            // tolerance scales with the largest absolute element, so all-negative matrices compare the same way
            Assert.True(NearlyEqual(-_a, -_a * (1 + 1e-10), 1e-9));
            Assert.False(NearlyEqual(-_a, -_a * (1 + 1e-8), 1e-9));
        }

        [Fact]
        private void NearlyEqualZeroElementTest()
        {
            // zero elements use epsilon as an absolute tolerance
            var small = new M2(0, 0, 0, 1e-12);
            Assert.True(NearlyEqual(M2.identity, M2.identity + small, 1e-9));
            Assert.False(NearlyEqual(M2.zero, small * 1e6, 1e-9));
        }

        [Fact]
        private void ShouldEqualTest()
        {
            _a.ShouldEqual(_a * (1 + 1e-10), 1e-9);
            Assert.ThrowsAny<System.Exception>(() => _a.ShouldEqual(_b));
        }
    }
}
