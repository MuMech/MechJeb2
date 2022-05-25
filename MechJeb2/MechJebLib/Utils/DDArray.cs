/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections.Generic;

#nullable enable

namespace MechJebLib.Utils
{
    public class DDArray : List<double>, IDisposable
    {
        private static readonly ObjectPool<DDArray> _pool = new ObjectPool<DDArray>(New);

        private static DDArray New()
        {
            return new DDArray();
        }

        // NOTE CAREFULLY: the Count/Capacity of the returned list may be > n
        public static DDArray Rent(int n)
        {
            DDArray list = _pool.Get();
            list.Clear();
            if (list.Capacity < n)
                list.Capacity = n;
            while (list.Count < n)
                list.Add(0);
            return list;
        }

        public static void Return(DDArray obj)
        {
            _pool.Return(obj);
        }

        public void Dispose()
        {
            _pool.Return(this);
        }
    }
}
