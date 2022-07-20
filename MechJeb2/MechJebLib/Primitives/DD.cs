/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MechJebLib.Utils;

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

        // NOTE CAREFULLY: the Count/Capacity of the returned list may be > n
        public static DD Rent(int n)
        {
            DD list = _pool.Get();
            if (list.Capacity < n)
                list.Capacity = n;
            while (list.Count < n)
                list.Add(0);
            list.N = n;
            return list;
        }

        public static void Clear(DD obj)
        {
            obj.Clear();
        }

        public static void Return(DD obj)
        {
            _pool.Return(obj);
        }

        public void Dispose()
        {
            _pool.Return(this);
        }
    }
}
