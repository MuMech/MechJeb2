using System;
using System.Collections.Generic;
using Smooth.Compare;

namespace Smooth.Collections {
	/// <summary>
	/// Analog to System.Collections.Generic.Comparer<T>.
	/// </summary>
	public abstract class Comparer<T> : IComparer<T> {
		private static IComparer<T> _default;
		
		public static IComparer<T> Default {
			get {
				if (_default == null) {
					_default = Finder.Comparer<T>();
				}
				return _default;
			}
			set { _default = value; }
		}

		public abstract int Compare(T lhs, T rhs);
	}
}