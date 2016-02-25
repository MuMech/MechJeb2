using System;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	
	#region Slinq
	
	public struct FlattenContext<T, C1, C2> {

		#region Slinqs

		public static Slinq<T, FlattenContext<T, C1, C2>> Flatten(Slinq<Slinq<T, C1>, C2> slinq) {
			return new Slinq<T, FlattenContext<T, C1, C2>>(
				skip,
				remove,
				dispose,
				new FlattenContext<T, C1, C2>(slinq));
		}

		#endregion

		#region Context
		
		private bool needsMove;
		private Slinq<Slinq<T, C1>, C2> chained;
		private Slinq<T, C1> selected;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private FlattenContext(Slinq<Slinq<T, C1>, C2> chained) {
			this.needsMove = false;
			this.chained = chained;
			this.selected = chained.current.isSome ? chained.current.value : new Slinq<T, C1>();

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates

		private static readonly Mutator<T, FlattenContext<T, C1, C2>> skip = Skip;
		private static readonly Mutator<T, FlattenContext<T, C1, C2>> remove = Remove;
		private static readonly Mutator<T, FlattenContext<T, C1, C2>> dispose = Dispose;

		private static void Skip(ref FlattenContext<T, C1, C2> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.selected.skip(ref context.selected.context, out context.selected.current);
			} else {
				context.needsMove = true;
			}

			if (!context.selected.current.isSome && context.chained.current.isSome) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
				while (context.chained.current.isSome) {
					context.selected = context.chained.current.value;
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

		private static void Remove(ref FlattenContext<T, C1, C2> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.selected.remove(ref context.selected.context, out context.selected.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref FlattenContext<T, C1, C2> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			context.selected.dispose(ref context.selected.context, out context.selected.current);
		}

		#endregion

	}
	
	#endregion

	#region Option
	
	public struct FlattenContext<T, C> {
		
		#region Slinqs
		
		public static Slinq<T, FlattenContext<T, C>> Flatten(Slinq<Option<T>, C> slinq) {
			return new Slinq<T, FlattenContext<T, C>>(
				skip,
				remove,
				dispose,
				new FlattenContext<T, C>(slinq));
		}
		
		#endregion
		
		#region Context
		
		private bool needsMove;
		private Slinq<Option<T>, C> chained;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414
		
		private FlattenContext(Slinq<Option<T>, C> chained) {
			this.needsMove = false;
			this.chained = chained;

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		private static readonly Mutator<T, FlattenContext<T, C>> skip = Skip;
		private static readonly Mutator<T, FlattenContext<T, C>> remove = Remove;
		private static readonly Mutator<T, FlattenContext<T, C>> dispose = Dispose;
		
		private static void Skip(ref FlattenContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.chained.current.value.isSome) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}

			if (context.chained.current.isSome) {
				next = context.chained.current.value;
			} else {
				next = new Option<T>();
				context.bd.Release();
			}
		}
		
		private static void Remove(ref FlattenContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			Skip(ref context, out next);
		}
		
		private static void Dispose(ref FlattenContext<T, C> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
		}
		
		#endregion
		
	}
	
	#endregion
	
}
