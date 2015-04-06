using System;
using System.Collections.Generic;
using Smooth.Collections;

namespace Smooth.Comparisons {
	/// <summary>
	/// Allocation free equality comparer for type T where T implements IEquatable<T>.
	/// 
	/// Only useful to circumvent potential JIT exceptions on platforms without JIT compilation.
	/// </summary>
	public class IEquatableEqualityComparer<T> : Smooth.Collections.EqualityComparer<T> where T : IEquatable<T> {
		public override bool Equals(T l, T r) {
			return l.Equals(r);
		}

		public override int GetHashCode(T t) {
			return t.GetHashCode();
		}
	}
}
