using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Delegates;
using Smooth.Dispose;

namespace Smooth.Pools {
	/// <summary>
	/// Pool that lends values of type T with an associated key of type K and defines a default key.
	/// </summary>
	public class KeyedPoolWithDefaultKey<K, T> : KeyedPool<K, T> {
		private readonly Either<K, DelegateFunc<K>> defaultKey;

		/// <summary>
		/// Creates a new keyed pool with the specified creation delegate, reset delegate, and default key.
		/// </summary>
		public KeyedPoolWithDefaultKey(DelegateFunc<K, T> create, DelegateFunc<T, K> reset, K defaultKey) : base (create, reset) {
			this.defaultKey = Either<K, DelegateFunc<K>>.Left(defaultKey);
		}

		/// <summary>
		/// Creates a new keyed pool with the specified creation delegate, reset delegate, and default key.
		/// </summary>
		public KeyedPoolWithDefaultKey(DelegateFunc<K, T> create, DelegateFunc<T, K> reset, DelegateFunc<K> defaultKeyFunc) : base (create, reset) {
			this.defaultKey = Either<K, DelegateFunc<K>>.Right(defaultKeyFunc);
		}

		/// <summary>
		/// Borrows a value with the default key from the pool.
		/// </summary>
		public T Borrow() {
			return Borrow(defaultKey.isLeft ? defaultKey.leftValue : defaultKey.rightValue());
		}

		/// <summary>
		/// Borrows a wrapped value with the default key from the pool.
		/// </summary>
		public Disposable<T> BorrowDisposable() {
			return BorrowDisposable(defaultKey.isLeft ? defaultKey.leftValue : defaultKey.rightValue());
		}
	}
}