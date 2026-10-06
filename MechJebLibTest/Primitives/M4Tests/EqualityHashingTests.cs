/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class EqualityHashingTests
    {
        private static readonly M4 _a = new M4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        private static readonly M4 _b = new M4(
            2, 0, 1, 3,
            1, 4, 0, 2,
            0, 1, 3, 1,
            5, 2, 1, 0);

        private static double[] RowMajor(M4 m)
        {
            double[] array = new double[16];
            m.CopyTo(array, 0);
            return array;
        }

        [Fact]
        private void EqualityOperatorTest()
        {
            M4 copy = M4.CopyFrom(RowMajor(_a), 0);

            Assert.True(_a == copy);
            Assert.False(_a != copy);
            Assert.False(_a == _b);
            Assert.True(_a != _b);
        }

        [Fact]
        private void EveryElementParticipatesInEqualityTest()
        {
            for (int k = 0; k < 16; k++)
            {
                double[] array = RowMajor(_a);
                array[k] += 1;
                M4 other = M4.CopyFrom(array, 0);

                Assert.False(_a == other);
                Assert.True(_a != other);
                Assert.False(_a.Equals(other));
            }
        }

        [Fact]
        private void NaNEqualityTest()
        {
            M4 m = M4.identity + new M4(0, 0, 0, 0, 0, 0, double.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            M4 copy = M4.CopyFrom(RowMajor(m), 0);

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
            Assert.False(M4.identity.Equals((object)M3.identity));
            Assert.False(_a.Equals("not a matrix"));
        }

        [Fact]
        private void HashCodeTest()
        {
            M4 copy = M4.CopyFrom(RowMajor(_a), 0);

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
            M4 small = new M4(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1e-12);
            Assert.True(NearlyEqual(M4.identity, M4.identity + small, 1e-9));
            Assert.False(NearlyEqual(M4.zero, small * 1e6, 1e-9));
        }

        [Fact]
        private void NearlyEqualNonFiniteTest()
        {
            // a NaN or Inf element makes the relative tolerance non-finite, which used to make every element compare equal
            M4 nan = new M4(double.NaN, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            M4 inf = new M4(double.PositiveInfinity, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            Assert.False(NearlyEqual(_a, _a + nan));
            Assert.False(NearlyEqual(_a + nan, _a * 2));
            Assert.False(NearlyEqual(_a + inf, _a * 2));
            Assert.True(NearlyEqual(_a + inf, _a + inf));
        }

        [Fact]
        private void ShouldEqualTest()
        {
            _a.ShouldEqual(_a * (1 + 1e-10), 1e-9);
            Assert.ThrowsAny<System.Exception>(() => _a.ShouldEqual(_b));
        }
    }
}
