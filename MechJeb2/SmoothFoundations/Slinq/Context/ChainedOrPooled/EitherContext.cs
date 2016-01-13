using System;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct EitherContext<C2, T, C> {

		#region Slinqs

		public static Slinq<T, EitherContext<C2, T, C>> Left(Slinq<T, C> left) {
			return new Slinq<T, EitherContext<C2, T, C>>(
				skip,
				remove,
				dispose,
				new EitherContext<C2, T, C>(left, default(Slinq<T, C2>), true));
		}

		public static Slinq<T, EitherContext<C2, T, C>> Right(Slinq<T, C2> right) {
			return new Slinq<T, EitherContext<C2, T, C>>(
				skip,
				remove,
				dispose,
				new EitherContext<C2, T, C>(default(Slinq<T, C>), right, false));
		}

		#endregion

		#region Context

		private readonly bool isLeft;
		private bool needsMove;
		private Slinq<T, C> left;
		private Slinq<T, C2> right;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private EitherContext(Slinq<T, C> left, Slinq<T, C2> right, bool isLeft) {
			this.needsMove = false;
			this.isLeft = isLeft;
			this.left = left;
			this.right = right;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion
		
		#region Delegates

		private static readonly Mutator<T, EitherContext<C2, T, C>> skip = Skip;
		private static readonly Mutator<T, EitherContext<C2, T, C>> remove = Remove;
		private static readonly Mutator<T, EitherContext<C2, T, C>> dispose = Dispose;

		private static void Skip(ref EitherContext<C2, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				if (context.isLeft) {
					context.left.skip(ref context.left.context, out context.left.current);
				} else {
					context.right.skip(ref context.right.context, out context.right.current);
				}
			} else {
				context.needsMove = true;
			}

			next = context.isLeft ? context.left.current : context.right.current;

			if (!next.isSome) {
				context.bd.Release();
			}
		}


		private static void Remove(ref EitherContext<C2, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			context.needsMove = false;
			if (context.isLeft) {
				context.left.remove(ref context.left.context, out context.left.current);
			} else {
				context.right.remove(ref context.right.context, out context.right.current);
			}
			Skip(ref context, out next);
		}

		private static void Dispose(ref EitherContext<C2, T, C> context, out Option<T> next) {
			next = Option<T>.None;

			context.bd.Release();

			if (context.isLeft) {
				context.left.dispose(ref context.left.context, out context.left.current);
			} else {
				context.right.dispose(ref context.right.context, out context.right.current);
			}
		}

		#endregion

	}
}


