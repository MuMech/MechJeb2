using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives
{
    public class VecTests
    {
        [Theory]
        [InlineData(0, 1)] // length 0 → bucket 0 → capacity 1
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 4)]
        [InlineData(4, 4)]
        [InlineData(5, 8)]
        [InlineData(7, 8)]
        [InlineData(8, 8)]
        [InlineData(9, 16)]
        [InlineData(16, 16)]
        [InlineData(17, 32)]
        [InlineData(348, 512)] // ex1 z size: nxnu*(N-1) = 12*29
        [InlineData(1024, 1024)]
        [InlineData(1025, 2048)]
        public void Rent_GivesPowerOfTwoCapacity(int length, int expectedCapacity)
        {
            using var v = Vec.Rent(length);
            Assert.Equal(length, v.Length);
            Assert.Equal(expectedCapacity, v.Capacity);
        }

        [Fact]
        public void Rent_NegativeLength_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Vec.Rent(-1));

        [Fact]
        public void Indexer_GetSet_RoundTrips()
        {
            using var v = Vec.Rent(10);
            for (int i = 0; i < v.Length; i++) v[i] = i * 1.5;
            for (int i = 0; i < v.Length; i++) Assert.Equal(i * 1.5, v[i]);
        }

        [Fact]
        public void Data_PointsAtBackingArray()
        {
            using var v = Vec.Rent(5);
            v[2] = 7.0;
            Assert.Equal(7.0, v.Data[2]);
            v.Data[3] = 9.0;
            Assert.Equal(9.0, v[3]);
        }

        [Fact]
        public void DisposeThenRent_SameSize_ReusesBackingArray()
        {
            double[] firstData;
            using (var v = Vec.Rent(10))
            {
                firstData = v.Data;
            } // Dispose returns to pool

            using var second = Vec.Rent(10);
            Assert.Same(firstData, second.Data);
        }

        [Fact]
        public void DisposeThenRent_SameBucketDifferentLengths_ReusesBackingArray()
        {
            // 5 and 7 both round up to bucket 3 (capacity 8).
            double[] firstData;
            using (var v = Vec.Rent(5))
            {
                firstData = v.Data;
            }

            using var second = Vec.Rent(7);
            Assert.Same(firstData, second.Data);
            Assert.Equal(7, second.Length);
            Assert.Equal(8, second.Capacity);
        }

        [Fact]
        public void DisposeThenRent_DifferentBucket_AllocatesFresh()
        {
            double[] firstData;
            using (var v = Vec.Rent(5)) // bucket 3, cap 8
            {
                firstData = v.Data;
            }

            using var second = Vec.Rent(20); // bucket 5, cap 32
            Assert.NotSame(firstData, second.Data);
            Assert.Equal(32, second.Capacity);
        }

        [Fact]
        public void Rent_WithZeroFalse_LeavesLeftoverData()
        {
            // Write data, dispose, rent the same bucket without zeroing — old values still there.
            using (var v = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) v[i] = 99.0;
            }

            using var reused = Vec.Rent(8);
            for (int i = 0; i < 8; i++) Assert.Equal(99.0, reused[i]);
        }

        [Fact]
        public void Rent_WithZeroTrue_ClearsBuffer()
        {
            using (var v = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) v[i] = 99.0;
            }

            using var reused = Vec.Rent(8, true);
            for (int i = 0; i < 8; i++) Assert.Equal(0.0, reused[i]);
        }

        [Fact]
        public void Rent_WithZeroTrue_OnlyClearsLogicalRegion()
        {
            // Bucket 3 (cap 8) — request length 5, zero only [0..5).
            using (var v = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) v[i] = 99.0;
            }

            using var reused = Vec.Rent(5, true);
            for (int i = 0; i < 5; i++) Assert.Equal(0.0, reused[i]);
            // Slots 5..7 in the backing array: zero flag did NOT touch them.
            for (int i = 5; i < 8; i++) Assert.Equal(99.0, reused.Data[i]);
        }

        [Fact]
        public void Foreach_IteratesOnlyOverLogicalRegion()
        {
            using (var v = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) v[i] = 99.0;
            }

            using var smaller = Vec.Rent(3, true);
            int       count   = 0;
            double    sum     = 0.0;
            foreach (double x in smaller)
            {
                count++;
                sum += x;
            }

            Assert.Equal(3, count);
            Assert.Equal(0.0, sum);
        }

        [Fact]
        public void IList_Indexer_Works()
        {
            using var     v      = Vec.Rent(4);
            IList<double> asList = v;
            asList[0] = 1.0;
            asList[1] = 2.0;
            Assert.Equal(1.0, asList[0]);
            Assert.Equal(2.0, asList[1]);
            Assert.Equal(4, asList.Count);
            Assert.False(asList.IsReadOnly);
        }

        [Fact]
        public void IReadOnlyList_Works()
        {
            using var v = Vec.Rent(3);
            v[0] = 10.0;
            v[1] = 20.0;
            v[2] = 30.0;
            IReadOnlyList<double> ro = v;
            Assert.Equal(3, ro.Count);
            Assert.Equal(20.0, ro[1]);
        }

        [Fact]
        public void IList_MutationOps_Throw()
        {
            using var     v      = Vec.Rent(3);
            IList<double> asList = v;
            Assert.Throws<NotSupportedException>(() => asList.Add(1.0));
            Assert.Throws<NotSupportedException>(() => asList.Insert(0, 1.0));
            Assert.Throws<NotSupportedException>(() => asList.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => asList.Remove(1.0));
            Assert.Throws<NotSupportedException>(() => asList.Clear());
        }

        [Fact]
        public void IList_IndexOf_Contains_RespectLogicalLength()
        {
            using (var seed = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) seed[i] = 99.0; // pollute backing array
            }

            using var v = Vec.Rent(3, true);
            v[0] = 1.0;
            v[1] = 2.0;
            v[2] = 3.0;
            IList<double> asList = v;
            Assert.Equal(1, asList.IndexOf(2.0));
            Assert.Equal(-1, asList.IndexOf(99.0)); // 99.0 lives at index 5..7 — past Length
            Assert.True(asList.Contains(3.0));
            Assert.False(asList.Contains(99.0));
        }

        [Fact]
        public void IList_CopyTo_CopiesLogicalRegion()
        {
            using var v = Vec.Rent(4, true);
            v[0] = 1.0;
            v[1] = 2.0;
            v[2] = 3.0;
            v[3] = 4.0;

            double[] dest = new double[6];
            ((ICollection<double>)v).CopyTo(dest, 1);

            Assert.Equal(0.0, dest[0]);
            Assert.Equal(1.0, dest[1]);
            Assert.Equal(2.0, dest[2]);
            Assert.Equal(3.0, dest[3]);
            Assert.Equal(4.0, dest[4]);
            Assert.Equal(0.0, dest[5]);
        }

        [Fact]
        public void RepeatedRentDispose_AllocatesOnceForGivenBucket()
        {
            // First rent populates the pool. Subsequent rounds reuse the same backing.
            double[] expected;
            using (var first = Vec.Rent(10))
            {
                expected = first.Data;
            }

            for (int i = 0; i < 1000; i++)
            {
                using var v = Vec.Rent(10);
                Assert.Same(expected, v.Data);
            }
        }
    }
}
