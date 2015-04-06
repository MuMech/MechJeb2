using System;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Slinq.Context {
	
	#region No parameter
	
	public struct SelectContext<U, T, C> {

		#region Slinqs

		public static Slinq<U, SelectContext<U, T, C>> Select(Slinq<T, C> slinq, DelegateFunc<T, U> selector) {
			return new Slinq<U, SelectContext<U, T, C>>(
				skip,
				remove,
				dispose,
				new SelectContext<U, T, C>(slinq, selector));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly DelegateFunc<T, U> selector;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private SelectContext(Slinq<T, C> chained, DelegateFunc<T, U> selector) {
			this.needsMove = false;
			this.chained = chained;
			this.selector = selector;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates

		private static readonly Mutator<U, SelectContext<U, T, C>> skip = Skip;
		private static readonly Mutator<U, SelectContext<U, T, C>> remove = Remove;
		private static readonly Mutator<U, SelectContext<U, T, C>> dispose = Dispose;

		private static void Skip(ref SelectContext<U, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			next = context.chained.current.isSome ? new Option<U>(context.selector(context.chained.current.value)): new Option<U>();

			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref SelectContext<U, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}

		private static void Dispose(ref SelectContext<U, T, C> context, out Option<U> next) {
			next = new Option<U>();		
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}
		
		#endregion

	}
	
	#endregion
	
	#region With parameter

	public struct SelectContext<U, T, C, P> {

		#region Slinqs

		public static Slinq<U, SelectContext<U, T, C, P>> Select(Slinq<T, C> slinq, DelegateFunc<T, P, U> selector, P parameter) {
			return new Slinq<U, SelectContext<U, T, C, P>>(
				skip,
				remove,
				dispose,
				new SelectContext<U, T, C, P>(slinq, selector, parameter));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly DelegateFunc<T, P, U> selector;
		private readonly P parameter;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private SelectContext(Slinq<T, C> chained, DelegateFunc<T, P, U> selector, P parameter) {
			this.needsMove = false;
			this.chained = chained;
			this.selector = selector;
			this.parameter = parameter;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<U, SelectContext<U, T, C, P>> skip = Skip;
		private static readonly Mutator<U, SelectContext<U, T, C, P>> remove = Remove;
		private static readonly Mutator<U, SelectContext<U, T, C, P>> dispose = Dispose;

		private static void Skip(ref SelectContext<U, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			next = context.chained.current.isSome ? new Option<U>(context.selector(context.chained.current.value, context.parameter)): new Option<U>();

			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref SelectContext<U, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref SelectContext<U, T, C, P> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}

		#endregion
		
	}

	#endregion

}
