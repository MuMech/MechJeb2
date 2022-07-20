/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Concurrent;

namespace MechJebLib.Utils
{
    // TODO: min and max object levels
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _objects;
        private readonly Func<T>          _newfun;
        private readonly Action<T>       _clearfun;

        public ObjectPool(Func<T> newfun, Action<T> clearfun)
        {
            _objects  = new ConcurrentBag<T>();
            _clearfun = clearfun;
            _newfun   = newfun;
        }

        public T Get()
        {
            return _objects.TryTake(out T item) ? item : _newfun();
        }

        public void Return(T item)
        {
            _clearfun(item);
            _objects.Add(item);
        }
    }
}
