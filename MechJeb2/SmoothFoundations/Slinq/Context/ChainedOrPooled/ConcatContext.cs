using System;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct ConcatContext<C2, T, C> {

		#region Slinqs

		public static Slinq<T, ConcatContext<C2, T, C>> Concat(Slinq<T, C> first, Slinq<T, C2> second) {
			return new Slinq<T, ConcatContext<C2, T, C>>(
				skip,
				remove,
				dispose,
				new ConcatContext<C2, T, C>(first, second));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> first;
		private Slinq<T, C2> second;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private ConcatContext(Slinq<T, C> first, Slinq<T, C2> second) {
			this.needsMove = false;
			this.first = first;
			this.second = second;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion
		
		#region Delegates

		private static readonly Mutator<T, ConcatContext<C2, T, C>> skip = Skip;
		private static readonly Mutator<T, ConcatContext<C2, T, C>> remove = Remove;
		private static readonly Mutator<T, ConcatContext<C2, T, C>> dispose = Dispose;

		private static void Skip(ref ConcatContext<C2, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				if (context.first.current.isSome) {
					context.first.skip(ref context.first.context, out context.first.current);
				} else {
					context.second.skip(ref context.second.context, out context.second.current);
				}
			} else {
				context.needsMove = true;
			}

			next = context.first.current.isSome ? context.first.current : context.second.current;

			if (!next.isSome) {
				context.bd.Release();
			}
		}


		private static void Remove(ref ConcatContext<C2, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			context.needsMove = false;
			if (context.first.current.isSome) {
				context.first.remove(ref context.first.context, out context.first.current);
			} else {
				context.second.remove(ref context.second.context, out context.second.current);
			}
			Skip(ref context, out next);
		}

		private static void Dispose(ref ConcatContext<C2, T, C> context, out Option<T> next) {
			next = new Option<T>();

			context.bd.Release();

			if (context.first.current.isSome) {
				context.first.dispose(ref context.first.context, out context.first.current);
				if (context.second.current.isSome) {
					context.second.dispose(ref context.second.context, out context.second.current);
				}
			} else {
				context.second.dispose(ref context.second.context, out context.second.current);
			}
		}

		#endregion

	}
}


