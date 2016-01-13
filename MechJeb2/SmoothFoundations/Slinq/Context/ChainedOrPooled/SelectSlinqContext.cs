using System;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Slinq.Context {
	
	#region No parameter
	
	public struct SelectSlinqContext<U, UC, T, C> {

		#region Slinqs

		public static Slinq<U, SelectSlinqContext<U, UC, T, C>> SelectMany(Slinq<T, C> slinq, DelegateFunc<T, Slinq<U, UC>> selector) {
			return new Slinq<U, SelectSlinqContext<U, UC, T, C>>(
				skip,
				remove,
				dispose,
				new SelectSlinqContext<U, UC, T, C>(slinq, selector));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private Slinq<U, UC> selected;
		private readonly DelegateFunc<T, Slinq<U, UC>> selector;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private SelectSlinqContext(Slinq<T, C> chained, DelegateFunc<T, Slinq<U, UC>> selector) {
			this.needsMove = false;
			this.chained = chained;
			this.selected = chained.current.isSome ? selector(chained.current.value) : new Slinq<U, UC>();
			this.selector = selector;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates

		private static readonly Mutator<U, SelectSlinqContext<U, UC, T, C>> skip = Skip;
		private static readonly Mutator<U, SelectSlinqContext<U, UC, T, C>> remove = Remove;
		private static readonly Mutator<U, SelectSlinqContext<U, UC, T, C>> dispose = Dispose;

		private static void Skip(ref SelectSlinqContext<U, UC, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.selected.skip(ref context.selected.context, out context.selected.current);
			} else {
				context.needsMove = true;
			}

			if (!context.selected.current.isSome && context.chained.current.isSome) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
				while (context.chained.current.isSome) {
					context.selected = context.selector(context.chained.current.value);
					if (context.selected.current.isSome) {
						break;
					} else {
						context.chained.skip(ref context.chained.context, out context.chained.current);
					}
				}
			}

			next = context.selected.current;
			
			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref SelectSlinqContext<U, UC, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.selected.remove(ref context.selected.context, out context.selected.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref SelectSlinqContext<U, UC, T, C> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			context.selected.dispose(ref context.selected.context, out context.selected.current);
		}

		#endregion

	}
	
	#endregion
	
	#region With parameter
	
	public struct SelectSlinqContext<U, UC, T, C, P> {
		
		#region Slinqs
		
		public static Slinq<U, SelectSlinqContext<U, UC, T, C, P>> SelectMany(Slinq<T, C> slinq, DelegateFunc<T, P, Slinq<U, UC>> selector, P parameter) {
			return new Slinq<U, SelectSlinqContext<U, UC, T, C, P>>(
				skip,
				remove,
				dispose,
				new SelectSlinqContext<U, UC, T, C, P>(slinq, selector, parameter));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private Slinq<U, UC> selected;
		private readonly DelegateFunc<T, P, Slinq<U, UC>> selector;
		private readonly P parameter;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private SelectSlinqContext(Slinq<T, C> chained, DelegateFunc<T, P, Slinq<U, UC>> selector, P parameter) {
			this.needsMove = false;
			this.chained = chained;
			this.selected = chained.current.isSome ? selector(chained.current.value, parameter) : new Slinq<U, UC>();
			this.selector = selector;
			this.parameter = parameter;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<U, SelectSlinqContext<U, UC, T, C, P>> skip = Skip;
		private static readonly Mutator<U, SelectSlinqContext<U, UC, T, C, P>> remove = Remove;
		private static readonly Mutator<U, SelectSlinqContext<U, UC, T, C, P>> dispose = Dispose;

		private static void Skip(ref SelectSlinqContext<U, UC, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.selected.skip(ref context.selected.context, out context.selected.current);
			} else {
				context.needsMove = true;
			}
			
			if (!context.selected.current.isSome && context.chained.current.isSome) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
				while (context.chained.current.isSome) {
					context.selected = context.selector(context.chained.current.value, context.parameter);
					if (context.selected.current.isSome) {
						break;
					} else {
						context.chained.skip(ref context.chained.context, out context.chained.current);
					}
				}
			}
			
			next = context.selected.current;
			
			if (!next.isSome) {
				context.bd.Release();
			}
		}
		
		private static void Remove(ref SelectSlinqContext<U, UC, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.selected.remove(ref context.selected.context, out context.selected.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref SelectSlinqContext<U, UC, T, C, P> context, out Option<U> next) {
			next = new Option<U>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			context.selected.dispose(ref context.selected.context, out context.selected.current);
		}

		#endregion
		
	}
	
	#endregion
	
}
