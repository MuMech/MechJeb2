using System;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Slinq.Context {
	
	#region Tuple
	
	public struct ZipAllContext<T2, C2, T, C> {
		
		#region Slinqs
		
		public static Slinq<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>> ZipAll(Slinq<T, C> left, Slinq<T2, C2> right, ZipRemoveFlags removeFlags) {
			return new Slinq<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>>(
				skip,
				remove,
				dispose,
				new ZipAllContext<T2, C2, T, C>(left, right, removeFlags));
		}
		
		#endregion
		
		#region Context
		
		private bool needsMove;
		private Slinq<T, C> left;
		private Slinq<T2, C2> right;
		private readonly ZipRemoveFlags removeFlags;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414
		
		private ZipAllContext(Slinq<T, C> left, Slinq<T2, C2> right, ZipRemoveFlags removeFlags) {
			this.needsMove = false;
			this.left = left;
			this.right = right;
			this.removeFlags = removeFlags;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>> skip = Skip;
		private static readonly Mutator<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>> remove = Remove;
		private static readonly Mutator<Tuple<Option<T>, Option<T2>>, ZipAllContext<T2, C2, T, C>> dispose = Dispose;
		
		private static void Skip(ref ZipAllContext<T2, C2, T, C> context, out Option<Tuple<Option<T>, Option<T2>>> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				if (context.left.current.isSome) {
					context.left.skip(ref context.left.context, out context.left.current);
					if (context.right.current.isSome) {
						context.right.skip(ref context.right.context, out context.right.current);
					}
				} else {
					context.right.skip(ref context.right.context, out context.right.current);
				}
			} else {
				context.needsMove = true;
			}
			
			if (context.left.current.isSome || context.right.current.isSome) {
				next = new Option<Tuple<Option<T>, Option<T2>>>(new Tuple<Option<T>, Option<T2>>(context.left.current, context.right.current));
			} else {
				next = new Option<Tuple<Option<T>, Option<T2>>>();
			}
			
			if (!next.isSome) {
				context.bd.Release();
			}
		}
		
		private static void Remove(ref ZipAllContext<T2, C2, T, C> context, out Option<Tuple<Option<T>, Option<T2>>> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			
			if (!context.left.current.isSome) {
				// noop
			} else if ((context.removeFlags & ZipRemoveFlags.Left) == ZipRemoveFlags.Left) {
				context.left.remove(ref context.left.context, out context.left.current);
			} else {
				context.left.skip(ref context.left.context, out context.left.current);
			}
			
			if (!context.right.current.isSome) {
				// noop
			} else if ((context.removeFlags & ZipRemoveFlags.Right) == ZipRemoveFlags.Right) {
				context.right.remove(ref context.right.context, out context.right.current);
			} else {
				context.right.skip(ref context.right.context, out context.right.current);
			}
			
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref ZipAllContext<T2, C2, T, C> context, out Option<Tuple<Option<T>, Option<T2>>> next) {
			next = new Option<Tuple<Option<T>, Option<T2>>>();
			
			context.bd.Release();
			
			if (context.left.current.isSome) {
				context.left.dispose(ref context.left.context, out context.left.current);
				if (context.right.current.isSome) {
					context.right.dispose(ref context.right.context, out context.right.current);
				}
			} else {
				context.right.dispose(ref context.right.context, out context.right.current);
			}
		}
		
		#endregion
		
	}
	
	#endregion

	#region With selector
	
	public struct ZipAllContext<U, T2, C2, T, C> {

		#region Slinqs

		public static Slinq<U, ZipAllContext<U, T2, C2, T, C>> ZipAll(Slinq<T, C> left, Slinq<T2, C2> right, DelegateFunc<Option<T>, Option<T2>, U> selector, ZipRemoveFlags removeFlags) {
			return new Slinq<U, ZipAllContext<U, T2, C2, T, C>>(
				skip,
				remove,
				dispose,
				new ZipAllContext<U, T2, C2, T, C>(left, right, selector, removeFlags));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> left;
		private Slinq<T2, C2> right;
		private readonly DelegateFunc<Option<T>, Option<T2>, U> selector;
		private readonly ZipRemoveFlags removeFlags;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private ZipAllContext(Slinq<T, C> left, Slinq<T2, C2> right, DelegateFunc<Option<T>, Option<T2>, U> selector, ZipRemoveFlags removeFlags) {
			this.needsMove = false;
			this.left = left;
			this.right = right;
			this.selector = selector;
			this.removeFlags = removeFlags;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion
		
		#region Delegates
		
		private static readonly Mutator<U, ZipAllContext<U, T2, C2, T, C>> skip = Skip;
		private static readonly Mutator<U, ZipAllContext<U, T2, C2, T, C>> remove = Remove;
		private static readonly Mutator<U, ZipAllContext<U, T2, C2, T, C>> dispose = Dispose;

		private static void Skip(ref ZipAllContext<U, T2, C2, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				if (context.left.current.isSome) {
					context.left.skip(ref context.left.context, out context.left.current);
					if (context.right.current.isSome) {
						context.right.skip(ref context.right.context, out context.right.current);
					}
				} else {
					context.right.skip(ref context.right.context, out context.right.current);
				}
			} else {
				context.needsMove = true;
			}

			if (context.left.current.isSome || context.right.current.isSome) {
				next = new Option<U>(context.selector(context.left.current, context.right.current));
			} else {
				next = new Option<U>();
			}
			
			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref ZipAllContext<U, T2, C2, T, C> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;

			if (!context.left.current.isSome) {
				// noop
			} else if ((context.removeFlags & ZipRemoveFlags.Left) == ZipRemoveFlags.Left) {
				context.left.remove(ref context.left.context, out context.left.current);
			} else {
				context.left.skip(ref context.left.context, out context.left.current);
			}
			
			if (!context.right.current.isSome) {
				// noop
			} else if ((context.removeFlags & ZipRemoveFlags.Right) == ZipRemoveFlags.Right) {
				context.right.remove(ref context.right.context, out context.right.current);
			} else {
				context.right.skip(ref context.right.context, out context.right.current);
			}

			Skip(ref context, out next);
		}
		
		private static void Dispose(ref ZipAllContext<U, T2, C2, T, C> context, out Option<U> next) {
			next = new Option<U>();
			
			context.bd.Release();

			if (context.left.current.isSome) {
				context.left.dispose(ref context.left.context, out context.left.current);
				if (context.right.current.isSome) {
					context.right.dispose(ref context.right.context, out context.right.current);
				}
			} else {
				context.right.dispose(ref context.right.context, out context.right.current);
			}
		}

		#endregion

	}
	
	#endregion
	
	#region With selector and parameter
	
	public struct ZipAllContext<U, T2, C2, T, C, P> {

		#region Slinqs
		
		public static Slinq<U, ZipAllContext<U, T2, C2, T, C, P>> ZipAll(Slinq<T, C> left, Slinq<T2, C2> right, DelegateFunc<Option<T>, Option<T2>, P, U> selector, P parameter, ZipRemoveFlags removeFlags) {
			return new Slinq<U, ZipAllContext<U, T2, C2, T, C, P>>(
				skip,
				remove,
				dispose,
				new ZipAllContext<U, T2, C2, T, C, P>(left, right, selector, parameter, removeFlags));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<T, C> left;
		private Slinq<T2, C2> right;
		private readonly DelegateFunc<Option<T>, Option<T2>, P, U> selector;
		private readonly P parameter;
		private readonly ZipRemoveFlags removeFlags;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private ZipAllContext(Slinq<T, C> left, Slinq<T2, C2> right, DelegateFunc<Option<T>, Option<T2>, P, U> selector, P parameter, ZipRemoveFlags removeFlags) {
			this.needsMove = false;
			this.left = left;
			this.right = right;
			this.selector = selector;
			this.parameter = parameter;
			this.removeFlags = removeFlags;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<U, ZipAllContext<U, T2, C2, T, C, P>> skip = Skip;
		private static readonly Mutator<U, ZipAllContext<U, T2, C2, T, C, P>> remove = Remove;
		private static readonly Mutator<U, ZipAllContext<U, T2, C2, T, C, P>> dispose = Dispose;

		private static void Skip(ref ZipAllContext<U, T2, C2, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				if (context.left.current.isSome) {
					context.left.skip(ref context.left.context, out context.left.current);
					if (context.right.current.isSome) {
						context.right.skip(ref context.right.context, out context.right.current);
					}
				} else {
					context.right.skip(ref context.right.context, out context.right.current);
				}
			} else {
				context.needsMove = true;
			}
			
			if (context.left.current.isSome || context.right.current.isSome) {
				next = new Option<U>(context.selector(context.left.current, context.right.current, context.parameter));
			} else {
				next = new Option<U>();
			}
			
			if (!next.isSome) {
				context.bd.Release();
			}
		}

		private static void Remove(ref ZipAllContext<U, T2, C2, T, C, P> context, out Option<U> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;

			if (!context.left.current.isSome) {
				// noop
			} else if ((context.removeFlags & ZipRemoveFlags.Left) == ZipRemoveFlags.Left) {
				context.left.remove(ref context.left.context, out context.left.current);
			} else {
				context.left.skip(ref context.left.context, out context.left.current);
			}
			
			if (!context.right.current.isSome) {
				// noop
			} else if ((context.removeFlags & ZipRemoveFlags.Right) == ZipRemoveFlags.Right) {
				context.right.remove(ref context.right.context, out context.right.current);
			} else {
				context.right.skip(ref context.right.context, out context.right.current);
			}

			Skip(ref context, out next);
		}
		
		private static void Dispose(ref ZipAllContext<U, T2, C2, T, C, P> context, out Option<U> next) {
			next = new Option<U>();
			
			context.bd.Release();

			if (context.left.current.isSome) {
				context.left.dispose(ref context.left.context, out context.left.current);
				if (context.right.current.isSome) {
					context.right.dispose(ref context.right.context, out context.right.current);
				}
			} else {
				context.right.dispose(ref context.right.context, out context.right.current);
			}
		}

		#endregion

	}
	
	#endregion

}
