using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Delegates;
using Smooth.Comparisons;
using Smooth.Pools;
using Smooth.Slinq.Context;

namespace Smooth.Slinq {

	/// <summary>
	/// Provides methods for creating basic Slinqs from various underlying collections or delegates.
	/// </summary>
	public static class Slinqable {

		#region Empty
		
		/// <summary>
		/// Returns an empty Slinq of the specified type.
		/// </summary>
		public static Slinq<T, Unit> Empty<T>() {
			return new Slinq<T, Unit>();
		}
		
		#endregion

		#region IEnumerable

		/// <summary>
		/// Returns a Slinq that enumerates over the specified enumerable.
		///
		/// The returned Slinq will be backed by an IEnumerator<T>, and thus will allocate an object or box.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, IEnumerableContext<T>> Slinq<T>(this IEnumerable<T> enumerable) {
			return IEnumerableContext<T>.Slinq(enumerable);
		}

		#endregion
		
		#region IList
		
		#region Explicit step
		
		/// <summary>
		/// Returns a Slinq that enumerates over the elements of the specified list using the specified start index and step.
		/// 
		/// If startIndex is outside the element range of the list, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, IListContext<T>> Slinq<T>(this IList<T> list, int startIndex, int step) {
			return IListContext<T>.Slinq(list, startIndex, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the element, index pairs of the specified list using the specified start index and step.
		/// 
		/// If startIndex is outside the element range of the list, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<Tuple<T, int>, IListContext<T>> SlinqWithIndex<T>(this IList<T> list, int startIndex, int step) {
			return IListContext<T>.SlinqWithIndex(list, startIndex, step);
		}

		#endregion
		
		#region Ascending
		
		/// <summary>
		/// Returns a Slinq that enumerates over the elements of the specified list in ascending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, IListContext<T>> Slinq<T>(this IList<T> list) {
			return IListContext<T>.Slinq(list, 0, 1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the element, index pairs of the specified list in ascending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<Tuple<T, int>, IListContext<T>> SlinqWithIndex<T>(this IList<T> list) {
			return IListContext<T>.SlinqWithIndex(list, 0, 1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the elements of the specified list in ascending order starting with the specified index.
		/// 
		/// If startIndex is outside the element range of the list, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, IListContext<T>> Slinq<T>(this IList<T> list, int startIndex) {
			return IListContext<T>.Slinq(list, startIndex, 1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the element, index pairs of the specified list in ascending order starting with the specified index.
		/// 
		/// If startIndex is outside the element range of the list, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<Tuple<T, int>, IListContext<T>> SlinqWithIndex<T>(this IList<T> list, int startIndex) {
			return IListContext<T>.SlinqWithIndex(list, startIndex, 1);
		}

		#endregion
		
		#region Descending

		/// <summary>
		/// Returns a Slinq that enumerates over the elements of the specified list in descending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, IListContext<T>> SlinqDescending<T>(this IList<T> list) {
			return IListContext<T>.Slinq(list, list.Count - 1, -1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the element, index pairs of the specified list in descending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<Tuple<T, int>, IListContext<T>> SlinqWithIndexDescending<T>(this IList<T> list) {
			return IListContext<T>.SlinqWithIndex(list, list.Count - 1, -1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the elements of the specified list in decending order starting with the specified index.
		/// 
		/// If startIndex is outside the element range of the list, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, IListContext<T>> SlinqDescending<T>(this IList<T> list, int startIndex) {
			return IListContext<T>.Slinq(list, startIndex, -1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the element, index pairs of the specified list in decending order starting with the specified index.
		/// 
		/// If startIndex is outside the element range of the list, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<Tuple<T, int>, IListContext<T>> SlinqWithIndexDescending<T>(this IList<T> list, int startIndex) {
			return IListContext<T>.SlinqWithIndex(list, startIndex, -1);
		}

		#endregion
		
		#endregion
		
		#region LinkedList
		
		#region Explicit step
		
		/// <summary>
		/// Returns a Slinq that starts with the value of the specified node and proceeds along node links.
		/// 
		/// If step is positive, the Slinq will move along Next links, if step is negative the Slinq will move along Previous links.  If step is zero the Slinq will stay in place.
		/// 
		/// If the specified node is null, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, LinkedListContext<T>> Slinq<T>(this LinkedListNode<T> node, int step) {
			return LinkedListContext<T>.Slinq(node, step);
		}

		/// <summary>
		/// Returns a Slinq that starts with the specified node and proceeds along node links.
		/// 
		/// If step is positive, the Slinq will move along Next links, if step is negative the Slinq will move along Previous links.  If step is zero the Slinq will stay in place.
		/// 
		/// If the specified node is null, the resulting Slinq will be empty.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<LinkedListNode<T>, LinkedListContext<T>> SlinqNodes<T>(this LinkedListNode<T> node, int step) {
			return LinkedListContext<T>.SlinqNodes(node, step);
		}

		#endregion

		#region Ascending / Descending
		
		/// <summary>
		/// Returns a Slinq that enumerates over the values of the specified linked list in ascending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, LinkedListContext<T>> Slinq<T>(this LinkedList<T> list) {
			return LinkedListContext<T>.Slinq(list.First, 1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the values of the specified linked list in decending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<T, LinkedListContext<T>> SlinqDescending<T>(this LinkedList<T> list) {
			return LinkedListContext<T>.Slinq(list.Last, -1);
		}

		/// <summary>
		/// Returns a Slinq that enumerates over the nodes of the specified linked list in ascending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<LinkedListNode<T>, LinkedListContext<T>> SlinqNodes<T>(this LinkedList<T> list) {
			return LinkedListContext<T>.SlinqNodes(list.First, 1);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates over the nodes of the specified linked list in decending order.
		/// 
		/// Slinqs created by this method will chain removal operations to the underlying list.
		/// </summary>
		public static Slinq<LinkedListNode<T>, LinkedListContext<T>> SlinqNodesDescending<T>(this LinkedList<T> list) {
			return LinkedListContext<T>.SlinqNodes(list.Last, -1);
		}
		
		#endregion

		#endregion

		#region Option
		
		/// <summary>
		/// Returns a Slinq that enumerates the specified option.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, OptionContext<T>> Slinq<T>(this Option<T> option) {
			return OptionContext<T>.Slinq(option);
		}
		
		#endregion

		#region Range
		
		/// <summary>
		/// Returns a Slinq that enumerates the integers within a specified range.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<int, FuncOptionContext<int, int>> Range(int start, int count) {
			if (count > 0) {
				var max = start + count - 1;
				if (max >= start) {
					return FuncOptionContext<int, int>.Sequence(start, (acc, last) => ++acc > last ? new Option<int>() : new Option<int>(acc), max);
				} else {
					throw new ArgumentOutOfRangeException();
				}
			} else if (count == 0) {
				return new Slinq<int, FuncOptionContext<int, int>>();
			} else {
				throw new ArgumentOutOfRangeException();
			}
		}
		
		#endregion

		#region Repeat
		
		/// <summary>
		/// Returns a Slinq that repeats the specified value.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, OptionContext<T>> Repeat<T>(T value) {
			return OptionContext<T>.Repeat(value);
		}
		
		/// <summary>
		/// Returns a Slinq that repeats the specified value the specified number of times.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, IntContext<T, OptionContext<T>>> Repeat<T>(T value, int count) {
			if (count > 0) {
				return OptionContext<T>.Repeat(value).Take(count);
			} else if (count == 0) {
				return new Slinq<T, IntContext<T, OptionContext<T>>>();
			} else {
				throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
		
		#region Sequence
		
		#region Generic Sequences
		
		/// <summary>
		/// Returns a Slinq that enumerates the sequence generated by specified seed value and selector function.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, FuncContext<T>> Sequence<T>(T seed, DelegateFunc<T, T> selector) {
			return FuncContext<T>.Sequence(seed, selector);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the sequence generated by specified seed value and selector function.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, FuncContext<T, P>> Sequence<T, P>(T seed, DelegateFunc<T, P, T> selector, P parameter) {
			return FuncContext<T, P>.Sequence(seed, selector, parameter);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the sequence generated by specified seed value and selector function.
		/// 
		/// The enumeration will end when the selector returns a None option.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, FuncOptionContext<T>> Sequence<T>(T seed, DelegateFunc<T, Option<T>> selector) {
			return FuncOptionContext<T>.Sequence(seed, selector);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the sequence generated by specified seed value and selector function.
		/// 
		/// The enumeration will end when the selector returns a None option.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<T, FuncOptionContext<T, P>> Sequence<T, P>(T seed, DelegateFunc<T, P, Option<T>> selector, P parameter) {
			return FuncOptionContext<T, P>.Sequence(seed, selector, parameter);
		}
		
		#endregion
		
		#region Type specific sequences
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Byte, FuncContext<Byte, Byte>> Sequence(Byte start, Byte step) {
			return FuncContext<Byte, Byte>.Sequence(start, (x, s) => (Byte) (x + s), step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<SByte, FuncContext<SByte, SByte>> Sequence(SByte start, SByte step) {
			return FuncContext<SByte, SByte>.Sequence(start, (x, s) => (SByte) (x + s), step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Int16, FuncContext<Int16, Int16>> Sequence(Int16 start, Int16 step) {
			return FuncContext<Int16, Int16>.Sequence(start, (x, s) => (Int16) (x + s), step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<UInt16, FuncContext<UInt16, UInt16>> Sequence(UInt16 start, UInt16 step) {
			return FuncContext<UInt16, UInt16>.Sequence(start, (x, s) => (UInt16) (x + s), step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Int32, FuncContext<Int32, Int32>> Sequence(Int32 start, Int32 step) {
			return FuncContext<Int32, Int32>.Sequence(start, (x, s) => x + s, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<UInt32, FuncContext<UInt32, UInt32>> Sequence(UInt32 start, UInt32 step) {
			return FuncContext<UInt32, UInt32>.Sequence(start, (x, s) => x + s, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Int64, FuncContext<Int64, Int64>> Sequence(Int64 start, Int64 step) {
			return FuncContext<Int64, Int64>.Sequence(start, (x, s) => x + s, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<UInt64, FuncContext<UInt64, UInt64>> Sequence(UInt64 start, UInt64 step) {
			return FuncContext<UInt64, UInt64>.Sequence(start, (x, s) => x + s, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Single, FuncContext<Single, Single>> Sequence(Single start, Single step) {
			return FuncContext<Single, Single>.Sequence(start, (x, s) => x + s, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Double, FuncContext<Double, Double>> Sequence(Double start, Double step) {
			return FuncContext<Double, Double>.Sequence(start, (x, s) => x + s, step);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the arithmetic sequence generated by the specified start and step values.
		/// 
		/// Slinqs created by this method do not support element removal.
		/// </summary>
		public static Slinq<Decimal, FuncContext<Decimal, Decimal>> Sequence(Decimal start, Decimal step) {
			return FuncContext<Decimal, Decimal>.Sequence(start, (x, s) => x + s, step);
		}
		
		#endregion
		
		#endregion

	}
}
