using System;
using System.Collections.Generic;
using System.Linq;
using Smooth.Collections;
using Smooth.Delegates;

namespace Smooth.Algebraics {
	/// <summary>
	/// Extension methods for Option<T>.
	/// </summary>
	public static class Option {
		/// <summary>
		/// Returns None if value == null; otherwise, Some(value).
		/// </summary>
		public static Option<T> Create<T>(T value) {
			return value == null ? Option<T>.None : new Option<T>(value);
		}
		
		/// <summary>
		/// Returns Some(value), regardless of whether or not value == null.
		/// </summary>
		public static Option<T> Some<T>(T value) {
			return new Option<T>(value);
		}
		
		/// <summary>
		/// Returns None.
		/// </summary>
		public static Option<T> None<T>(T value) {
			return Option<T>.None;
		}

		/// <summary>
		/// Returns None if value == null; otherwise, Some(value).
		/// </summary>
		public static Option<T> ToOption<T>(this T value) {
			return value == null ? Option<T>.None : new Option<T>(value);
		}
		
		/// <summary>
		/// Returns Some(value), regardless of whether or not value == null.
		/// </summary>
		public static Option<T> ToSome<T>(this T value) {
			return new Option<T>(value);
		}
		
		/// <summary>
		/// Returns None.
		/// </summary>
		public static Option<T> ToNone<T>(this T value) {
			return Option<T>.None;
		}

		/// <summary>
		/// Flattens a nested option.
		/// </summary>
		public static Option<T> Flatten<T>(this Option<Option<T>> option) {
			return option.value;
		}
	}

	/// <summary>
	/// Struct representing an optional value of type T.  An option that contains the value t is called a "Some" or "Some(t)".  An empty option is called a "None". Option<T> can be thought of as a far more robust version of Nullable<T> or as an IEnumerable<T> that may contain exactly 0 or 1 element(s).
	/// 
	/// An Option<T> has two fields:
	/// 
	/// public readonly bool isSome;
	/// 
	/// public readonly T value;
	/// 
	/// Use the isSome field to determine if the option contains a value, and the value field to read the value.  Nothing prevents access to the value field of an empty option, it is up to the user to adherere to the Some / None semantics.
	/// </summary>
	[System.Serializable]
	public struct Option<T> : IComparable<Option<T>>, IEquatable<Option<T>> {
		/// <summary>
		/// A static None option for type T.
		/// </summary>
		public static readonly Option<T> None = new Option<T>();

		/// <summary>
		/// True if the option contains a value; otherwise, false.
		/// </summary>
		public readonly bool isSome;

		/// <summary>
		/// If the option isSome, the value contained by the option; otherwise, default(T).
		/// </summary>
		public readonly T value;

		/// <summary>
		/// True if the option is empty; otherwise, false.
		/// </summary>
		public bool isNone { get { return !isSome; } }

		/// <summary>
		/// Creates a Some option that contains the specified value.
		/// 
		/// Note: Use the default contructor to create a None option.
		/// </summary>
		public Option(T value) {
			this.isSome = true;
			this.value = value;
		}

		/// <summary>
		/// If the option isSome, returns the result of someFunc applied to the option's value; otherwise, returns noneValue.
		/// </summary>
		public U Cata<U>(DelegateFunc<T, U> someFunc, U noneValue) { return isSome ? someFunc(value) : noneValue; }

		/// <summary>
		/// If the option isSome, returns the result of someFunc applied to the option's value and p; otherwise, returns noneValue.
		/// </summary>
		public U Cata<U, P>(DelegateFunc<T, P, U> someFunc, P p, U noneValue) { return isSome ? someFunc(value, p) : noneValue; }

		/// <summary>
		/// If the option isSome, returns the result of someFunc applied to the option's value; otherwise, returns the result of noneFunc.
		/// </summary>
		public U Cata<U>(DelegateFunc<T, U> someFunc, DelegateFunc<U> noneFunc) { return isSome ? someFunc(value) : noneFunc(); }

		/// <summary>
		/// If the option isSome, returns the result of someFunc applied to the option's value and p; otherwise, returns the result of noneFunc.
		/// </summary>
		public U Cata<U, P>(DelegateFunc<T, P, U> someFunc, P p, DelegateFunc<U> noneFunc) { return isSome ? someFunc(value, p) : noneFunc(); }

		/// <summary>
		/// If the option isSome, returns the result of someFunc applied to the option's value and p; otherwise, returns the result of noneFunc applied to p2.
		/// </summary>
		public U Cata<U, P, P2>(DelegateFunc<T, P, U> someFunc, P p, DelegateFunc<P2, U> noneFunc, P2 p2) { return isSome ? someFunc(value, p) : noneFunc(p2); }

		/// <summary>
		/// Returns true if the option contains the specified value according to the default equality comparer; otherwise, false.
		/// </summary>
		public bool Contains(T t) { return isSome && Smooth.Collections.EqualityComparer<T>.Default.Equals(value, t); }
		
		/// <summary>
		/// Returns true if the option contains the specified value according to the specified equality comparer; otherwise, false.
		/// </summary>
		public bool Contains(T t, IEqualityComparer<T> comparer) { return isSome && comparer.Equals(value, t); }

		/// <summary>
		/// If the option isNone, invokes the specified delegate; otherwise, does nothing.
		/// </summary>
		public void IfEmpty(DelegateAction action) { if (!isSome) action(); }
		
		/// <summary>
		/// If the option isNone, invokes the specified delegate with p; otherwise, does nothing.
		/// </summary>
		public void IfEmpty<P>(DelegateAction<P> action, P p) { if (!isSome) action(p); }
		
		/// <summary>
		/// If the option isSome, invokes the specified delegate with the option's value; otherwise, does nothing.
		/// </summary>
		public void ForEach(DelegateAction<T> action) { if (isSome) action(value); }
		
		/// <summary>
		/// If the option isSome, invokes the specified delegate with the option's value and p; otherwise, does nothing.
		/// </summary>
		public void ForEach<P>(DelegateAction<T, P> action, P p) { if (isSome) action(value, p); }
		
		/// <summary>
		/// If the option isSome, invokes the someAction with the option's value; otherwise, invokes noneAction.
		/// </summary>
		public void ForEachOr(DelegateAction<T> someAction, DelegateAction noneAction) { if (isSome) someAction(value); else noneAction(); }
		
		/// <summary>
		/// If the option isSome, invokes the someAction with the option's value and p; otherwise, invokes noneAction.
		/// </summary>
		public void ForEachOr<P>(DelegateAction<T, P> someAction, P p, DelegateAction noneAction) { if (isSome) someAction(value, p); else noneAction(); }
		
		/// <summary>
		/// If the option isSome, invokes the someAction with the option's value; otherwise, invokes noneAction with p2.
		/// </summary>
		public void ForEachOr<P2>(DelegateAction<T> someAction, DelegateAction<P2> noneAction, P2 p2) { if (isSome) someAction(value); else noneAction(p2); }
		
		/// <summary>
		/// If the option isSome, invokes the someAction with the option's value and p; otherwise, invokes noneAction with p2.
		/// </summary>
		public void ForEachOr<P, P2>(DelegateAction<T, P> someAction, P p, DelegateAction<P2> noneAction, P2 p2) { if (isSome) someAction(value, p); else noneAction(p2); }
		
		/// <summary>
		/// If the option isSome, returns the option; otherwise, returns noneOption.
		/// </summary>
		public Option<T> Or(Option<T> noneOption) { return isSome ? this : noneOption; }
		
		/// <summary>
		/// If the option isSome, returns the option; otherwise, returns the result of noneFunc.
		/// </summary>
		public Option<T> Or(DelegateFunc<Option<T>> noneFunc) { return isSome ? this : noneFunc(); }
		
		/// <summary>
		/// If the option isSome, returns the option; otherwise, returns the result of noneFunc applied to p.
		/// </summary>
		public Option<T> Or<P>(DelegateFunc<P, Option<T>> noneFunc, P p) { return isSome ? this : noneFunc(p); }

		/// <summary>
		/// If the option isSome, returns an option containing the specified selector applied to the option's value; otherwise, returns an empty option.
		/// </summary>
		public Option<U> Select<U>(DelegateFunc<T, U> selector) { return isSome ? new Option<U>(selector(value)) : Option<U>.None; }

		/// <summary>
		/// If the option isSome, returns an option containing the specified selector applied to the option's value and p; otherwise, returns an empty option.
		/// </summary>
		public Option<U> Select<U, P>(DelegateFunc<T, P, U> selector, P p) { return isSome ? new Option<U>(selector(value, p)) : Option<U>.None; }

		/// <summary>
		/// If the option isSome, returns the specified selector applied to the option's value; otherwise, returns an empty option.
		/// </summary>
		public Option<U> SelectMany<U>(DelegateFunc<T, Option<U>> selector) { return isSome ? selector(value) : Option<U>.None; }
		
		/// <summary>
		/// If the option isSome, returns the specified selector applied to the option's value and p; otherwise, returns an empty option.
		/// </summary>
		public Option<U> SelectMany<U, P>(DelegateFunc<T, P, Option<U>> selector, P p) { return isSome ? selector(value, p) : Option<U>.None; }

		/// <summary>
		/// If the option isSome, returns the option's value; otherwise, returns noneValue.
		/// </summary>
		public T ValueOr(T noneValue) { return isSome ? value : noneValue; }
		
		/// <summary>
		/// If the option isSome, returns the option's value; otherwise, returns the result of noneFunc.
		/// </summary>
		public T ValueOr(DelegateFunc<T> noneFunc) { return isSome ? value : noneFunc(); }
		
		/// <summary>
		/// If the option isSome, returns the option's value; otherwise, returns the result of noneFunc applied to p.
		/// </summary>
		public T ValueOr<P>(DelegateFunc<P, T> noneFunc, P p) { return isSome ? value : noneFunc(p); }

		/// <summary>
		/// If the option isSome and the specified predicate applied to the option's value is true, returns the option; otherwise, returns an empty option.
		/// </summary>
		public Option<T> Where(DelegateFunc<T, bool> predicate) { return isSome && predicate(value) ? this : Option<T>.None; }

		/// <summary>
		/// If the option isSome and the specified predicate applied to the option's value and p is true, returns the option; otherwise, returns an empty option.
		/// </summary>
		public Option<T> Where<P>(DelegateFunc<T, P, bool> predicate, P p) { return isSome && predicate(value, p) ? this : Option<T>.None; }
		
		/// <summary>
		/// If the option isSome and the specified predicate applied to the option's value is false, returns the option; otherwise, returns an empty option.
		/// </summary>
		public Option<T> WhereNot(DelegateFunc<T, bool> predicate) { return isSome && !predicate(value) ? this : Option<T>.None; }
		
		/// <summary>
		/// If the option isSome and the specified predicate applied to the option's value and p is false, returns the option; otherwise, returns an empty option.
		/// </summary>
		public Option<T> WhereNot<P>(DelegateFunc<T, P, bool> predicate, P p) { return isSome && !predicate(value, p) ? this : Option<T>.None; }

		public override bool Equals(object o) {
			return o is Option<T> && this.Equals((Option<T>) o);
		}
		
		public bool Equals(Option<T> other) {
			return isSome ? other.Contains(value) : other.isNone;
		}
		
		public override int GetHashCode() {
			return Smooth.Collections.EqualityComparer<T>.Default.GetHashCode(value);
		}

		public int CompareTo(Option<T> other) {
			return isSome ? (other.isSome ? Smooth.Collections.Comparer<T>.Default.Compare(value, other.value) : 1) : other.isSome ? -1 : 0;
		}

		public static bool operator == (Option<T> lhs, Option<T> rhs) {
			return lhs.Equals(rhs);
		}

		public static bool operator != (Option<T> lhs, Option<T> rhs) {
			return !lhs.Equals(rhs);
		}

		public static bool operator > (Option<T> lhs, Option<T> rhs) {
			return lhs.CompareTo(rhs) > 0;
		}

		public static bool operator < (Option<T> lhs, Option<T> rhs) {
			return lhs.CompareTo(rhs) < 0;
		}

		public static bool operator >= (Option<T> lhs, Option<T> rhs) {
			return lhs.CompareTo(rhs) >= 0;
		}

		public static bool operator <= (Option<T> lhs, Option<T> rhs) {
			return lhs.CompareTo(rhs) <= 0;
		}

		public override string ToString() {
			return isSome ? "Some(" + value + ")" : "None";
		}
	}
}
