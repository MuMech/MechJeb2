using System;
using System.Collections.Generic;
using Smooth.Algebraics;

namespace Smooth.Collections {
	/// <summary>
	/// Helper class for enuemrating the elements of an IList<T> using a start index and step value.
	/// </summary>
	public class IListStepper<T> : IEnumerable<T> {
		private readonly IList<T> list;
		private readonly int startIndex;
		private readonly int step;
		
		private IListStepper() {}
		
		public IListStepper(IList<T> list, int startIndex, int step) {
			this.list = list;
			this.startIndex = startIndex;
			this.step = step;
		}
		
		public IEnumerator<T> GetEnumerator() {
			for (int i = startIndex; 0 <= i && i < list.Count; i += step) {
				yield return list[i];
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
	
	/// <summary>
	/// Helper class for enuemrating the element, index pairs of an IList<T> using a start index and step value.
	/// </summary>
	public class IListStepperWithIndex<T> : IEnumerable<Tuple<T, int>> {
		private readonly IList<T> list;
		private readonly int startIndex;
		private readonly int step;
		
		private IListStepperWithIndex() {}
		
		public IListStepperWithIndex(IList<T> list, int startIndex, int step) {
			this.list = list;
			this.startIndex = startIndex;
			this.step = step;
		}
		
		public IEnumerator<Tuple<T, int>> GetEnumerator() {
			for (int i = startIndex; 0 <= i && i < list.Count; i += step) {
				yield return new Tuple<T, int>(list[i], i);
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
