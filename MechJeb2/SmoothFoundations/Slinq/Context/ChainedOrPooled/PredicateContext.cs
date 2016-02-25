using System;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Slinq.Context {
	
	#region No parameter
	
	public struct PredicateContext<T, C> {

		#region Slinqs

		public static Slinq<T, PredicateContext<T, C>> TakeWhile(Slinq<T, C> slinq, DelegateFunc<T, bool> predicate) {
			return new Slinq<T, PredicateContext<T, C>>(
				takeWhileSkip,
				takeWhileRemove,
				dispose,
				new PredicateContext<T, C>(slinq, predicate));
		}
		
		public static Slinq<T, PredicateContext<T, C>> Where(Slinq<T, C> slinq, DelegateFunc<T, bool> predicate) {
			return new Slinq<T, PredicateContext<T, C>>(
				whereSkip,
				whereRemove,
				dispose,
				new PredicateContext<T, C>(slinq, predicate));
		}

		#endregion

		#region Context

		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly DelegateFunc<T, bool> predicate;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private PredicateContext(Slinq<T, C> chained, DelegateFunc<T, bool> predicate) {
			this.needsMove = false;
			this.chained = chained;
			this.predicate = predicate;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion

		#region Dispose

		private static readonly Mutator<T, PredicateContext<T, C>> dispose = Dispose;

		private static void Dispose(ref PredicateContext<T, C> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}

		#endregion

		#region TakeWhile

		private static readonly Mutator<T, PredicateContext<T, C>> takeWhileSkip = TakeWhileSkip;
		private static readonly Mutator<T, PredicateContext<T, C>> takeWhileRemove = TakeWhileRemove;

		private static void TakeWhileSkip(ref PredicateContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			if (!context.chained.current.isSome) {
				next = context.chained.current;
				context.bd.Release();
			} else if (context.predicate(context.chained.current.value)) {
				next = context.chained.current;
			} else {
				next = new Option<T>();
				context.bd.Release();
				context.chained.dispose(ref context.chained.context, out context.chained.current);
			}
		}
		
		private static void TakeWhileRemove(ref PredicateContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			TakeWhileSkip(ref context, out next);
		}

		#endregion

		#region Where
		
		private static readonly Mutator<T, PredicateContext<T, C>> whereSkip = WhereSkip;
		private static readonly Mutator<T, PredicateContext<T, C>> whereRemove = WhereRemove;

		private static void WhereSkip(ref PredicateContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.predicate(context.chained.current.value)) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}

			next = context.chained.current;

			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void WhereRemove(ref PredicateContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			WhereSkip(ref context, out next);
		}

		#endregion

	}
	
	#endregion
	
	#region With parameter
	
	public struct PredicateContext<T, C, P> {

		#region Slinqs
		
		public static Slinq<T, PredicateContext<T, C, P>> TakeWhile(Slinq<T, C> slinq, DelegateFunc<T, P, bool> predicate, P parameter) {
			return new Slinq<T, PredicateContext<T, C, P>>(
				takeWhileSkip,
				takeWhileRemove,
				dispose,
				new PredicateContext<T, C, P>(slinq, predicate, parameter));
		}
		
		public static Slinq<T, PredicateContext<T, C, P>> Where(Slinq<T, C> slinq, DelegateFunc<T, P, bool> predicate, P parameter) {
			return new Slinq<T, PredicateContext<T, C, P>>(
				whereSkip,
				whereRemove,
				dispose,
				new PredicateContext<T, C, P>(slinq, predicate, parameter));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly DelegateFunc<T, P, bool> predicate;
		private readonly P parameter;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private PredicateContext(Slinq<T, C> chained, DelegateFunc<T, P, bool> predicate, P parameter) {
			this.needsMove = false;
			this.chained = chained;
			this.predicate = predicate;
			this.parameter = parameter;

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Dispose
		
		private static readonly Mutator<T, PredicateContext<T, C, P>> dispose = Dispose;
		
		private static void Dispose(ref PredicateContext<T, C, P> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}
		
		#endregion
		
		#region TakeWhile
		
		private static readonly Mutator<T, PredicateContext<T, C, P>> takeWhileSkip = TakeWhileSkip;
		private static readonly Mutator<T, PredicateContext<T, C, P>> takeWhileRemove = TakeWhileRemove;

		private static void TakeWhileSkip(ref PredicateContext<T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			if (!context.chained.current.isSome) {
				next = context.chained.current;
				context.bd.Release();
			} else if (context.predicate(context.chained.current.value, context.parameter)) {
				next = context.chained.current;
			} else {
				next = new Option<T>();
				context.bd.Release();
				context.chained.dispose(ref context.chained.context, out context.chained.current);
			}
		}

		private static void TakeWhileRemove(ref PredicateContext<T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			TakeWhileSkip(ref context, out next);
		}
		
		#endregion
		
		#region Where
		
		private static readonly Mutator<T, PredicateContext<T, C, P>> whereSkip = WhereSkip;
		private static readonly Mutator<T, PredicateContext<T, C, P>> whereRemove = WhereRemove;

		private static void WhereSkip(ref PredicateContext<T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.predicate(context.chained.current.value, context.parameter)) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}

			next = context.chained.current;

			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void WhereRemove(ref PredicateContext<T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			WhereSkip(ref context, out next);
		}
		
		#endregion
		
	}
	
	#endregion
	
}