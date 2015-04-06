using System;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Slinq.Context {
	public struct FuncOptionContext<T> {

		#region Slinqs
		
		public static Slinq<T, FuncOptionContext<T>> Sequence(T seed, DelegateFunc<T, Option<T>> selector) {
			return new Slinq<T, FuncOptionContext<T>>(
				skip,
				remove,
				dispose,
				new FuncOptionContext<T>(seed, selector));
		}
		
		#endregion

		#region Context
	
		private bool needsMove;
		private Option<T> acc;
		private readonly DelegateFunc<T, Option<T>> selector;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private FuncOptionContext(T seed, DelegateFunc<T, Option<T>> selector) {
			this.needsMove = false;
			this.acc = new Option<T>(seed);
			this.selector = selector;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates

		private static readonly Mutator<T, FuncOptionContext<T>> skip = Skip;
		private static readonly Mutator<T, FuncOptionContext<T>> remove = skip;
		private static readonly Mutator<T, FuncOptionContext<T>> dispose = Dispose;
		
		private static void Skip(ref FuncOptionContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.acc = context.selector(context.acc.value);
			} else {
				context.needsMove = true;
			}
			
			next = context.acc;
			
			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref FuncOptionContext<T> context, out Option<T> next) {
			throw new NotSupportedException();
		}

		private static void Dispose(ref FuncOptionContext<T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
		}

		#endregion

	}

	public struct FuncOptionContext<T, P> {
		
		#region Slinqs
		
		public static Slinq<T, FuncOptionContext<T, P>> Sequence(T seed, DelegateFunc<T, P, Option<T>> selector, P parameter) {
			return new Slinq<T, FuncOptionContext<T, P>>(
				skip,
				remove,
				dispose,
				new FuncOptionContext<T, P>(seed, selector, parameter));
		}
		
		#endregion

		#region Context
		
		private bool needsMove;
		private Option<T> acc;
		private readonly DelegateFunc<T, P, Option<T>> selector;
		private readonly P parameter;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private FuncOptionContext(T seed, DelegateFunc<T, P, Option<T>> selector, P parameter) {
			this.needsMove = false;
			this.acc = new Option<T>(seed);
			this.selector = selector;
			this.parameter = parameter;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<T, FuncOptionContext<T, P>> skip = Skip;
		private static readonly Mutator<T, FuncOptionContext<T, P>> remove = skip;
		private static readonly Mutator<T, FuncOptionContext<T, P>> dispose = Dispose;
		
		private static void Skip(ref FuncOptionContext<T, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.acc = context.selector(context.acc.value, context.parameter);
			} else {
				context.needsMove = true;
			}

			next = context.acc;

			if (!next.isSome) {
				context.bd.Release();
			}
		}
		
		private static void Remove(ref FuncOptionContext<T, P> context, out Option<T> next) {
			throw new NotSupportedException();
		}
		
		private static void Dispose(ref FuncOptionContext<T, P> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
		}
		
		#endregion

	}
}


