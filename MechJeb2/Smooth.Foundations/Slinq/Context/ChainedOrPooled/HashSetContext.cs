using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Delegates;
using Smooth.Dispose;

namespace Smooth.Slinq.Context {

	#region Bare

	public struct HashSetContext<T, C> {

		#region Slinqs

		public static Slinq<T, HashSetContext<T, C>> Distinct(Slinq<T, C> slinq, Disposable<HashSet<T>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<T, C>>(
				distinctSkip,
				distinctRemove,
				dispose,
				new HashSetContext<T, C>(slinq, hashSet, release));
		}

		public static Slinq<T, HashSetContext<T, C>> Except(Slinq<T, C> slinq, Disposable<HashSet<T>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<T, C>>(
				exceptSkip,
				exceptRemove,
				dispose,
				new HashSetContext<T, C>(slinq, hashSet, release));
		}

		public static Slinq<T, HashSetContext<T, C>> Intersect(Slinq<T, C> slinq, Disposable<HashSet<T>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<T, C>>(
				intersectSkip,
				intersectRemove,
				dispose,
				new HashSetContext<T, C>(slinq, hashSet, release));
		}

		#endregion

		#region Instance

		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly Disposable<HashSet<T>> hashSet;
		private readonly bool release;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private HashSetContext(Slinq<T, C> chained, Disposable<HashSet<T>> hashSet, bool release) {
			this.needsMove = false;
			this.chained = chained;
			this.hashSet = hashSet;
			this.release = release;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion
		
		#region Delegates

		#region Dispose

		private static readonly Mutator<T, HashSetContext<T, C>> dispose = Dispose;
		
		private static void Dispose(ref HashSetContext<T, C> context, out Option<T> next) {
			next = new Option<T>();

			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			if (context.release) {
				context.hashSet.Dispose();
			}
		}
	
		#endregion

		#region Distinct

		private static readonly Mutator<T, HashSetContext<T, C>> distinctSkip = DistinctSkip;
		private static readonly Mutator<T, HashSetContext<T, C>> distinctRemove = DistinctRemove;

		private static void DistinctSkip(ref HashSetContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			while (context.chained.current.isSome && !context.hashSet.value.Add(context.chained.current.value)) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}

			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}

		private static void DistinctRemove(ref HashSetContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			DistinctSkip(ref context, out next);
		}
		
		#endregion
		
		#region Except

		private static readonly Mutator<T, HashSetContext<T, C>> exceptSkip = ExceptSkip;
		private static readonly Mutator<T, HashSetContext<T, C>> exceptRemove = ExceptRemove;
		
		private static void ExceptSkip(ref HashSetContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			while (context.chained.current.isSome && context.hashSet.value.Contains(context.chained.current.value)) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}

			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void ExceptRemove(ref HashSetContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			ExceptSkip(ref context, out next);
		}

		#endregion

		#region Intersect
		
		private static readonly Mutator<T, HashSetContext<T, C>> intersectSkip = IntersectSkip;
		private static readonly Mutator<T, HashSetContext<T, C>> intersectRemove = IntersectRemove;
		
		private static void IntersectSkip(ref HashSetContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}

			while (context.chained.current.isSome && !context.hashSet.value.Remove(context.chained.current.value)) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}

			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void IntersectRemove(ref HashSetContext<T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			IntersectSkip(ref context, out next);
		}
		
		#endregion
		
		#endregion

	}

	#endregion
	
	#region Keyed
	
	public struct HashSetContext<K, T, C> {
		
		#region Slinqs
		
		public static Slinq<T, HashSetContext<K, T, C>> Distinct(Slinq<T, C> slinq, DelegateFunc<T, K> selector, Disposable<HashSet<K>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<K, T, C>>(
				distinctSkip,
				distinctRemove,
				dispose,
				new HashSetContext<K, T, C>(slinq, selector, hashSet, release));
		}

		public static Slinq<T, HashSetContext<K, T, C>> Except(Slinq<T, C> slinq, DelegateFunc<T, K> selector, Disposable<HashSet<K>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<K, T, C>>(
				exceptSkip,
				exceptRemove,
				dispose,
				new HashSetContext<K, T, C>(slinq, selector, hashSet, release));
		}

		public static Slinq<T, HashSetContext<K, T, C>> Intersect(Slinq<T, C> slinq, DelegateFunc<T, K> selector, Disposable<HashSet<K>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<K, T, C>>(
				HashSetContext<K, T, C>.intersectSkip,
				HashSetContext<K, T, C>.intersectRemove,
				HashSetContext<K, T, C>.dispose,
				new HashSetContext<K, T, C>(slinq, selector, hashSet, release));
		}

		#endregion

		#region Instance
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly DelegateFunc<T, K> selector;
		private readonly Disposable<HashSet<K>> hashSet;
		private readonly bool release;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private HashSetContext(Slinq<T, C> chained, DelegateFunc<T, K> selector, Disposable<HashSet<K>> hashSet, bool release) {
			this.needsMove = false;
			this.chained = chained;
			this.selector = selector;
			this.hashSet = hashSet;
			this.release = release;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		#region Dispose
		
		private static readonly Mutator<T, HashSetContext<K, T, C>> dispose = Dispose;
		
		private static void Dispose(ref HashSetContext<K, T, C> context, out Option<T> next) {
			next = new Option<T>();
			
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			if (context.release) {
				context.hashSet.Dispose();
			}
		}
		
		#endregion
		
		#region Distinct
		
		private static readonly Mutator<T, HashSetContext<K, T, C>> distinctSkip = DistinctSkip;
		private static readonly Mutator<T, HashSetContext<K, T, C>> distinctRemove = DistinctRemove;
		
		private static void DistinctSkip(ref HashSetContext<K, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.hashSet.value.Add(context.selector(context.chained.current.value))) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}
			
			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void DistinctRemove(ref HashSetContext<K, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			DistinctSkip(ref context, out next);
		}
		
		#endregion
		
		#region Except
		
		private static readonly Mutator<T, HashSetContext<K, T, C>> exceptSkip = ExceptSkip;
		private static readonly Mutator<T, HashSetContext<K, T, C>> exceptRemove = ExceptRemove;
		
		private static void ExceptSkip(ref HashSetContext<K, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && context.hashSet.value.Contains(context.selector(context.chained.current.value))) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}
			
			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void ExceptRemove(ref HashSetContext<K, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			ExceptSkip(ref context, out next);
		}
		
		#endregion
		
		#region Intersect
		
		private static readonly Mutator<T, HashSetContext<K, T, C>> intersectSkip = IntersectSkip;
		private static readonly Mutator<T, HashSetContext<K, T, C>> intersectRemove = IntersectRemove;
		
		private static void IntersectSkip(ref HashSetContext<K, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.hashSet.value.Remove(context.selector(context.chained.current.value))) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}
			
			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void IntersectRemove(ref HashSetContext<K, T, C> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			IntersectSkip(ref context, out next);
		}
		
		#endregion
		
		#endregion

	}
	
	#endregion

	#region Keyed with parameter
	
	public struct HashSetContext<K, T, C, P> {
		
		#region Slinqs
		
		public static Slinq<T, HashSetContext<K, T, C, P>> Distinct(Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, Disposable<HashSet<K>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<K, T, C, P>>(
				distinctSkip,
				distinctRemove,
				dispose,
				new HashSetContext<K, T, C, P>(slinq, selector, parameter, hashSet, release));
		}

		public static Slinq<T, HashSetContext<K, T, C, P>> Except(Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, Disposable<HashSet<K>> hashSet, bool release) {
			return new Slinq<T, HashSetContext<K, T, C, P>>(
				exceptSkip,
				exceptRemove,
				dispose,
				new HashSetContext<K, T, C, P>(slinq, selector, parameter, hashSet, release));
		}

		public static Slinq<T, HashSetContext<K, T, C, P>> Intersect(Slinq<T, C> slinq, DelegateFunc<T, P, K> selector, P parameter, Disposable<HashSet<K>> hashSet,  bool release) {
			return new Slinq<T, HashSetContext<K, T, C, P>>(
				intersectSkip,
				intersectRemove,
				dispose,
				new HashSetContext<K, T, C, P>(slinq, selector, parameter, hashSet, release));
		}

		#endregion

		#region Instance
		
		private bool needsMove;
		private Slinq<T, C> chained;
		private readonly DelegateFunc<T, P, K> selector;
		private readonly P parameter;
		private readonly Disposable<HashSet<K>> hashSet;
		private readonly bool release;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private HashSetContext(Slinq<T, C> chained, DelegateFunc<T, P, K> selector, P parameter, Disposable<HashSet<K>> hashSet, bool release) {
			this.needsMove = false;
			this.chained = chained;
			this.selector = selector;
			this.parameter = parameter;
			this.hashSet = hashSet;
			this.release = release;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		#region Dispose
		
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> dispose = Dispose;
		
		private static void Dispose(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			next = new Option<T>();
			
			context.bd.Release();
			context.chained.dispose(ref context.chained.context, out context.chained.current);
			if (context.release) {
				context.hashSet.Dispose();
			}
		}
		
		#endregion
		
		#region Distinct
		
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> distinctSkip = DistinctSkip;
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> distinctRemove = DistinctRemove;
		
		private static void DistinctSkip(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.hashSet.value.Add(context.selector(context.chained.current.value, context.parameter))) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}
			
			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void DistinctRemove(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			DistinctSkip(ref context, out next);
		}
		
		#endregion
		
		#region Except
		
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> exceptSkip = ExceptSkip;
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> exceptRemove = ExceptRemove;
		
		private static void ExceptSkip(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && context.hashSet.value.Contains(context.selector(context.chained.current.value, context.parameter))) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}
			
			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void ExceptRemove(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			ExceptSkip(ref context, out next);
		}
		
		#endregion
		
		#region Intersect
		
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> intersectSkip = IntersectSkip;
		private static readonly Mutator<T, HashSetContext<K, T, C, P>> intersectRemove = IntersectRemove;
		
		private static void IntersectSkip(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			} else {
				context.needsMove = true;
			}
			
			while (context.chained.current.isSome && !context.hashSet.value.Remove(context.selector(context.chained.current.value, context.parameter))) {
				context.chained.skip(ref context.chained.context, out context.chained.current);
			}
			
			if (!context.chained.current.isSome) {
				context.bd.Release();
				if (context.release) {
					context.hashSet.Dispose();
				}
			}
			next = context.chained.current;
		}
		
		private static void IntersectRemove(ref HashSetContext<K, T, C, P> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			context.needsMove = false;
			context.chained.remove(ref context.chained.context, out context.chained.current);
			IntersectSkip(ref context, out next);
		}
		
		#endregion
		
		#endregion

	}
	
	#endregion
	
}
