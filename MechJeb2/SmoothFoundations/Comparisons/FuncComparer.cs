using System;
using System.Collections.Generic;
using Smooth.Collections;

namespace Smooth.Comparisons {
	/// <summary>
	/// Performs type-specific comparisons using the comparison delegate supplied to the constructor.
	/// </summary>
	public class FuncComparer<T> : Smooth.Collections.Comparer<T> {
		private readonly Comparison<T> comparison;

		/// <summary>
		/// Instantiate a comparer for type T using the specified comparison
		/// </summary>
		public FuncComparer(Comparison<T> comparison) {
			this.comparison = comparison;
		}

		public FuncComparer(IComparer<T> comparer) {
			this.comparison = comparer.Compare;
		}

		public override int Compare(T t1, T t2) {
			return comparison(t1, t2);
		}
	}
}
