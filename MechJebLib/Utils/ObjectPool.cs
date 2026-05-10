/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private readonly ConcurrentBag<T> _globalPool = new ConcurrentBag<T>();
        private readonly ThreadLocal<Stack<T>> _localPool = new ThreadLocal<Stack<T>>(() => new Stack<T>());

        public ObjectPool(Func<T> create, Action<T> reset)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        public T Borrow()
        {
            if (UseGlobal)
            {
                if (_globalPool.TryTake(out T item)) return item;
            }
            else
            {
                Stack<T> stack = _localPool.Value;
                if (stack.Count > 0) return stack.Pop();
            }

            return _create();
        }

        public void Release(T item)
        {
            _reset(item);
            if (UseGlobal)
                _globalPool.Add(item);
            else
                _localPool.Value.Push(item);
        }
    }
}
