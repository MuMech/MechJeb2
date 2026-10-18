/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MechJebLib.Utils
{
    public class ObjectPoolBase
    {
        // Tests flip this to false (via [assembly: TestFramework]) so each thread
        // gets its own pool — keeps allocation-counting tests from contaminating
        // each other when xunit runs them in parallel.
        internal static bool UseGlobal = true;
    }

    public class ObjectPool<T> : ObjectPoolBase where T : class
    {
        private readonly Func<T> _create;
        private readonly Action<T> _reset;

        private readonly Store _global = new Store();
        private readonly ThreadLocal<Store> _local = new ThreadLocal<Store>(() => new Store());

        public ObjectPool(Func<T> create, Action<T> reset)
        {
            _create = create;
            _reset = reset;
        }

        public T Borrow()
        {
            Store store = UseGlobal ? _global : _local.Value;

            lock (store)
            {
                if (store.Items.Count > 0)
                {
                    T item = store.Items.Pop();
                    store.Members.Remove(item);
                    return item;
                }
            }

            return _create();
        }

        public void Release(T item)
        {
            Store store = UseGlobal ? _global : _local.Value;

            // catch double-release bugs and throw
            lock (store)
            {
                if (!store.Members.Add(item))
                    throw new InvalidOperationException($"{typeof(T).Name} released to ObjectPool twice");
            }

            // resets dispose children into other pools, so run them outside the lock to avoid nesting pool locks.
            _reset(item);

            lock (store)
                store.Items.Push(item);
        }

        // Items is always a subset of Members, an item is added to Members before its reset and only pushed after.
        private sealed class Store
        {
            public readonly Stack<T> Items = new Stack<T>();
            public readonly HashSet<T> Members = new HashSet<T>(ReferenceComparer.Instance);
        }

        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public bool Equals(T x, T y) => ReferenceEquals(x, y);

            public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}
