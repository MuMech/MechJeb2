using UnityEngine;
using System;
using System.Collections.Generic;
using Smooth.Collections;
using Smooth.Comparisons;
using Smooth.Compare.Comparers;
using Smooth.Events;

namespace Smooth.Compare {
	/// <summary>
	/// Manages comparer registration and lookup for Smooth.Compare.
	/// </summary>
	public static class Finder {
		private const string customConfigurationClassName = "Smooth.Compare.CustomConfiguration";

		/// <summary>
		/// Wrapped caller / delegate for finder events.  To subscribe, add a delegate to the enclosed Handle event.
		/// </summary>
		public static GenericEvent<ComparerType, EventType, Type> OnEvent;

		private static readonly IEqualityComparer<Type> typeComparer = new FuncEqualityComparer<Type>((a, b) => a == b);
		
		private static readonly Dictionary<Type, object> comparers = new Dictionary<Type, object>(typeComparer);
		private static readonly Dictionary<Type, object> equalityComparers = new Dictionary<Type, object>(typeComparer) { { typeof(Type), typeComparer } };

		private static readonly Configuration config;

		#region Types with specific comparers

		/// <summary>
		/// Registers an equality comparer for type T where T is an enumeration.
		/// 
		/// Note: Enumerations are handled automatically and do not need to be registered when JIT is enabled.
		/// </summary>
		public static void RegisterEnum<T>() {
			var type = typeof(T);

			if (type.IsEnum) {
				switch(Type.GetTypeCode(type)) {
				case TypeCode.Int64:
				case TypeCode.UInt64:
					Register<T>(new Blittable64EqualityComparer<T>());
					break;
				default:
					Register<T>(new Blittable32EqualityComparer<T>());
					break;
				}
			} else {
				Debug.LogError("Tried to register a non-enumeration type as an enumeration.");
			}
		}

		/// <summary>
		/// Registers sort order and equality comparers for KeyValuePair<K, V>s.
		/// 
		/// Note: The comparison operations will rely on the comparers for K and V.
		///
		/// Note: KeyValuePair<,>s are handled automatically and do not need to be registered when JIT is enabled.
		/// </summary>
		public static void RegisterKeyValuePair<K, V>() {
			Register(new KeyValuePairComparer<K, V>());
			Register(new KeyValuePairEqualityComparer<K, V>());
		}

		#endregion

		#region IComparable<T> / IEquatable<T>

		/// <summary>
		/// Registers a sort order comparer for type T where T implements IComparable<T>.
		/// 
		/// Used to circumvent potential JIT exceptions on platforms without JIT compilation.
		/// </summary>
		public static void RegisterIComparable<T>() where T : IComparable<T> {
			Register<T>(new IComparableComparer<T>());
		}

		/// <summary>
		/// Registers an equality comparer for type T where T implements IEquatable<T>.
		/// 
		/// Used to circumvent potential JIT exceptions on platforms without JIT compilation.
		/// </summary>
		public static void RegisterIEquatable<T>() where T : IEquatable<T> {
			Register<T>(new IEquatableEqualityComparer<T>());
		}

		/// <summary>
		/// Registers sort order and equality comparers for type T where T implements IComparable<T> and IEquatable<T>.
		/// 
		/// Used to circumvent potential JIT exceptions on platforms without JIT compilation.
		/// </summary>
		public static void RegisterIComparableIEquatable<T>() where T : IComparable<T>, IEquatable<T> {
			Register<T>(new IComparableComparer<T>());
			Register<T>(new IEquatableEqualityComparer<T>());
		}

		#endregion

		#region Comparer + Equality Comparer
		
		/// <summary>
		/// Registers a sort order comparer with the specified comparison and an equality comparer with the specified equals function for type T.
		/// </summary>
		public static void Register<T>(Comparison<T> comparison, Func<T, T, bool> equals) {
			Register<T>(new FuncComparer<T>(comparison));
			Register<T>(new FuncEqualityComparer<T>(equals));
		}
		
		/// <summary>
		/// Registers a sort order comparer with the specified comparison and an equality comparer with the specified equals and hashCode functions for type T.
		/// </summary>
		public static void Register<T>(Comparison<T> comparison, Func<T, T, bool> equals, Func<T, int> hashCode) {
			Register<T>(new FuncComparer<T>(comparison));
			Register<T>(new FuncEqualityComparer<T>(equals, hashCode));
		}
		
		/// <summary>
		/// Registers the specified sort order comparer and equality comparer for type T.
		/// 
		/// Note: On platforms without JIT compilation, the supplied comparers should respectively inherit from Smooth.Collections.Comparer<T> and Smooth.Collections.EqualityComparer<T> in order to force the AOT compiler to create the proper generic types.
		/// </summary>
		public static void Register<T>(IComparer<T> comparer, IEqualityComparer<T> equalityComparer) {
			Register<T>(comparer);
			Register<T>(equalityComparer);
		}
		
		#endregion

		#region Comparer

		/// <summary>
		/// Registers a sort order comparer with the specified comparison for type T.
		/// </summary>
		public static void Register<T>(Comparison<T> comparison) {
			Register<T>(new FuncComparer<T>(comparison));
		}
		
		/// <summary>
		/// Registers the specified sort order comparer for type T.
		/// 
		/// Note: On platforms without JIT compilation, the supplied comparer should inherit from Smooth.Collections.Comparer<T> in order to force the AOT compiler to create the proper generic types.
		/// </summary>
		public static void Register<T>(IComparer<T> comparer) {
			var comparerType = ComparerType.Comparer;
			var type = typeof(T);

			if (comparer == null) {
				Debug.LogError("Tried to register a null comparer for: " + type.FullName);
			} else {
				lock (comparers) {
					if (comparers.ContainsKey(type)) {
						OnEvent.Raise(comparerType, EventType.AlreadyRegistered, type);
					} else {
						comparers.Add(type, comparer);
						OnEvent.Raise(comparerType, EventType.Registered, type);
					}
				}
			}
		}
		
		/// <summary>
		/// Finds or creates a sort order comparer for type T.
		/// 
		/// Note: Do not call this method directly as it is part of the internal API and a new comparers may be created on every call.  Use Smooth.Collections.Comparer<T>.Default to get the default sort order comparer.
		/// </summary>
		public static IComparer<T> Comparer<T>() {
			var comparerType = ComparerType.Comparer;
			var type = typeof(T);
			
			lock(comparers) {
				object registered;
				if (comparers.TryGetValue(type, out registered)) {
					OnEvent.Raise(comparerType, EventType.FindRegistered, type);
					return (IComparer<T>) registered;
				}
			}

			OnEvent.Raise(comparerType, EventType.FindUnregistered, type);

			if (typeof(IComparable<T>).IsAssignableFrom(type)) {
				OnEvent.Raise(comparerType, EventType.EfficientDefault, type);
				return System.Collections.Generic.Comparer<T>.Default;
			}
			
			if (config.UseJit) {
				var custom = config.Comparer<T>();
				if (custom.isSome) {
					OnEvent.Raise(comparerType, EventType.CustomJit, type);
					return custom.value;
				}
			}

			if ((typeof(IComparable)).IsAssignableFrom(type)) {
				OnEvent.Raise(comparerType, type.IsValueType ? EventType.InefficientDefault : EventType.EfficientDefault, type);
				return System.Collections.Generic.Comparer<T>.Default;
			}

			OnEvent.Raise(comparerType, EventType.InvalidDefault, type);
			return System.Collections.Generic.Comparer<T>.Default;
		}
		
		#endregion

		#region EqualityComparer

		/// <summary>
		/// Registers an equality comparer with the specified equals function for type T.
		/// </summary>
		public static void Register<T>(Func<T, T, bool> equals) {
			Register<T>(new FuncEqualityComparer<T>(equals));
		}
		
		/// <summary>
		/// Registers an equality comparer with the specified equals and hashCode functions for type T.
		/// </summary>
		public static void Register<T>(Func<T, T, bool> equals, Func<T, int> hashCode) {
			Register<T>(new FuncEqualityComparer<T>(equals, hashCode));
		}
		
		/// <summary>
		/// Registers the specified equality comparer for type T.
		/// 
		/// Note: On platforms without JIT compilation, the supplied comparer should inherit from Smooth.Collections.EqualityComparer<T> in order to force the AOT compiler to create the proper generic types.
		/// </summary>
		public static void Register<T>(IEqualityComparer<T> equalityComparer) {
			var comparerType = ComparerType.EqualityComparer;
			var type = typeof(T);
			
			if (equalityComparer == null) {
				Debug.LogError("Tried to register a null equality comparer for: " + type.FullName);
			} else {
				lock (equalityComparers) {
					if (equalityComparers.ContainsKey(type)) {
						OnEvent.Raise(comparerType, EventType.AlreadyRegistered, type);
					} else {
						equalityComparers.Add(type, equalityComparer);
						OnEvent.Raise(comparerType, EventType.Registered, type);
					}
				}
			}
		}

		/// <summary>
		/// Finds or creates an equality comparer for type T.
		/// 
		/// Note: Do not call this method directly as it is part of the internal API and a new comparers may be created on every call.  Use Smooth.Collections.EqualityComparer<T>.Default to get the default equality comparer.
		/// </summary>
		public static IEqualityComparer<T> EqualityComparer<T>() {
			var comparerType = ComparerType.EqualityComparer;
			var type = typeof(T);

			lock(equalityComparers) {
				object registered;
				if (equalityComparers.TryGetValue(type, out registered)) {
					OnEvent.Raise(comparerType, EventType.FindRegistered, type);
					return (IEqualityComparer<T>) registered;
				}
			}

			OnEvent.Raise(comparerType, EventType.FindUnregistered, type);
			
			if (typeof(IEquatable<T>).IsAssignableFrom(type)) {
				OnEvent.Raise(comparerType, EventType.EfficientDefault, type);
				return System.Collections.Generic.EqualityComparer<T>.Default;
			}

			if (config.UseJit) {
				var custom = config.EqualityComparer<T>();
				if (custom.isSome) {
					OnEvent.Raise(comparerType, EventType.CustomJit, type);
					return custom.value;
				}
			}

			OnEvent.Raise(comparerType, type.IsValueType ? EventType.InefficientDefault : EventType.EfficientDefault, type);
			return System.Collections.Generic.EqualityComparer<T>.Default;
		}

		#endregion

		#region Initialization

		static Finder() {
			try {
				var customType = Type.GetType(customConfigurationClassName, false);
				if (customType != null) {
					if (typeof(Configuration).IsAssignableFrom(customType)) {
						var ctor = customType.GetConstructor(Type.EmptyTypes);
						if (ctor != null) {
							config = (Configuration) customType.GetConstructor(Type.EmptyTypes).Invoke(null);
						} else {
							Debug.LogError("A " + customConfigurationClassName + " class exists in your project, but will not be used because it does have a default constructor.");
						}
					} else {
						Debug.LogError("A " + customConfigurationClassName + " class exists in your project, but will not be used because it does not inherit from " + typeof(Configuration).FullName + ".");
					}
				}
			} catch (Exception e) {
				Debug.LogError(e);
			} finally {
				config = config ?? new Configuration();
			}

			try {
				config.RegisterComparers();
			} catch (Exception e) {
				Debug.LogError(e);
			}
		}

		#endregion
	}
}
