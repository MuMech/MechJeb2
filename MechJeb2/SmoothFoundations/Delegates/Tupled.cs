using System;
using Smooth.Algebraics;

namespace Smooth.Delegates {

	/// <summary>
	/// Provides methods for invoking a delegate that takes individual parameters using a tuple.
	/// 
	/// Many Smooth methods allow the use of an optional parameter that is passed to a delegate in order to capture state without allocating a closure.  One use case of this class is to convert a tupled state parameter into individual parameters.
	/// 
	/// For instance:
	/// 
	/// option.Where((player, p) => player.team == p._1 && player.score > p._2, Tuple.Create(myTeam, myScore))
	/// 
	/// Could be written as:
	/// 
	/// option.Where((v, p) => Tupled.Call((player, team, score) => player.team == team && player.score > score, v, p), Tuple.Create(myTeam, myScore))
	/// 
	/// While this form is slightly longer and requires an extra method call, it can be useful as a "quick and dirty" way to use meaningful parameter names in complicated delegates.
	/// </summary>
	public static class Tupled {

		#region Action, Tuple

		public static void Call<T1>(this DelegateAction<T1> action, Tuple<T1> t) {
			action(t._1);
		}

		public static void Call<T1, T2>(this DelegateAction<T1, T2> action, Tuple<T1, T2> t) {
			action(t._1, t._2);
		}
		
		public static void Call<T1, T2, T3>(this DelegateAction<T1, T2, T3> action, Tuple<T1, T2, T3> t) {
			action(t._1, t._2, t._3);
		}
		
		public static void Call<T1, T2, T3, T4>(this DelegateAction<T1, T2, T3, T4> action, Tuple<T1, T2, T3, T4> t) {
			action(t._1, t._2, t._3, t._4);
		}
		
		public static void Call<T1, T2, T3, T4, T5>(this DelegateAction<T1, T2, T3, T4, T5> action, Tuple<T1, T2, T3, T4, T5> t) {
			action(t._1, t._2, t._3, t._4, t._5);
		}
		
		public static void Call<T1, T2, T3, T4, T5, T6>(this DelegateAction<T1, T2, T3, T4, T5, T6> action, Tuple<T1, T2, T3, T4, T5, T6> t) {
			action(t._1, t._2, t._3, t._4, t._5, t._6);
		}
		
		public static void Call<T1, T2, T3, T4, T5, T6, T7>(this DelegateAction<T1, T2, T3, T4, T5, T6, T7> action, Tuple<T1, T2, T3, T4, T5, T6, T7> t) {
			action(t._1, t._2, t._3, t._4, t._5, t._6, t._7);
		}
		
		public static void Call<T1, T2, T3, T4, T5, T6, T7, T8>(this DelegateAction<T1, T2, T3, T4, T5, T6, T7, T8> action, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> t) {
			action(t._1, t._2, t._3, t._4, t._5, t._6, t._7, t._8);
		}
		
		public static void Call<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this DelegateAction<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> t) {
			action(t._1, t._2, t._3, t._4, t._5, t._6, t._7, t._8, t._9);
		}

		#endregion

		#region Action, First, Tuple

		public static void Call<F, T1>(this DelegateAction<F, T1> action, F first, Tuple<T1> t) {
			action(first, t._1);
		}
		
		public static void Call<F, T1, T2>(this DelegateAction<F, T1, T2> action, F first, Tuple<T1, T2> t) {
			action(first, t._1, t._2);
		}
		
		public static void Call<F, T1, T2, T3>(this DelegateAction<F, T1, T2, T3> action, F first, Tuple<T1, T2, T3> t) {
			action(first, t._1, t._2, t._3);
		}
		
		public static void Call<F, T1, T2, T3, T4>(this DelegateAction<F, T1, T2, T3, T4> action, F first, Tuple<T1, T2, T3, T4> t) {
			action(first, t._1, t._2, t._3, t._4);
		}
		
		public static void Call<F, T1, T2, T3, T4, T5>(this DelegateAction<F, T1, T2, T3, T4, T5> action, F first, Tuple<T1, T2, T3, T4, T5> t) {
			action(first, t._1, t._2, t._3, t._4, t._5);
		}
		
		public static void Call<F, T1, T2, T3, T4, T5, T6>(this DelegateAction<F, T1, T2, T3, T4, T5, T6> action, F first, Tuple<T1, T2, T3, T4, T5, T6> t) {
			action(first, t._1, t._2, t._3, t._4, t._5, t._6);
		}
		
		public static void Call<F, T1, T2, T3, T4, T5, T6, T7>(this DelegateAction<F, T1, T2, T3, T4, T5, T6, T7> action, F first, Tuple<T1, T2, T3, T4, T5, T6, T7> t) {
			action(first, t._1, t._2, t._3, t._4, t._5, t._6, t._7);
		}
		
		public static void Call<F, T1, T2, T3, T4, T5, T6, T7, T8>(this DelegateAction<F, T1, T2, T3, T4, T5, T6, T7, T8> action, F first, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> t) {
			action(first, t._1, t._2, t._3, t._4, t._5, t._6, t._7, t._8);
		}

		#endregion

		#region Func, Tuple

		public static R Call<T1, R>(this DelegateFunc<T1, R> func, Tuple<T1> t) {
			return func(t._1);
		}
		
		public static R Call<T1, T2, R>(this DelegateFunc<T1, T2, R> func, Tuple<T1, T2> t) {
			return func(t._1, t._2);
		}
		
		public static R Call<T1, T2, T3, R>(this DelegateFunc<T1, T2, T3, R> func, Tuple<T1, T2, T3> t) {
			return func(t._1, t._2, t._3);
		}
		
		public static R Call<T1, T2, T3, T4, R>(this DelegateFunc<T1, T2, T3, T4, R> func, Tuple<T1, T2, T3, T4> t) {
			return func(t._1, t._2, t._3, t._4);
		}
		
		public static R Call<T1, T2, T3, T4, T5, R>(this DelegateFunc<T1, T2, T3, T4, T5, R> func, Tuple<T1, T2, T3, T4, T5> t) {
			return func(t._1, t._2, t._3, t._4, t._5);
		}
		
		public static R Call<T1, T2, T3, T4, T5, T6, R>(this DelegateFunc<T1, T2, T3, T4, T5, T6, R> func, Tuple<T1, T2, T3, T4, T5, T6> t) {
			return func(t._1, t._2, t._3, t._4, t._5, t._6);
		}
		
		public static R Call<T1, T2, T3, T4, T5, T6, T7, R>(this DelegateFunc<T1, T2, T3, T4, T5, T6, T7, R> func, Tuple<T1, T2, T3, T4, T5, T6, T7> t) {
			return func(t._1, t._2, t._3, t._4, t._5, t._6, t._7);
		}
		
		public static R Call<T1, T2, T3, T4, T5, T6, T7, T8, R>(this DelegateFunc<T1, T2, T3, T4, T5, T6, T7, T8, R> func, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> t) {
			return func(t._1, t._2, t._3, t._4, t._5, t._6, t._7, t._8);
		}
		
		public static R Call<T1, T2, T3, T4, T5, T6, T7, T8, T9, R>(this DelegateFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, R> func, Tuple<T1, T2, T3, T4, T5, T6, T7, T8, T9> t) {
			return func(t._1, t._2, t._3, t._4, t._5, t._6, t._7, t._8, t._9);
		}

		#endregion

		#region Func, First, Tuple

		public static R Call<F, T1, R>(this DelegateFunc<F, T1, R> func, F first, Tuple<T1> t) {
			return func(first, t._1);
		}
		
		public static R Call<F, T1, T2, R>(this DelegateFunc<F, T1, T2, R> func, F first, Tuple<T1, T2> t) {
			return func(first, t._1, t._2);
		}
		
		public static R Call<F, T1, T2, T3, R>(this DelegateFunc<F, T1, T2, T3, R> func, F first, Tuple<T1, T2, T3> t) {
			return func(first, t._1, t._2, t._3);
		}
		
		public static R Call<F, T1, T2, T3, T4, R>(this DelegateFunc<F, T1, T2, T3, T4, R> func, F first, Tuple<T1, T2, T3, T4> t) {
			return func(first, t._1, t._2, t._3, t._4);
		}
		
		public static R Call<F, T1, T2, T3, T4, T5, R>(this DelegateFunc<F, T1, T2, T3, T4, T5, R> func, F first, Tuple<T1, T2, T3, T4, T5> t) {
			return func(first, t._1, t._2, t._3, t._4, t._5);
		}
		
		public static R Call<F, T1, T2, T3, T4, T5, T6, R>(this DelegateFunc<F, T1, T2, T3, T4, T5, T6, R> func, F first, Tuple<T1, T2, T3, T4, T5, T6> t) {
			return func(first, t._1, t._2, t._3, t._4, t._5, t._6);
		}
		
		public static R Call<F, T1, T2, T3, T4, T5, T6, T7, R>(this DelegateFunc<F, T1, T2, T3, T4, T5, T6, T7, R> func, F first, Tuple<T1, T2, T3, T4, T5, T6, T7> t) {
			return func(first, t._1, t._2, t._3, t._4, t._5, t._6, t._7);
		}
		
		public static R Call<F, T1, T2, T3, T4, T5, T6, T7, T8, R>(this DelegateFunc<F, T1, T2, T3, T4, T5, T6, T7, T8, R> func, F first, Tuple<T1, T2, T3, T4, T5, T6, T7, T8> t) {
			return func(first, t._1, t._2, t._3, t._4, t._5, t._6, t._7, t._8);
		}

		#endregion

	}
}
