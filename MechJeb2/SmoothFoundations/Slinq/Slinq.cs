using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Comparisons;
using Smooth.Delegates;
using Smooth.Dispose;
using Smooth.Pools;
using Smooth.Slinq.Collections;
using Smooth.Slinq.Context;

namespace Smooth.Slinq {

	/// <summary>
	/// Allocation-free enumerator with advanced, LINQ-like functionality.
	///
	/// The basic operations on a Slinq are:
	/// 
	/// current, which is either a Some option containing the current value of the enumeration, or a None option if the enumeration is complete.
	/// 
	/// Skip, which moves the Slinq to the next element in the enumeration.
	/// 
	/// Remove, which removes the current element from the underlying representation and moves the Slinq to the next element in the enumeration.
	/// 
	/// Dispose, which cancels the remainder of the enumeration and releases any shared resources held by the Slinq.
	/// 
	/// If a Skip or Remove call completes the enumeration, any shared resources held by the Slinq will be released automatically.  Dispose only needs to be called when abandoning an incomplete enumeration.
	/// 
	/// Slinq<T, C> and its extension classes define many operations inspired by Linq and functional programming languages such as Scala and F#.
	/// 
	/// Slinq methods that take delegate parameter(s) come in two forms, a basic version and a version that takes an additional, user defined parameter of generic type P.  This extra parameter is passed to the delegate(s) in order to capture any state required for the operation without the need for a closure.
	/// 
	/// Note: Because Slinqs are value types, it is possible to backtrack to an earlier point in an enumeration by storing multiple copies of a Slinq.  This is not supported and will lead to unspecified behavior.
	/// 
	/// If DETECT_BACKTRACK is defined in Smooth.Slinq.Context.BacktrackDetector, backtracking will be detected and throw an exception.  This should only be used for debugging purposes as it will severely reduce performance.
	/// </summary>
	public struct Slinq<T, C> {

		#region Internal API
		
		/// <summary>
		/// Part of the internal API.
		/// </summary>
		public readonly Mutator<T, C> skip;

		/// <summary>
		/// Part of the internal API.
		/// </summary>
		public readonly Mutator<T, C> remove;

		/// <summary>
		/// Part of the internal API.
		/// </summary>
		public readonly Mutator<T, C> dispose;

		/// <summary>
		/// Part of the internal API.
		/// </summary>
		public C context;

		/// <summary>
		/// Part of the internal API.
		/// </summary>
		public Slinq(Mutator<T, C> skip, Mutator<T, C> remove, Mutator<T, C> dispose, C context) {
			this.skip = skip;
			this.remove = remove;
			this.dispose = dispose;
			this.context = context;
			
			skip(ref this.context, out current);
		}
		
		#endregion

		#region Basic operations for manual enumerations / while loops

		/// <summary>
		/// Either a Some option containing the current value of the enumeration, or a None option if the enumeration is complete.
		/// 
		/// Note: This is a public field for reasons concerning the internal API and should not be modified by user code.
		/// </summary>
		public Option<T> current;

		/// <summary>
		/// Moves the Slinq to the next element in the enumeration.  If the Slinq is empty this will have no effect.
		/// </summary>
		public void Skip() {
			if (current.isSome) {
				skip(ref context, out current);
			}
		}

		/// <summary>
		/// Moves the Slinq to the next element in the enumeration and returns the Slinq.  If the Slinq is empty this will have no effect.
		/// </summary>
		public Slinq<T, C> SkipAndReturn() {
			if (current.isSome) {
				skip(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Removes the current element from the underlying sequence and moves the Slinq to the next element in the enumeration.  If the Slinq is empty this will have no effect.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public void Remove() {
			if (current.isSome) {
				remove(ref context, out current);
			}
		}
		
		/// <summary>
		/// Removes the current element from the underlying sequence, moves the Slinq to the next element in the enumeration, and returns the Slinq.  If the Slinq is empty this will have no effect.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveAndReturn() {
			if (current.isSome) {
				remove(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Sets the current value of the Slinq to None and releases any shared resources held by the Slinq.  If the Slinq is empty this will have no effect.
		/// 
		/// Slinqs are automatically disposed when they become empty, but if you are done with a Slinq that still contains values, you must call Dispose to ensure the release and/or disposal of any resources held by the Slinq.
		/// </summary>
		public void Dispose() {
			if (current.isSome) {
				dispose(ref context, out current);
			}
		}

		#endregion

		#region Parameterized skips

		/// <summary>
		/// Enumerates the remaining elements of the Slinq.  Useful if you want to force execution of the underlying chain.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public void SkipAll() {
			while (current.isSome) {
				skip(ref context, out current);
			}
		}
		
		/// <summary>
		/// Enumerates up to the specified number of elements from the Slinq.
		/// 
		/// If count is greater than or equal to the number of elements remaining, all the remaining elements will be enumerated.
		/// 
		/// If count is less than or equal to zero, no elements will be enumerated.
		/// 
		/// After perfoming this operation, the Slinq will be positioned count elements further along the enumeration.
		/// </summary>
		public Slinq<T, C> Skip(int count) {
			while (current.isSome && count-- > 0) {
				skip(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq while the specified predicate returns true.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		public Slinq<T, C> SkipWhile(DelegateFunc<T, bool> predicate) {
			while (current.isSome && predicate(current.value)) {
				skip(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq while the specified predicate returns true.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		public Slinq<T, C> SkipWhile<P>(DelegateFunc<T, P, bool> predicate, P parameter) {
			while (current.isSome && predicate(current.value, parameter)) {
				skip(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq while the specified selector returns a Some option.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the selector returns None.
		/// </summary>
		public U SkipWhile<U>(U seed, DelegateFunc<U, T, Option<U>> selector) {
			while (current.isSome) {
				var next = selector(seed, current.value);
				if (next.isSome) {
					skip(ref context, out current);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq while the specified selector returns a Some option.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the selector returns None.
		/// </summary>
		public U SkipWhile<U, P>(U seed, DelegateFunc<U, T, P, Option<U>> selector, P parameter) {
			while (current.isSome) {
				var next = selector(seed, current.value, parameter);
				if (next.isSome) {
					skip(ref context, out current);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}

		#endregion

		#region Parameterized removes
		
		/// <summary>
		/// Enumerates the remaining elements from the Slinq, removes them from the underlying sequence, and returns the number of elements removed.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public int RemoveAll() {
			var count = 0;
			while (current.isSome) {
				remove(ref context, out current);
				++count;
			}
			return count;
		}
		
		/// <summary>
		/// Enumerates the remaining elements from the Slinq, removes them from the underlying sequence, and returns the number of elements removed.
		/// 
		/// After an element is removed the specified then action will be called with the element.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public int RemoveAll(DelegateAction<T> then) {
			var count = 0;
			while (current.isSome) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed);
				++count;
			}
			return count;
		}
		
		/// <summary>
		/// Enumerates the remaining elements from the Slinq, removes them from the underlying sequence, and returns the number of elements removed.
		/// 
		/// After an element is removed the specified then action will be called with the element and the specified then parameter.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public int RemoveAll<P>(DelegateAction<T, P> then, P thenParameter) {
			var count = 0;
			while (current.isSome) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed, thenParameter);
				++count;
			}
			return count;
		}
		
		/// <summary>
		/// Enumerates up to the specified number of elements from the Slinq and removes them from the underlying sequence.
		/// 
		/// If count is greater than or equal to the number of elements remaining, all the remaining elements will be removed.
		/// 
		/// If count is less than or equal to zero, no elements will be removed.
		/// 
		/// After perfoming this operation, the Slinq will be positioned count elements further along the enumeration.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> Remove(int count) {
			while (current.isSome && count-- > 0) {
				remove(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates up to the specified number of elements from the Slinq and removes them from the underlying sequence.
		/// 
		/// After an element is removed the specified then action will be called with the element.
		/// 
		/// If count is greater than or equal to the number of elements remaining, all the remaining elements will be removed.
		/// 
		/// If count is less than or equal to zero, no elements will be removed.
		/// 
		/// After perfoming this operation, the Slinq will be positioned count elements further along the enumeration.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> Remove(int count, DelegateAction<T> then) {
			while (current.isSome && count-- > 0) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates up to the specified number of elements from the Slinq and removes them from the underlying sequence.
		/// 
		/// After an element is removed the specified then action will be called with the element and the specified then parameter.
		/// 
		/// If count is greater than or equal to the number of elements remaining, all the remaining elements will be removed.
		/// 
		/// If count is less than or equal to zero, no elements will be removed.
		/// 
		/// After perfoming this operation, the Slinq will be positioned count elements further along the enumeration.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> Remove<P>(int count, DelegateAction<T, P> then, P thenParameter) {
			while (current.isSome && count-- > 0) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed, thenParameter);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq and removes them from the underlying sequence while the specified predicate returns true.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveWhile(DelegateFunc<T, bool> predicate) {
			while (current.isSome && predicate(current.value)) {
				remove(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq and removes them from the underlying sequence while the specified predicate returns true.
		/// 
		/// After an element is removed the specified then action will be called with the element.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveWhile(DelegateFunc<T, bool> predicate, DelegateAction<T> then) {
			while (current.isSome && predicate(current.value)) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq and removes them from the underlying sequence while the specified predicate returns true.
		/// 
		/// After an element is removed the specified then action will be called with the element and the specifed then parameter.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveWhile<P>(DelegateFunc<T, bool> predicate, DelegateAction<T, P> then, P thenParameter) {
			while (current.isSome && predicate(current.value)) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed, thenParameter);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq and removes them from the underlying sequence while the specified predicate returns true.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveWhile<P>(DelegateFunc<T, P, bool> predicate, P parameter) {
			while (current.isSome && predicate(current.value, parameter)) {
				remove(ref context, out current);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq and removes them from the underlying sequence while the specified predicate returns true.
		/// 
		/// After an element is removed the specified then action will be called with the element.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveWhile<P>(DelegateFunc<T, P, bool> predicate, P parameter, DelegateAction<T> then) {
			while (current.isSome && predicate(current.value, parameter)) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed);
			}
			return this;
		}
		
		/// <summary>
		/// Enumerates elements from the Slinq and removes them from the underlying sequence while the specified predicate returns true.
		/// 
		/// After an element is removed the specified then action will be called with the element and the specified then parameter.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public Slinq<T, C> RemoveWhile<P, P2>(DelegateFunc<T, P, bool> predicate, P parameter, DelegateAction<T, P2> then, P2 thenParameter) {
			while (current.isSome && predicate(current.value, parameter)) {
				var removed = current.value;
				remove(ref context, out current);
				then(removed, thenParameter);
			}
			return this;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq and removes them from the underlying sequence while the specified selector returns a Some option.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public U RemoveWhile<U>(U seed, DelegateFunc<U, T, Option<U>> selector) {
			while (current.isSome) {
				var next = selector(seed, current.value);
				if (next.isSome) {
					remove(ref context, out current);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq and removes them from the underlying sequence while the specified selector returns a Some option.
		/// 
		/// After an element is removed the specified then action will be called with the element.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public U RemoveWhile<U>(U seed, DelegateFunc<U, T, Option<U>> selector, DelegateAction<T> then) {
			while (current.isSome) {
				var next = selector(seed, current.value);
				if (next.isSome) {
					var removed = current.value;
					remove(ref context, out current);
					then(removed);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq and removes them from the underlying sequence while the specified selector returns a Some option.
		/// 
		/// After an element is removed the specified then action will be called with the element and the specified then parameter.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the predicate returns false.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public U RemoveWhile<U, P>(U seed, DelegateFunc<U, T, Option<U>> selector, DelegateAction<T, P> then, P thenParameter) {
			while (current.isSome) {
				var next = selector(seed, current.value);
				if (next.isSome) {
					var removed = current.value;
					remove(ref context, out current);
					then(removed, thenParameter);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq and removes them from the underlying sequence while the specified selector returns a Some option.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the selector returns None.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public U RemoveWhile<U, P>(U seed, DelegateFunc<U, T, P, Option<U>> selector, P parameter) {
			while (current.isSome) {
				var next = selector(seed, current.value, parameter);
				if (next.isSome) {
					remove(ref context, out current);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq and removes them from the underlying sequence while the specified selector returns a Some option.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the selector returns None.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public U RemoveWhile<U, P>(U seed, DelegateFunc<U, T, P, Option<U>> selector, P parameter, DelegateAction<T> then) {
			while (current.isSome) {
				var next = selector(seed, current.value, parameter);
				if (next.isSome) {
					var removed = current.value;
					remove(ref context, out current);
					then(removed);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}

		/// <summary>
		/// Aggregates elements from the Slinq and removes them from the underlying sequence while the specified selector returns a Some option.
		/// 
		/// After perfoming this operation, the Slinq will be positioned on the first element for which the selector returns None.
		/// </summary>
		/// <exception cref="NotSupportedException">The Slinq or an underlying Slinq in the chain does not support element removal.</exception>
		public U RemoveWhile<U, P, P2>(U seed, DelegateFunc<U, T, P, Option<U>> selector, P parameter, DelegateAction<T, P2> then, P2 thenParameter) {
			while (current.isSome) {
				var next = selector(seed, current.value, parameter);
				if (next.isSome) {
					var removed = current.value;
					remove(ref context, out current);
					then(removed, thenParameter);
					seed = next.value;
				} else {
					break;
				}
			}
			return seed;
		}
		
		#endregion
		
		#region Consuming operations

		/// <summary>
		/// Analog to Enumerable.Aggregate().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T Aggregate(DelegateFunc<T, T, T> selector) {
			return AggregateOrNone(selector).ValueOr(() => { throw new InvalidOperationException(); });
		}

		/// <summary>
		/// Analog to Enumerable.Aggregate(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> AggregateOrNone(DelegateFunc<T, T, T> selector) {
			if (current.isSome) {
				var acc = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					acc = selector(acc, current.value);
					skip(ref context, out current);
				}
				return new Option<T>(acc);
			} else {
				return current;
			}
		}

		/// <summary>
		/// Analog to Enumerable.Aggregate(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> AggregateOrNone<P>(DelegateFunc<T, T, P, T> selector, P parameter) {
			if (current.isSome) {
				var acc = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					acc = selector(acc, current.value, parameter);
					skip(ref context, out current);
				}
				return new Option<T>(acc);
			} else {
				return current;
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Aggregate().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public U Aggregate<U>(U seed, DelegateFunc<U, T, U> selector) {
			while (current.isSome) {
				seed = selector(seed, current.value);
				skip(ref context, out current);
			}
			return seed;
		}
		
		/// <summary>
		/// Analog to Enumerable.Aggregate().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public V Aggregate<U, V>(U seed, DelegateFunc<U, T, U> selector, DelegateFunc<U, V> resultSelector) {
			return resultSelector(Aggregate(seed, selector));
		}
		
		/// <summary>
		/// Analog to Enumerable.Aggregate().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public U Aggregate<U, P>(U seed, DelegateFunc<U, T, P, U> selector, P parameter) {
			while (current.isSome) {
				seed = selector(seed, current.value, parameter);
				skip(ref context, out current);
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq while the selector returns a Some option.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public U AggregateWhile<U>(U seed, DelegateFunc<U, T, Option<U>> selector) {
			while (current.isSome) {
				var next = selector(seed, current.value);
				if (next.isSome) {
					skip(ref context, out current);
					seed = next.value;
				} else {
					dispose(ref context, out current);
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Aggregates elements from the Slinq while the selector returns a Some option.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public U AggregateWhile<U, P>(U seed, DelegateFunc<U, T, P, Option<U>> selector, P parameter) {
			while (current.isSome) {
				var next = selector(seed, current.value, parameter);
				if (next.isSome) {
					skip(ref context, out current);
					seed = next.value;
				} else {
					dispose(ref context, out current);
					break;
				}
			}
			return seed;
		}
		
		/// <summary>
		/// Analog to Enumerable.All().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool All(DelegateFunc<T, bool> predicate) {
			while (current.isSome) {
				if (!predicate(current.value)) {
					dispose(ref context, out current);
					return false;
				}
				skip(ref context, out current);
			}
			return true;
		}
		
		/// <summary>
		/// Analog to Enumerable.All().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool All<P>(DelegateFunc<T, P, bool> predicate, P parameter) {
			while (current.isSome) {
				if (!predicate(current.value, parameter)) {
					dispose(ref context, out current);
					return false;
				}
				skip(ref context, out current);
			}
			return true;
		}
		
		/// <summary>
		/// Analog to Enumerable.Any().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool Any() {
			if (current.isSome) {
				dispose(ref context, out current);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Analog to Enumerable.Any().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool Any(DelegateFunc<T, bool> predicate) {
			while (current.isSome) {
				if (predicate(current.value)) {
					dispose(ref context, out current);
					return true;
				}
				skip(ref context, out current);
			}
			return false;
		}
		
		/// <summary>
		/// Analog to Enumerable.Any().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool Any<P>(DelegateFunc<T, P, bool> predicate, P parameter) {
			while (current.isSome) {
				if (predicate(current.value, parameter)) {
					dispose(ref context, out current);
					return true;
				}
				skip(ref context, out current);
			}
			return false;
		}

		/// <summary>
		/// Analog to Enumerable.Contains().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool Contains(T value) {
			return Contains(value, Smooth.Collections.EqualityComparer<T>.Default);
		}

		/// <summary>
		/// Analog to Enumerable.Contains().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool Contains(T value, IEqualityComparer<T> comparer) {
			while (current.isSome) {
				if (comparer.Equals(value, current.value)) {
					dispose(ref context, out current);
					return true;
				}
				skip(ref context, out current);
			}
			return false;
		}

		/// <summary>
		/// Analog to Enumerable.Count().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public int Count() {
			var count = 0;
			while (current.isSome) {
				++count;
				skip(ref context, out current);
			}
			return count;
		}
		
		/// <summary>
		/// Analog to Enumerable.ElementAt().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T ElementAt(int index) {
			return ElementAtOrNone(index).ValueOr(() => { throw new ArgumentOutOfRangeException(); });
		}

		/// <summary>
		/// Analog to Enumerable.ElementAtOrDefault().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T ElementAtOrDefault(int index) {
			return ElementAtOrNone(index).value;
		}
		
		/// <summary>
		/// Analog to Enumerable.ElementAt(), but returns an option rather than throwing an exception if the element does not exist.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> ElementAtOrNone(int index) {
			if (index > 0) {
				return Skip(index - 1).FirstOrNone();
			} else if (index == 0) {
				return FirstOrNone();
			} else {
				Dispose();
				return Option<T>.None;
			}
		}

		/// <summary>
		/// Analog to Enumerable.First().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T First() {
			return FirstOrNone().ValueOr(() => { throw new InvalidOperationException(); });
		}

		/// <summary>
		/// Analog to Enumerable.First().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T First(DelegateFunc<T, bool> predicate) {
			return FirstOrNone(predicate).ValueOr(() => { throw new InvalidOperationException(); });
		}
		
		/// <summary>
		/// Analog to Enumerable.FirstOrDefault().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T FirstOrDefault() {
			return FirstOrNone().value;
		}
		
		/// <summary>
		/// Analog to Enumerable.FirstOrDefault().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T FirstOrDefault(DelegateFunc<T, bool> predicate) {
			return FirstOrNone(predicate).value;
		}
		
		/// <summary>
		/// Analog to Enumerable.First(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> FirstOrNone() {
			if (current.isSome) {
				var first = current;
				dispose(ref context, out current);
				return first;
			} else {
				return current;
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.First(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> FirstOrNone(DelegateFunc<T, bool> predicate) {
			while (current.isSome && !predicate(current.value)) {
				skip(ref context, out current);
			}
			return FirstOrNone();
		}
		
		/// <summary>
		/// Analog to Enumerable.First(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> FirstOrNone<P>(DelegateFunc<T, P, bool> predicate, P parameter) {
			while (current.isSome && !predicate(current.value, parameter)) {
				skip(ref context, out current);
			}
			return FirstOrNone();
		}
		
		/// <summary>
		/// Performs the specified action on each remaining element in the Slinq.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public void ForEach(DelegateAction<T> action) {
			while (current.isSome) {
				action(current.value);
				skip(ref context, out current);
			}
		}
		
		/// <summary>
		/// Performs the specified action on each remaining element in the Slinq with the specified parameter.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public void ForEach<P>(DelegateAction<T, P> action, P parameter) {
			while (current.isSome) {
				action(current.value, parameter);
				skip(ref context, out current);
			}
		}

		/// <summary>
		/// Analog to Enumerable.Last().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T Last() {
			return LastOrNone().ValueOr(() => { throw new InvalidOperationException(); });
		}
		
		/// <summary>
		/// Analog to Enumerable.Last().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T Last(DelegateFunc<T, bool> predicate) {
			return LastOrNone(predicate).ValueOr(() => { throw new InvalidOperationException(); });
		}
		
		/// <summary>
		/// Analog to Enumerable.LastOrDefault().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T LastOrDefault() {
			return LastOrNone().value;
		}
		
		/// <summary>
		/// Analog to Enumerable.LastOrDefault().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T LastOrDefault(DelegateFunc<T, bool> predicate) {
			return LastOrNone(predicate).value;
		}
		
		/// <summary>
		/// Analog to Enumerable.Last(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> LastOrNone() {
			var last = Option<T>.None;
			while (current.isSome) {
				last = current;
				skip(ref context, out current);
			}
			return last;
		}
		
		/// <summary>
		/// Analog to Enumerable.Last(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> LastOrNone(DelegateFunc<T, bool> predicate) {
			var last = Option<T>.None;
			while (current.isSome) {
				if (predicate(current.value)) {
					last = current;
				}
				skip(ref context, out current);
			}
			return last;
		}
		
		/// <summary>
		/// Analog to Enumerable.Last(), but returns an option rather than throwing an exception if the Slinq is empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> LastOrNone<P>(DelegateFunc<T, P, bool> predicate, P parameter) {
			var last = Option<T>.None;
			while (current.isSome) {
				if (predicate(current.value, parameter)) {
					last = current;
				}
				skip(ref context, out current);
			}
			return last;
		}

		/// <summary>
		/// Analog to Enumerable.Max().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T Max() {
			return MaxOrNone().ValueOr(() => {
				if (typeof(T).IsClass) {
					return default(T);
				} else {
					throw new InvalidOperationException();
				}
			});
		}

		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone() {
			return MaxOrNone(Comparisons<T>.Default);
		}

		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone(IComparer<T> comparer) {
			return MaxOrNone(Comparisons<T>.ToComparison(comparer));
		}
		
		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone(Comparison<T> comparison) {
			if (current.isSome) {
				var value = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					if (comparison(value, current.value) < 0) {
						value = current.value;
					}
					skip(ref context, out current);
				}
				return new Option<T>(value);
			} else {
				return current;
			}
		}

		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone<K>(DelegateFunc<T, K> selector) {
			return MaxOrNone(selector, Comparisons<K>.Default);
		}

		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone<K>(DelegateFunc<T, K> selector, IComparer<K> comparer) {
			return MaxOrNone(selector, Comparisons<K>.ToComparison(comparer));
		}
		
		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone<K>(DelegateFunc<T, K> selector, Comparison<K> comparison) {
			if (current.isSome) {
				var key = selector(current.value);
				var value = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					var currentKey = selector(current.value);
					if (comparison(key, currentKey) < 0) {
						key = currentKey;
						value = current.value;
					}
					skip(ref context, out current);
				}
				return new Option<T>(value);
			} else {
				return current;
			}
		}

		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone<K, P>(DelegateFunc<T, P, K> selector, P parameter) {
			return MaxOrNone(selector, parameter, Comparisons<K>.Default);
		}

		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone<K, P>(DelegateFunc<T, P, K> selector, P parameter, IComparer<K> comparer) {
			return MaxOrNone(selector, parameter, Comparisons<K>.ToComparison(comparer));
		}
		
		/// <summary>
		/// Analog to Enumerable.Max(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MaxOrNone<K, P>(DelegateFunc<T, P, K> selector, P parameter, Comparison<K> comparison) {
			if (current.isSome) {
				var key = selector(current.value, parameter);
				var value = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					var currentKey = selector(current.value, parameter);
					if (comparison(key, currentKey) < 0) {
						key = currentKey;
						value = current.value;
					}
					skip(ref context, out current);
				}
				return new Option<T>(value);
			} else {
				return current;
			}
		}

		
		/// <summary>
		/// Analog to Enumerable.Min().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T Min() {
			return MinOrNone().ValueOr(() => {
				if (typeof(T).IsClass) {
					return default(T);
				} else {
					throw new InvalidOperationException();
				}
			});
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone() {
			return MinOrNone(Comparisons<T>.Default);
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> Min(IComparer<T> comparer) {
			return MinOrNone(Comparisons<T>.ToComparison(comparer));
		}
		
		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone(Comparison<T> comparison) {
			if (current.isSome) {
				var value = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					if (comparison(value, current.value) > 0) {
						value = current.value;
					}
					skip(ref context, out current);
				}
				return new Option<T>(value);
			} else {
				return current;
			}
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone<K>(DelegateFunc<T, K> selector) {
			return MinOrNone(selector, Comparisons<K>.Default);
		}
		
		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone<K>(DelegateFunc<T, K> selector, IComparer<K> comparer) {
			return MinOrNone(selector, Comparisons<K>.ToComparison(comparer));
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone<K>(DelegateFunc<T, K> selector, Comparison<K> comparison) {
			if (current.isSome) {
				var key = selector(current.value);
				var value = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					var currentKey = selector(current.value);
					if (comparison(key, currentKey) > 0) {
						key = currentKey;
						value = current.value;
					}
					skip(ref context, out current);
				}
				return new Option<T>(value);
			} else {
				return current;
			}
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone<K, P>(DelegateFunc<T, P, K> selector, P parameter) {
			return MinOrNone(selector, parameter, Comparisons<K>.Default);
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone<K, P>(DelegateFunc<T, P, K> selector, P parameter, IComparer<K> comparer) {
			return MinOrNone(selector, parameter, Comparisons<K>.ToComparison(comparer));
		}

		/// <summary>
		/// Analog to Enumerable.Min(), but returns an option rather than throwing an exception if the Slinq is empty and T is a value type.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> MinOrNone<K, P>(DelegateFunc<T, P, K> selector, P parameter, Comparison<K> comparison) {
			if (current.isSome) {
				var key = selector(current.value, parameter);
				var value = current.value;
				skip(ref context, out current);
				while (current.isSome) {
					var currentKey = selector(current.value, parameter);
					if (comparison(key, currentKey) > 0) {
						key = currentKey;
						value = current.value;
					}
					skip(ref context, out current);
				}
				return new Option<T>(value);
			} else {
				return current;
			}
		}

		/// <summary>
		/// Analog to Enumerable.SequenceEqual().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool SequenceEqual<C2>(Slinq<T, C2> other) {
			return SequenceEqual(other, Comparisons<T>.ToPredicate(Smooth.Collections.EqualityComparer<T>.Default));
		}

		/// <summary>
		/// Analog to Enumerable.SequenceEqual().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool SequenceEqual<C2>(Slinq<T, C2> other, EqualityComparer<T> equalityComparer) {
			return SequenceEqual(other, Comparisons<T>.ToPredicate(equalityComparer));
		}
		
		/// <summary>
		/// Analog to Enumerable.SequenceEqual().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public bool SequenceEqual<T2, C2>(Slinq<T2, C2> other, DelegateFunc<T, T2, bool> predicate) {
			while (current.isSome && other.current.isSome) {
				if (predicate(current.value, other.current.value)) {
					skip(ref context, out current);
					other.skip(ref other.context, out other.current);
				} else {
					dispose(ref context, out current);
					other.dispose(ref other.context, out other.current);
					return false;
				}
			}

			if (current.isSome) {
				dispose(ref context, out current);
				return false;
			} else if (other.current.isSome) {
				other.dispose(ref other.context, out other.current);
				return false;
			}

			return true;
		}
		
		/// <summary>
		/// Analog to Enumerable.Single().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T Single() {
			return SingleOrNone().ValueOr(() => { throw new InvalidOperationException(); });
		}
		
		/// <summary>
		/// Analog to Enumerable.SingleOrDefault().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public T SingleOrDefault() {
			if (current.isSome) {
				return SingleOrNone().ValueOr(() => { throw new InvalidOperationException(); });
			} else {
				return default(T);
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Single(), but returns an option rather than throwing an exception if the Slinq is empty or contains more than one element.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Option<T> SingleOrNone() {
			if (current.isSome) {
				var head = current;
				skip(ref context, out current);

				if (current.isSome) {
					dispose(ref context, out current);
					return new Option<T>();
				} else {
					return head;
				}
			} else {
				return current;
			}
		}

		/// <summary>
		/// Splits the remaining elements into a pair of lists, with the first list containing elements starting from the current position of the Slinq and the second list containing up to the last count elements from the end of the Slinq.
		/// 
		/// If count is greater than or equal to the number of elements remaining, the first list will be empty and the second list will contain all the remaining elements.
		/// 
		/// If count is less than or equal to zero, the first list will contain all the remaining elements and the second list will be empty.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Tuple<LinkedHeadTail<T>, LinkedHeadTail<T>> SplitRight(int count) {
			if (current.isSome) {
				if (count > 0) {
					var left = new LinkedHeadTail<T>();
					var right = new LinkedHeadTail<T>(current.value);
					
					skip(ref context, out current);
					
					while (current.isSome && right.count <= count) {
						right.tail.next = Linked<T>.Borrow(current.value);
						right.tail = right.tail.next;
						++right.count;
						
						skip(ref context, out current);
					}
					
					if (right.count > count) {
						left.head = right.head;
						left.tail = right.head;
						left.count = 1;
						
						right.head = right.head.next;
						--right.count;
						
						while (current.isSome) {
							right.tail.next = Linked<T>.Borrow(current.value);
							right.tail = right.tail.next;
							
							left.tail = right.head;
							right.head = right.head.next;
							++left.count;
							
							skip(ref context, out current);
						}
						left.tail.next = null;
					}
					
					return new Tuple<LinkedHeadTail<T>, LinkedHeadTail<T>>(left, right);
				} else {
					return new Tuple<LinkedHeadTail<T>, LinkedHeadTail<T>>(ToLinked(), new LinkedHeadTail<T>());
				}
			} else {
				return new Tuple<LinkedHeadTail<T>, LinkedHeadTail<T>>();
			}
		}

		#endregion

		#region Add / convert to collection

		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified collection and returns the collection.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public IC AddTo<IC>(IC collection) where IC : ICollection<T> {
			while (current.isSome) {
				collection.Add(current.value);
				skip(ref context, out current);
			}
			return collection;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified collection and returns the collection.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Disposable<IC> AddTo<IC>(Disposable<IC> collection) where IC : ICollection<T> {
			AddTo(collection.value);
			return collection;
		}

		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified collection using the specified selector and returns the collection.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public IC AddTo<U, IC>(IC collection, DelegateFunc<T, U> selector) where IC : ICollection<U> {
			while (current.isSome) {
				collection.Add(selector(current.value));
				skip(ref context, out current);
			}
			return collection;
		}

		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified collection using the specified selector and returns the collection.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Disposable<IC> AddTo<U, IC>(Disposable<IC> collection, DelegateFunc<T, U> selector) where IC : ICollection<U> {
			AddTo(collection.value, selector);
			return collection;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified collection using the specified selector and returns the collection.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public IC AddTo<U, IC, P>(IC collection, DelegateFunc<T, P, U> selector, P parameter) where IC : ICollection<U> {
			while (current.isSome) {
				collection.Add(selector(current.value, parameter));
				skip(ref context, out current);
			}
			return collection;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified collection using the specified selector and returns the collection.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Disposable<IC> AddTo<U, IC, P>(Disposable<IC> collection, DelegateFunc<T, P, U> selector, P parameter) where IC : ICollection<U> {
			AddTo(collection.value, selector, parameter);
			return collection;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified LinkedHeadTail<T> and returns the result.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<T> AddTo(LinkedHeadTail<T> list) {
			if (current.isSome) {
				list.Append(current.value);
				skip(ref context, out current);
				
				while (current.isSome) {
					list.tail.next = Linked<T>.Borrow(current.value);
					list.tail = list.tail.next;
					++list.count;
					skip(ref context, out current);
				}
			}
			return list;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified LinkedHeadTail<T> in reverse order and returns the result.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<T> AddToReverse(LinkedHeadTail<T> list) {
			if (current.isSome) {
				var ht = new LinkedHeadTail<T>(current.value);
				skip(ref context, out current);
				
				while (current.isSome) {
					var node = Linked<T>.Borrow(current.value);
					node.next = ht.head;
					ht.head = node;
					++ht.count;
					skip(ref context, out current);
				}
				
				list.Append(ht);
				return list;
			} else {
				return list;
			}
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified LinkedHeadTail<K, T> using the specified key selector and returns the result.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> AddTo<K>(LinkedHeadTail<K, T> list, DelegateFunc<T, K> selector) {
			if (current.isSome) {
				list.Append(selector(current.value), current.value);
				skip(ref context, out current);
				
				while (current.isSome) {
					list.tail.next = Linked<K, T>.Borrow(selector(current.value), current.value);
					list.tail = list.tail.next;
					++list.count;
					skip(ref context, out current);
				}
			}
			return list;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified LinkedHeadTail<K, T> using the specified key selector and returns the result.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> AddTo<K, P>(LinkedHeadTail<K, T> list, DelegateFunc<T, P, K> selector, P parameter) {
			if (current.isSome) {
				list.Append(selector(current.value, parameter), current.value);
				skip(ref context, out current);
				
				while (current.isSome) {
					list.tail.next = Linked<K, T>.Borrow(selector(current.value, parameter), current.value);
					list.tail = list.tail.next;
					++list.count;
					skip(ref context, out current);
				}
			}
			return list;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified LinkedHeadTail<K, T> in reverse order using the specified key selector and returns the result.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> AddToReverse<K>(LinkedHeadTail<K, T> list, DelegateFunc<T, K> selector) {
			if (current.isSome) {
				var ht = new LinkedHeadTail<K, T>(selector(current.value), current.value);
				skip(ref context, out current);
				
				while (current.isSome) {
					var node = Linked<K, T>.Borrow(selector(current.value), current.value);
					node.next = ht.head;
					ht.head = node;
					++ht.count;
					skip(ref context, out current);
				}
				
				list.Append(ht);
				return list;
			} else {
				return list;
			}
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified LinkedHeadTail<K, T> in reverse order using the specified key selector and returns the result.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> AddToReverse<K, P>(LinkedHeadTail<K, T> list, DelegateFunc<T, P, K> selector, P parameter) {
			if (current.isSome) {
				var ht = new LinkedHeadTail<K, T>(selector(current.value, parameter), current.value);
				skip(ref context, out current);
				
				while (current.isSome) {
					var node = Linked<K, T>.Borrow(selector(current.value, parameter), current.value);
					node.next = ht.head;
					ht.head = node;
					++ht.count;
					skip(ref context, out current);
				}
				
				list.Append(ht);
				return list;
			} else {
				return list;
			}
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified Lookup<K, T> using the specified key selector and returns the lookup.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Lookup<K, T> AddTo<K>(Lookup<K, T> lookup, DelegateFunc<T, K> selector) {
			while (current.isSome) {
				lookup.Add(selector(current.value), current.value);
				skip(ref context, out current);
			}
			return lookup;
		}
		
		/// <summary>
		/// Adds the remaining elements in the Slinq to the specified Lookup<K, T> using the specified key selector and returns the lookup.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Lookup<K, T> AddTo<K, P>(Lookup<K, T> lookup, DelegateFunc<T, P, K> selector, P parameter) {
			while (current.isSome) {
				lookup.Add(selector(current.value, parameter), current.value);
				skip(ref context, out current);
			}
			return lookup;
		}
		
		/// <summary>
		/// Converts the Slinq into a singly linked list.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<T> ToLinked() {
			return AddTo(new LinkedHeadTail<T>());
		}
		
		/// <summary>
		/// Converts the Slinq into an order-reversed singly linked list.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<T> ToLinkedReverse() {
			return AddToReverse(new LinkedHeadTail<T>());
		}
		
		/// <summary>
		/// Converts the Slinq into a singly linked key, value list using the specified key selector.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> ToLinked<K>(DelegateFunc<T, K> selector) {
			return AddTo(new LinkedHeadTail<K, T>(), selector);
		}

		/// <summary>
		/// Converts the Slinq into a singly linked key, value list using the specified key selector.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> ToLinked<K, P>(DelegateFunc<T, P, K> selector, P parameter) {
			return AddTo(new LinkedHeadTail<K, T>(), selector, parameter);
		}
		
		/// <summary>
		/// Converts the Slinq into an order-reversed singly linked key, value list using the specified key selector.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> ToLinkedReverse<K>(DelegateFunc<T, K> selector) {
			return AddToReverse(new LinkedHeadTail<K, T>(), selector);
		}
		
		/// <summary>
		/// Converts the Slinq into an order-reversed singly linked key, value list using the specified key selector.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public LinkedHeadTail<K, T> ToLinkedReverse<K, P>(DelegateFunc<T, P, K> selector, P parameter) {
			return AddToReverse(new LinkedHeadTail<K, T>(), selector, parameter);
		}
		
		/// <summary>
		/// Converts the Slinq into a lookup using the specified key selector.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Lookup<K, T> ToLookup<K>(DelegateFunc<T, K> selector) {
			return AddTo(Lookup<K, T>.Borrow(Smooth.Collections.EqualityComparer<K>.Default), selector);
		}
		
		/// <summary>
		/// Converts the Slinq into a lookup using the specified key selector and equality comparer.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Lookup<K, T> ToLookup<K>(DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return AddTo(Lookup<K, T>.Borrow(comparer), selector);
		}
		
		/// <summary>
		/// Converts the Slinq into a lookup using the specified key selector and equality comparer.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Lookup<K, T> ToLookup<K, P>(DelegateFunc<T, P, K> selector, P parameter) {
			return AddTo(Lookup<K, T>.Borrow(Smooth.Collections.EqualityComparer<K>.Default), selector, parameter);
		}
		
		/// <summary>
		/// Converts the Slinq into a lookup using the specified key selector and equality comparer.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public Lookup<K, T> ToLookup<K, P>(DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return AddTo(Lookup<K, T>.Borrow(comparer), selector, parameter);
		}
		
		/// <summary>
		/// Converts the Slinq into a List<T> borrowed from ListPool<T>.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public List<T> ToList() {
			return AddTo(ListPool<T>.Instance.Borrow());
		}

		#endregion

	}

	/// <summary>
	/// Provides methods for the creation of chained Slinqs as well as various type specific Slinq comprehensions.
	/// </summary>
	public static class Slinq {

		#region Chained Slinqs
		
		/// <summary>
		/// Returns a chained Slinq that performs a running aggegatation over the specified Slinq.
		/// </summary>
		public static Slinq<U, AggregateContext<U, T, C>> AggregateRunning<U, T, C>(this Slinq<T, C> slinq, U seed, DelegateFunc<U, T, U> selector) {
			return AggregateContext<U, T, C>.AggregateRunning(slinq, seed, selector);
		}
		
		/// <summary>
		/// Returns a chained Slinq that performs a running aggegatation over the specified Slinq.
		/// </summary>
		public static Slinq<U, AggregateContext<U, T, C, P>> AggregateRunning<U, T, C, P>(this Slinq<T, C> slinq, U seed, DelegateFunc<U, T, P, U> selector, P parameter) {
			return AggregateContext<U, T, C, P>.AggregateRunning(slinq, seed, selector, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.Concat().
		/// </summary>
		public static Slinq<T, ConcatContext<C2, T, C>> Concat<C2, T, C>(this Slinq<T, C> first, Slinq<T, C2> second) {
			return ConcatContext<C2, T, C>.Concat(first, second);
		}
		
		/// <summary>
		/// Analog to Enumerable.DefaultIfEmpty().
		/// </summary>
		public static Slinq<T, EitherContext<OptionContext<T>, T, C>> DefaultIfEmpty<T, C>(this Slinq<T, C> slinq) {
			return slinq.current.isSome ?
				EitherContext<OptionContext<T>, T, C>.Left(slinq) :
					EitherContext<OptionContext<T>, T, C>.Right(OptionContext<T>.Slinq(new Option<T>(default(T))));
		}

		/// <summary>
		/// Analog to Enumerable.DefaultIfEmpty().
		/// </summary>
		public static Slinq<T, EitherContext<OptionContext<T>, T, C>> DefaultIfEmpty<T, C>(this Slinq<T, C> slinq, T defaultValue) {
			return slinq.current.isSome ?
				EitherContext<OptionContext<T>, T, C>.Left(slinq) :
					EitherContext<OptionContext<T>, T, C>.Right(OptionContext<T>.Slinq(new Option<T>(defaultValue)));
		}

		/// <summary>
		/// Analog to Enumerable.Distinct().
		/// </summary>
		public static Slinq<T, HashSetContext<T, C>> Distinct<T, C>(this Slinq<T, C> slinq) {
			return HashSetContext<T, C>.Distinct(slinq, HashSetPool<T>.Instance.BorrowDisposable(), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Distinct().
		/// </summary>
		public static Slinq<T, HashSetContext<T, C>> Distinct<T, C>(this Slinq<T, C> slinq, IEqualityComparer<T> comparer) {
			return HashSetContext<T, C>.Distinct(slinq, HashSetPool<T>.Instance.BorrowDisposable(comparer), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Distinct().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C>> Distinct<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector) {
			return HashSetContext<K, T, C>.Distinct(slinq, selector, HashSetPool<K>.Instance.BorrowDisposable(), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Distinct().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C>> Distinct<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return HashSetContext<K, T, C>.Distinct(slinq, selector, HashSetPool<K>.Instance.BorrowDisposable(comparer), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Distinct().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C, P>> Distinct<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter) {
			return HashSetContext<K, T, C, P>.Distinct(slinq, selector, parameter, HashSetPool<K>.Instance.BorrowDisposable(), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Distinct().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C, P>> Distinct<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return HashSetContext<K, T, C, P>.Distinct(slinq, selector, parameter, HashSetPool<K>.Instance.BorrowDisposable(comparer), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Except().
		/// </summary>
		public static Slinq<T, HashSetContext<T, C>> Except<C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other) {
			return HashSetContext<T, C>.Except(slinq, other.AddTo(HashSetPool<T>.Instance.BorrowDisposable()), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Except().
		/// </summary>
		public static Slinq<T, HashSetContext<T, C>> Except<C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, IEqualityComparer<T> comparer) {
			return HashSetContext<T, C>.Except(slinq, other.AddTo(HashSetPool<T>.Instance.BorrowDisposable(comparer)), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Except().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C>> Except<K, C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, K> selector) {
			return HashSetContext<K, T, C>.Except(slinq, selector, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(), selector), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Except().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C>> Except<K, C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return HashSetContext<K, T, C>.Except(slinq, selector, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(comparer), selector), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Except().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C, P>> Except<K, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, P, K> selector, P parameter) {
			return HashSetContext<K, T, C, P>.Except(slinq, selector, parameter, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(), selector, parameter), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Except().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C, P>> Except<K, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return HashSetContext<K, T, C, P>.Except(slinq, selector, parameter, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(comparer), selector, parameter), true);
		}

		/// <summary>
		/// Returns a chained Slinq that enumerates over each of the nested elements in the specified Slinq.
		/// 
		/// See: SelectMany().
		/// </summary>
		public static Slinq<T, FlattenContext<T, C1, C2>> Flatten<T, C1, C2>(this Slinq<Slinq<T, C1>, C2> slinq) {
			return FlattenContext<T, C1, C2>.Flatten(slinq);
		}

		/// <summary>
		/// Returns a chained Slinq that enumerates over each of the nested elements in the specified Slinq.
		/// 
		/// See: SelectMany().
		/// </summary>
		public static Slinq<T, FlattenContext<T, C>> Flatten<T, C>(this Slinq<Option<T>, C> slinq) {
			return FlattenContext<T, C>.Flatten(slinq);
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupBy().
		/// </summary>
		public static Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> GroupBy<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector) {
			return slinq.ToLookup(selector, Smooth.Collections.EqualityComparer<K>.Default).SlinqAndDispose();
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupBy().
		/// </summary>
		public static Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> GroupBy<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return slinq.ToLookup(selector, comparer).SlinqAndDispose();
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupBy().
		/// </summary>
		public static Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> GroupBy<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter) {
			return slinq.ToLookup(selector, parameter, Smooth.Collections.EqualityComparer<K>.Default).SlinqAndDispose();
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupBy().
		/// </summary>
		public static Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> GroupBy<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return slinq.ToLookup(selector, parameter, comparer).SlinqAndDispose();
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupJoin(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, GroupJoinContext<U, K, T2, T, C>> GroupJoin<U, K, T2, C2, T, C>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, K> outerSelector, DelegateFunc<T2, K> innerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, U> resultSelector) {
			return inner.ToLookup(innerSelector, Smooth.Collections.EqualityComparer<K>.Default).GroupJoinAndDispose(outer, outerSelector, resultSelector);
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupJoin(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, GroupJoinContext<U, K, T2, T, C>> GroupJoin<U, K, T2, C2, T, C>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, K> outerSelector, DelegateFunc<T2, K> innerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, U> resultSelector, IEqualityComparer<K> comparer) {
			return inner.ToLookup(innerSelector, comparer).GroupJoinAndDispose(outer, outerSelector, resultSelector);
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupJoin(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, GroupJoinContext<U, K, T2, T, C, P>> GroupJoin<U, K, T2, C2, T, C, P>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, P, K> outerSelector, DelegateFunc<T2, P, K> innerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, P, U> resultSelector, P parameter) {
			return inner.ToLookup(innerSelector, parameter, Smooth.Collections.EqualityComparer<K>.Default).GroupJoinAndDispose(outer, outerSelector, resultSelector, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.GroupJoin(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, GroupJoinContext<U, K, T2, T, C, P>> GroupJoin<U, K, T2, C2, T, C, P>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, P, K> outerSelector, DelegateFunc<T2, P, K> innerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, P, U> resultSelector, P parameter, IEqualityComparer<K> comparer) {
			return inner.ToLookup(innerSelector, parameter, comparer).GroupJoinAndDispose(outer, outerSelector, resultSelector, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.Intersect().
		/// </summary>
		public static Slinq<T, HashSetContext<T, C>> Intersect<C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other) {
			return HashSetContext<T, C>.Intersect(slinq, other.AddTo(HashSetPool<T>.Instance.BorrowDisposable()), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Intersect().
		/// </summary>
		public static Slinq<T, HashSetContext<T, C>> Intersect<C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, IEqualityComparer<T> comparer) {
			return HashSetContext<T, C>.Intersect(slinq, other.AddTo(HashSetPool<T>.Instance.BorrowDisposable(comparer)), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Intersect().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C>> Intersect<K, C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, K> selector) {
			return HashSetContext<K, T, C>.Intersect(slinq, selector, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(), selector), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Intersect().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C>> Intersect<K, C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return HashSetContext<K, T, C>.Intersect(slinq, selector, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(comparer), selector), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Intersect().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C, P>> Intersect<K, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, P, K> selector, P parameter) {
			return HashSetContext<K, T, C, P>.Intersect(slinq, selector, parameter, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(), selector, parameter), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.Intersect().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, C, P>> Intersect<K, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return HashSetContext<K, T, C, P>.Intersect(slinq, selector, parameter, other.AddTo(HashSetPool<K>.Instance.BorrowDisposable(comparer), selector, parameter), true);
		}

		/// <summary>
		/// Analog to Enumerable.Join(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, JoinContext<U, K, T2, T, C>> Join<U, K, T2, C2, T, C>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, K> outerSelector, DelegateFunc<T2, K> innerSelector, DelegateFunc<T, T2, U> resultSelector) {
			return inner.ToLookup(innerSelector, Smooth.Collections.EqualityComparer<K>.Default).JoinAndDispose(outer, outerSelector, resultSelector);
		}
		
		/// <summary>
		/// Analog to Enumerable.Join(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, JoinContext<U, K, T2, T, C>> Join<U, K, T2, C2, T, C>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, K> outerSelector, DelegateFunc<T2, K> innerSelector, DelegateFunc<T, T2, U> resultSelector, IEqualityComparer<K> comparer) {
			return inner.ToLookup(innerSelector, comparer).JoinAndDispose(outer, outerSelector, resultSelector);
		}
		
		/// <summary>
		/// Analog to Enumerable.Join(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, JoinContext<U, K, T2, T, C, P>> Join<U, K, T2, C2, T, C, P>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, P, K> outerSelector, DelegateFunc<T2, P, K> innerSelector, DelegateFunc<T, T2, P, U> resultSelector, P parameter) {
			return inner.ToLookup(innerSelector, parameter, Smooth.Collections.EqualityComparer<K>.Default).JoinAndDispose(outer, outerSelector, resultSelector, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.Join(), with removal operations chained to the outer Slinq.
		/// </summary>
		public static Slinq<U, JoinContext<U, K, T2, T, C, P>> Join<U, K, T2, C2, T, C, P>(this Slinq<T, C> outer, Slinq<T2, C2> inner, DelegateFunc<T, P, K> outerSelector, DelegateFunc<T2, P, K> innerSelector, DelegateFunc<T, T2, P, U> resultSelector, P parameter, IEqualityComparer<K> comparer) {
			return inner.ToLookup(innerSelector, parameter, comparer).JoinAndDispose(outer, outerSelector, resultSelector, parameter);
		}

		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderBy<T, C>(this Slinq<T, C> slinq) {
			return OrderBy(slinq, Comparisons<T>.ToComparison(Smooth.Collections.Comparer<T>.Default), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderBy<T, C>(this Slinq<T, C> slinq, IComparer<T> comparer) {
			return OrderBy(slinq, Comparisons<T>.ToComparison(comparer), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderBy<T, C>(this Slinq<T, C> slinq, Comparison<T> comparison) {
			return OrderBy(slinq, comparison, true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByDescending<T, C>(this Slinq<T, C> slinq) {
			return OrderBy(slinq, Comparisons<T>.ToComparison(Smooth.Collections.Comparer<T>.Default), false);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByDescending<T, C>(this Slinq<T, C> slinq, IComparer<T> comparer) {
			return OrderBy(slinq, Comparisons<T>.ToComparison(comparer), false);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByDescending<T, C>(this Slinq<T, C> slinq, Comparison<T> comparison) {
			return OrderBy(slinq, comparison, false);
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq as ordered by the specified key selector, comparison, and ordering.
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderBy<T, C>(this Slinq<T, C> slinq, Comparison<T> comparison, bool ascending) {
			return slinq.current.isSome ?
				Linked.Sort(slinq.ToLinked(), comparison, ascending).SlinqAndDispose() :
					new Slinq<T, LinkedContext<T>>();
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector) {
			return OrderBy(slinq, selector, Comparisons<K>.ToComparison(Smooth.Collections.Comparer<K>.Default), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IComparer<K> comparer) {
			return OrderBy(slinq, selector, Comparisons<K>.ToComparison(comparer), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, Comparison<K> comparison) {
			return OrderBy(slinq, selector, comparison, true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderByDescending<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector) {
			return OrderBy(slinq, selector, Comparisons<K>.ToComparison(Smooth.Collections.Comparer<K>.Default), false);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderByDescending<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IComparer<K> comparer) {
			return OrderBy(slinq, selector, Comparisons<K>.ToComparison(comparer), false);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderByDescending<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, Comparison<K> comparison) {
			return OrderBy(slinq, selector, comparison, false);
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq as ordered by the specified key selector, comparison, and ordering.
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, Comparison<K> comparison, bool ascending) {
			return slinq.current.isSome ?
				Linked.Sort(slinq.ToLinked(selector), comparison, ascending).SlinqAndDispose() :
					new Slinq<T, LinkedContext<K, T>>();
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter) {
			return OrderBy(slinq, selector, parameter, Comparisons<K>.ToComparison(Smooth.Collections.Comparer<K>.Default), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IComparer<K> comparer) {
			return OrderBy(slinq, selector, parameter, Comparisons<K>.ToComparison(comparer), true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderBy().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, Comparison<K> comparison) {
			return OrderBy(slinq, selector, parameter, comparison, true);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderByDescending<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter) {
			return OrderBy(slinq, selector, parameter, Comparisons<K>.ToComparison(Smooth.Collections.Comparer<K>.Default), false);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderByDescending<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IComparer<K> comparer) {
			return OrderBy(slinq, selector, parameter, Comparisons<K>.ToComparison(comparer), false);
		}
		
		/// <summary>
		/// Analog to Enumerable.OrderByDescending().
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderByDescending<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, Comparison<K> comparison) {
			return OrderBy(slinq, selector, parameter, comparison, false);
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq as ordered by the specified key selector, comparison, and ordering.
		/// 
		/// This method uses a linked list merge sort algorithm and has O(n) space complexity and O(n log n) average and worst case time complexity.
		/// 
		/// Note: The Slinq API does not provide methods for ThenBy() orderings, to sort by multiple values in succession you should supply a composite key and/or comparision.
		/// </summary>
		public static Slinq<T, LinkedContext<K, T>> OrderBy<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, Comparison<K> comparison, bool ascending) {
			return slinq.current.isSome ?
				Linked.Sort(slinq.ToLinked(selector, parameter), comparison, ascending).SlinqAndDispose() :
					new Slinq<T, LinkedContext<K, T>>();
		}

		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and the default equality comparer for K, with the groups ordered by the default sort order comparer for K.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroup<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector) {
			return slinq.ToLookup(selector, Smooth.Collections.EqualityComparer<K>.Default).SortKeys(Comparisons<K>.Default, true).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered by the specified sort order comparer.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroup<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IEqualityComparer<K> equalityComparer, IComparer<K> comparer) {
			return slinq.ToLookup(selector, equalityComparer).SortKeys(Comparisons<K>.ToComparison(comparer), true).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered by the specified comparison.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroup<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IEqualityComparer<K> equalityComparer, Comparison<K> comparison) {
			return slinq.ToLookup(selector, equalityComparer).SortKeys(comparison, true).FlattenAndDispose().SlinqAndDispose();
		}

		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and the default equality comparer for K, with the groups ordered descendingly by the default sort order comparer for K.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroupDescending<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector) {
			return slinq.ToLookup(selector, Smooth.Collections.EqualityComparer<K>.Default).SortKeys(Comparisons<K>.Default, false).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered descendingly by the specified sort order comparer.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroupDescending<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IEqualityComparer<K> equalityComparer, IComparer<K> comparer) {
			return slinq.ToLookup(selector, equalityComparer).SortKeys(Comparisons<K>.ToComparison(comparer), false).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered descendingly by the specified comparison.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroupDescending<K, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, K> selector, IEqualityComparer<K> equalityComparer, Comparison<K> comparison) {
			return slinq.ToLookup(selector, equalityComparer).SortKeys(comparison, false).FlattenAndDispose().SlinqAndDispose();
		}

		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and the default equality comparer for K, with the groups ordered by the default sort order comparer for K.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroup<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter) {
			return slinq.ToLookup(selector, parameter, Smooth.Collections.EqualityComparer<K>.Default).SortKeys(Comparisons<K>.Default, true).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered by the specified sort order comparer.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroup<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> equalityComparer, IComparer<K> comparer) {
			return slinq.ToLookup(selector, parameter, equalityComparer).SortKeys(Comparisons<K>.ToComparison(comparer), true).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered by the specified comparison.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroup<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> equalityComparer, Comparison<K> comparison) {
			return slinq.ToLookup(selector, parameter, equalityComparer).SortKeys(comparison, true).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and the default equality comparer for K, with the groups ordered descendingly by the default sort order comparer for K.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroupDescending<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter) {
			return slinq.ToLookup(selector, parameter, Smooth.Collections.EqualityComparer<K>.Default).SortKeys(Comparisons<K>.Default, false).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered descendingly by the specified sort order comparer.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroupDescending<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> equalityComparer, IComparer<K> comparer) {
			return slinq.ToLookup(selector, parameter, equalityComparer).SortKeys(Comparisons<K>.ToComparison(comparer), false).FlattenAndDispose().SlinqAndDispose();
		}
		
		/// <summary>
		/// Returns a chained Slinq that enumerates the elements of the specified Slinq ordered by grouping the elements according to the specified key selector and equality comparer, with the groups ordered descendingly by the specified comparison.
		/// 
		/// This method has O(n + k) space compexity and O(n + k log k) time complexity where n is the number of elements and k is the number of keys.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> OrderByGroupDescending<K, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> equalityComparer, Comparison<K> comparison) {
			return slinq.ToLookup(selector, parameter, equalityComparer).SortKeys(comparison, false).FlattenAndDispose().SlinqAndDispose();
		}

		/// <summary>
		/// Analog to Enumerable.Reverse().
		/// </summary>
		public static Slinq<T, LinkedContext<T>> Reverse<T, C>(this Slinq<T, C> slinq) {
			return slinq.current.isSome ?
				slinq.ToLinkedReverse().SlinqAndDispose() :
					new Slinq<T, LinkedContext<T>>();
		}
		
		/// <summary>
		/// Analog to Enumerable.Select().
		/// </summary>
		public static Slinq<U, SelectContext<U, T, C>> Select<U, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, U> selector) {
			return SelectContext<U, T, C>.Select(slinq, selector);
		}
		
		/// <summary>
		/// Analog to Enumerable.Select().
		/// </summary>
		public static Slinq<U, SelectContext<U, T, C, P>> Select<U, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, U> selector, P parameter) {
			return SelectContext<U, T, C, P>.Select(slinq, selector, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.SelectMany().
		/// </summary>
		public static Slinq<U, SelectSlinqContext<U, UC, T, C>> SelectMany<U, UC, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, Slinq<U, UC>> selector) {
			return SelectSlinqContext<U, UC, T, C>.SelectMany(slinq, selector);
		}
		
		/// <summary>
		/// Analog to Enumerable.SelectMany().
		/// </summary>
		public static Slinq<U, SelectSlinqContext<U, UC, T, C, P>> SelectMany<U, UC, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, Slinq<U, UC>> selector, P parameter) {
			return SelectSlinqContext<U, UC, T, C, P>.SelectMany(slinq, selector, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.SelectMany().
		/// </summary>
		public static Slinq<U, SelectOptionContext<U, T, C>> SelectMany<U, T, C>(this Slinq<T, C> slinq, DelegateFunc<T, Option<U>> selector) {
			return SelectOptionContext<U, T, C>.SelectMany(slinq, selector);
		}
		
		/// <summary>
		/// Analog to Enumerable.SelectMany().
		/// </summary>
		public static Slinq<U, SelectOptionContext<U, T, C, P>> SelectMany<U, T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, Option<U>> selector, P parameter) {
			return SelectOptionContext<U, T, C, P>.SelectMany(slinq, selector, parameter);
		}

		/// <summary>
		/// Analog to Enumerable.Take().
		/// </summary>
		public static Slinq<T, IntContext<T, C>> Take<T, C>(this Slinq<T, C> slinq, int count) {
			return IntContext<T, C>.Take(slinq, count);
		}
		
		/// <summary>
		/// Returns a chained Slinq that contains up to count elements from the end of the specified Slinq.
		/// 
		/// If count is greater than or equal to the number of elements remaining, the returned Slinq will contain all the remaining elements.
		/// 
		/// If count is less than or equal to zero, the returned Slinq will be empty.
		/// </summary>
		public static Slinq<T, LinkedContext<T>> TakeRight<T, C>(this Slinq<T, C> slinq, int count) {
			if (slinq.current.isSome) {
				var split = slinq.SplitRight(count);
				split._1.DisposeInBackground();
				return split._2.SlinqAndDispose();
			} else {
				return new Smooth.Slinq.Slinq<T, LinkedContext<T>>();
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.TakeWhile().
		/// </summary>
		public static Slinq<T, PredicateContext<T, C>> TakeWhile<T, C>(this Slinq<T, C> slinq, DelegateFunc<T, bool> predicate) {
			return PredicateContext<T, C>.TakeWhile(slinq, predicate);
		}
		
		/// <summary>
		/// Analog to Enumerable.TakeWhile().
		/// </summary>
		public static Slinq<T, PredicateContext<T, C, P>> TakeWhile<T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, bool> predicate, P parameter) {
			return PredicateContext<T, C, P>.TakeWhile(slinq, predicate, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.Where().
		/// </summary>
		public static Slinq<T, PredicateContext<T, C>> Where<T, C>(this Slinq<T, C> slinq, DelegateFunc<T, bool> predicate) {
			return PredicateContext<T, C>.Where(slinq, predicate);
		}
		
		/// <summary>
		/// Analog to Enumerable.Where().
		/// </summary>
		public static Slinq<T, PredicateContext<T, C, P>> Where<T, C, P>(this Slinq<T, C> slinq, DelegateFunc<T, P, bool> predicate, P parameter) {
			return PredicateContext<T, C, P>.Where(slinq, predicate, parameter);
		}
		
		/// <summary>
		/// Analog to Enumerable.Union().
		/// </summary>
		public static Slinq<T, HashSetContext<T, ConcatContext<C2, T, C>>> Union<C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other) {
			return slinq.Concat(other).Distinct(Smooth.Collections.EqualityComparer<T>.Default);
		}
		
		/// <summary>
		/// Analog to Enumerable.Union().
		/// </summary>
		public static Slinq<T, HashSetContext<T, ConcatContext<C2, T, C>>> Union<C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, IEqualityComparer<T> comparer) {
			return slinq.Concat(other).Distinct(comparer);
		}
		
		/// <summary>
		/// Analog to Enumerable.Union().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, ConcatContext<C2, T, C>>> Union<K, C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, K> selector) {
			return slinq.Concat(other).Distinct(selector, Smooth.Collections.EqualityComparer<K>.Default);
		}
		
		/// <summary>
		/// Analog to Enumerable.Union().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, ConcatContext<C2, T, C>>> Union<K, C2, T, C>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return slinq.Concat(other).Distinct(selector, comparer);
		}
		
		/// <summary>
		/// Analog to Enumerable.Union().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, ConcatContext<C2, T, C>, P>> Union<K, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, P, K> selector, P parameter) {
			return slinq.Concat(other).Distinct(selector, parameter, Smooth.Collections.EqualityComparer<K>.Default);
		}
		
		/// <summary>
		/// Analog to Enumerable.Union().
		/// </summary>
		public static Slinq<T, HashSetContext<K, T, ConcatContext<C2, T, C>, P>> Union<K, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T, C2> other, DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return slinq.Concat(other).Distinct(selector, parameter, comparer);
		}
		
		/// <summary>
		/// Analog to Enumerable.Zip() that combines elements into tuples and chains removal operations to the left Slinq.
		/// </summary>
		public static Slinq<Tuple<T, T2>, ZipContext<T2, C2, T, C>> Zip<T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with) {
			return ZipContext<T2, C2, T, C>.Zip(slinq, with, ZipRemoveFlags.Left);
		}
		
		/// <summary>
		/// Analog to Enumerable.Zip() that combines elements into tuples and chains removal operations to the the specified Slinq(s).
		/// </summary>
		public static Slinq<Tuple<T, T2>, ZipContext<T2, C2, T, C>> Zip<T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with, ZipRemoveFlags removeFlags) {
			return ZipContext<T2, C2, T, C>.Zip(slinq, with, removeFlags);
		}

		/// <summary>
		/// Analog to Enumerable.Zip() that chains removal operations to the left Slinq.
		/// </summary>
		public static Slinq<U, ZipContext<U, T2, C2, T, C>> Zip<U, T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<T, T2, U> selector) {
			return ZipContext<U, T2, C2, T, C>.Zip(slinq, with, selector, ZipRemoveFlags.Left);
		}
		
		/// <summary>
		/// Analog to Enumerable.Zip() that chains removal operations to the specified Slinq(s).
		/// </summary>
		public static Slinq<U, ZipContext<U, T2, C2, T, C>> Zip<U, T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<T, T2, U> selector, ZipRemoveFlags removeFlags) {
			return ZipContext<U, T2, C2, T, C>.Zip(slinq, with, selector, removeFlags);
		}

		/// <summary>
		/// Analog to Enumerable.Zip() that chains removal operations to the left Slinq.
		/// </summary>
		public static Slinq<U, ZipContext<U, T2, C2, T, C, P>> Zip<U, T2, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<T, T2, P, U> selector, P parameter) {
			return ZipContext<U, T2, C2, T, C, P>.Zip(slinq, with, selector, parameter, ZipRemoveFlags.Left);
		}
		
		/// <summary>
		/// Analog to Enumerable.Zip() that chains removal operations to the specified Slinq(s).
		/// </summary>
		public static Slinq<U, ZipContext<U, T2, C2, T, C, P>> Zip<U, T2, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<T, T2, P, U> selector, P parameter, ZipRemoveFlags removeFlags) {
			return ZipContext<U, T2, C2, T, C, P>.Zip(slinq, with, selector, parameter, removeFlags);
		}

		/// <summary>
		/// Returns a Slinq that combines the corresponding elements from the supplied Slinqs into tuples and chains removal operations to the left Slinq.
		/// 
		/// The returned Slinq will continue enumerating until it reaches the end of the longer of the supplied Slinqs.
		/// 
		/// While a Slinq has elements remaining, the enumerated tuples will contain Some options containing the corresponding elements from the Slinq in the corresponding position.
		///
		/// If either of the Slinqs is empty while the other still has elements remaining, the enumerated tuples will contain None options in place of the shorter Slinq's element until the enumeration is complete.
		/// 
		/// Removal operations will not be chained to an empty Slinq.
		/// </summary>
		public static Slinq<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>> ZipAll<T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with) {
			return ZipAllContext<T2, C2, T, C>.ZipAll(slinq, with, ZipRemoveFlags.Left);
		}
		
		/// <summary>
		/// Returns a Slinq that combines the corresponding elements from the supplied Slinqs into tuples and chains removal operations to the specified Slinq(s).
		/// 
		/// The returned Slinq will continue enumerating until it reaches the end of the longer of the supplied Slinqs.
		/// 
		/// While a Slinq has elements remaining, the enumerated tuples will contain Some options containing the corresponding elements from the Slinq in the corresponding position.
		///
		/// If either of the Slinqs is empty while the other still has elements remaining, the enumerated tuples will contain None options in place of the shorter Slinq's element until the enumeration is complete.
		/// 
		/// Removal operations will not be chained to an empty Slinq.
		/// </summary>
		public static Slinq<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>> ZipAll<T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with, ZipRemoveFlags removeFlags) {
			return ZipAllContext<T2, C2, T, C>.ZipAll(slinq, with, removeFlags);
		}
		
		/// <summary>
		/// Returns a Slinq that combines the corresponding elements from the supplied Slinqs using the supplied selector and chains removal operations to the left Slinq.
		/// 
		/// The returned Slinq will continue enumerating until it reaches the end of the longer of the supplied Slinqs.
		/// 
		/// While a Slinq has elements remaining, the selector will be passed Some options containing the corresponding elements from the Slinq.
		///
		/// If either of the Slinqs is empty while the other still has elements remaining, None options will be passed to the selector in place of the shorter Slinq's element until the enumeration is complete.
		/// 
		/// Removal operations will not be chained to an empty Slinq.
		/// </summary>
		public static Slinq<U, ZipAllContext<U, T2, C2, T, C>> ZipAll<U, T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<Option<T>, Option<T2>, U> selector) {
			return ZipAllContext<U, T2, C2, T, C>.ZipAll(slinq, with, selector, ZipRemoveFlags.Left);
		}
		
		/// <summary>
		/// Returns a Slinq that combines the corresponding elements from the supplied Slinqs using the supplied selector and chains removal operations to the specified Slinq(s).
		/// 
		/// The returned Slinq will continue enumerating until it reaches the end of the longer of the supplied Slinqs.
		/// 
		/// While a Slinq has elements remaining, the selector will be passed Some options containing the corresponding elements from the Slinq.
		///
		/// If either of the Slinqs is empty while the other still has elements remaining, None options will be passed to the selector in place of the shorter Slinq's element until the enumeration is complete.
		/// 
		/// Removal operations will not be chained to an empty Slinq.
		/// </summary>
		public static Slinq<U, ZipAllContext<U, T2, C2, T, C>> ZipAll<U, T2, C2, T, C>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<Option<T>, Option<T2>, U> selector, ZipRemoveFlags removeFlags) {
			return ZipAllContext<U, T2, C2, T, C>.ZipAll(slinq, with, selector, removeFlags);
		}
		
		/// <summary>
		/// Returns a Slinq that combines the corresponding elements from the supplied Slinqs using the supplied selector and chains removal operations to the left Slinq.
		/// 
		/// The returned Slinq will continue enumerating until it reaches the end of the longer of the supplied Slinqs.
		/// 
		/// While a Slinq has elements remaining, the selector will be passed Some options containing the corresponding elements from the Slinq.
		///
		/// If either of the Slinqs is empty while the other still has elements remaining, None options will be passed to the selector in place of the shorter Slinq's element until the enumeration is complete.
		/// 
		/// Removal operations will not be chained to an empty Slinq.
		/// </summary>
		public static Slinq<U, ZipAllContext<U, T2, C2, T, C, P>> ZipAll<U, T2, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<Option<T>, Option<T2>, P, U> selector, P parameter) {
			return ZipAllContext<U, T2, C2, T, C, P>.ZipAll(slinq, with, selector, parameter, ZipRemoveFlags.Left);
		}
		
		/// <summary>
		/// Returns a Slinq that combines the corresponding elements from the supplied Slinqs using the supplied selector and chains removal operations to the specified Slinq(s).
		/// 
		/// The returned Slinq will continue enumerating until it reaches the end of the longer of the supplied Slinqs.
		/// 
		/// While a Slinq has elements remaining, the selector will be passed Some options containing the corresponding elements from the Slinq.
		///
		/// If either of the Slinqs is empty while the other still has elements remaining, None options will be passed to the selector in place of the shorter Slinq's element until the enumeration is complete.
		/// 
		/// Removal operations will not be chained to an empty Slinq.
		/// </summary>
		public static Slinq<U, ZipAllContext<U, T2, C2, T, C, P>> ZipAll<U, T2, C2, T, C, P>(this Slinq<T, C> slinq, Slinq<T2, C2> with, DelegateFunc<Option<T>, Option<T2>, P, U> selector, P parameter, ZipRemoveFlags removeFlags) {
			return ZipAllContext<U, T2, C2, T, C, P>.ZipAll(slinq, with, selector, parameter, removeFlags);
		}
		
		/// <summary>
		/// Zips the specified Slinq with a zero-based index.
		/// </summary>
		public static Slinq<Tuple<T, int>, ZipContext<int, FuncContext<int, int>, T, C>> ZipWithIndex<T, C>(this Slinq<T, C> slinq) {
			return ZipContext<int, FuncContext<int, int>, T, C>.Zip(slinq, Slinqable.Sequence(0, 1), ZipRemoveFlags.Left);
		}
		
		#endregion

		#region Type specific aggregations

		#region Average

		/// <summary>
		/// Analog to Enumerable.Average().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Double Average<C>(this Slinq<Int32, C> slinq) {
			return AverageOrNone(slinq).ValueOr(() => { throw new InvalidOperationException(); });
		}

		/// <summary>
		/// Analog to Enumerable.Average().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Double Average<C>(this Slinq<Int64, C> slinq) {
			return AverageOrNone(slinq).ValueOr(() => { throw new InvalidOperationException(); });
		}
		
		/// <summary>
		/// Analog to Enumerable.Average().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Single Average<C>(this Slinq<Single, C> slinq) {
			return AverageOrNone(slinq).ValueOr(() => { throw new InvalidOperationException(); });
		}
		
		/// <summary>
		/// Analog to Enumerable.Average().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Double Average<C>(this Slinq<Double, C> slinq) {
			return AverageOrNone(slinq).ValueOr(() => { throw new InvalidOperationException(); });
		}
		
//		/// <summary>
//		/// Analog to Enumerable.Average().
//		/// 
//		/// This operation will consume and dispose the Slinq.
//		/// </summary>
//		public static Decimal Average<C>(this Slinq<Decimal, C> slinq) {
//			return AverageOrNone(slinq).ValueOr(() => { throw new InvalidOperationException(); });
//		}

		/// <summary>
		/// Analog to Enumerable.Average(), but returns an option rather than throwing an exception on empty input.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Option<Double> AverageOrNone<C>(this Slinq<Int32, C> slinq) {
			if (slinq.current.isSome) {
				var count = 0;
				Double acc = 0;
				while (slinq.current.isSome) {
					acc += slinq.current.value;
					slinq.skip(ref slinq.context, out slinq.current);
					++count;
				}
				return new Option<Double>(acc / count);
			} else {
				return new Option<Double>();
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Average(), but returns an option rather than throwing an exception on empty input.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Option<Double> AverageOrNone<C>(this Slinq<Int64, C> slinq) {
			if (slinq.current.isSome) {
				var count = 0;
				Double acc = 0;
				while (slinq.current.isSome) {
					acc += slinq.current.value;
					slinq.skip(ref slinq.context, out slinq.current);
					++count;
				}
				return new Option<Double>(acc / count);
			} else {
				return new Option<Double>();
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Average(), but returns an option rather than throwing an exception on empty input.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Option<Single> AverageOrNone<C>(this Slinq<Single, C> slinq) {
			if (slinq.current.isSome) {
				var count = 0;
				Single acc = 0;
				while (slinq.current.isSome) {
					acc += slinq.current.value;
					slinq.skip(ref slinq.context, out slinq.current);
					++count;
				}
				return new Option<Single>(acc / count);
			} else {
				return new Option<Single>();
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Average(), but returns an option rather than throwing an exception on empty input.
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Option<Double> AverageOrNone<C>(this Slinq<Double, C> slinq) {
			if (slinq.current.isSome) {
				var count = 0;
				Double acc = 0;
				while (slinq.current.isSome) {
					acc += slinq.current.value;
					slinq.skip(ref slinq.context, out slinq.current);
					++count;
				}
				return new Option<Double>(acc / count);
			} else {
				return new Option<Double>();
			}
		}
		
//		/// <summary>
//		/// Analog to Enumerable.Average(), but returns an option rather than throwing an exception on empty input.
//		/// 
//		/// This operation will consume and dispose the Slinq.
//		/// </summary>
//		public static Option<Decimal> Average<C>(this Slinq<Decimal, C> slinq) {
//			if (slinq.current.isSome) {
//				var count = 0;
//				var acc = 0m;
//				while (slinq.current.isSome) {
//					acc += slinq.current.value;
//					slinq.skip(ref slinq.context, out slinq.current);
//					++count;
//				}
//				return new Option<Decimal>(acc / count);
//			} else {
//				return new Option<Decimal>();
//			}
//		}

		#endregion
		
		#region Sum
		
		/// <summary>
		/// Analog to Enumerable.Sum().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Int32 Sum<C>(this Slinq<Int32, C> slinq) {
			checked {
				Int32 acc = 0;
				while (slinq.current.isSome) {
					acc += slinq.current.value;
					slinq.skip(ref slinq.context, out slinq.current);
				}
				return acc;
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Sum().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Int64 Sum<C>(this Slinq<Int64, C> slinq) {
			checked {
				Int64 acc = 0;
				while (slinq.current.isSome) {
					acc += slinq.current.value;
					slinq.skip(ref slinq.context, out slinq.current);
				}
				return acc;
			}
		}
		
		/// <summary>
		/// Analog to Enumerable.Sum().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Single Sum<C>(this Slinq<Single, C> slinq) {
			Single acc = 0;
			while (slinq.current.isSome) {
				acc += slinq.current.value;
				slinq.skip(ref slinq.context, out slinq.current);
			}
			return acc;
		}
		
		/// <summary>
		/// Analog to Enumerable.Sum().
		/// 
		/// This operation will consume and dispose the Slinq.
		/// </summary>
		public static Double Sum<C>(this Slinq<Double, C> slinq) {
			Double acc = 0;
			while (slinq.current.isSome) {
				acc += slinq.current.value;
				slinq.skip(ref slinq.context, out slinq.current);
			}
			return acc;
		}
		
//		/// <summary>
//		/// Analog to Enumerable.Sum().
//		/// 
//		/// This operation will consume and dispose the Slinq.
//		/// </summary>
//		public static Decimal Sum<C>(this Slinq<Decimal, C> slinq) {
//			var acc = 0m;
//			while (slinq.current.isSome) {
//				acc += slinq.current.value;
//				slinq.skip(ref slinq.context, out slinq.current);
//			}
//			return acc;
//		}
		
		#endregion

		#endregion

	}
}
