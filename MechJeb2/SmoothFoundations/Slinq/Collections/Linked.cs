using System;
using System.Collections.Generic;
using Smooth.Delegates;
using Smooth.Dispose;
using Smooth.Slinq.Context;

namespace Smooth.Slinq.Collections {

	/// <summary>
	/// Represents a node in a singly linked list.
	/// </summary>
	public class Linked<T> : IDisposable {
		private static object poolLock = new object();
		private static Linked<T> pool;

		/// <summary>
		/// The next node in the list.
		/// </summary>
		public Linked<T> next;

		/// <summary>
		/// The value contained in the node.
		/// </summary>
		public T value;

		private Linked() {}

		/// <summary>
		/// Returns a pooled list node with the specified value.
		/// </summary>
		public static Linked<T> Borrow(T value) {
			Linked<T> node;

			lock (poolLock) {
				if (pool == null) {
					node = new Linked<T>();
				} else {
					node = pool;
					pool = pool.next;
					node.next = null;
				}
			}
			node.value = value;
			return node;
		}

		/// <summary>
		/// Detaches the node's tail, clears the node's value, and releases the node to the pool.
		/// 
		/// Note: Nodes should generally not be released one at a time as it will incur much greater overhead than batching releases using LinkedHeadTail<T>s.
		/// </summary>
		public void TrimAndDispose() {
			value = default(T);
			
			lock (poolLock) {
				next = pool;
				pool = this;
			}
		}
		
		/// <summary>
		/// Relinquishes ownership of the node and adds it to the disposal queue.
		/// </summary>
		public void DisposeInBackground() {
			DisposalQueue.Enqueue(this);
		}
		
		/// <summary>
		/// Traverses the node list that starts with this node, clears the value of each node, and releases the resulting node list to the pool.
		/// </summary>
		public void Dispose() {
			var dt = default(T);

			value = dt;

			var runner = this;

			while (runner.next != null) {
				runner = runner.next;
				runner.value = dt;
			}
			
			lock (poolLock) {
				runner.next = pool;
				pool = this;
			}
		}
	}

	/// <summary>
	/// Represents a node in a singly linked list of key, value pairs.
	/// </summary>
	public class Linked<K, T> : IDisposable {
		private static object poolLock = new object();
		private static Linked<K, T> pool;

		/// <summary>
		/// The next node in the list.
		/// </summary>
		public Linked<K, T> next;

		/// <summary>
		/// The key contained in the node.
		/// </summary>
		public K key;

		/// <summary>
		/// The value contained in the node.
		/// </summary>
		public T value;
		
		private Linked() {}
		
		/// <summary>
		/// Returns a pooled list node with the specified key and value.
		/// </summary>
		public static Linked<K, T> Borrow(K key, T value) {
			Linked<K, T> node;
			
			lock (poolLock) {
				if (pool == null) {
					node = new Linked<K, T>();
				} else {
					node = pool;
					pool = pool.next;
					node.next = null;
				}
			}
			node.key = key;
			node.value = value;
			return node;
		}

		/// <summary>
		/// Detaches the node's tail, clears the node's key and value, and releases the node to the pool.
		/// 
		/// Note: Nodes should generally not be released one at a time as it will incur much greater overhead than batching releases using LinkedHeadTail<T>s.
		/// </summary>
		public void TrimAndDispose() {
			key = default(K);
			value = default(T);
			
			lock (poolLock) {
				next = pool;
				pool = this;
			}
		}

		/// <summary>
		/// Relinquishes ownership of the node and adds it to the disposal queue.
		/// </summary>
		public void DisposeInBackground() {
			DisposalQueue.Enqueue(this);
		}

		/// <summary>
		/// Traverses the node list that starts with this node, clears the key and value of each node, and releases the resulting node list to the pool.
		/// </summary>
		public void Dispose() {
			var dk = default(K);
			var dt = default(T);
		
			key = dk;
			value = dt;
			
			var runner = this;

			while (runner.next != null) {
				runner = runner.next;
				runner.key = dk;
				runner.value = dt;
			}
			
			lock (poolLock) {
				runner.next = pool;
				pool = this;
			}
		}
	}

	/// <summary>
	/// Represents a singly linked list.
	/// </summary>
	public struct LinkedHeadTail<T> : IEquatable<LinkedHeadTail<T>> {
		/// <summary>
		/// The first node in the list.
		/// </summary>
		public Linked<T> head;

		/// <summary>
		/// The last node in the list.
		/// </summary>
		public Linked<T> tail;

		/// <summary>
		/// The number of elements in the list.
		/// </summary>
		public int count;

		/// <summary>
		/// Returns a list containing a single node with the specified value.
		/// </summary>
		public LinkedHeadTail(T value) : this (Linked<T>.Borrow(value)) {}

		/// <summary>
		/// Returns a list that starts with the specified node.
		/// 
		/// The constructor will traverse the node links to set the tail and count fields.
		/// 
		/// If the specified node is null, the resulting list will be empty.
		/// </summary>
		public LinkedHeadTail(Linked<T> head) {
			if (head == null) {
				this.head = null;
				this.tail = null;
				this.count = 0;
			} else {
				this.head = head;
				this.tail = head;
				this.count = 1;
				
				while (tail.next != null) {
					tail = tail.next;
					++count;
				}
			}
		}
		
		public override bool Equals(object other) {
			return other is LinkedHeadTail<T> && this.Equals((LinkedHeadTail<T>) other);
		}

		public bool Equals(LinkedHeadTail<T> other) {
			return head == other.head;
		}

		public override int GetHashCode() {
			return head.GetHashCode();
		}

		public static bool operator == (LinkedHeadTail<T> lhs, LinkedHeadTail<T> rhs) {
			return lhs.head == rhs.head;
		}

		public static bool operator != (LinkedHeadTail<T> lhs, LinkedHeadTail<T> rhs) {
			return lhs.head != rhs.head;
		}

		#region Append

		/// <summary>
		/// Appends a pooled node with with specified value to the end of the list.
		/// </summary>
		public void Append(T value) {
			var node = Linked<T>.Borrow(value);
			if (head == null) {
				head = node;
			} else {
				tail.next = node;
			}
			tail = node;
			++count;
		}

		/// <summary>
		/// Appends the specified node to the end of the list.
		/// 
		/// The node links will be traversed to determine the new tail and count.
		/// 
		/// If the specified node is null, the list will not be modified.
		/// </summary>
		public void Append(Linked<T> node) {
			if (node != null) {
				if (head == null) {
					head = node;
				} else {
					tail.next = node;
				}
				tail = node;
				++count;

				while (tail.next != null) {
					tail = tail.next;
					++count;
				}
			}
		}
		
		/// <summary>
		/// Appends the specified list to the end of this list.
		/// 
		/// This list and the specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public void Append(LinkedHeadTail<T> other) {
			if (other.count == 0) {
				// noop
			} else if (head == null) {
				head = other.head;
				tail = other.tail;
				count = other.count;
			} else {
				tail.next = other.head;
				tail = other.tail;
				count += other.count;
			}
		}

		#endregion

		#region Release

		/// <summary>
		/// Adds the head of the list to the disposal queue.
		/// 
		/// The list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// </summary>
		public void DisposeInBackground() {
			if (head != null) {
				head.DisposeInBackground();
			}
			head = null;
			tail = null;
			count = 0;
		}
		
		/// <summary>
		/// Releases the head of the list to the node pool.
		/// 
		/// The list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// </summary>
		public void Dispose() {
			if (head != null) {
				head.Dispose();
			}
			head = null;
			tail = null;
			count = 0;
		}

		#endregion

		#region Slinqs

		/// <summary>
		/// Returns a Slinq that enumerates the values contained in the list without reliquishing ownership of the nodes.
		/// 
		/// The caller of this method is responsible for making sure the returned Slinq is not used after the nodes are modified or returned to the pool.
		/// </summary>
		public Slinq<T, LinkedContext<T>> SlinqAndKeep() {
			return LinkedContext<T>.Slinq(this, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the values contained in the list without reliquishing ownership of the nodes.
		/// 
		/// The caller of this method is responsible for making sure the returned Slinq is not used after the nodes are modified or returned to the pool.
		/// 
		/// If backtrack detection is enabled, the supplied backtrack detector can be returned to the pool using its TryReleaseShared method to prevent subsequent enumeration of the returned Slinq.
		/// </summary>
		public Slinq<T, LinkedContext<T>> SlinqAndKeep(BacktrackDetector bd) {
			return LinkedContext<T>.Slinq(this, bd, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the values contained in the list.
		/// 
		/// Ownership of the nodes contained in the list is transferred to the Slinq.  When the Slinq is disposed, the nodes will be added to the disposal queue.
		/// </summary>
		public Slinq<T, LinkedContext<T>> SlinqAndDispose() {
			var slinq = LinkedContext<T>.Slinq(this, true);

			this.head = null;
			this.tail = null;
			this.count = 0;

			return slinq;
		}
		
		#endregion

		#region Lookup

		/// <summary>
		/// Adds the nodes in the list to the specified lookup using the specified key selector.
		/// 
		/// Ownership of the nodes contained in this list is transerred to the lookup.
		/// </summary>
		public Lookup<K, T> AddTo<K>(Lookup<K, T> lookup, DelegateFunc<T, K> selector) {
			while (head != null) {
				var node = head;
				head = head.next;
				node.next = null;
				
				lookup.Add(selector(node.value), node);
			}
			tail = null;
			count = 0;
			return lookup;
		}

		/// <summary>
		/// Adds the nodes in the list to the specified lookup using the specified key selector.
		/// 
		/// Ownership of the nodes contained in this list is transerred to the lookup.
		/// </summary>
		public Lookup<K, T> AddTo<K, P>(Lookup<K, T> lookup, DelegateFunc<T, P, K> selector, P parameter) {
			while (head != null) {
				var node = head;
				head = head.next;
				node.next = null;
				
				lookup.Add(selector(node.value, parameter), node);
			}
			tail = null;
			count = 0;
			return lookup;
		}

		/// <summary>
		/// Returns a pooled lookup with the default key comparer containing the nodes in the list as partitioned by the specified key selector.
		/// 
		/// Ownership of the nodes contained in this list is transerred to the lookup.
		/// </summary>
		public Lookup<K, T> ToLookup<K>(DelegateFunc<T, K> selector) {
			return AddTo(Lookup<K, T>.Borrow(Smooth.Collections.EqualityComparer<K>.Default), selector);
		}
		
		/// <summary>
		/// Returns a pooled lookup with the specified key comparer containing the nodes in the list as partitioned by the specified key selector.
		/// 
		/// Ownership of the nodes contained in this list is transerred to the lookup.
		/// </summary>
		public Lookup<K, T> ToLookup<K>(DelegateFunc<T, K> selector, IEqualityComparer<K> comparer) {
			return AddTo(Lookup<K, T>.Borrow(comparer), selector);
		}

		/// <summary>
		/// Returns a pooled lookup with the default key comparer containing the nodes in the list as partitioned by the specified key selector.
		/// 
		/// Ownership of the nodes contained in this list is transerred to the lookup.
		/// </summary>
		public Lookup<K, T> ToLookup<K, P>(DelegateFunc<T, P, K> selector, P parameter) {
			return AddTo(Lookup<K, T>.Borrow(Smooth.Collections.EqualityComparer<K>.Default), selector, parameter);
		}

		/// <summary>
		/// Returns a pooled lookup with the specified key comparer containing the nodes in the list as partitioned by the specified key selector.
		/// 
		/// Ownership of the nodes contained in this list is transerred to the lookup.
		/// </summary>
		public Lookup<K, T> ToLookup<K, P>(DelegateFunc<T, P, K> selector, P parameter, IEqualityComparer<K> comparer) {
			return AddTo(Lookup<K, T>.Borrow(comparer), selector, parameter);
		}
		
		#endregion

	}

	/// <summary>
	/// Represents a singly linked list.
	/// </summary>
	public struct LinkedHeadTail<K, T> : IEquatable<LinkedHeadTail<K, T>> {
		/// <summary>
		/// The first node in the list.
		/// </summary>
		public Linked<K, T> head;
		
		/// <summary>
		/// The last node in the list.
		/// </summary>
		public Linked<K, T> tail;

		/// <summary>
		/// The number of elements in the list.
		/// </summary>
		public int count;

		/// <summary>
		/// Returns a list containing a single node with the specified key and value.
		/// </summary>
		public LinkedHeadTail(K key, T value) : this (Linked<K, T>.Borrow(key, value)) {}

		/// <summary>
		/// Returns a list that starts with the specified node.
		/// 
		/// The constructor will traverse the specified node's links to set the tail and count fields.
		/// 
		/// If the specified node is null, the resulting list will be empty.
		/// </summary>
		public LinkedHeadTail(Linked<K, T> head) {
			if (head == null) {
				this.head = null;
				this.tail = null;
				this.count = 0;
			} else {
				this.head = head;
				this.tail = head;
				this.count = 1;

				while (tail.next != null) {
					tail = tail.next;
					++count;
				}
			}
		}

		public override bool Equals(object other) {
			return other is LinkedHeadTail<K, T> && this.Equals((LinkedHeadTail<K, T>) other);
		}
		
		public bool Equals(LinkedHeadTail<K, T> other) {
			return head == other.head;
		}
		
		public override int GetHashCode() {
			return head.GetHashCode();
		}
		
		public static bool operator == (LinkedHeadTail<K, T> lhs, LinkedHeadTail<K, T> rhs) {
			return lhs.head == rhs.head;
		}
		
		public static bool operator != (LinkedHeadTail<K, T> lhs, LinkedHeadTail<K, T> rhs) {
			return lhs.head != rhs.head;
		}

		#region Append

		/// <summary>
		/// Appends a pooled node with with specified key and value to the end of the list.
		/// </summary>
		public void Append(K key, T value) {
			var node = Linked<K, T>.Borrow(key, value);
			if (head == null) {
				head = node;
			} else {
				tail.next = node;
			}
			tail = node;
			++count;
		}
		
		/// <summary>
		/// Appends the specified node to the end of the list.
		/// 
		/// The specified node's links will be traversed to determine the new tail and count.
		/// 
		/// If the specified node is null, the list will not be modified.
		/// </summary>
		public void Append(Linked<K, T> node) {
			if (head == null) {
				head = node;
			} else {
				tail.next = node;
			}
			tail = node;
			++count;
			
			while (tail.next != null) {
				tail = tail.next;
				++count;
			}
		}
		
		/// <summary>
		/// Appends the specified list to the end of this list.
		/// 
		/// This list and the specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public void Append(LinkedHeadTail<K, T> other) {
			if (other.count == 0) {
				// noop
			} else if (head == null) {
				head = other.head;
				tail = other.tail;
				count = other.count;
			} else {
				tail.next = other.head;
				tail = other.tail;
				count += other.count;
			}
		}
		
		#endregion
		
		#region Release

		/// <summary>
		/// Adds the head of the list to the disposal queue.
		/// 
		/// The list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// </summary>
		public void DisposeInBackground() {
			if (head != null) {
				head.DisposeInBackground();
			}
			head = null;
			tail = null;
			count = 0;
		}
		
		/// <summary>
		/// Releases the head of the list to the node pool.
		/// 
		/// The list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// </summary>
		public void Dispose() {
			if (head != null) {
				head.Dispose();
			}
			head = null;
			tail = null;
			count = 0;
		}

		#endregion

		#region Slinqs
		
		/// <summary>
		/// Returns a Slinq that enumerates the values contained in the list without reliquishing ownership of the nodes.
		/// 
		/// The caller of this method is responsible for making sure the returned Slinq is not used after the nodes are modified or returned to the pool.
		/// </summary>
		public Slinq<T, LinkedContext<K, T>> SlinqAndKeep() {
			return LinkedContext<K, T>.Slinq(this, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the values contained in the list without reliquishing ownership of the nodes.
		/// 
		/// The caller of this method is responsible for making sure the returned Slinq is not used after the nodes are modified or returned to the pool.
		/// 
		/// If backtrack detection is enabled, the supplied backtrack detector can be returned to the pool using its TryReleaseShared method to prevent subsequent enumeration of the returned Slinq.
		/// </summary>
		public Slinq<T, LinkedContext<K, T>> SlinqAndKeep(BacktrackDetector bd) {
			return LinkedContext<K, T>.Slinq(this, bd, false);
		}
		
		/// <summary>
		/// Returns a Slinq that enumerates the values contained in the list.
		/// 
		/// Ownership of the nodes contained in the list is transferred to the Slinq.  When the Slinq is disposed, the nodes will be added to the disposal queue.
		/// </summary>
		public Slinq<T, LinkedContext<K, T>> SlinqAndDispose() {
			var slinq = LinkedContext<K, T>.Slinq(this, true);
			
			this.head = null;
			this.tail = null;
			this.count = 0;
			
			return slinq;
		}
		
		#endregion

	}

	/// <summary>
	/// Extension methods for Linked<> and LinkedHeadTail<>.
	/// </summary>
	public static class Linked {

		/// <summary>
		/// Reverses the specified list.
		/// 
		/// The specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public static LinkedHeadTail<T> Reverse<T>(this LinkedHeadTail<T> list) {
			var reversed = new LinkedHeadTail<T>();
			reversed.tail = list.head;
			reversed.count = list.count;

			while (list.head != null) {
				var node = list.head;
				list.head = list.head.next;
				node.next = reversed.head;
				reversed.head = node;
			}
			return reversed;
		}

		/// <summary>
		/// Reverses the specified list.
		/// 
		/// The specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public static LinkedHeadTail<K, T> Reverse<K, T>(this LinkedHeadTail<K, T> list) {
			var reversed = new LinkedHeadTail<K, T>();
			reversed.tail = list.head;
			reversed.count = list.count;
			
			while (list.head != null) {
				var node = list.head;
				list.head = list.head.next;
				node.next = reversed.head;
				reversed.head = node;
			}
			return reversed;
		}
		
		/// <summary>
		/// Sorts the specified list using the specified comparison and ordering.
		///
		/// This method uses an introspective merge sort algorithm that will optimally sort rather than split lists with 3 or fewer nodes.
		/// 
		/// The specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public static LinkedHeadTail<T> Sort<T>(LinkedHeadTail<T> input, Comparison<T> comparison, bool ascending) {
			if (input.count <= 1) {
				return input;
			} else if (input.count == 2) {
				if ((ascending ? comparison(input.head.value, input.tail.value) : comparison(input.tail.value, input.head.value)) <= 0) {
					return input;
				} else {
					input.head.next = null;
					input.tail.next = input.head;
					input.head = input.tail;
					input.tail = input.head.next;
					return input;
				}
			} else if (input.count == 3) {
				var ordering = ascending ? 1 : -1;

				var b = input.head.next;

				if (ordering * comparison(input.head.value, b.value) <= 0) {
					if (ordering * comparison(b.value, input.tail.value) <= 0) {
						// a, b, c
					} else if (ordering * comparison(input.head.value, input.tail.value) <= 0) {
						// a, c, b
						input.tail.next = b;
						input.head.next = input.tail;
						input.tail = b;
						b.next = null;
					} else {
						// c, a, b
						input.tail.next = input.head;
						input.head = input.tail;
						input.tail = b;
						b.next = null;
					}
				} else if (ordering * comparison(b.value, input.tail.value) <= 0) {
					if (ordering * comparison(input.head.value, input.tail.value) <= 0) {
						// b, a, c
						input.head.next = input.tail;
						b.next = input.head;
						input.head = b;
					} else {
						// b, c, a
						input.tail.next = input.head;
						input.tail = input.head;
						input.tail.next = null;
						input.head = b;
					}
				} else {
					// c, b, a
					input.tail = input.head;
					input.head = b.next;
					input.head.next = b;
					b.next = input.tail;
					input.tail.next = null;
				}
				return input;
			} else {
				var left = new LinkedHeadTail<T>();
				var right = new LinkedHeadTail<T>();
				
				left.count = input.count / 2;
				right.count = input.count - left.count;
				
				left.head = input.head;
				left.tail = input.head;
				
				right.tail = input.tail;
				
				for (int i = 1; i < left.count; ++i) {
					left.tail = left.tail.next;
				}
				
				right.head = left.tail.next;
				left.tail.next = null;
				
				return Merge(Sort(left, comparison, ascending), Sort(right, comparison, ascending), comparison, ascending);
			}
		}

		/// <summary>
		/// Sorts the specified list using the specified comparison and ordering.
		///
		/// This method uses an introspective merge sort algorithm that will optimally sort rather than split lists with 3 or fewer nodes.
		/// 
		/// The specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public static LinkedHeadTail<K, T> Sort<K, T>(LinkedHeadTail<K, T> input, Comparison<K> comparison, bool ascending) {
			if (input.count <= 1) {
				return input;
			} else if (input.count == 2) {
				if ((ascending ? comparison(input.head.key, input.tail.key) : comparison(input.tail.key, input.head.key)) <= 0) {
					return input;
				} else {
					input.head.next = null;
					input.tail.next = input.head;
					input.head = input.tail;
					input.tail = input.head.next;
					return input;
				}
			} else if (input.count == 3) {
				var ordering = ascending ? 1 : -1;
				
				var b = input.head.next;
				
				if (ordering * comparison(input.head.key, b.key) <= 0) {
					if (ordering * comparison(b.key, input.tail.key) <= 0) {
						// a, b, c
					} else if (ordering * comparison(input.head.key, input.tail.key) <= 0) {
						// a, c, b
						input.tail.next = b;
						input.head.next = input.tail;
						input.tail = b;
						b.next = null;
					} else {
						// c, a, b
						input.tail.next = input.head;
						input.head = input.tail;
						input.tail = b;
						b.next = null;
					}
				} else if (ordering * comparison(b.key, input.tail.key) <= 0) {
					if (ordering * comparison(input.head.key, input.tail.key) <= 0) {
						// b, a, c
						input.head.next = input.tail;
						b.next = input.head;
						input.head = b;
					} else {
						// b, c, a
						input.tail.next = input.head;
						input.tail = input.head;
						input.tail.next = null;
						input.head = b;
					}
				} else {
					// c, b, a
					input.tail = input.head;
					input.head = b.next;
					input.head.next = b;
					b.next = input.tail;
					input.tail.next = null;
				}
				return input;
			} else {
				var left = new LinkedHeadTail<K, T>();
				var right = new LinkedHeadTail<K, T>();
				
				left.count = input.count / 2;
				right.count = input.count - left.count;
				
				left.head = input.head;
				left.tail = input.head;
				
				right.tail = input.tail;
				
				for (int i = 1; i < left.count; ++i) {
					left.tail = left.tail.next;
				}
				
				right.head = left.tail.next;
				left.tail.next = null;
				
				return Merge(Sort(left, comparison, ascending), Sort(right, comparison, ascending), comparison, ascending);
			}
		}
		
		/// <summary>
		/// Merges the specified sorted lists using the specified comparison and ordering.  Elements from the left list will appear before elements from the right on equal comparisons.
		/// 
		/// The specified lists must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified lists and any variables containing their nodes.
		/// </summary>
		public static LinkedHeadTail<T> Merge<T>(LinkedHeadTail<T> left, LinkedHeadTail<T> right, Comparison<T> comparison, bool ascending) {
			if (left.count == 0) {
				return right;
			} else if (right.count == 0) {
				return left;
			}
			
			var ordering = ascending ? 1 : -1;
			
			if (ordering * comparison(left.tail.value, right.head.value) <= 0) {
				left.tail.next = right.head;
				left.tail = right.tail;
				left.count += right.count;
				return left;
			} else if (ordering * comparison(left.head.value, right.tail.value) > 0) {
				right.tail.next = left.head;
				right.tail = left.tail;
				right.count += left.count;
				return right;
			}

			var sorted = new LinkedHeadTail<T>();
			sorted.count = left.count + right.count;
			
			if (ordering * comparison(left.head.value, right.head.value) <= 0) {
				sorted.head = left.head;
				sorted.tail = left.head;
				left.head = left.head.next;
			} else {
				sorted.head = right.head;
				sorted.tail = right.head;
				right.head = right.head.next;
			}
			
			while (left.head != null && right.head != null) {
				if (ordering * comparison(left.head.value, right.head.value) <= 0) {
					sorted.tail.next = left.head;
					sorted.tail = left.head;
					left.head = left.head.next;
				} else {
					sorted.tail.next = right.head;
					sorted.tail = right.head;
					right.head = right.head.next;
				}
			}
			
			if (left.head == null) {
				sorted.tail.next = right.head;
				sorted.tail = right.tail;
			} else {
				sorted.tail.next = left.head;
				sorted.tail = left.tail;
			} 
			
			return sorted;
		}

		/// <summary>
		/// Merges the specified sorted lists using the specified comparison and ordering.  Elements from the left list will appear before elements from the right on equal comparisons.
		/// 
		/// The specified lists must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified lists and any variables containing their nodes.
		/// </summary>
		public static LinkedHeadTail<K, T> Merge<K, T>(LinkedHeadTail<K, T> left, LinkedHeadTail<K, T> right, Comparison<K> comparison, bool ascending) {
			if (left.count == 0) {
				return right;
			} else if (right.count == 0) {
				return left;
			}

			var ordering = ascending ? 1 : -1;

			if (ordering * comparison(left.tail.key, right.head.key) <= 0) {
				left.tail.next = right.head;
				left.tail = right.tail;
				left.count += right.count;
				return left;
			} else if (ordering * comparison(left.head.key, right.tail.key) > 0) {
				right.tail.next = left.head;
				right.tail = left.tail;
				right.count += left.count;
				return right;
			}

			var sorted = new LinkedHeadTail<K, T>();
			sorted.count = left.count + right.count;

			if (ordering * comparison(left.head.key, right.head.key) <= 0) {
				sorted.head = left.head;
				sorted.tail = left.head;
				left.head = left.head.next;
			} else {
				sorted.head = right.head;
				sorted.tail = right.head;
				right.head = right.head.next;
			}

			while (left.head != null && right.head != null) {
				if (ordering * comparison(left.head.key, right.head.key) <= 0) {
					sorted.tail.next = left.head;
					sorted.tail = left.head;
					left.head = left.head.next;
				} else {
					sorted.tail.next = right.head;
					sorted.tail = right.head;
					right.head = right.head.next;
				}
			}

			if (left.head == null) {
				sorted.tail.next = right.head;
				sorted.tail = right.tail;
			} else {
				sorted.tail.next = left.head;
				sorted.tail = left.tail;
			} 

			return sorted;
		}

		/// <summary>
		/// Sorts the specified list using the specified comparison and ordering using an insertion sort algorithm.
		/// 
		/// The specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public static LinkedHeadTail<T> InsertionSort<T>(LinkedHeadTail<T> input, Comparison<T> comparison, bool ascending) {
			if (input.count <= 1) {
				return input;
			} else {
				var ordering = ascending ? 1 : -1;
				
				var sorted = new LinkedHeadTail<T>();
				sorted.count = input.count;
				
				var insert = input.head;
				input.head = input.head.next;
				
				insert.next = null;
				sorted.head = insert;
				sorted.tail = insert;
				
				var min = insert.value;
				var max = insert.value;
				
				while (input.head != null) {
					insert = input.head;
					input.head = input.head.next;
					
					var current = insert.value;
					
					if (ordering * comparison(current, max) >= 0) {
						max = current;
						insert.next = null;
						sorted.tail.next = insert;
						sorted.tail = insert;
					} else if (ordering * comparison(current, min) < 0) {
						min = current;
						insert.next = sorted.head;
						sorted.head = insert;
					} else {
						var after = sorted.head;
						while (after.next != null && ordering * comparison(current, after.next.value) >= 0) {
							after = after.next;
						}
						insert.next = after.next;
						after.next = insert;
					}
				}
				
				return sorted;
			}
		}

		/// <summary>
		/// Sorts the specified list using the specified comparison and ordering using an insertion sort algorithm.
		/// 
		/// The specified list must be well formed when calling this method or the program will enter an invalid state, resulting in unspecified behaviour.
		/// 
		/// Calling this method will invalidate the specified list and any variables containing its nodes.
		/// </summary>
		public static LinkedHeadTail<K, T> InsertionSort<K, T>(LinkedHeadTail<K, T> input, Comparison<K> comparison, bool ascending) {
			if (input.count <= 1) {
				return input;
			} else {
				var ordering = ascending ? 1 : -1;

				var sorted = new LinkedHeadTail<K, T>();
				sorted.count = input.count;

				var insert = input.head;
				input.head = input.head.next;

				insert.next = null;
				sorted.head = insert;
				sorted.tail = insert;

				var min = insert.key;
				var max = insert.key;

				while (input.head != null) {
					insert = input.head;
					input.head = input.head.next;

					var current = insert.key;

					if (ordering * comparison(current, max) >= 0) {
						max = current;
						insert.next = null;
						sorted.tail.next = insert;
						sorted.tail = insert;
					} else if (ordering * comparison(current, min) < 0) {
						min = current;
						insert.next = sorted.head;
						sorted.head = insert;
					} else {
						var after = sorted.head;
						while (after.next != null && ordering * comparison(current, after.next.key) >= 0) {
							after = after.next;
						}
						insert.next = after.next;
						after.next = insert;
					}
				}
				
				return sorted;
			}
		}
	}
}
