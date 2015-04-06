using System;

namespace Smooth.Delegates {
	public delegate void DelegateAction();
	public delegate void DelegateAction<in T1>(T1 _1);
	public delegate void DelegateAction<in T1, in T2>(T1 _1, T2 _2);
	public delegate void DelegateAction<in T1, in T2, in T3>(T1 _1, T2 _2, T3 _3);
	public delegate void DelegateAction<in T1, in T2, in T3, in T4>(T1 _1, T2 _2, T3 _3, T4 _4);
	public delegate void DelegateAction<in T1, in T2, in T3, in T4, in T5>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5);
	public delegate void DelegateAction<in T1, in T2, in T3, in T4, in T5, in T6>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6);
	public delegate void DelegateAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7);
	public delegate void DelegateAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8);
	public delegate void DelegateAction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9);

	public delegate R DelegateFunc<out R>();
	public delegate R DelegateFunc<in T1, out R>(T1 _1);
	public delegate R DelegateFunc<in T1, in T2, out R>(T1 _1, T2 _2);
	public delegate R DelegateFunc<in T1, in T2, in T3, out R>(T1 _1, T2 _2, T3 _3);
	public delegate R DelegateFunc<in T1, in T2, in T3, in T4, out R>(T1 _1, T2 _2, T3 _3, T4 _4);
	public delegate R DelegateFunc<in T1, in T2, in T3, in T4, in T5, out R>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5);
	public delegate R DelegateFunc<in T1, in T2, in T3, in T4, in T5, in T6, out R>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6);
	public delegate R DelegateFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out R>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7);
	public delegate R DelegateFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out R>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8);
	public delegate R DelegateFunc<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, in T9, out R>(T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9);
}