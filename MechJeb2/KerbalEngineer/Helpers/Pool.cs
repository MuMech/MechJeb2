using System.Collections.Generic;

namespace KerbalEngineer
{
    /// <summary>
    ///     Pool of object
    /// </summary>
    public class Pool<T> {
        
        private readonly Stack<T> values = new Stack<T>();

        private readonly CreateDelegate<T> create;
        private readonly ResetDelegate<T> reset;

        public delegate R CreateDelegate<out R>();
        public delegate void ResetDelegate<in T1>(T1 a);
        
        /// <summary>
        ///     Creates an empty pool with the specified object creation and reset delegates.
        /// </summary>
        public Pool(CreateDelegate<T> create, ResetDelegate<T> reset) {
            this.create = create;
            this.reset = reset;
        }

        /// <summary>
        ///     Borrows an object from the pool.
        /// </summary>
        public T Borrow() {
            lock (values) {
                return values.Count > 0 ? values.Pop() : create();
            }
        }
        
        /// <summary>
        ///     Release an object, reset it and returns it to the pool.
        /// </summary>
        public void Release(T value) {
            reset(value);
            lock (values) {
                values.Push(value);
            }
        }
        
        /// <summary>
        ///     Current size of the pool.
        /// </summary>
        public int Count()
        {
            return values.Count;
        }
    }
}