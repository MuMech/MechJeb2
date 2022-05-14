/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Collections.Concurrent;

namespace MechJebLib.Utils
{
    // TODO: min and max object levels
    public class ObjectPool<T> where T : new()
    {
        private readonly ConcurrentBag<T> _objects;

        public ObjectPool()
        {
            _objects = new ConcurrentBag<T>();
        }

        public T Get()
        {
            return _objects.TryTake(out T item) ? item : new T();
        }

        public void Return(T item)
        {
            _objects.Add(item);
        }
    }
}
