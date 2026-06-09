using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Utils;

namespace MechJebLib.Primitives
{
    public sealed class Vec : IList<double>, IReadOnlyList<double>, IDisposable
    {
        private static readonly ObjectPool<Vec>[] _pools = MakePools();

        private static ObjectPool<Vec>[] MakePools()
        {
            var arr = new ObjectPool<Vec>[Bucket.COUNT];
            for (int k = 0; k < Bucket.COUNT; k++)
            {
                int capacity = Bucket.SizeFor(k);
                int k1 = k;
                arr[k] = new ObjectPool<Vec>(
                    () => new Vec(new double[capacity], k1),
                    v => v.Length = 0);
            }

            return arr;
        }

        private Vec(double[] data, int bucket)
        {
            Data = data;
            _bucket = bucket;
            Length = 0;
        }

        public readonly double[] Data;
        public int Length; // warn: do not write this
        private readonly int _bucket;

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data.Length;
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length;
        }

        public static Vec Rent(int length, bool zero = false)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            int bucket = Bucket.IndexFor(length);
            Vec v = _pools[bucket].Borrow();
            v.Length = length;
            if (zero) Array.Clear(v.Data, 0, length);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _pools[_bucket].Release(this);

        public double this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Data[i] = value;
        }

        public Vec Dup() => Rent(Length).CopyFrom(this);

        // ---- Convenience BLAS-1 wrappers (delegate to VecOps, length = this.Length) ----
        // All other vectors are assumed to have Length ≥ this.Length. No runtime check.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Fill(double value)
        {
            VecOps.Fill(Data, value, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Scal(double a)
        {
            VecOps.Scal(a, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec MaxZero()
        {
            VecOps.MaxZero(Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Vec destination) =>
            VecOps.Copy(Data, destination.Data, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec CopyFrom(Vec source)
        {
            VecOps.Copy(source.Data, Data, Length);
            return this;
        }

        // this ← a·x + this
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Axpy(double a, Vec x)
        {
            VecOps.Axpy(a, x.Data, Data, Length);
            return this;
        }

        // this ← a·x + b·this
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Axpby(double a, Vec x, double b)
        {
            VecOps.Axpby(a, x.Data, b, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Swap(Vec other)
        {
            VecOps.Swap(Data, other.Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(Vec other) =>
            VecOps.Dot(Data, other.Data, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Mul(Vec other)
        {
            VecOps.Mul(Data, other.Data, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Div(Vec other)
        {
            VecOps.Div(Data, other.Data, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Add(Vec other)
        {
            VecOps.Add(Data, other.Data, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Sub(Vec other)
        {
            VecOps.Sub(Data, other.Data, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Shift(double a)
        {
            VecOps.Shift(Data, a, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec Abs()
        {
            VecOps.Abs(Data, Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Nrm2() => VecOps.Nrm2(Data, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NrmInf() => VecOps.NrmInf(Data, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Asum() => VecOps.Asum(Data, Length);

        // Hides LINQ's Enumerable.Sum extension on Vec — instance methods win
        // overload resolution, so callers using System.Linq still get this fast path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Sum() => VecOps.Sum(Data, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Iamax() => VecOps.Iamax(Data, Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec LinComb1(Vec y0,
            double a1, Vec x1)
        {
            VecOps.LinComb1(Data, y0.Data,
                a1, x1.Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec LinComb2(Vec y0,
            double a1, Vec x1, double a2, Vec x2)
        {
            VecOps.LinComb2(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec LinComb3(Vec y0,
            double a1, Vec x1, double a2, Vec x2, double a3, Vec x3)
        {
            VecOps.LinComb3(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, a3, x3.Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec LinComb4(Vec y0,
            double a1, Vec x1, double a2, Vec x2,
            double a3, Vec x3, double a4, Vec x4)
        {
            VecOps.LinComb4(Data, y0.Data,
                a1, x1.Data, a2, x2.Data,
                a3, x3.Data, a4, x4.Data, Length);
            return this;
        }

        public Vec LinComb5(Vec y0,
            double a1, Vec x1, double a2, Vec x2,
            double a3, Vec x3, double a4, Vec x4, double a5, Vec x5)
        {
            VecOps.LinComb5(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, a3, x3.Data,
                a4, x4.Data, a5, x5.Data, Length);
            return this;
        }

        public Vec LinComb6(Vec y0,
            double a1, Vec x1, double a2, Vec x2,
            double a3, Vec x3, double a4, Vec x4,
            double a5, Vec x5, double a6, Vec x6)
        {
            VecOps.LinComb6(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, a3, x3.Data,
                a4, x4.Data, a5, x5.Data, a6, x6.Data, Length);
            return this;
        }

        public Vec LinComb7(Vec y0,
            double a1, Vec x1, double a2, Vec x2,
            double a3, Vec x3, double a4, Vec x4,
            double a5, Vec x5, double a6, Vec x6, double a7, Vec x7)
        {
            VecOps.LinComb7(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, a3, x3.Data,
                a4, x4.Data, a5, x5.Data, a6, x6.Data,
                a7, x7.Data, Length);
            return this;
        }

        public Vec LinComb8(Vec y0,
            double a1, Vec x1, double a2, Vec x2,
            double a3, Vec x3, double a4, Vec x4,
            double a5, Vec x5, double a6, Vec x6,
            double a7, Vec x7, double a8, Vec x8)
        {
            VecOps.LinComb8(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, a3, x3.Data,
                a4, x4.Data, a5, x5.Data, a6, x6.Data,
                a7, x7.Data, a8, x8.Data, Length);
            return this;
        }

        public Vec LinComb9(Vec y0,
            double a1, Vec x1, double a2, Vec x2,
            double a3, Vec x3, double a4, Vec x4,
            double a5, Vec x5, double a6, Vec x6,
            double a7, Vec x7, double a8, Vec x8, double a9, Vec x9)
        {
            VecOps.LinComb9(Data, y0.Data,
                a1, x1.Data, a2, x2.Data, a3, x3.Data,
                a4, x4.Data, a5, x5.Data, a6, x6.Data,
                a7, x7.Data, a8, x8.Data, a9, x9.Data, Length);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vec CubicHermiteInterpolant(double x1, Vec y1, Vec yp1,
            double x2, Vec y2, Vec yp2, double x)
        {
            VecOps.CubicHermiteInterpolant(x1, y1.Data, yp1.Data, x2, y2.Data, yp2.Data,
                x, Data, Length);
            return this;
        }

        // ---- IList<double> (explicit so the class indexer is the fast path) ----
        double IList<double>.this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data[i];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Data[i] = value;
        }

        int ICollection<double>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length;
        }

        bool ICollection<double>.IsReadOnly => false;

        int IList<double>.IndexOf(double item)
        {
            for (int i = 0; i < Length; i++)
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Data[i] == item)
                    return i;
            return -1;
        }

        bool ICollection<double>.Contains(double item)
        {
            for (int i = 0; i < Length; i++)
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (Data[i] == item)
                    return true;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICollection<double>.CopyTo(double[] array, int arrayIndex) => Array.Copy(Data, 0, array, arrayIndex, Length);

        void IList<double>.      Insert(int index, double item) => throw new NotSupportedException();
        void IList<double>.      RemoveAt(int index)            => throw new NotSupportedException();
        void ICollection<double>.Add(double item)               => throw new NotSupportedException();
        void ICollection<double>.Clear()                        => throw new NotSupportedException();
        bool ICollection<double>.Remove(double item)            => throw new NotSupportedException();

        // ---- IReadOnlyList<double> ----
        double IReadOnlyList<double>.this[int i]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Data[i];
        }

        int IReadOnlyCollection<double>.Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length;
        }

        // ---- Enumeration (struct enumerator avoids boxing on foreach over Vec) ----
        public Enumerator                       GetEnumerator() => new Enumerator(this);
        IEnumerator<double> IEnumerable<double>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.                GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<double>
        {
            private readonly Vec _vec;
            private int _index;

            internal Enumerator(Vec vec)
            {
                _vec = vec;
                _index = -1;
            }

            public double Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _vec.Data[_index];
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Current;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _vec.Length;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => _index = -1;

            public void Dispose() { }
        }

        private static class Bucket
        {
            // 2^30 = ~1B doubles = ~8GB. Plenty for any sane PIPG/ODE buffer.
            public const int COUNT = 31;

            private static readonly int[] _log2 = { 0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30, 8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31 };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int IndexFor(int length)
            {
                if (length <= 1) return 0;
                uint v = (uint)(length - 1);
                v |= v >> 1;
                v |= v >> 2;
                v |= v >> 4;
                v |= v >> 8;
                v |= v >> 16;
                return _log2[(v * 0x07C4ACDDu) >> 27] + 1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int SizeFor(int bucket) => 1 << bucket;
        }
    }
}
