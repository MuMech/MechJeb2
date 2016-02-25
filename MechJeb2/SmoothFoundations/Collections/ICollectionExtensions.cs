using System;
using System.Collections.Generic;

namespace Smooth.Collections {

	/// <summary>
	/// Extension methods for ICollection<>s.
	/// </summary>
	public static class ICollectionExtensions {

		#region AddAll

		/// <summary>
		/// Adds the specified values to and returns the specific collection.
		/// </summary>
		public static IC AddAll<IC, T>(this IC collection, params T[] values) where IC : ICollection<T> {
			for (int i = 0; i < values.Length; ++i) {
				collection.Add(values[i]);
			}
			return collection;
		}
		
		/// <summary>
		/// Adds the specified values to and returns the specific collection.
		/// </summary>
		public static IC AddAll<IC, T>(this IC collection, IList<T> values) where IC : ICollection<T> {
			for (int i = 0; i < values.Count; ++i) {
				collection.Add(values[i]);
			}
			return collection;
		}

		/// <summary>
		/// Adds the specified values to and returns the specific collection.
		/// </summary>
		public static IC AddAll<IC, T>(this IC collection, IEnumerable<T> values) where IC : ICollection<T> {
			foreach (var value in values) {
				collection.Add(value);
			}
			return collection;
		}
		
		#endregion

	}

}
