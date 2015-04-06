using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Smooth.Dispose {

	/// <summary>
	/// Queues pooled resources for cleanup by a background thread.
	/// 
	/// By default, the disposal thread is woken up at the end of LateUpdate, when there is likely to be free CPU time available while GPU operations are in progress.
	/// 
	/// Various pools may be locked and unlocked while resources are released, potentially causing contention if pooled resources are borrowed during the disposal process.
	/// 
	/// Advanced users who are using pools from the main thread during the rendering phase may want to customize the point in the Unity event loop when the queue lock is pulsed, potentially pulsing from a Camera event.
	/// </summary>
	public static class DisposalQueue {
		private static readonly object queueLock = new object();
		private static Queue<IDisposable> enqueue = new Queue<IDisposable>();
		private static Queue<IDisposable> dispose = new Queue<IDisposable>();

		/// <summary>
		/// Adds the specified item to the disposal queue.
		/// </summary>
		public static void Enqueue(IDisposable item) {
			lock (queueLock) {
				enqueue.Enqueue(item);
			}
		}

		/// <summary>
		/// Pulses the queue lock, potentially waking up the disposal thread.
		/// </summary>
		public static void Pulse() {
			lock (queueLock) {
				Monitor.Pulse(queueLock);
			}
		}

		private static void Dispose() {
			while (true) {
				lock (queueLock) {
					while (enqueue.Count == 0) {
						Monitor.Wait(queueLock);
					}
					var t = enqueue;
					enqueue = dispose;
					dispose = t;
				}
				while (dispose.Count > 0) {
					try {
						dispose.Dequeue().Dispose();
					} catch (ThreadAbortException) {
					} catch (Exception e) {
						Debug.LogError(e);
					}
				}
			}
		}

		static DisposalQueue() {
			new Thread(new ThreadStart(Dispose)).Start();
			new GameObject(typeof(SmoothDisposer).Name).AddComponent<SmoothDisposer>();
		}
	}
}
