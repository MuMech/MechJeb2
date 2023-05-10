/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Concurrent;
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

        private readonly        ConcurrentBag<T>              _globalPool = new ConcurrentBag<T>();
        private static readonly ThreadLocal<ConcurrentBag<T>> _localPool  = new ThreadLocal<ConcurrentBag<T>>(() => new ConcurrentBag<T>());

        private ConcurrentBag<T> _pool => UseGlobal ? _globalPool : _localPool.Value;

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
            return _pool.TryTake(out T item) ? item : _newfun();
        }

        public void Return(T item)
        {
            _clearfun?.Invoke(item);
            _pool.Add(item);
        }
    }
}
