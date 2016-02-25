//#define DETECT_BACKTRACK

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Smooth.Slinq.Context {

	/// <summary>
	/// Used to find backtracking Slinq usage.
	/// 
	/// If DETECT_BACKTRACK is defined, backtrack detectors will lock a reference context and test the backtrack state on every Slinq operation.  This will severely reduce performance and should only be used for debugging purposes.
	/// 
	/// If DETECT_BACKTRACK is not defined, detection operations will not be compiled into the application.
	/// 
	/// Note: Backtrack detection does not work reliably across multiple threads.  If pooled objects are shared or passed between threads, external locking and/or ownership management is the repsonsibility of the user.
	/// </summary>
	public struct BacktrackDetector {
		private static readonly BacktrackDetector noDetection = new BacktrackDetector();
		private static readonly Stack<ReferenceContext> contextPool;

		public static readonly bool enabled;

		private class ReferenceContext {
			public int borrowId;
			public int touchId;
		}

		private readonly ReferenceContext context;
		private int borrowId;
		private int touchId;

		private BacktrackDetector(ReferenceContext context) {
			this.context = context;
			this.borrowId = context.borrowId;
			this.touchId = context.touchId;
		}

		public static BacktrackDetector Borrow() {
			if (enabled) {
				lock (contextPool) {
					return new BacktrackDetector(contextPool.Count > 0 ? contextPool.Pop() : new ReferenceContext());
				}
			} else {
				return noDetection;
			}
		}

		[Conditional("DETECT_BACKTRACK")]
		public void DetectBacktrack() {
			lock (context) {
				if (context.borrowId == borrowId && context.touchId == touchId) {
					context.touchId = ++touchId;
				} else {
					throw new BacktrackException();
				}
			}
		}
		
		[Conditional("DETECT_BACKTRACK")]
		public void Release() {
			lock (context) {
				if (context.borrowId == borrowId && context.touchId == touchId) {
					++context.borrowId;
				} else {
					throw new BacktrackException();
				}
			}

			lock (contextPool) {
				contextPool.Push(context);
			}
		}

		[Conditional("DETECT_BACKTRACK")]
		public void TryReleaseShared() {
			lock (context) {
				if (context.borrowId == borrowId) {
					++context.borrowId;
				} else {
					return;
				}
			}

			lock (contextPool) {
				contextPool.Push(context);
			}
		}

		static BacktrackDetector() {
#if DETECT_BACKTRACK
			contextPool = new Stack<ReferenceContext>();
			enabled = true;
#else
			contextPool = null;
			enabled = false;
#endif
			if (enabled && UnityEngine.Application.isPlaying) {
				UnityEngine.Debug.Log("Smooth.Slinq is running with backtrack detection enabled which will significantly reduce performance and should only be used for debugging purposes.");
			}
		}
	}
}
