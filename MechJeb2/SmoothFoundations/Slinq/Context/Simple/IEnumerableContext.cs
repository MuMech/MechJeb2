using System;
using System.Collections.Generic;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct IEnumerableContext<T> {	

		#region Slinqs
		
		public static Slinq<T, IEnumerableContext<T>> Slinq(IEnumerable<T> enumerable) {
			return new Slinq<T, IEnumerableContext<T>>(
				skip,
				remove,
				dispose,
				new IEnumerableContext<T>(enumerable));
		}

		#endregion
		
		#region Instance
		
		private readonly IEnumerator<T> enumerator;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private IEnumerableContext(IEnumerable<T> enumerable) {
			this.enumerator = enumerable.GetEnumerator();

			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion

		#region Delegates

		private static readonly Mutator<T, IEnumerableContext<T>> skip = Skip;
		private static readonly Mutator<T, IEnumerableContext<T>> remove = Remove;
		private static readonly Mutator<T, IEnumerableContext<T>> dispose = Dispose;

		private static void Skip(ref IEnumerableContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.enumerator.MoveNext()) {
				next = new Option<T>(context.enumerator.Current);
			} else {
				next = new Option<T>();
				context.bd.Release();
				context.enumerator.Dispose();
			}
		}

		private static void Remove(ref IEnumerableContext<T> context, out Option<T> next) {
			throw new NotSupportedException();
		}
		
		private static void Dispose(ref IEnumerableContext<T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
			context.enumerator.Dispose();
		}

		#endregion

	}
}
