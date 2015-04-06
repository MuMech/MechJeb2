using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Delegates;
using Smooth.Dispose;

namespace Smooth.Pools {
	/// <summary>
	/// Pool that lends values of type T with an optional initializer that takes a value of type U.
	/// </summary>
	public class PoolWithInitializer<T, U> : Pool<T> {
		private readonly DelegateAction<T, U> initialize;

		/// <summary>
		/// Creates a new pool with the specified creation, reset, and initialization delegates.
		/// </summary>
		public PoolWithInitializer(DelegateFunc<T> create, DelegateAction<T> reset, DelegateAction<T, U> initialize) : base (create, reset) {
			this.initialize = initialize;
		}

		/// <summary>
		/// Borrows a value from the pool and initializes it with the specified value.
		/// </summary>
		public T Borrow(U u) {
			var value = Borrow();
			initialize(value, u);
			return value;
		}

		/// <summary>
		/// Borrows a wrapped value from from the pool and initializes it with the specified value.
		/// </summary>
		public Disposable<T> BorrowDisposable(U u) {
			var wrapper = BorrowDisposable();
			initialize(wrapper.value, u);
			return wrapper;
		}
	}
}