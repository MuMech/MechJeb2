using System;
using Smooth.Algebraics;
using Smooth.Delegates;
using Smooth.Slinq.Collections;

namespace Smooth.Slinq.Context {

	#region Bare

	public struct GroupJoinContext<U, K, T2, T, C> {

		#region Slinqs

		public static Slinq<U, GroupJoinContext<U, K, T2, T, C>> GroupJoin(Lookup<K, T2> lookup, Slinq<T, C> outer, DelegateFunc<T, K> outerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, U> resultSelector, bool release) {
			return new Slinq<U, GroupJoinContext<U, K, T2, T, C>>(
				skip,
				remove,
				dispose,
				new GroupJoinContext<U, K, T2, T, C>(lookup, outer, outerSelector, resultSelector, release));
		}

		#endregion

		#region Instance

		private bool needsMove;
		private readonly Lookup<K, T2> lookup;
		private readonly DelegateFunc<T, K> outerSelector;
		private readonly DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, U> resultSelector;
		private readonly bool release;
		private Slinq<T, C> chained;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private GroupJoinContext(Lookup<K, T2> lookup, Slinq<T, C> outer, DelegateFunc<T, K> outerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, U> resultSelector, bool release) {
			this.needsMove = false;
			this.lookup = lookup;
			this.outerSelector = outerSelector;
			this.resultSelector = resultSelector;
			this.chained = outer;
			this.release = release;

			this.bd = BacktrackDetector.Borrow();
		}

		#endregion
		
		#region Delegates

		private static readonly Mutator<U, GroupJoinContext<U, K, T2, T, C>> skip = Skip;
		private static readonly Mutator<U, GroupJoinContext<U, K, T2, T, C>> remove = Remove;
		private static readonly Mutator<U, GroupJoinContext<U, K, T2, T, C>> dispose = Dispose;

		private static void Skip(ref GroupJoinContext<U, K, T2, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			if (context.chained.current.isSome) {
				var bd = BacktrackDetector.Borrow();
				next = new Option<U>(context.resultSelector(
					context.chained.current.value,
					context.lookup.GetValues(context.outerSelector(context.chained.current.value)).SlinqAndKeep(bd)));
				bd.TryReleaseShared();
			} else {
				next = new Option<U>();
				context.bd.Release();
				if (context.release) {
					context.lookup.DisposeInBackground();
				}
			}
		}

		private static void Remove(ref GroupJoinContext<U, K, T2, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();

			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}

		private static void Dispose(ref GroupJoinContext<U, K, T2, T, C> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			if (context.release) {
				context.lookup.DisposeInBackground();
			}
		}

		#endregion

	}

	#endregion
	
	#region With parameter
	
	public struct GroupJoinContext<U, K, T2, T, C, P> {
		
		#region Slinqs
		
		public static Slinq<U, GroupJoinContext<U, K, T2, T, C, P>> GroupJoin(Lookup<K, T2> lookup, Slinq<T, C> outer, DelegateFunc<T, P, K> outerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, P, U> resultSelector, P parameter, bool release) {
			return new Slinq<U, GroupJoinContext<U, K, T2, T, C, P>>(
				skip,
				remove,
				dispose,
				new GroupJoinContext<U, K, T2, T, C, P>(lookup, outer, outerSelector, resultSelector, parameter, release));
		}

		#endregion

		#region Instance
		
		private bool needsMove;
		private readonly Lookup<K, T2> lookup;
		private readonly DelegateFunc<T, P, K> outerSelector;
		private readonly DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, P, U> resultSelector;
		private readonly P parameter;
		private readonly bool release;
		private Slinq<T, C> chained;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private GroupJoinContext(Lookup<K, T2> lookup, Slinq<T, C> outer, DelegateFunc<T, P, K> outerSelector, DelegateFunc<T, Slinq<T2, LinkedContext<T2>>, P, U> resultSelector, P parameter, bool release) {
			this.needsMove = false;
			this.lookup = lookup;
			this.outerSelector = outerSelector;
			this.resultSelector = resultSelector;
			this.parameter = parameter;
			this.chained = outer;
			this.release = release;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<U, GroupJoinContext<U, K, T2, T, C, P>> skip = Skip;
		private static readonly Mutator<U, GroupJoinContext<U, K, T2, T, C, P>> remove = Remove;
		private static readonly Mutator<U, GroupJoinContext<U, K, T2, T, C, P>> dispose = Dispose;

		private static void Skip(ref GroupJoinContext<U, K, T2, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			if (context.chained.current.isSome) {
				var bd = BacktrackDetector.Borrow();
				next = new Option<U>(context.resultSelector(
					context.chained.current.value,
					context.lookup.GetValues(context.outerSelector(context.chained.current.value, context.parameter)).SlinqAndKeep(bd),
					context.parameter));
				bd.TryReleaseShared();
			} else {
				next = new Option<U>();
				context.bd.Release();
				if (context.release) {
					context.lookup.DisposeInBackground();
				}
			}
		}	

		private static void Remove(ref GroupJoinContext<U, K, T2, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref GroupJoinContext<U, K, T2, T, C, P> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			if (context.release) {
				context.lookup.DisposeInBackground();
			}
		}

		#endregion
		
	}
	
	#endregion

}
