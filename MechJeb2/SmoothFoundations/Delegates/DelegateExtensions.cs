using System;

namespace Smooth.Delegates {
	/// <summary>
	/// Provides extension methods related to delegate usage.
	/// </summary>
	public static class DelegateExtensions {
		/// <summary>
		/// Calls the specified action with the specified value.
		/// </summary>
		public static void Apply<T>(this T t, DelegateAction<T> a) {
			a(t);
		}
		
		/// <summary>
		/// Calls the specified action with the specified value and parameter.
		/// </summary>
		public static void Apply<T, P>(this T t, DelegateAction<T, P> a, P p) {
			a(t, p);
		}

		/// <summary>
		/// Calls the specified function with the specified value and returns the result.
		/// </summary>
		public static U Apply<T, U>(this T t, DelegateFunc<T, U> f) {
			return f(t);
		}

		/// <summary>
		/// Calls the specified function with the specified value and parameter and returns the result.
		/// </summary>
		public static U Apply<T, U, P>(this T t, DelegateFunc<T, P, U> f, P p) {
			return f(t, p);
		}
	}
}
