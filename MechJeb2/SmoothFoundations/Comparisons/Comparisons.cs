using System;
using System.Collections.Generic;
using Smooth.Delegates;

namespace Smooth.Comparisons {
	/// <summary>
	/// Provides various methods for creating comparisons.
	/// </summary>
	public static class Comparisons {
		/// <summary>
		/// Reverses the ordering of the specified comparison.
		/// </summary>
		public static Comparison<T> Reverse<T>(Comparison<T> comparison) {
			return (a, b) => comparison(b, a);
		}
		
		/// <summary>
		/// Prepends null sorting to the specified reference type comparison, with nulls preceeding non-nulls.
		/// </summary>
		public static Comparison<T> NullsFirst<T>(Comparison<T> comparison) where T : class {
			return (a, b) => a == null ? (b == null ? 0 : -1) : b == null ? 1 : comparison(a, b);
		}
		
		/// <summary>
		/// Prepends null sorting to the specified reference type comparison, with nulls suceeding non-nulls.
		/// </summary>
		public static Comparison<T> NullsLast<T>(Comparison<T> comparison) where T : class  {
			return (a, b) => a == null ? (b == null ? 0 : 1) : b == null ? -1 : comparison(a, b);
		}
		
		/// <summary>
		/// Converts the specified comparison for value type T into a comparison for Nullable<T>s, with nulls preceeding non-nulls.
		/// </summary>
		public static Comparison<Nullable<T>> NullableNullsFirst<T>(Comparison<T> comparison) where T : struct {
			return (a, b) => a == null ? (b == null ? 0 : -1) : b == null ? 1 : comparison(a.Value, b.Value);
		}
		
		/// <summary>
		/// Converts the specified comparison for value type T into a comparison for Nullable<T>s, with nulls suceeding non-nulls.
		/// </summary>
		public static Comparison<Nullable<T>> NullableNullsLast<T>(Comparison<T> comparison) where T : struct {
			return (a, b) => a == null ? (b == null ? 0 : 1) : b == null ? -1 : comparison(a.Value, b.Value);
		}
	}

	/// <summary>
	/// Caches delegates for the comparsion methods of IComparer<T>s and IEqualityComparer<T>s.
	/// </summary>
	public static class Comparisons<T> {
		private static Dictionary<IComparer<T>, Comparison<T>> toComparison = new Dictionary<IComparer<T>, Comparison<T>>();
		private static Dictionary<IEqualityComparer<T>, DelegateFunc<T, T, bool>> toPredicate = new Dictionary<IEqualityComparer<T>, DelegateFunc<T, T, bool>>();

		/// <summary>
		/// The comparison method of the default sort comparer for T in delegate form.
		/// </summary>
		public static Comparison<T> Default { get { return ToComparison(Smooth.Collections.Comparer<T>.Default); } }

		/// <summary>
		/// The comparison method of the default equality comparer for T in delegate form.
		/// </summary>
		public static DelegateFunc<T, T, bool> DefaultPredicate { get { return ToPredicate(Smooth.Collections.EqualityComparer<T>.Default); } }

		/// <summary>
		/// Returns the comparison method of the specfied sort comparer in delegate form.
		/// </summary>
		public static Comparison<T> ToComparison(IComparer<T> comparer) {
			Comparison<T> c;
			lock (toComparison) {
				if (!toComparison.TryGetValue(comparer, out c)) {
					c = comparer.Compare;
					toComparison[comparer] = c;
				}
			}
			return c;
		}

		/// <summary>
		/// Returns the comparison method of the specfied equality comparer in delegate form.
		/// </summary>
		public static DelegateFunc<T, T, bool> ToPredicate(IEqualityComparer<T> equalityComparer) {
			DelegateFunc<T, T, bool> c;
			lock (toPredicate) {
				if (!toPredicate.TryGetValue(equalityComparer, out c)) {
					c = equalityComparer.Equals;
					toPredicate[equalityComparer] = c;
				}
			}
			return c;
		}
	}
}
