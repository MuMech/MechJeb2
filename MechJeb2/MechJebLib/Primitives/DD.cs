/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Primitives
{
    public class DD : List<double>, IDisposable
    {
        public int N;

        private static readonly ObjectPool<DD> _pool = new ObjectPool<DD>(New, Clear);

        private DD()
        {
        }

        private static DD New()
        {
            return new DD();
        }

        public void CopyTo(IList<double> other)
        {
            for (int i = 0; i < N; i++)
                other[i] = this[i];
        }

        public static DD Rent(int n)
        {
            DD list = _pool.Get();
            while (list.Count < n)
                list.Add(0);
            if (list.Count > n)
                list.RemoveRange(n, list.Count - n);
            list.N = n;
            return list;
        }

        public static DD Rent(double[] other)
        {
            DD list = Rent(other.Length);
            for (int i = 0; i < other.Length; i++)
                list[i] = other[i];

            return list;
        }

        public static void Clear(DD obj)
        {
            // do not resize the array to zero to avoid creating garbage
            // we will return nonsense in the array values (not zeros) for performance
        }

        public static void Return(DD obj)
        {
            _pool.Return(obj);
        }

        public void Dispose()
        {
            _pool.Return(this);
        }

        public override string ToString()
        {
            return DoubleArrayString(this);
        }
    }
}
