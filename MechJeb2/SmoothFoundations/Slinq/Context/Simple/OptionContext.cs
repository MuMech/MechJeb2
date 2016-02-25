using System;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct OptionContext<T> {

		#region Slinqs

		public static Slinq<T, OptionContext<T>> Slinq(Option<T> option) {
			return new Slinq<T, OptionContext<T>>(
				optionSkip,
				remove,
				dispose,
				new OptionContext<T>(option));
		}
		
		public static Slinq<T, OptionContext<T>> Repeat(T value) {
			return new Slinq<T, OptionContext<T>>(
				repeatSkip,
				remove,
				dispose,
				new OptionContext<T>(new Option<T>(value)));
		}
		
		#endregion
		
		#region Context
		
		private Option<T> option;

		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414
		
		private OptionContext(Option<T> option) {
			this.option = option;
			
			this.bd = BacktrackDetector.Borrow();
		}

		#endregion

		#region Delegates

		#region Remove / Dispose

		private static readonly Mutator<T, OptionContext<T>> remove = Remove;
		private static readonly Mutator<T, OptionContext<T>> dispose = Dispose;
		
		private static void Remove(ref OptionContext<T> context, out Option<T> next) {
			throw new NotSupportedException();
		}

		private static void Dispose(ref OptionContext<T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
		}

		#endregion

		#region Option

		private static readonly Mutator<T, OptionContext<T>> optionSkip = OptionSkip;

		private static void OptionSkip(ref OptionContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			next = context.option;

			if (context.option.isSome) {
				context.option = new Option<T>();
			} else {
				context.bd.Release();
			}
		}

		#endregion

		#region Repeat
		
		private static readonly Mutator<T, OptionContext<T>> repeatSkip = RepeatSkip;

		private static void RepeatSkip(ref OptionContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			next = context.option;
		}

		#endregion

		#endregion

	}
}
