using System;
using System.Collections.Generic;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct IListContext<T> {

		#region Slinqs

		public static Slinq<T, IListContext<T>> Slinq(IList<T> list, int startIndex, int step) {
			return new Slinq<T, IListContext<T>>(
				skip,
				remove,
				dispose,
				new IListContext<T>(list, startIndex, step));
		}
		
		public static Slinq<Tuple<T, int>, IListContext<T>> SlinqWithIndex(IList<T> list, int startIndex, int step) {
			return new Slinq<Tuple<T, int>, IListContext<T>>(
				skipWithIndex,
				removeWithIndex,
				disposeWithIndex,
				new IListContext<T>(list, startIndex, step));
		}

		#endregion

		#region Instance

		private IList<T> list;
		private int size;
		private int index;
		private readonly int step;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private IListContext(IList<T> list, int startIndex, int step) {
			this.list = list;
			this.size = list.Count;
			this.index = startIndex - step;
			this.step = step;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion

		#region Delegates

		#region Values

		private static readonly Mutator<T, IListContext<T>> skip = Skip;
		private static readonly Mutator<T, IListContext<T>> remove = Remove;
		private static readonly Mutator<T, IListContext<T>> dispose = Dispose;
		
		private static void Skip(ref IListContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			var index = context.index + context.step;

			if (0 <= index && index < context.size) {
				next = new Option<T>(context.list[index]);
				context.index = index;
			} else {
				next = new Option<T>();
				context.bd.Release();
			}
		}

		private static void Remove(ref IListContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			context.list.RemoveAt(context.index);
			
			if (context.step == 0) {
				next = new Option<T>();
				context.bd.Release();
			} else {
				if (context.step > 0) {
					--context.index;
				}
				--context.size;
				
				Skip(ref context, out next);
			}
		}

		private static void Dispose(ref IListContext<T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
		}

		#endregion

		#region Values with index

		private static readonly Mutator<Tuple<T, int>, IListContext<T>> skipWithIndex = SkipWithIndex;
		private static readonly Mutator<Tuple<T, int>, IListContext<T>> removeWithIndex = RemoveWithIndex;
		private static readonly Mutator<Tuple<T, int>, IListContext<T>> disposeWithIndex = DisposeWithIndex;

		private static void SkipWithIndex(ref IListContext<T> context, out Option<Tuple<T, int>> next) {
			context.bd.DetectBacktrack();
			
			var index = context.index + context.step;
			
			if (0 <= index && index < context.size) {
				next = new Option<Tuple<T, int>>(new Tuple<T, int>(context.list[index], index));
				context.index = index;
			} else {
				next = new Option<Tuple<T, int>>();
				context.bd.Release();
			}
		}
		
		private static void RemoveWithIndex(ref IListContext<T> context, out Option<Tuple<T, int>> next) {
			context.bd.DetectBacktrack();
			
			context.list.RemoveAt(context.index);

			if (context.step == 0) {
				next = new Option<Tuple<T, int>>();
				context.bd.Release();
			} else {
				if (context.step > 0) {
					--context.index;
				}
				--context.size;
				
				SkipWithIndex(ref context, out next);
			}
		}
		
		private static void DisposeWithIndex(ref IListContext<T> context, out Option<Tuple<T, int>> next) {
			next = new Option<Tuple<T, int>>();
			context.bd.Release();
		}

		#endregion
		
		#endregion
		
	}
}
