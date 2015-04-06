using System;
using Smooth.Delegates;

namespace Smooth.Events {
	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction Handle;

		/// <summary>
		/// Raises the wrapped event.
		/// </summary>
		public void Raise() { var handle = Handle; if (handle != null) { handle(); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameter.
		/// </summary>
		public void Raise(T1 t1) { var handle = Handle; if (handle != null) { handle(t1); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2) { var handle = Handle; if (handle != null) { handle(t1, t2); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3) { var handle = Handle; if (handle != null) { handle(t1, t2, t3); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3, T4> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3, T4> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3, T4 t4) { var handle = Handle; if (handle != null) { handle(t1, t2, t3, t4); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3, T4, T5> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3, T4, T5> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) { var handle = Handle; if (handle != null) { handle(t1, t2, t3, t4, t5); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3, T4, T5, T6> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3, T4, T5, T6> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) { var handle = Handle; if (handle != null) { handle(t1, t2, t3, t4, t5, t6); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3, T4, T5, T6, T7> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3, T4, T5, T6, T7> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) { var handle = Handle; if (handle != null) { handle(t1, t2, t3, t4, t5, t6, t7); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3, T4, T5, T6, T7, T8> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3, T4, T5, T6, T7, T8> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8) { var handle = Handle; if (handle != null) { handle(t1, t2, t3, t4, t5, t6, t7, t8); } }
	}

	/// <summary>
	/// Struct wrapped event that allows raising the event from outside the containing class.
	/// </summary>
	public struct GenericEvent<T1, T2, T3, T4, T5, T6, T7, T8, T9> {
		/// <summary>
		/// The wrapped event.
		/// </summary>
		public event DelegateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> Handle;

		/// <summary>
		/// Raises the wrapped event with the specifed parameters.
		/// </summary>
		public void Raise(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9) { var handle = Handle; if (handle != null) { handle(t1, t2, t3, t4, t5, t6, t7, t8, t9); } }
	}
}
