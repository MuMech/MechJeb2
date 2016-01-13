using System;
using System.Collections.Generic;
using Smooth.Collections;

namespace Smooth.Algebraics {
	/// <summary>
	/// Extension methods for Tuple<>s.
	/// </summary>
	public static class Tuple {
		public static Tuple<T1> Create<T1>(T1 t1) {
			return new Tuple<T1>(t1);
		}

		public static Tuple<T1, T2> Create<T1, T2>(T1 t1, T2 t2) {
			return new Tuple<T1, T2>(t1, t2);
		}

		public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 t1, T2 t2, T3 t3) {
			return new Tuple<T1, T2, T3>(t1, t2, t3);
		}

		public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4) {
			return new Tuple<T1, T2, T3, T4>(t1, t2, t3, t4);
		}

		public static Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) {
			return new Tuple<T1, T2, T3, T4, T5>(t1, t2, t3, t4, t5);
		}

		public static Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) {
			return new Tuple<T1, T2, T3, T4, T5, T6>(t1, t2, t3, t4, t5, t6);
		}
		
		public static Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) {
			return new Tuple<T1, T2, T3, T4, T5, T6, T7>(t1, t2, t3, t4, t5, t6, t7);
		}
		
		public static Tuple<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8) {
			return new Tuple<T1, T2, T3, T4, T5, T6, T7, T8>(t1, t2, t3, t4, t5, t6, t7, t8);
		}

		public static Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9) {
			return new Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(t1, t2, t3, t4, t5, t6, t7, t8, t9);
		}
	}

	/// <summary>
	/// Struct representing a sequence of one element, aka: a singleton.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1> :
		IComparable<Tuple<T1>>,
		IEquatable<Tuple<T1>> {

		public readonly T1 _1;

		public Tuple(T1 _1) {
			this._1 = _1;
		}

		public override bool Equals(object o) {
			return o is Tuple<T1> && this.Equals((Tuple<T1>) o);
		}
		
		public bool Equals(Tuple<T1> t) {
			return Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1);
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				return hash;
			}
		}

		public int CompareTo(Tuple<T1> other) {
			return Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1);
		}
		
		public static bool operator == (Tuple<T1> lhs, Tuple<T1> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1> lhs, Tuple<T1> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Tuple<T1> lhs, Tuple<T1> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1> lhs, Tuple<T1> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1> lhs, Tuple<T1> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1> lhs, Tuple<T1> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}

		public override string ToString() {
			return "(" + _1 + ")";
		}
	}

	/// <summary>
	/// Struct representing a sequence of two elements, aka: an ordered pair.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2> :
		IComparable<Tuple<T1, T2>>,
		IEquatable<Tuple<T1, T2>> {

		public readonly T1 _1;
		public readonly T2 _2;

		public Tuple(T1 _1, T2 _2) {
			this._1 = _1;
			this._2 = _2;
		}

		public override bool Equals(object o) {
			return o is Tuple<T1, T2> && this.Equals((Tuple<T1, T2>) o);
		}
		
		public bool Equals(Tuple<T1, T2> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2));
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				return hash;
			}
		}

		public int CompareTo(Tuple<T1, T2> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2);
		}
		
		public static bool operator == (Tuple<T1, T2> lhs, Tuple<T1, T2> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2> lhs, Tuple<T1, T2> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Tuple<T1, T2> lhs, Tuple<T1, T2> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2> lhs, Tuple<T1, T2> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2> lhs, Tuple<T1, T2> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2> lhs, Tuple<T1, T2> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ")";
		}
	}

	/// <summary>
	/// Struct representing a sequence of three elements, aka: an ordered triplet.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3> :
		IComparable<Tuple<T1, T2, T3>>,
		IEquatable<Tuple<T1, T2, T3>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;

		public Tuple(T1 _1, T2 _2, T3 _3) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
		}

		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3> && this.Equals((Tuple<T1, T2, T3>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3));
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				return hash;
			}
		}

		public int CompareTo(Tuple<T1, T2, T3> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3);
		}
		
		public static bool operator == (Tuple<T1, T2, T3> lhs, Tuple<T1, T2, T3> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3> lhs, Tuple<T1, T2, T3> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Tuple<T1, T2, T3> lhs, Tuple<T1, T2, T3> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3> lhs, Tuple<T1, T2, T3> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3> lhs, Tuple<T1, T2, T3> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3> lhs, Tuple<T1, T2, T3> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ")";
		}
	}

	/// <summary>
	/// Struct representing a sequence of four elements.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3, T4> :
		IComparable<Tuple<T1, T2, T3, T4>>,
		IEquatable<Tuple<T1, T2, T3, T4>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;
		public readonly T4 _4;

		public Tuple(T1 _1, T2 _2, T3 _3, T4 _4) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
			this._4 = _4;
		}

		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3, T4> && this.Equals((Tuple<T1, T2, T3, T4>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3, T4> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3) &&
				Smooth.Collections.EqualityComparer<T4>.Default.Equals(_4, t._4));
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T4>.Default.GetHashCode(_4);
				return hash;
			}
		}

		public int CompareTo(Tuple<T1, T2, T3, T4> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T4>.Default.Compare(_4, other._4);
		}
		
		public static bool operator == (Tuple<T1, T2, T3, T4> lhs, Tuple<T1, T2, T3, T4> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3, T4> lhs, Tuple<T1, T2, T3, T4> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Tuple<T1, T2, T3, T4> lhs, Tuple<T1, T2, T3, T4> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3, T4> lhs, Tuple<T1, T2, T3, T4> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3, T4> lhs, Tuple<T1, T2, T3, T4> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3, T4> lhs, Tuple<T1, T2, T3, T4> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ", " + _4 + ")";
		}
	}

	/// <summary>
	/// Struct representing a sequence of five elements.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3, T4, T5> :
		IComparable<Tuple<T1, T2, T3, T4, T5>>,
		IEquatable<Tuple<T1, T2, T3, T4, T5>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;
		public readonly T4 _4;
		public readonly T5 _5;

		public Tuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
			this._4 = _4;
			this._5 = _5;
		}
		
		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3, T4, T5> && this.Equals((Tuple<T1, T2, T3, T4, T5>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3, T4, T5> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3) &&
				Smooth.Collections.EqualityComparer<T4>.Default.Equals(_4, t._4) &&
				Smooth.Collections.EqualityComparer<T5>.Default.Equals(_5, t._5));
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T4>.Default.GetHashCode(_4);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T5>.Default.GetHashCode(_5);
				return hash;
			}
		}
		
		public int CompareTo(Tuple<T1, T2, T3, T4, T5> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T4>.Default.Compare(_4, other._4); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T5>.Default.Compare(_5, other._5);
		}
		
		public static bool operator == (Tuple<T1, T2, T3, T4, T5> lhs, Tuple<T1, T2, T3, T4, T5> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3, T4, T5> lhs, Tuple<T1, T2, T3, T4, T5> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Tuple<T1, T2, T3, T4, T5> lhs, Tuple<T1, T2, T3, T4, T5> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3, T4, T5> lhs, Tuple<T1, T2, T3, T4, T5> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3, T4, T5> lhs, Tuple<T1, T2, T3, T4, T5> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3, T4, T5> lhs, Tuple<T1, T2, T3, T4, T5> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ", " + _4 + ", " + _5 + ")";
		}
	}

	/// <summary>
	/// Struct representing a sequence of six elements.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3, T4, T5, T6> :
		IComparable<Tuple<T1, T2, T3, T4, T5, T6>>,
		IEquatable<Tuple<T1, T2, T3, T4, T5, T6>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;
		public readonly T4 _4;
		public readonly T5 _5;
		public readonly T6 _6;

		public Tuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
			this._4 = _4;
			this._5 = _5;
			this._6 = _6;
		}
		
		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3, T4, T5, T6> && this.Equals((Tuple<T1, T2, T3, T4, T5, T6>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3, T4, T5, T6> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3) &&
				Smooth.Collections.EqualityComparer<T4>.Default.Equals(_4, t._4) &&
				Smooth.Collections.EqualityComparer<T5>.Default.Equals(_5, t._5) &&
				Smooth.Collections.EqualityComparer<T6>.Default.Equals(_6, t._6));
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T4>.Default.GetHashCode(_4);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T5>.Default.GetHashCode(_5);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T6>.Default.GetHashCode(_6);
				return hash;
			}
		}
		
		public int CompareTo(Tuple<T1, T2, T3, T4, T5, T6> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T4>.Default.Compare(_4, other._4); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T5>.Default.Compare(_5, other._5); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T6>.Default.Compare(_6, other._6);
		}
		
		public static bool operator == (Tuple<T1, T2, T3, T4, T5, T6> lhs, Tuple<T1, T2, T3, T4, T5, T6> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3, T4, T5, T6> lhs, Tuple<T1, T2, T3, T4, T5, T6> rhs) {
			return !lhs.Equals(rhs);
		}
		
		public static bool operator > (Tuple<T1, T2, T3, T4, T5, T6> lhs, Tuple<T1, T2, T3, T4, T5, T6> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3, T4, T5, T6> lhs, Tuple<T1, T2, T3, T4, T5, T6> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3, T4, T5, T6> lhs, Tuple<T1, T2, T3, T4, T5, T6> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3, T4, T5, T6> lhs, Tuple<T1, T2, T3, T4, T5, T6> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ", " + _4 + ", " + _5 + ", " + _6 + ")";
		}
	}
	
	/// <summary>
	/// Struct representing a sequence of seven elements.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3, T4, T5, T6, T7> :
		IComparable<Tuple<T1, T2, T3, T4, T5, T6, T7>>,
		IEquatable<Tuple<T1, T2, T3, T4, T5, T6, T7>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;
		public readonly T4 _4;
		public readonly T5 _5;
		public readonly T6 _6;
		public readonly T7 _7;

		public Tuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
			this._4 = _4;
			this._5 = _5;
			this._6 = _6;
			this._7 = _7;
		}
		
		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3, T4, T5, T6, T7> && this.Equals((Tuple<T1, T2, T3, T4, T5, T6, T7>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3, T4, T5, T6, T7> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3) &&
				Smooth.Collections.EqualityComparer<T4>.Default.Equals(_4, t._4) &&
				Smooth.Collections.EqualityComparer<T5>.Default.Equals(_5, t._5) &&
				Smooth.Collections.EqualityComparer<T6>.Default.Equals(_6, t._6) &&
				Smooth.Collections.EqualityComparer<T7>.Default.Equals(_7, t._7));
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T4>.Default.GetHashCode(_4);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T5>.Default.GetHashCode(_5);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T6>.Default.GetHashCode(_6);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T7>.Default.GetHashCode(_7);
				return hash;
			}
		}
		
		public int CompareTo(Tuple<T1, T2, T3, T4, T5, T6, T7> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T4>.Default.Compare(_4, other._4); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T5>.Default.Compare(_5, other._5); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T6>.Default.Compare(_6, other._6); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T7>.Default.Compare(_7, other._7);
		}
		
		public static bool operator == (Tuple<T1, T2, T3, T4, T5, T6, T7> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3, T4, T5, T6, T7> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7> rhs) {
			return !lhs.Equals(rhs);
		}
		
		public static bool operator > (Tuple<T1, T2, T3, T4, T5, T6, T7> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3, T4, T5, T6, T7> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3, T4, T5, T6, T7> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3, T4, T5, T6, T7> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ", " + _4 + ", " + _5 + ", " + _6 + ", " + _7 + ")";
		}
	}
	
	/// <summary>
	/// Struct representing a sequence of eight elements.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3, T4, T5, T6, T7, T8> :
		IComparable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>>,
		IEquatable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;
		public readonly T4 _4;
		public readonly T5 _5;
		public readonly T6 _6;
		public readonly T7 _7;
		public readonly T8 _8;

		public Tuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
			this._4 = _4;
			this._5 = _5;
			this._6 = _6;
			this._7 = _7;
			this._8 = _8;
		}
		
		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3, T4, T5, T6, T7, T8> && this.Equals((Tuple<T1, T2, T3, T4, T5, T6, T7, T8>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3, T4, T5, T6, T7, T8> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3) &&
				Smooth.Collections.EqualityComparer<T4>.Default.Equals(_4, t._4) &&
				Smooth.Collections.EqualityComparer<T5>.Default.Equals(_5, t._5) &&
				Smooth.Collections.EqualityComparer<T6>.Default.Equals(_6, t._6) &&
				Smooth.Collections.EqualityComparer<T7>.Default.Equals(_7, t._7) &&
				Smooth.Collections.EqualityComparer<T8>.Default.Equals(_8, t._8));
		}
		
		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T4>.Default.GetHashCode(_4);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T5>.Default.GetHashCode(_5);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T6>.Default.GetHashCode(_6);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T7>.Default.GetHashCode(_7);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T8>.Default.GetHashCode(_8);
				return hash;
			}
		}
		
		public int CompareTo(Tuple<T1, T2, T3, T4, T5, T6, T7, T8> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T4>.Default.Compare(_4, other._4); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T5>.Default.Compare(_5, other._5); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T6>.Default.Compare(_6, other._6); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T7>.Default.Compare(_7, other._7); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T8>.Default.Compare(_8, other._8);
		}
		
		public static bool operator == (Tuple<T1, T2, T3, T4, T5, T6, T7, T8> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3, T4, T5, T6, T7, T8> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> rhs) {
			return !lhs.Equals(rhs);
		}
		
		public static bool operator > (Tuple<T1, T2, T3, T4, T5, T6, T7, T8> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3, T4, T5, T6, T7, T8> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3, T4, T5, T6, T7, T8> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3, T4, T5, T6, T7, T8> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ", " + _4 + ", " + _5 + ", " + _6 + ", " + _7 + ", " + _8 + ")";
		}
	}

	/// <summary>
	/// Struct representing a sequence of nine elements.
	/// </summary>
	[System.Serializable]
	public struct Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> :
		IComparable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>>,
		IEquatable<Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>> {

		public readonly T1 _1;
		public readonly T2 _2;
		public readonly T3 _3;
		public readonly T4 _4;
		public readonly T5 _5;
		public readonly T6 _6;
		public readonly T7 _7;
		public readonly T8 _8;
		public readonly T9 _9;

		public Tuple(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9) {
			this._1 = _1;
			this._2 = _2;
			this._3 = _3;
			this._4 = _4;
			this._5 = _5;
			this._6 = _6;
			this._7 = _7;
			this._8 = _8;
			this._9 = _9;
		}
		
		public override bool Equals(object o) {
			return o is Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> && this.Equals((Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>) o);
		}
		
		public bool Equals(Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> t) {
			return (
				Smooth.Collections.EqualityComparer<T1>.Default.Equals(_1, t._1) &&
				Smooth.Collections.EqualityComparer<T2>.Default.Equals(_2, t._2) &&
				Smooth.Collections.EqualityComparer<T3>.Default.Equals(_3, t._3) &&
				Smooth.Collections.EqualityComparer<T4>.Default.Equals(_4, t._4) &&
				Smooth.Collections.EqualityComparer<T5>.Default.Equals(_5, t._5) &&
				Smooth.Collections.EqualityComparer<T6>.Default.Equals(_6, t._6) &&
				Smooth.Collections.EqualityComparer<T7>.Default.Equals(_7, t._7) &&
				Smooth.Collections.EqualityComparer<T8>.Default.Equals(_8, t._8) &&
				Smooth.Collections.EqualityComparer<T9>.Default.Equals(_9, t._9));
		}

		public override int GetHashCode() {
			unchecked {
				int hash = 17;
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T1>.Default.GetHashCode(_1);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T2>.Default.GetHashCode(_2);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T3>.Default.GetHashCode(_3);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T4>.Default.GetHashCode(_4);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T5>.Default.GetHashCode(_5);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T6>.Default.GetHashCode(_6);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T7>.Default.GetHashCode(_7);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T8>.Default.GetHashCode(_8);
				hash = 29 * hash + Smooth.Collections.EqualityComparer<T9>.Default.GetHashCode(_9);
				return hash;
			}
		}
		
		public int CompareTo(Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> other) {
			int c;
			c = Smooth.Collections.Comparer<T1>.Default.Compare(_1, other._1); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T2>.Default.Compare(_2, other._2); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T3>.Default.Compare(_3, other._3); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T4>.Default.Compare(_4, other._4); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T5>.Default.Compare(_5, other._5); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T6>.Default.Compare(_6, other._6); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T7>.Default.Compare(_7, other._7); if (c != 0) { return c; }
			c = Smooth.Collections.Comparer<T8>.Default.Compare(_8, other._8); if (c != 0) { return c; }
			return Smooth.Collections.Comparer<T9>.Default.Compare(_9, other._9);
		}
		
		public static bool operator == (Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> rhs) {
			return lhs.Equals(rhs);
		}
		
		public static bool operator != (Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}
		
		public static bool operator < (Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}
		
		public static bool operator >= (Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}
		
		public static bool operator <= (Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> lhs, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}
		
		public override string ToString() {
			return "(" + _1 + ", " + _2 + ", " + _3 + ", " + _4 + ", " + _5 + ", " + _6 + ", " + _7 + ", " + _8 + ", " + _9 + ")";
		}
	}
}
