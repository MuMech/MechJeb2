using System;
using Smooth.Algebraics;
using Smooth.Slinq.Collections;

namespace Smooth.Slinq.Context {

	#region Just values

	public struct LinkedContext<T> {

		#region Slinqs

		public static Slinq<T, LinkedContext<T>> Slinq(LinkedHeadTail<T> list, bool release) {
			return new Slinq<T, LinkedContext<T>>(
				skip,
				remove,
				dispose,
				new LinkedContext<T>(list, BacktrackDetector.Borrow(), release));
		}
		
		public static Slinq<T, LinkedContext<T>> Slinq(LinkedHeadTail<T> list, BacktrackDetector bd, bool release) {
			return new Slinq<T, LinkedContext<T>>(
				skip,
				remove,
				dispose,
				new LinkedContext<T>(list, bd, release));
		}
		
		#endregion

		#region Instance
		
		private readonly LinkedHeadTail<T> list;
		private readonly bool release;
		private Linked<T> runner;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private LinkedContext(LinkedHeadTail<T> list, BacktrackDetector bd, bool release) {
			this.list = list;
			this.runner = list.head;
			this.release = release;

			this.bd = bd;
		}
		
		#endregion

		#region Delegates
		
		private static readonly Mutator<T, LinkedContext<T>> skip = Skip;
		private static readonly Mutator<T, LinkedContext<T>> remove = Remove;
		private static readonly Mutator<T, LinkedContext<T>> dispose = Dispose;

		private static void Skip(ref LinkedContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.runner == null) {
				next = new Option<T>();
				context.bd.Release();
				if (context.release) {
					context.list.DisposeInBackground();
				}
			} else {
				next = new Option<T>(context.runner.value);
				context.runner = context.runner.next;
			}
		}

		private static void Remove(ref LinkedContext<T> context, out Option<T> next) {
			throw new NotSupportedException();
		}

		private static void Dispose(ref LinkedContext<T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			if (context.release) {
				context.list.DisposeInBackground();
			}
		}

		#endregion

	}

	#endregion

	#region Keyed
	
	public struct LinkedContext<K, T> {
		
		#region Slinqs
		
		public static Slinq<T, LinkedContext<K, T>> Slinq(LinkedHeadTail<K, T> list, bool release) {
			return new Slinq<T, LinkedContext<K, T>>(
				skip,
				remove,
				dispose,
				new LinkedContext<K, T>(list, BacktrackDetector.Borrow(), release));
		}
		
		public static Slinq<T, LinkedContext<K, T>> Slinq(LinkedHeadTail<K, T> list, BacktrackDetector bd, bool release) {
			return new Slinq<T, LinkedContext<K, T>>(
				skip,
				remove,
				dispose,
				new LinkedContext<K, T>(list, bd, release));
		}
		
		#endregion
		
		#region Instance
		
		private readonly LinkedHeadTail<K, T> list;
		private readonly bool release;
		private Linked<K, T> runner;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private LinkedContext(LinkedHeadTail<K, T> list, BacktrackDetector bd, bool release) {
			this.list = list;
			this.runner = list.head;
			this.release = release;

			this.bd = bd;
		}
		
		#endregion

		#region Delegates
		
		private static readonly Mutator<T, LinkedContext<K, T>> skip = Skip;
		private static readonly Mutator<T, LinkedContext<K, T>> remove = Remove;
		private static readonly Mutator<T, LinkedContext<K, T>> dispose = Dispose;
		
		private static void Skip(ref LinkedContext<K, T> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.runner == null) {
				next = new Option<T>();
				context.bd.Release();
				if (context.release) {
					context.list.DisposeInBackground();
				}
			} else {
				next = new Option<T>(context.runner.value);
				context.runner = context.runner.next;
			}
		}
		
		private static void Remove(ref LinkedContext<K, T> context, out Option<T> next) {
			throw new NotSupportedException();
		}
		
		private static void Dispose(ref LinkedContext<K, T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			if (context.release) {
				context.list.DisposeInBackground();
			}
		}
		
		#endregion
		
	}

	#endregion

}
