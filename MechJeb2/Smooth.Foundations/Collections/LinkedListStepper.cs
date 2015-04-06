using System;
using System.Collections.Generic;

namespace Smooth.Collections {
	/// <summary>
	/// Helper class for enuemrating the values in a LinkedList<T> using a start node and step value.
	/// </summary>
	public class LinkedListStepper<T> : IEnumerable<T> {
		private readonly LinkedListNode<T> startNode;
		private readonly int step;

		private LinkedListStepper() {}

		public LinkedListStepper(LinkedListNode<T> startNode, int step) {
			this.startNode = startNode;
			this.step = step;
		}
		
		public IEnumerator<T> GetEnumerator() {
			var node = startNode;

			while (node != null) {
				yield return node.Value;

				var step = this.step;
				while (step > 0 && node != null) {
					node = node.Next;
					--step;
				}
				while (step < 0 && node != null) {
					node = node.Previous;
					++step;
				}
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	/// <summary>
	/// Helper class for enuemrating the nodes in a LinkedList<T> using a start node and step value.
	/// </summary>
	public class LinkedListStepperNodes<T> : IEnumerable<LinkedListNode<T>> {
		private readonly LinkedListNode<T> startNode;
		private readonly int step;
		
		private LinkedListStepperNodes() {}
		
		public LinkedListStepperNodes(LinkedListNode<T> startNode, int step) {
			this.startNode = startNode;
			this.step = step;
		}
		
		public IEnumerator<LinkedListNode<T>> GetEnumerator() {
			var node = startNode;
			
			while (node != null) {
				yield return node;
				
				var step = this.step;
				while (step > 0 && node != null) {
					node = node.Next;
					--step;
				}
				while (step < 0 && node != null) {
					node = node.Previous;
					++step;
				}
			}
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}
}
