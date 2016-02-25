using System;
using System.Collections.Generic;
using Smooth.Algebraics;

namespace Smooth.Slinq.Context {
	public struct LinkedListContext<T> {

		#region Slinqs
		
		public static Slinq<T, LinkedListContext<T>> Slinq(LinkedListNode<T> node, int step) {
			return new Slinq<T, LinkedListContext<T>>(
				skip,
				remove,
				dispose,
				new LinkedListContext<T>(node, step));
		}
		
		public static Slinq<LinkedListNode<T>, LinkedListContext<T>> SlinqNodes(LinkedListNode<T> node, int step) {
			return new Slinq<LinkedListNode<T>, LinkedListContext<T>>(
				skipNodes,
				removeNodes,
				disposeNodes,
				new LinkedListContext<T>(node, step));
		}

		#endregion
		
		#region Instance

		private bool needsMove;
		private LinkedListNode<T> node;
		private readonly int step;
		
		#pragma warning disable 0414
		private BacktrackDetector bd;
		#pragma warning restore 0414

		private LinkedListContext(LinkedListNode<T> node, int step) {
			this.needsMove = false;
			this.node = node;
			this.step = step;
			
			this.bd = BacktrackDetector.Borrow();
		}
		
		#endregion
		
		#region Delegates
		
		#region Values
		
		private static readonly Mutator<T, LinkedListContext<T>> skip = Skip;
		private static readonly Mutator<T, LinkedListContext<T>> remove = Remove;
		private static readonly Mutator<T, LinkedListContext<T>> dispose = Dispose;

		private static void Skip(ref LinkedListContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				var step = context.step;
				while (step > 0 && context.node != null) {
					context.node = context.node.Next;
					--step;
				}
				while (step < 0 && context.node != null) {
					context.node = context.node.Previous;
					++step;
				}
			} else {
				context.needsMove = true;
			}

			
			if (context.node == null) {
				next = new Option<T>();
				context.bd.Release();
			} else {
				next = new Option<T>(context.node.Value);
			}
		}

		private static void Remove(ref LinkedListContext<T> context, out Option<T> next) {
			context.bd.DetectBacktrack();

			if (context.step == 0) {
				next = new Option<T>();
				context.node.List.Remove(context.node);
				context.bd.Release();
			} else {
				var node = context.node;
				Skip(ref context, out next);
				node.List.Remove(node);
			}
		}

		private static void Dispose(ref LinkedListContext<T> context, out Option<T> next) {
			next = new Option<T>();
			context.bd.Release();
		}
		
		#endregion

		#region Nodes
		
		private static readonly Mutator<LinkedListNode<T>, LinkedListContext<T>> skipNodes = SkipNodes;
		private static readonly Mutator<LinkedListNode<T>, LinkedListContext<T>> removeNodes = RemoveNodes;
		private static readonly Mutator<LinkedListNode<T>, LinkedListContext<T>> disposeNodes = DisposeNodes;

		private static void SkipNodes(ref LinkedListContext<T> context, out Option<LinkedListNode<T>> next) {
			context.bd.DetectBacktrack();
			
			if (context.needsMove) {
				var step = context.step;
				while (step > 0 && context.node != null) {
					context.node = context.node.Next;
					--step;
				}
				while (step < 0 && context.node != null) {
					context.node = context.node.Previous;
					++step;
				}
			} else {
				context.needsMove = true;
			}
	
			if (context.node == null) {
				next = new Option<LinkedListNode<T>>();
				context.bd.Release();
			} else {
				next = new Option<LinkedListNode<T>>(context.node);
			}
		}
		
		private static void RemoveNodes(ref LinkedListContext<T> context, out Option<LinkedListNode<T>> next) {
			context.bd.DetectBacktrack();
			
			if (context.step == 0) {
				next = new Option<LinkedListNode<T>>();
				context.node.List.Remove(context.node);
				context.bd.Release();
			} else {
				var node = context.node;
				SkipNodes(ref context, out next);
				node.List.Remove(node);
			}
		}

		private static void DisposeNodes(ref LinkedListContext<T> context, out Option<LinkedListNode<T>> next) {
			next = new Option<LinkedListNode<T>>();
			context.bd.Release();
		}

		#endregion
		
		#endregion

	}
}
