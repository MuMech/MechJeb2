using System;
using System.Collections.Generic;
using Smooth.Delegates;
using Smooth.Dispose;
using Smooth.Slinq.Context;

namespace Smooth.Slinq.Collections {
	/// <summary>
	/// Represents a list of keys each mapped to a list of values.
	/// </summary>
	public class Lookup<K, T> : IDisposable {
		private static readonly Stack<Lookup<K, T>> pool = new Stack<Lookup<K, T>>();

		/// <summary>
		/// The list of keys mapped by the lookup.
		/// </summary>
		public LinkedHeadTail<K> keys;

		/// <summary>
		/// The dictionary used to map keys to lists of values.
		/// </summary>
		public readonly Dictionary<K, LinkedHeadTail<T>> dictionary;

		private Lookup() {}

		private Lookup(IEqualityComparer<K> comparer) {
			this.dictionary = new Dictionary<K, LinkedHeadTail<T>>(comparer);
			this.keys = new LinkedHeadTail<K>();
		}
		
		/// <summary>
		/// Returns a pooled lookup for the specified comparer.
		/// </summary>
		public static Lookup<K, T> Borrow(IEqualityComparer<K> comparer) {
			lock (pool) { return pool.Count > 0 ? pool.Pop() : new Lookup<K, T>(comparer); }
		}

		/// <summary>
		/// Relinquishes ownership of the lookup and adds it to the disposal queue.
		/// </summary>
		public void DisposeInBackground() {
			DisposalQueue.Enqueue(this);
		}

		/// <summary>
		/// Releases the lookup and any key and/or value nodes it contains to their respective pools.
		/// </summary>
		public void Dispose() {
			var valuesAcc = new LinkedHeadTail<T>();
			var runner = keys.head;

			while (runner != null) {
				valuesAcc.Append(RemoveValues(runner.value));
				runner = runner.next;
			}

			if (dictionary.Count > 0) {
				UnityEngine.Debug.LogWarning("Lookup had dictionary keys that were not in the key list.");
				foreach (var values in dictionary.Values) {
					valuesAcc.Append(values);
				}
				dictionary.Clear();
			}

			keys.Dispose();
			valuesAcc.Dispose();

			lock (pool) {
				pool.Push(this);
			}
		}

		/// <summary>
		/// Appends the specified value to the value list for the specified key.  If the key was previously unmapped it is appended to the key list.
		/// </summary>
		public void Add(K key, T value) {
			LinkedHeadTail<T> values;
			if (!dictionary.TryGetValue(key, out values)) {
				keys.Append(key);
			}
			values.Append(value);
			dictionary[key] = values;
		}

		/// <summary>
		/// Appends the specified value to the value list for the specified key.  If the key was previously unmapped it is appended to the key list.
		/// 
		/// Calling this method transfers ownership of the specified node and any linked nodes to the lookup.
		/// </summary>
		public void Add(K key, Linked<T> value) {
			LinkedHeadTail<T> values;
			if (!dictionary.TryGetValue(key, out values)) {
				keys.Append(key);
			}
			values.Append(value);
			dictionary[key] = values;
		}
		
		/// <summary>
		/// Appends the specified list to the value list for the specified key.  If the key was previously unmapped it is appended to the key list.
		/// 
		/// Calling this method transfers ownership of the nodes in the specified list to the lookup.
		/// </summary>
		public void Add(K key, LinkedHeadTail<T> values) {
			LinkedHeadTail<T> existing;
			if (dictionary.TryGetValue(key, out existing)) {
				existing.Append(values);
				dictionary[key] = existing;
			} else {
				keys.Append(key);
				dictionary[key] = values;
			}
		}

		/// <summary>
		/// Returns the list of values for the specified key, without transfer of ownership.
		/// 
		/// The caller of this method is responsible for mananging the scope of the returned nodes.
		/// </summary>
		public LinkedHeadTail<T> GetValues(K key) {
			LinkedHeadTail<T> values;
			dictionary.TryGetValue(key, out values);
			return values;
		}

		/// <summary>
		/// Returns the list of values for the specified key with ownership of the nodes transferred to the caller.
		/// 
		/// The caller of this method is responsible for the disposal of the returned nodes.
		/// </summary>
		public LinkedHeadTail<T> RemoveValues(K key) {
			LinkedHeadTail<T> values;
			if (dictionary.TryGetValue(key, out values)) {
				dictionary.Remove(key);
			}
			return values;
		}

		/// <summary>
		/// Sorts the lookup's keys using the specified comparison and ordering.
		/// 
		/// This method uses an introspective merge sort algorithm that will optimally sort rather than split lists with 3 or fewer nodes.
		/// </summary>
		public Lookup<K, T> SortKeys(Comparison<K> comparison, bool ascending) {
			keys = Linked.Sort(keys, comparison, ascending);
			return this;
		}

		/// <summary>
		/// Returns a list of all the values contained in this lookup and adds the lookup to the disposal queue.
		/// 
		/// Items in the list will be ordered based on the ordering of the key list, then by the position within value list for the item's key.
		/// 
		/// Ownership of the returned nodes is transferred to the caller, who is responsible for their disposal.
		/// </summary>
		public LinkedHeadTail<T> FlattenAndDispose() {
			var values = new LinkedHeadTail<T>();
			var runner = keys.head;
			
			while (runner != null) {
				values.Append(RemoveValues(runner.value));
				runner = runner.next;
			}
			
			keys.DisposeInBackground();
			DisposeInBackground();
			
			return values;
		}

		#region Slinqs

		#region GroupBy

		/// <summary>
		/// Returns a Slinq that enumerates the key, value groupings contained in the lookup, with the values returned in Slinq form.
		/// 
		/// Ownership of the lookup and any values it contains is retained by the caller.
		/// </summary>
		public Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> SlinqAndKeep() {
			return GroupByContext<K, T>.Slinq(this, false);
		}

		/// <summary>
		/// Returns a Slinq that enumerates the key, value groupings contained in the lookup, with the values returned in Slinq form.
		/// 
		/// As the groupings are enumerated, ownership of the values in each grouping is transferred to the Slinq contained in the grouping.
		/// 
		/// When the enumeration is complete, the lookup and any unenumerated values it contains are added to the disposal queue.
		/// </summary>
		public Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> SlinqAndDispose() {
			return GroupByContext<K, T>.Slinq(this, true);
		}

		/// <summary>
		/// Returns a Slinq that enumerates the key, value groupings contained in the lookup, with the values returned in Linked form.
		/// 
		/// Ownership of the lookup and any values it contains is retained by the caller.
		/// </summary>
		public Slinq<Grouping<K, T>, GroupByContext<K, T>> SlinqLinkedAndKeep() {
			return GroupByContext<K, T>.SlinqLinked(this, false);
		}

		/// <summary>
		/// Returns a Slinq that enumerates the key, value groupings contained in the lookup, with the values returned in Linked form.
		/// 
		/// As the groupings are enumerated, ownership of the values in each grouping is transferred to the owner of the grouping, who is responsible for their disposal.
		/// 
		/// When the enumeration is complete, the lookup and any unenumerated values it contains are added to the disposal queue.
		/// </summary>
		public Slinq<Grouping<K, T>, GroupByContext<K, T>> SlinqLinkedAndDispose() {
			return GroupByContext<K, T>.SlinqLinked(this, true);
		}
		
		#endregion

		#region GroupJoin

		/// <summary>
		/// Returns a Slinq that enumerates a group join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// Ownership of the lookup and any values it contains is retained by the caller.
		/// </summary>
		public Slinq<U, GroupJoinContext<U, K, T, T2, C2>> GroupJoinAndKeep<U, T2, C2>(Slinq<T2, C2> outer, DelegateFunc<T2, K> outerSelector, DelegateFunc<T2, Slinq<T, LinkedContext<T>>, U> resultSelector) {
			return GroupJoinContext<U, K, T, T2, C2>.GroupJoin(this, outer, outerSelector, resultSelector, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates a group join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// Ownership of the lookup and any values it contains is retained by the caller.
		/// </summary>
		public Slinq<U, GroupJoinContext<U, K, T, T2, C2, P>> GroupJoinAndKeep<U, T2, C2, P>(Slinq<T2, C2> outer, DelegateFunc<T2, P, K> outerSelector, DelegateFunc<T2, Slinq<T, LinkedContext<T>>, P, U> resultSelector, P parameter) {
			return GroupJoinContext<U, K, T, T2, C2, P>.GroupJoin(this, outer, outerSelector, resultSelector, parameter, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates a group join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// When the enumeration is complete, the lookup and any values it contains are added to the disposal queue.
		/// </summary>
		public Slinq<U, GroupJoinContext<U, K, T, T2, C2>> GroupJoinAndDispose<U, T2, C2>(Slinq<T2, C2> outer, DelegateFunc<T2, K> outerSelector, DelegateFunc<T2, Slinq<T, LinkedContext<T>>, U> resultSelector) {
			return GroupJoinContext<U, K, T, T2, C2>.GroupJoin(this, outer, outerSelector, resultSelector, true);
		}

		/// <summary>
		/// Returns a Slinq that enumerates a group join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// When the enumeration is complete, the lookup and any values it contains are added to the disposal queue.
		/// </summary>
		public Slinq<U, GroupJoinContext<U, K, T, T2, C2, P>> GroupJoinAndDispose<U, T2, C2, P>(Slinq<T2, C2> outer, DelegateFunc<T2, P, K> outerSelector, DelegateFunc<T2, Slinq<T, LinkedContext<T>>, P, U> resultSelector, P parameter) {
			return GroupJoinContext<U, K, T, T2, C2, P>.GroupJoin(this, outer, outerSelector, resultSelector, parameter, true);
		}
		
		#endregion
		
		#region Join

		/// <summary>
		/// Returns a Slinq that enumerates an inner join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// Ownership of the lookup and any values it contains is retained by the caller.
		/// </summary>
		public Slinq<U, JoinContext<U, K, T, T2, C2>> JoinAndKeep<U, T2, C2>(Slinq<T2, C2> outer, DelegateFunc<T2, K> outerSelector, DelegateFunc<T2, T, U> resultSelector) {
			return JoinContext<U, K, T, T2, C2>.Join(this, outer, outerSelector, resultSelector, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates an inner join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// Ownership of the lookup and any values it contains is retained by the caller.
		/// </summary>
		public Slinq<U, JoinContext<U, K, T, T2, C2, P>> JoinAndKeep<U, T2, C2, P>(Slinq<T2, C2> outer, DelegateFunc<T2, P, K> outerSelector, DelegateFunc<T2, T, P, U> resultSelector, P parameter) {
			return JoinContext<U, K, T, T2, C2, P>.Join(this, outer, outerSelector, resultSelector, parameter, false);
		}

		/// <summary>
		/// Returns a Slinq that enumerates an inner join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// When the enumeration is complete, the lookup and any values it contains are added to the disposal queue.
		/// </summary>
		public Slinq<U, JoinContext<U, K, T, T2, C2>> JoinAndDispose<U, T2, C2>(Slinq<T2, C2> outer, DelegateFunc<T2, K> outerSelector, DelegateFunc<T2, T, U> resultSelector) {
			return JoinContext<U, K, T, T2, C2>.Join(this, outer, outerSelector, resultSelector, true);
		}

		/// <summary>
		/// Returns a Slinq that enumerates an inner join of the lookup with the specified Slinq using the specified selectors.
		/// 
		/// When the enumeration is complete, the lookup and any values it contains are added to the disposal queue.
		/// </summary>
		public Slinq<U, JoinContext<U, K, T, T2, C2, P>> JoinAndDispose<U, T2, C2, P>(Slinq<T2, C2> outer, DelegateFunc<T2, P, K> outerSelector, DelegateFunc<T2, T, P, U> resultSelector, P parameter) {
			return JoinContext<U, K, T, T2, C2, P>.Join(this, outer, outerSelector, resultSelector, parameter, true);
		}

		#endregion

		#endregion

	}
}
