/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
// ReSharper disable InconsistentNaming

namespace MechJebLib.Primitives
{
    public class Vn : IList<double>, IReadOnlyList<double>, IDisposable
    {
        private int _n;

        private static readonly ObjectPool<Vn> _pool   = new ObjectPool<Vn>(New, Clear);
        private readonly        List<double>   _values = new List<double>();

        public Vn Abs()
        {
            Vn abs = Rent(_n);

            for (int i = 0; i < _n; i++)
                abs[i] = Math.Abs(this[i]);

            return abs;
        }

        public double AbsMax()
        {
            double absmax = double.NegativeInfinity;
            for (int i = 0; i < _n; i++)
                if (Math.Abs(this[i]) > absmax)
                    absmax = this[i];

            return absmax;
        }

        public Vn Dup()
        {
            Vn dup = Rent(_n);

            for (int i = 0; i < _n; i++)
                dup[i] = this[i];

            return dup;
        }

        public Vn MutAdd(double rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] += rhs;

            return this;
        }

        public Vn MutAdd(IReadOnlyList<double> rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] += rhs[i];

            return this;
        }

        public Vn MutSub(double rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] -= rhs;

            return this;
        }

        public Vn MutSub(IReadOnlyList<double> rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] -= rhs[i];

            return this;
        }

        public Vn MutTimes(double rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] *= rhs;

            return this;
        }

        public Vn MutTimes(IReadOnlyList<double> rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] *= rhs[i];

            return this;
        }

        public Vn MutDiv(double rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] /= rhs;

            return this;
        }

        public Vn MutDiv(IReadOnlyList<double> rhs)
        {
            for (int i = 0; i < _n; i++)
                this[i] /= rhs[i];

            return this;
        }

        public double magnitude
        {
            get
            {
                // FIXME: overflow
                double sumsq = 0;
                for (int i = 0; i < _n; i++)
                    sumsq += this[i] * this[i];

                return Math.Sqrt(sumsq);
            }
        }

        private Vn()
        {
        }

        private static Vn New()
        {
            return new Vn();
        }

        public Vn CopyFrom(IReadOnlyList<double> other)
        {
            for (int i = 0; i < _n; i++)
                this[i] = other[i];

            return this;
        }

        public void CopyTo(IList<double> other)
        {
            for (int i = 0; i < _n; i++)
                other[i] = this[i];
        }

        public void Set(int index, V3 v)
        {
            this[index]     = v.x;
            this[index + 1] = v.y;
            this[index + 2] = v.z;
        }

        public V3 Get(int index)
        {
            return new V3(this[index], this[index + 1], this[index + 2]);
        }

        public static Vn Rent(int n)
        {
            Vn list = _pool.Borrow();
            while (list.Count < n)
                list.Add(0);
            if (list.Count > n)
                list.RemoveRange(n, list.Count - n);
            list._n = n;
            return list;
        }

        private void RemoveRange(int i, int listCount)
        {
            _values.RemoveRange(i, listCount);
        }

        public static Vn Rent(double[] other)
        {
            Vn list = Rent(other.Length);
            for (int i = 0; i < other.Length; i++)
                list[i] = other[i];

            return list;
        }

        private static void Clear(Vn obj)
        {
            // do not resize the array to zero to avoid creating garbage
            // we will return nonsense in the array values (not zeros) for performance
        }

        public static void Return(Vn obj)
        {
            _pool.Release(obj);
        }

        public void Dispose()
        {
            _pool.Release(this);
        }

        public IEnumerator<double> GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public override string ToString()
        {
            return DoubleArrayString(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_values).GetEnumerator();
        }

        public void Add(double item)
        {
            _values.Add(item);
        }

        public void Clear()
        {
            _values.Clear();
        }

        public bool Contains(double item)
        {
            return _values.Contains(item);
        }

        public void CopyTo(double[] array, int arrayIndex)
        {
            _values.CopyTo(array, arrayIndex);
        }

        public bool Remove(double item)
        {
            return _values.Remove(item);
        }

        public int  Count      => _values.Count;

        public bool IsReadOnly => ((ICollection<double>) _values).IsReadOnly;

        public int IndexOf(double item)
        {
            return _values.IndexOf(item);
        }

        public void Insert(int index, double item)
        {
            _values.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _values.RemoveAt(index);
        }

        public double this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }
    }
}
