using System;
using Smooth.Algebraics;
using Smooth.Slinq.Collections;

namespace Smooth.Slinq.Context {

	public struct GroupByContext<K, T> {

		#region Slinqs
		
		public static Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> Slinq(Lookup<K, T> lookup, bool release) {
			return new Slinq<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>>(
				slinqedSkip,
				slinqedRemove,
				slinqedDispose,
				new GroupByContext<K, T>(lookup, release));
		}
		
		public static Slinq<Grouping<K, T>, GroupByContext<K, T>> SlinqLinked(Lookup<K, T> lookup, bool release) {
			return new Slinq<Grouping<K, T>, GroupByContext<K, T>>(
				linkedSkip,
				linkedRemove,
				linkedDispose,
				new GroupByContext<K, T>(lookup, release));
		}
		
		#endregion

		#region Instance

		private readonly Lookup<K, T> lookup;
		private readonly bool release;
		private Linked<K> runner;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private GroupByContext(Lookup<K, T> lookup, bool release) {
			this.lookup = lookup;
			this.release = release;
			this.runner = lookup.keys.head;

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion

		#region Delegates

		#region Linked

		private static readonly Mutator<Grouping<K, T>, GroupByContext<K, T>> linkedSkip = LinkedSkip;
		private static readonly Mutator<Grouping<K, T>, GroupByContext<K, T>> linkedRemove = LinkedRemove;
		private static readonly Mutator<Grouping<K, T>, GroupByContext<K, T>> linkedDispose = LinkedDispose;
		
		private static void LinkedSkip(ref GroupByContext<K, T> context, out Option<Grouping<K, T>> next) {
			context.bd.DetectBacktrack();

			if (context.runner == null) {
				next = new Option<Grouping<K, T>>();
				context.bd.Release();
				if (context.release) {
					context.lookup.DisposeInBackground();
				}
			} else {
				next = new Option<Grouping<K, T>>(
					new Grouping<K, T>(context.runner.value,
				                   context.release ?
				                   context.lookup.RemoveValues(context.runner.value) :
				                   context.lookup.GetValues(context.runner.value)));
				context.runner = context.runner.next;
			}
		}

		private static void LinkedRemove(ref GroupByContext<K, T> context, out Option<Grouping<K, T>> next) {
			throw new NotSupportedException();
		}
		
		private static void LinkedDispose(ref GroupByContext<K, T> context, out Option<Grouping<K, T>> next) {
			next = new Option<Grouping<K, T>>();
			context.bd.Release();
			if (context.release) {
				context.lookup.DisposeInBackground();
			}
		}
		
		#endregion

		#region Slinqed
		
		private static readonly Mutator<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> slinqedSkip = SlinqedSkip;
		private static readonly Mutator<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> slinqedRemove = SlinqedRemove;
		private static readonly Mutator<Grouping<K, T, LinkedContext<T>>, GroupByContext<K, T>> slinqedDispose = SlinqedDispose;
		
		private static void SlinqedSkip(ref GroupByContext<K, T> context, out Option<Grouping<K, T, LinkedContext<T>>> next) {
			context.bd.DetectBacktrack();
			
			if (context.runner == null) {
				next = new Option<Grouping<K, T, LinkedContext<T>>>();
				context.bd.Release();
				if (context.release) {
					context.lookup.DisposeInBackground();
				}
			} else {
				next = new Option<Grouping<K, T, LinkedContext<T>>>(
					new Grouping<K, T, LinkedContext<T>>(context.runner.value,
				                                     context.release ?
				                                     context.lookup.RemoveValues(context.runner.value).SlinqAndDispose() :
				                                     context.lookup.GetValues(context.runner.value).SlinqAndKeep()));
				context.runner = context.runner.next;
			}
		}
		
		private static void SlinqedRemove(ref GroupByContext<K, T> context, out Option<Grouping<K, T, LinkedContext<T>>> next) {
			throw new NotSupportedException();
		}
		
		private static void SlinqedDispose(ref GroupByContext<K, T> context, out Option<Grouping<K, T, LinkedContext<T>>> next) {
			next = new Option<Grouping<K, T, LinkedContext<T>>>();
			context.bd.Release();
			if (context.release) {
				context.lookup.DisposeInBackground();
			}
		}
		
		#endregion

		#endregion

	}
}