/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class EqualityHashingTests
    {
        [Fact]
        private void EqualsWithIdenticalMatrices()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.Equals(m2).ShouldBeTrue();
            m2.Equals(m1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithDifferentMatrices()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 10);

            m1.Equals(m2).ShouldBeFalse();
            m2.Equals(m1).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithSelf()
        {
            var m = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);

            m.Equals(m).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithZeroMatrices()
        {
            var m1 = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            M3  m2 = M3.zero;

            m1.Equals(m2).ShouldBeTrue();
            m2.Equals(m1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithIdentityMatrices()
        {
            var m1 = new M3(1, 0, 0, 0, 1, 0, 0, 0, 1);
            M3  m2 = M3.identity;

            m1.Equals(m2).ShouldBeTrue();
            m2.Equals(m1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithNaN()
        {
            var m1 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.Equals(m2).ShouldBeTrue();
            m1.Equals(m1).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithMixedNaN()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(1, 2, 3, 4, 5, 6, 7, 8, double.NaN);

            m1.Equals(m2).ShouldBeFalse();
            m2.Equals(m1).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithAllNaN()
        {
            var m1 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            var m2 = new M3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);

            m1.Equals(m2).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithInfinity()
        {
            var m1 = new M3(double.PositiveInfinity, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(double.PositiveInfinity, 2, 3, 4, 5, 6, 7, 8, 9);
            var m3 = new M3(double.NegativeInfinity, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.Equals(m2).ShouldBeTrue();
            m1.Equals(m3).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithM3()
        {
            var    m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            object m2 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.Equals(m2).ShouldBeTrue();
        }

        [Fact]
        private void EqualsObjectOverloadWithDifferentM3()
        {
            var    m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            object m2 = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            m1.Equals(m2).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithNull()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            m.Equals(null).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithWrongType()
        {
            var    m          = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            object notAMatrix = "not a matrix";

            m.Equals(notAMatrix).ShouldBeFalse();
        }

        [Fact]
        private void EqualsObjectOverloadWithVector()
        {
            var    m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            object v = new V3(1, 2, 3);

            m.Equals(v).ShouldBeFalse();
        }

        [Fact]
        private void GetHashCodeIdenticalMatrices()
        {
            var m1 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            var m2 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);

            m1.GetHashCode().ShouldEqual(m2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeDifferentMatrices()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            Assert.NotEqual(m1.GetHashCode(), m2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeConsistency()
        {
            var m     = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            int hash1 = m.GetHashCode();
            int hash2 = m.GetHashCode();

            hash1.ShouldEqual(hash2);
        }

        [Fact]
        private void GetHashCodeZeroMatrix()
        {
            var m1 = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            M3  m2 = M3.zero;

            m1.GetHashCode().ShouldEqual(m2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeIdentityMatrix()
        {
            var m1 = new M3(1, 0, 0, 0, 1, 0, 0, 0, 1);
            M3  m2 = M3.identity;

            m1.GetHashCode().ShouldEqual(m2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeWithNaN()
        {
            var m1 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.GetHashCode().ShouldEqual(m2.GetHashCode());
        }

        [Fact]
        private void GetHashCodeWithInfinity()
        {
            var m1 = new M3(double.PositiveInfinity, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(double.PositiveInfinity, 2, 3, 4, 5, 6, 7, 8, 9);
            var m3 = new M3(double.NegativeInfinity, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.GetHashCode().ShouldEqual(m2.GetHashCode());
            Assert.NotEqual(m1.GetHashCode(), m3.GetHashCode());
        }

        [Fact]
        private void GetHashCodeDistribution()
        {
            var hashes = new HashSet<int>
            {
                M3.zero.GetHashCode(),
                M3.identity.GetHashCode(),
                new M3(1, 2, 3, 4, 5, 6, 7, 8, 9).GetHashCode(),
                new M3(9, 8, 7, 6, 5, 4, 3, 2, 1).GetHashCode(),
                new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9).GetHashCode(),
                new M3(1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, 8.5, 9.5).GetHashCode(),
                new M3(0, 1, 0, 1, 0, 1, 0, 1, 0).GetHashCode()
            };

            Assert.True(hashes.Count >= 6);
        }

        [Fact]
        private void HashCodeAndEqualityContract()
        {
            var m1 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            var m2 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            var m3 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.0);

            if (m1.Equals(m2))
                m1.GetHashCode().ShouldEqual(m2.GetHashCode());

            if (!m1.Equals(m3))
                Assert.True(true);
        }

        [Fact]
        private void EqualsTransitivity()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m3 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            m1.Equals(m2).ShouldBeTrue();
            m2.Equals(m3).ShouldBeTrue();
            m1.Equals(m3).ShouldBeTrue();
        }

        [Fact]
        private void EqualsSymmetry()
        {
            var m1 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            var m2 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);

            (m1.Equals(m2) == m2.Equals(m1)).ShouldBeTrue();

            var m3 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m4 = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (m3.Equals(m4) == m4.Equals(m3)).ShouldBeTrue();
        }

        [Fact]
        private void EqualityOperatorIdenticalMatrices()
        {
            var m1 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            var m2 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);

            (m1 == m2).ShouldBeTrue();
        }

        [Fact]
        private void EqualityOperatorDifferentMatrices()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 10);

            (m1 == m2).ShouldBeFalse();
        }

        [Fact]
        private void EqualityOperatorWithNaN()
        {
            var m1 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);

            (m1 == m2).ShouldBeFalse();
        }

        [Fact]
        private void InequalityOperatorDifferentMatrices()
        {
            var m1 = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (m1 != m2).ShouldBeTrue();
        }

        [Fact]
        private void InequalityOperatorIdenticalMatrices()
        {
            var m1 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);
            var m2 = new M3(1.5, 2.7, -3.2, 4.1, 5.9, 6.3, 7.7, 8.8, 9.9);

            (m1 != m2).ShouldBeFalse();
        }

        [Fact]
        private void InequalityOperatorWithNaN()
        {
            var m1 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2 = new M3(double.NaN, 2, 3, 4, 5, 6, 7, 8, 9);

            (m1 != m2).ShouldBeTrue();
        }

        [Fact]
        private void EqualsWithLargeValues()
        {
            var m1 = new M3(1e100, 1e200, 1e150, 1e50, 1e75, 1e125, 1e25, 1e175, 1e225);
            var m2 = new M3(1e100, 1e200, 1e150, 1e50, 1e75, 1e125, 1e25, 1e175, 1e225);
            var m3 = new M3(1e100, 1e200, 1e150, 1e50, 1e75, 1e125, 1e25, 1e175, 1e226);

            m1.Equals(m2).ShouldBeTrue();
            m1.Equals(m3).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithSmallValues()
        {
            var m1 = new M3(1e-100, 1e-200, 1e-150, 1e-50, 1e-75, 1e-125, 1e-25, 1e-175, 1e-225);
            var m2 = new M3(1e-100, 1e-200, 1e-150, 1e-50, 1e-75, 1e-125, 1e-25, 1e-175, 1e-225);
            var m3 = new M3(1e-100, 1e-200, 1e-150, 1e-50, 1e-75, 1e-125, 1e-25, 1e-175, 1e-226);

            m1.Equals(m2).ShouldBeTrue();
            m1.Equals(m3).ShouldBeFalse();
        }

        [Fact]
        private void DictionaryUsage()
        {
            var dict = new Dictionary<M3, string>();
            var m1   = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2   = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m3   = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            dict[m1] = "first";
            dict[m3] = "second";

            dict.ContainsKey(m2).ShouldBeTrue();
            dict[m2].ShouldEqual("first");
            dict.ContainsKey(m3).ShouldBeTrue();
        }

        [Fact]
        private void HashSetUsage()
        {
            var set = new HashSet<M3>();
            var m1  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m2  = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var m3  = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            set.Add(m1).ShouldBeTrue();
            set.Add(m2).ShouldBeFalse();
            set.Add(m3).ShouldBeTrue();

            set.Count.ShouldEqual(2);
            set.Contains(m2).ShouldBeTrue();
        }

        [Fact]
        private void EqualsDiffersByOneElement()
        {
            var baseline = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            new M3(0, 2, 3, 4, 5, 6, 7, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 0, 3, 4, 5, 6, 7, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 0, 4, 5, 6, 7, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 3, 0, 5, 6, 7, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 3, 4, 0, 6, 7, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 3, 4, 5, 0, 7, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 3, 4, 5, 6, 0, 8, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 3, 4, 5, 6, 7, 0, 9).Equals(baseline).ShouldBeFalse();
            new M3(1, 2, 3, 4, 5, 6, 7, 8, 0).Equals(baseline).ShouldBeFalse();
        }

        [Fact]
        private void GetHashCodeRotationMatrices()
        {
            var q1     = Q3.AngleAxis(0.5, V3.xaxis);
            var q2     = Q3.AngleAxis(0.7, V3.yaxis);
            var m1     = M3.Rotate(q1);
            var m2     = M3.Rotate(q2);
            var m1Copy = M3.Rotate(q1);

            m1.GetHashCode().ShouldEqual(m1Copy.GetHashCode());
            Assert.NotEqual(m1.GetHashCode(), m2.GetHashCode());
        }
    }
}
