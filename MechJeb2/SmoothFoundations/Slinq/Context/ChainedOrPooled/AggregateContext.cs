using System;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Slinq.Context {

	#region Bare

	public struct AggregateContext<U, T, C> {

		#region Slinqs
		
		public static Slinq<U, AggregateContext<U, T, C>> AggregateRunning(Slinq<T, C> slinq, U seed, DelegateFunc<U, T, U> selector) {
			return new Slinq<U, AggregateContext<U, T, C>>(
				skip,
				remove,
				dispose,
				new AggregateContext<U, T, C>(slinq, seed, selector));
		}
		
		#endregion

		#region Context
	
		private bool needsMove;
		private Slinq<T, C> chained;
		private U acc;
		private readonly DelegateFunc<U, T, U> selector;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private AggregateContext(Slinq<T, C> chained, U seed, DelegateFunc<U, T, U> selector) {
			this.needsMove = false;
			this.chained = chained;
			this.acc = seed;
			this.selector = selector;

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion

		#region Delegates

		private static readonly Mutator<U, AggregateContext<U, T, C>> skip = Skip;
		private static readonly Mutator<U, AggregateContext<U, T, C>> remove = Remove;
		private static readonly Mutator<U, AggregateContext<U, T, C>> dispose = Dispose;
		
		private static void Skip(ref AggregateContext<U, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			if (context.chained.current.isSome) {
				context.acc = context.selector(context.acc, context.chained.current.value);
				next = new Option<U>(context.acc);
			} else {
				next = new Option<U>();
				context.bd.Release();
			}
		}

		private static void Remove(ref AggregateContext<U, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}

		private static void Dispose(ref AggregateContext<U, T, C> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}

		#endregion

	}

	#endregion
	
	#region With parameter
	
	public struct AggregateContext<U, T, C, P> {
		
		#region Slinqs
		
		public static Slinq<U, AggregateContext<U, T, C, P>> AggregateRunning(Slinq<T, C> slinq, U seed, DelegateFunc<U, T, P, U> selector, P parameter) {
			return new Slinq<U, AggregateContext<U, T, C, P>>(
				skip, remove, dispose,
				new AggregateContext<U, T, C, P>(slinq, seed, selector, parameter));
		}
		
		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private U acc;
		private readonly DelegateFunc<U, T, P, U> selector;
		private readonly P parameter;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private AggregateContext(Slinq<T, C> chained, U seed, DelegateFunc<U, T, P, U> selector, P parameter) {
			this.needsMove = false;
			this.chained = chained;
			this.acc = seed;
			this.selector = selector;
			this.parameter = parameter;

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<U, AggregateContext<U, T, C, P>> skip = Skip;
		private static readonly Mutator<U, AggregateContext<U, T, C, P>> remove = Remove;
		private static readonly Mutator<U, AggregateContext<U, T, C, P>> dispose = Dispose;
		
		private static void Skip(ref AggregateContext<U, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			if (context.chained.current.isSome) {
				context.acc = context.selector(context.acc, context.chained.current.value, context.parameter);
				next = new Option<U>(context.acc);
			} else {
				next = new Option<U>();
				context.bd.Release();
			}
		}

		private static void Remove(ref AggregateContext<U, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref AggregateContext<U, T, C, P> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}
		
		#endregion
		
	}
	
	#endregion

}


