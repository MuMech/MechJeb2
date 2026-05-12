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
        public void Count_ReturnsLogicalLength_NotCapacity()
        {
            // Rent 5 → bucket 3, Capacity 8. Count must reflect Length, not the bucket size.
            using var v = Vec.Rent(5);
            Assert.Equal(5, v.Count);
            Assert.Equal(8, v.Capacity);
        }

        [Fact]
        public void Count_MatchesIListCount()
        {
            using var v = Vec.Rent(3);
            Assert.Equal(((IList<double>)v).Count, v.Count);
        }

        // ---------- Dup ----------

        [Fact]
        public void Dup_ReturnsIndependentCopy()
        {
            using var src = Vec.Rent(4);
            src[0] = 1.0;
            src[1] = 2.0;
            src[2] = 3.0;
            src[3] = 4.0;

            using Vec copy = src.Dup();
            Assert.Equal(src.Length, copy.Length);
            for (int i = 0; i < src.Length; i++) Assert.Equal(src[i], copy[i]);

            // Mutating the copy must not affect the source.
            copy[0] = 99.0;
            Assert.Equal(1.0, src[0]);
        }

        [Fact]
        public void Dup_AllocatesFreshBackingArray()
        {
            using var src  = Vec.Rent(4);
            using Vec copy = src.Dup();
            Assert.NotSame(src.Data, copy.Data);
        }

        [Fact]
        public void Dup_RespectsLogicalLength_NotCapacity()
        {
            // Stale capacity bytes in src must not bleed into the copy's logical region.
            using (var prev = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) prev[i] = -99.0;
            }

            using var src = Vec.Rent(5, true);
            for (int i = 0; i < 5; i++) src[i] = i + 1;

            using Vec copy = src.Dup();
            Assert.Equal(5, copy.Length);
            for (int i = 0; i < 5; i++) Assert.Equal(i + 1, copy[i]);
        }

        // ---------- CopyFrom(Vec) ----------

        [Fact]
        public void CopyFrom_OverwritesLogicalRegion()
        {
            using var src = Vec.Rent(3);
            using var dst = Vec.Rent(3, true);
            src[0] = 1.0;
            src[1] = 2.0;
            src[2] = 3.0;

            Vec returned = dst.CopyFrom(src);
            Assert.Same(dst, returned);
            Assert.Equal(1.0, dst[0]);
            Assert.Equal(2.0, dst[1]);
            Assert.Equal(3.0, dst[2]);
        }

        [Fact]
        public void CopyFrom_UsesDestLength_NotCapacity()
        {
            // dst (length 5) on a bucket with Capacity 8. CopyFrom must only touch dst[0..5).
            using (var prev = Vec.Rent(8))
            {
                for (int i = 0; i < 8; i++) prev[i] = -99.0;
            }

            using var src = Vec.Rent(5, true);
            for (int i = 0; i < 5; i++) src[i] = i + 1;

            using var dst = Vec.Rent(5);
            for (int i = 5; i < dst.Capacity; i++) dst.Data[i] = 7.0;
            dst.CopyFrom(src);

            for (int i = 0; i < 5; i++) Assert.Equal(i + 1, dst[i]);
            for (int i = 5; i < dst.Capacity; i++) Assert.Equal(7.0, dst.Data[i]);
        }

        // ---------- Fluent return-this ----------

        [Fact]
        public void Fill_ReturnsSelf_ForChaining()
        {
            using var v        = Vec.Rent(3);
            Vec       returned = v.Fill(2.0);
            Assert.Same(v, returned);
            Assert.Equal(new[] { 2.0, 2.0, 2.0 }, new[] { v[0], v[1], v[2] });
        }

        [Fact]
        public void Scal_ReturnsSelf_ForChaining()
        {
            using var v = Vec.Rent(3);
            v[0] = 1.0;
            v[1] = 2.0;
            v[2] = 3.0;
            Vec returned = v.Scal(2.0);
            Assert.Same(v, returned);
            Assert.Equal(new[] { 2.0, 4.0, 6.0 }, new[] { v[0], v[1], v[2] });
        }

        // ---------- Mul ----------

        [Fact]
        public void Mul_HadamardInPlace()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 4.0;
            y[1] = 5.0;
            y[2] = 6.0;

            Vec returned = x.Mul(y);
            Assert.Same(x, returned);
            Assert.Equal(new[] { 4.0, 10.0, 18.0 }, new[] { x[0], x[1], x[2] });
            // Operand is untouched.
            Assert.Equal(new[] { 4.0, 5.0, 6.0 }, new[] { y[0], y[1], y[2] });
        }

        // ---------- Div ----------

        [Fact]
        public void Div_ElementwiseInPlace()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 10.0;
            x[1] = 20.0;
            x[2] = 30.0;
            y[0] = 2.0;
            y[1] = 4.0;
            y[2] = 5.0;

            Vec returned = x.Div(y);
            Assert.Same(x, returned);
            Assert.Equal(new[] { 5.0, 5.0, 6.0 }, new[] { x[0], x[1], x[2] });
            Assert.Equal(new[] { 2.0, 4.0, 5.0 }, new[] { y[0], y[1], y[2] });
        }

        // ---------- Add ----------

        [Fact]
        public void Add_ElementwiseInPlace()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;

            Vec returned = x.Add(y);
            Assert.Same(x, returned);
            Assert.Equal(new[] { 11.0, 22.0, 33.0 }, new[] { x[0], x[1], x[2] });
            Assert.Equal(new[] { 10.0, 20.0, 30.0 }, new[] { y[0], y[1], y[2] });
        }

        // ---------- Sub ----------

        [Fact]
        public void Sub_ElementwiseInPlace()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 10.0;
            x[1] = 20.0;
            x[2] = 30.0;
            y[0] = 1.0;
            y[1] = 2.0;
            y[2] = 3.0;

            Vec returned = x.Sub(y);
            Assert.Same(x, returned);
            Assert.Equal(new[] { 9.0, 18.0, 27.0 }, new[] { x[0], x[1], x[2] });
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, new[] { y[0], y[1], y[2] });
        }

        // ---------- Shift ----------

        [Fact]
        public void Shift_AddsScalarInPlace()
        {
            using var v = Vec.Rent(3);
            v[0] = 1.0;
            v[1] = 2.0;
            v[2] = 3.0;

            Vec returned = v.Shift(10.0);
            Assert.Same(v, returned);
            Assert.Equal(new[] { 11.0, 12.0, 13.0 }, new[] { v[0], v[1], v[2] });
        }

        // ---------- Abs ----------

        [Fact]
        public void Abs_InPlace()
        {
            using var v = Vec.Rent(4);
            v[0] = 1.0;
            v[1] = -2.0;
            v[2] = 3.0;
            v[3] = -4.0;

            Vec returned = v.Abs();
            Assert.Same(v, returned);
            Assert.Equal(new[] { 1.0, 2.0, 3.0, 4.0 }, new[] { v[0], v[1], v[2], v[3] });
        }

        // ---------- Fluent returns on remaining mutators ----------

        [Fact]
        public void MaxZero_ReturnsSelf()
        {
            using var v = Vec.Rent(3);
            v[0] = 1.0;
            v[1] = -2.0;
            v[2] = 3.0;
            Vec returned = v.MaxZero();
            Assert.Same(v, returned);
            Assert.Equal(new[] { 1.0, 0.0, 3.0 }, new[] { v[0], v[1], v[2] });
        }

        [Fact]
        public void Axpy_ReturnsSelf()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;
            Vec returned = y.Axpy(2.0, x);
            Assert.Same(y, returned);
            Assert.Equal(new[] { 12.0, 24.0, 36.0 }, new[] { y[0], y[1], y[2] });
        }

        [Fact]
        public void Axpby_ReturnsSelf()
        {
            using var x = Vec.Rent(3);
            using var y = Vec.Rent(3);
            x[0] = 1.0;
            x[1] = 2.0;
            x[2] = 3.0;
            y[0] = 10.0;
            y[1] = 20.0;
            y[2] = 30.0;
            Vec returned = y.Axpby(2.0, x, 3.0);
            Assert.Same(y, returned);
            Assert.Equal(new[] { 32.0, 64.0, 96.0 }, new[] { y[0], y[1], y[2] });
        }

        [Fact]
        public void Swap_ReturnsSelf()
        {
            using var x = Vec.Rent(2);
            using var y = Vec.Rent(2);
            x[0] = 1.0;
            x[1] = 2.0;
            y[0] = 10.0;
            y[1] = 20.0;
            Vec returned = x.Swap(y);
            Assert.Same(x, returned);
            Assert.Equal(new[] { 10.0, 20.0 }, new[] { x[0], x[1] });
            Assert.Equal(new[] { 1.0, 2.0 }, new[] { y[0], y[1] });
        }

        [Fact]
        public void LinComb1_ReturnsSelf()
        {
            using var y  = Vec.Rent(2, true);
            using var y0 = Vec.Rent(2);
            using var x1 = Vec.Rent(2);
            y0[0] = 1.0;
            y0[1] = 2.0;
            x1[0] = 10.0;
            x1[1] = 20.0;
            Vec returned = y.LinComb1(y0, 3.0, x1);
            Assert.Same(y, returned);
            // y = y0 + 3·x1 = (1+30, 2+60) = (31, 62)
            Assert.Equal(31.0, y[0]);
            Assert.Equal(62.0, y[1]);
        }

        [Fact]
        public void LinComb2_ReturnsSelf()
        {
            using var y  = Vec.Rent(2, true);
            using var y0 = Vec.Rent(2, true);
            using var x1 = Vec.Rent(2);
            using var x2 = Vec.Rent(2);
            x1[0] = 1.0;
            x1[1] = 1.0;
            x2[0] = 1.0;
            x2[1] = 1.0;
            Vec returned = y.LinComb2(y0, 2.0, x1, 3.0, x2);
            Assert.Same(y, returned);
            Assert.Equal(5.0, y[0]);
            Assert.Equal(5.0, y[1]);
        }

        [Fact]
        public void LinComb3_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = 1.0;
            Vec returned = y.LinComb3(y0, 1.0, x1, 2.0, x2, 3.0, x3);
            Assert.Same(y, returned);
            Assert.Equal(6.0, y[0]);
        }

        [Fact]
        public void LinComb4_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            using var x4 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = x4[0] = 1.0;
            Vec returned = y.LinComb4(y0, 1.0, x1, 2.0, x2, 3.0, x3, 4.0, x4);
            Assert.Same(y, returned);
            Assert.Equal(10.0, y[0]);
        }

        [Fact]
        public void LinComb5_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            using var x4 = Vec.Rent(1);
            using var x5 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = x4[0] = x5[0] = 1.0;
            Vec returned = y.LinComb5(y0, 1.0, x1, 1.0, x2, 1.0, x3, 1.0, x4, 1.0, x5);
            Assert.Same(y, returned);
            Assert.Equal(5.0, y[0]);
        }

        [Fact]
        public void LinComb6_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            using var x4 = Vec.Rent(1);
            using var x5 = Vec.Rent(1);
            using var x6 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = x4[0] = x5[0] = x6[0] = 1.0;
            Vec returned = y.LinComb6(y0, 1.0, x1, 1.0, x2, 1.0, x3, 1.0, x4, 1.0, x5, 1.0, x6);
            Assert.Same(y, returned);
            Assert.Equal(6.0, y[0]);
        }

        [Fact]
        public void LinComb7_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            using var x4 = Vec.Rent(1);
            using var x5 = Vec.Rent(1);
            using var x6 = Vec.Rent(1);
            using var x7 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = x4[0] = x5[0] = x6[0] = x7[0] = 1.0;
            Vec returned = y.LinComb7(y0, 1.0, x1, 1.0, x2, 1.0, x3, 1.0, x4, 1.0, x5, 1.0, x6, 1.0, x7);
            Assert.Same(y, returned);
            Assert.Equal(7.0, y[0]);
        }

        [Fact]
        public void LinComb8_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            using var x4 = Vec.Rent(1);
            using var x5 = Vec.Rent(1);
            using var x6 = Vec.Rent(1);
            using var x7 = Vec.Rent(1);
            using var x8 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = x4[0] = x5[0] = x6[0] = x7[0] = x8[0] = 1.0;
            Vec returned = y.LinComb8(y0,
                1.0, x1, 1.0, x2, 1.0, x3, 1.0, x4,
                1.0, x5, 1.0, x6, 1.0, x7, 1.0, x8);
            Assert.Same(y, returned);
            Assert.Equal(8.0, y[0]);
        }

        [Fact]
        public void LinComb9_ReturnsSelf()
        {
            using var y  = Vec.Rent(1, true);
            using var y0 = Vec.Rent(1, true);
            using var x1 = Vec.Rent(1);
            using var x2 = Vec.Rent(1);
            using var x3 = Vec.Rent(1);
            using var x4 = Vec.Rent(1);
            using var x5 = Vec.Rent(1);
            using var x6 = Vec.Rent(1);
            using var x7 = Vec.Rent(1);
            using var x8 = Vec.Rent(1);
            using var x9 = Vec.Rent(1);
            x1[0] = x2[0] = x3[0] = x4[0] = x5[0] = x6[0] = x7[0] = x8[0] = x9[0] = 1.0;
            Vec returned = y.LinComb9(y0,
                1.0, x1, 1.0, x2, 1.0, x3, 1.0, x4,
                1.0, x5, 1.0, x6, 1.0, x7, 1.0, x8, 1.0, x9);
            Assert.Same(y, returned);
            Assert.Equal(9.0, y[0]);
        }

        // ---------- Fluent chain ----------

        [Fact]
        public void FluentChain_DupAbsScalShift_ComputesScaleVector()
        {
            // The SelectInitialStep pattern: |y|·Rtol + Atol without mutating y.
            using var y = Vec.Rent(3);
            y[0] = 2.0;
            y[1] = -4.0;
            y[2] = 6.0;

            using Vec scale = y.Dup().Abs().Scal(0.5).Shift(1.0);

            // |y| = (2, 4, 6) → ·0.5 → (1, 2, 3) → +1 → (2, 3, 4)
            Assert.Equal(new[] { 2.0, 3.0, 4.0 }, new[] { scale[0], scale[1], scale[2] });
            // Source must not have been mutated.
            Assert.Equal(new[] { 2.0, -4.0, 6.0 }, new[] { y[0], y[1], y[2] });
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
