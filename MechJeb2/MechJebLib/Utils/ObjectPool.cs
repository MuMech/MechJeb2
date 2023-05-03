/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MechJebLib.Utils
{
    public class ObjectPoolBase
    {
        // The object pool is global state, and global state is horrible for unit tests, so by setting
        // UseGlobal = false the tests will use per-thread object pools, which keeps them from scribbling
        // over each other's objectpools.  This lets me write tests to check for allocations to make sure
        // that the use of the threadpool isn't leaking objects on successive invocations.
        internal static bool UseGlobal = true;
    }

    // TODO: min and max object levels
    public class ObjectPool<T> : ObjectPoolBase
    {
        private readonly Func<T>    _newfun;
        private readonly Action<T>? _clearfun;

        private readonly        ConcurrentBag<T>     _globalPool = new ConcurrentBag<T>();
        private static readonly ThreadLocal<Stack<T>> _localPool  = new ThreadLocal<Stack<T>>(() => new Stack<T>());

        public ObjectPool(Func<T> newfun)
        {
            _clearfun = null;
            _newfun   = newfun;
        }

        public ObjectPool(Func<T> newfun, Action<T> clearfun)
        {
            _clearfun = clearfun;
            _newfun   = newfun;
        }

        public T Get()
        {
            if (UseGlobal)
                return _globalPool.TryTake(out T item) ? item : _newfun();
            else
                return _localPool.Value.TryPop(out T item) ? item : _newfun();
        }

        public void Return(T item)
        {
            _clearfun?.Invoke(item);

            if (UseGlobal)
                _globalPool.Add(item);
            else
                _localPool.Value.Push(item);
        }
    }
}
