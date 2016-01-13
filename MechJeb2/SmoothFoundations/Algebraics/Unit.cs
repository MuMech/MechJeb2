using System;

namespace Smooth.Algebraics {

	/// <summary>
	/// Represents a type that holds no information.  Units have no state, and all instances of Unit are considered equal.
	/// </summary>
	public struct Unit : IComparable<Unit>, IEquatable<Unit> {
		public override bool Equals(object o) { return o is Unit; }
		
		public bool Equals(Unit other) { return true; }
		
		public override int GetHashCode() { return 0; }
		
		public int CompareTo(Unit other) { return 0; }

		public static bool operator == (Unit lhs, Unit rhs) { return true; }
		
		public static bool operator >= (Unit lhs, Unit rhs) { return true; }
		
		public static bool operator <= (Unit lhs, Unit rhs) { return true; }

		public static bool operator != (Unit lhs, Unit rhs) { return false; }

		public static bool operator > (Unit lhs, Unit rhs) { return false; }
		
		public static bool operator < (Unit lhs, Unit rhs) { return false; }

		public override string ToString() { return "Unit"; }
	}
}
