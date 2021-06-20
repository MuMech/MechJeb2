using System.Collections.Concurrent;

namespace MuMech.MathJ
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
