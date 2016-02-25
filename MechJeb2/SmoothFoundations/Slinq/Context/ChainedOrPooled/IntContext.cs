using System;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct IntContext<T, C> {

		#region Slinqs

		public static Slinq<T, IntContext<T, C>> Take(Slinq<T, C> slinq, int count) {
			return new Slinq<T, IntContext<T, C>>(
				skip,
				remove,
				dispose,
				new IntContext<T, C>(slinq, count));
		}
		
		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private int count;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private IntContext(Slinq<T, C> chained, int count) {
			this.needsMove = false;
			this.chained = chained;
			this.count = count;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion
		
		#region Delegates

		private static readonly Mutator<T, IntContext<T, C>> skip = Skip;
		private static readonly Mutator<T, IntContext<T, C>> remove = Remove;
		private static readonly Mutator<T, IntContext<T, C>> dispose = Dispose;

		private static void Skip(ref IntContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.count-- > 0) {
				if (context.needsMove) {
					context.chained.skip(ref context.chained.context, out context.chained.current);
				} else {
					context.needsMove = true;
				}
			} else if (context.chained.current.isSome) {
				context.chained.dispose(ref context.chained.context, out context.chained.current);
			}

			next = context.chained.current;

			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref IntContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref IntContext<T, C> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}

		#endregion

	}
}


