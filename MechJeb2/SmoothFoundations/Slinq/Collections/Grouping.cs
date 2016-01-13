using System;

namespace Smooth.Slinq.Collections {
	/// <summary>
	/// Represents a list of values associated with a key.
	/// 
	/// The values are stored in Linked form to allow element reordering without the creation of new list nodes.
	/// </summary>
	public struct Grouping<K, T> {
		/// <summary>
		/// The key associtated with the values.
		/// </summary>
		public readonly K key;

		/// <summary>
		/// The values associated with the key.
		/// </summary>
		public LinkedHeadTail<T> values;
		
		/// <summary>
		/// Returns a grouping for the specified key and values.
		/// </summary>
		public Grouping(K key, LinkedHeadTail<T> values) {
			this.key = key;
			this.values = values;
		}
	}
	
	/// <summary>
	/// Represents a list of values associated with a key.
	/// 
	/// The values are stored in Slinq form for API simplicity and consistency.
	/// </summary>
	public struct Grouping<K, T, C> {
		/// <summary>
		/// The key associtated with the values.
		/// </summary>
		public readonly K key;

		/// <summary>
		/// The values associated with the key.
		/// </summary>
		public Slinq<T, C> values;

		/// <summary>
		/// Returns a grouping for the specified key and values.
		/// </summary>
		public Grouping(K key, Slinq<T, C> values) {
			this.key = key;
			this.values = values;
		}
	}

	/// <summary>
	/// Extension methods for performing operations related to groupings without specifying generic parameters.
	/// </summary>
	public static class Grouping {
		/// <summary>
		/// Returns a grouping for the specified key and values.
		/// </summary>
		public static Grouping<K, T> Create<K, T>(K key, LinkedHeadTail<T> values) {
			return new Grouping<K, T>(key, values);
		}

		/// <summary>
		/// Returns a grouping for the specified key and values.
		/// </summary>
		public static Grouping<K, T, C> Create<K, T, C>(K key, Slinq<T, C> values) {
			return new Grouping<K, T, C>(key, values);
		}
	}
}
