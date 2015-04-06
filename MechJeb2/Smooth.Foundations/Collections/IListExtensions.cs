using UnityEngine;
using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Comparisons;

namespace Smooth.Collections {
	
	/// <summary>
	/// Extension methods for IList<>s.
	/// </summary>
	public static class IListExtensions {
		
		#region Randomize
		
		/// <summary>
		/// If the specified list is empty, returns an empty option; otherwise, returns an option containing a random element from the specified list.
		/// </summary>
		public static Option<T> Random<T>(this IList<T> list) {
			return list.Count == 0 ? Option<T>.None : new Option<T>(list[UnityEngine.Random.Range(0, list.Count)]);
		}

		/// <summary>
		/// Shuffles the element order of the specified list.
		/// </summary>
		public static void Shuffle<T>(this IList<T> ts) {
			var count = ts.Count;
			var last = count - 1;
			for (var i = 0; i < last; ++i) {
				var r = UnityEngine.Random.Range(i, count);
				var tmp = ts[i];
				ts[i] = ts[r];
				ts[r] = tmp;
			}
		}

		#endregion
		
		#region Sort
		
		/// <summary>
		/// Sorts the specified list using an insertion sort algorithm and the default sort comparer for T.
		/// </summary>
		/// <remarks>
		/// Insertion sort is a O(n²) time complexity algorithm and should not be used on arbitrary lists.
		/// However, it has a best case time complexity of O(n) for lists that are already sorted and is quite fast when used on nearly sorted input.
		/// </remarks>
		public static void InsertionSort<T>(this IList<T> ts) {
			InsertionSort(ts, Comparisons<T>.Default);
		}
		
		/// <summary>
		/// Sorts the specified list using an insertion sort algorithm and the specified comparer.
		/// </summary>
		/// <remarks>
		/// Insertion sort is a O(n²) time complexity algorithm and should not be used on arbitrary lists.
		/// However, it has a best case time complexity of O(n) for lists that are already sorted and is quite fast when used on nearly sorted input.
		/// </remarks>
		public static void InsertionSort<T>(this IList<T> ts, IComparer<T> comparer) {
			InsertionSort(ts, Comparisons<T>.ToComparison(comparer));
		}
		
		/// <summary>
		/// Sorts the specified list using an insertion sort algorithm and the specified comparison.
		/// </summary>
		/// <remarks>
		/// Insertion sort is a O(n²) time complexity algorithm and should not be used on arbitrary lists.
		/// However, it has a best case time complexity of O(n) for lists that are already sorted and is quite fast when used on nearly sorted input.
		/// </remarks>
		public static void InsertionSort<T>(this IList<T> ts, Comparison<T> comparison) {
			for (int right = 1; right < ts.Count; ++right) {
				var insert = ts[right];
				var left = right - 1;
				while (left >= 0 && comparison(ts[left], insert) > 0) {
					ts[left + 1] = ts[left];
					--left;
				}
				ts[left + 1] = insert;
			}
		}

		#endregion

	}
}
