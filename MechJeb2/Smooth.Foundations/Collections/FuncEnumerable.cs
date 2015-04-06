using System;
using System.Collections.Generic;
using Smooth.Algebraics;
using Smooth.Delegates;

namespace Smooth.Collections {
	/// <summary>
	/// Enumerable that contains the elements defined by a seed value and step function.
	/// </summary>
	public class FuncEnumerable<T> : IEnumerable<T> {
		private readonly T seed;
		private readonly Either<DelegateFunc<T, T>, DelegateFunc<T, Option<T>>> step;

		private FuncEnumerable() {}

		public FuncEnumerable(T seed, DelegateFunc<T, T> step) {
			this.seed = seed;
			this.step = Either<DelegateFunc<T, T>, DelegateFunc<T, Option<T>>>.Left(step);
		}
		
		public FuncEnumerable(T seed, DelegateFunc<T, Option<T>> step) {
			this.seed = seed;
			this.step = Either<DelegateFunc<T, T>, DelegateFunc<T, Option<T>>>.Right(step);
		}

		public IEnumerator<T> GetEnumerator() {
			if (step.isLeft) {
				var current = seed;
				while (true) {
					yield return current;
					current = step.leftValue(current);
				}
			} else {
				var current = new Option<T>(seed);
				while (current.isSome) {
					yield return current.value;
					current = step.rightValue(current.value);
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	/// <summary>
	/// Enumerable that contains the elements defined by a seed value and step function.
	/// </summary>
	public class FuncEnumerable<T, P> : IEnumerable<T> {
		private readonly T seed;
		private readonly Either<DelegateFunc<T, P, T>, DelegateFunc<T, P, Option<T>>> step;
		private readonly P parameter;
		
		private FuncEnumerable() {}
		
		public FuncEnumerable(T seed, DelegateFunc<T, P, T> step, P parameter) {
			this.seed = seed;
			this.step = Either<DelegateFunc<T, P, T>, DelegateFunc<T, P, Option<T>>>.Left(step);
			this.parameter = parameter;
		}
		
		public FuncEnumerable(T seed, DelegateFunc<T, P, Option<T>> step, P parameter) {
			this.seed = seed;
			this.step = Either<DelegateFunc<T, P, T>, DelegateFunc<T, P, Option<T>>>.Right(step);
			this.parameter = parameter;
		}
		
		public IEnumerator<T> GetEnumerator() {
			if (step.isLeft) {
				var current = seed;
				while (true) {
					yield return current;
					current = step.leftValue(current, parameter);
				}
			} else {
				var current = new Option<T>(seed);
				while (current.isSome) {
					yield return current.value;
					current = step.rightValue(current.value, parameter);
				}
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
