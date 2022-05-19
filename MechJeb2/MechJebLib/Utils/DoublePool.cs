/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Buffers;
using System.Collections.Generic;
using Smooth.Pools;

#nullable enable

namespace MechJebLib.Utils
{
    public class DoublePool : ObjectPool<List<double>>
    {
        public static readonly DoublePool Pool = new DoublePool();

        // NOTE CAREFULLY: the Count/Capacity of the returned list may be > n
        public List<double> Rent(int n)
        {
            List<double> list = Get();
            list.Clear();
            if (list.Capacity < n)
                list.Capacity = n;
            while (list.Count < n)
                list.Add(0);
            return list;
        }
    }
}
