using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime;
using Smooth.Algebraics;
using Smooth.Collections;
using Smooth.Compare.Comparers;
using Smooth.Platform;

namespace Smooth.Compare {
	///
	/// Configuration class for Smooth.Compare.
	///
	/// To supply a custom configuration, simply add a class to your project called Smooth.Compare.CustomConfiguration that inherits from this type.
	/// 
	/// If a custom configuration exists, it will override the the default configuration.
	/// 
	/// Note: Don't edit this class directly, as it may get updated in future versions of Smooth.Compare.
	///
	public class Configuration {
		///
		/// Default constructor that simply adds a listener to Finder.OnEvent.
		/// 
		/// If you supply a custom configuration, don't register types or do any comparsions from the constructor as the finder will not be fully initialized yet.
		///
		public Configuration() {
			Finder.OnEvent.Handle += HandleFinderEvent;
		}

		/// <summary>
		/// Method called by the finder to set up registrations before any comparer requests are handled.
		/// 
		/// If you supply a custom configuration and want to apply the default registrations, add a call to base.RegisterComparers() from your method override.
		/// </summary>
		public virtual void RegisterComparers() {
			#region Common structs without type specific equality and/or hashcode methods
			
			Finder.Register<Color32>((a, b) => Color32ToInt(a) == Color32ToInt(b), Color32ToInt);
			
			#endregion
			
			#region Basic types for platforms without JIT compilation
			
			if (NoJit) {
				//
				// Note: On non-JIT platforms, you must be careful to not get too cute / abstract with your registrations or the AOT may not create
				// all the necessary generic types and you'll end up with JIT exceptions.  When in doubt, add each registration directly with a
				// individual method call the compiler can easily discover with static inspection, and if supplying your own comparer
				// implementation either inherit from the corresponding Smooth.Collections.Comparer or wrap your comparer in a FuncComparer to
				// force the compiler to create the proper types.
				//
				
				#region System built-in types
				
				Finder.RegisterIComparableIEquatable<Boolean>();
				
				Finder.RegisterIComparableIEquatable<Char>();
				
				Finder.RegisterIComparableIEquatable<Byte>();
				Finder.RegisterIComparableIEquatable<SByte>();
				
				Finder.RegisterIComparableIEquatable<Int16>();
				Finder.RegisterIComparableIEquatable<UInt16>();
				
				Finder.RegisterIComparableIEquatable<Int32>();
				Finder.RegisterIComparableIEquatable<UInt32>();
				
				Finder.RegisterIComparableIEquatable<Int64>();
				Finder.RegisterIComparableIEquatable<UInt64>();
				
				Finder.RegisterIComparableIEquatable<Single>();
				Finder.RegisterIComparableIEquatable<Double>();
				
				Finder.RegisterIComparableIEquatable<Decimal>();
				
				#endregion
				
				#region System.Runtime handles
				
				Finder.Register<RuntimeTypeHandle>((a, b) => a.Equals(b));
				Finder.Register<RuntimeFieldHandle>((a, b) => a == b);
				Finder.Register<RuntimeMethodHandle>((a, b) => a == b);
				
				#endregion
				
				#region UnityEngine structs
				
				//
				// Note: UnityEngine structs do not adhere to the contract of equality.
				//
				// Thus they should not be used as Dictionary keys or in other use cases that rely on a correct equality implementation.
				//
				
				Finder.Register<Color>((a, b) => a == b);
				
				Finder.Register<Vector2>((a, b) => a == b);
				Finder.Register<Vector3>((a, b) => a == b);
				Finder.Register<Vector4>((a, b) => a == b);
				
				Finder.Register<Quaternion>((a, b) => a == b);
				
				#endregion
				
				#region UnityEngine enums
				
				Finder.RegisterEnum<AudioSpeakerMode>();
				Finder.RegisterEnum<EventModifiers>();
				Finder.RegisterEnum<UnityEngine.EventType>();
				Finder.RegisterEnum<KeyCode>();
				Finder.RegisterEnum<PrimitiveType>();
				Finder.RegisterEnum<RuntimePlatform>();

				#endregion

				#region Smooth enums
				
				Finder.RegisterEnum<BasePlatform>();
				Finder.RegisterEnum<ComparerType>();
				Finder.RegisterEnum<EventType>();

				#endregion
			}
			
			#endregion
		}

		/// <summary>
		/// Listens for finder events which are useful for finding potential comparison problems.
		/// 
		/// The default implementation logs warnings on registration collisions, the use of inefficient or invalid comparers, and unregistered find requests for value types if JIT is disabled.
		/// </summary>
		public virtual void HandleFinderEvent(ComparerType comparerType, EventType eventType, Type type) {
			switch (eventType) {
			case EventType.FindUnregistered:
				if (NoJit && type.IsValueType) {
					Debug.LogWarning("A " + comparerType.ToStringCached() + " has been requested for a non-registered value type with JIT disabled, this is a fragile operation and may result in a JIT exception.\nType:" + type.FullName);
				}
				break;
			case EventType.InefficientDefault:
				Debug.LogWarning("A " + comparerType.ToStringCached() + " has been requested that will perform inefficient comparisons and/or cause boxing allocations.\nType:" + type.FullName);
				break;
			case EventType.InvalidDefault:
				Debug.LogWarning("A " + comparerType.ToStringCached() + " has been requested for a non-comparable type.  Using the comparer will cause exceptions.\nType:" + type.FullName);
				break;
			case EventType.AlreadyRegistered:
				Debug.LogWarning("Tried to register a " + comparerType.ToStringCached() + " over an existing registration.\nType: " + type.FullName);
				break;
			default:
				break;
			}
		}

		/// <summary>
		/// This can be used to override the platform setting and enable or disable automatic comparer creation, which can be quite useful while testing in different environments.
		/// </summary>
		public virtual bool UseJit { get { return Runtime.hasJit; } }
		
		/// <summary>
		/// Convenience method for !UseJit.
		/// </summary>
		public bool NoJit { get { return !UseJit; } }

		/// <summary>
		/// If JIT is enabled, this method is called by the finder when it is asked to supply a sort order comparer for an unregistered, non-IComparable<T> type.
		///
		/// If you want to write custom comparers using reflection, you can do so by overriding this method.
		/// </summary>
		/// <returns>An option containing a sort order comparer for type T, or None to use the default comparer</returns>
		public virtual Option<IComparer<T>> Comparer<T>() {
			return Factory.Comparer<T>();
		}

		/// <summary>
		/// If JIT is enabled, this method is called by the finder when it is asked to supply an equality comparer for an unregistered, non-IEquatable<T> type..
		///
		/// If you want to write custom equality comparers using reflection, you can do so by overriding this method.
		/// </summary>
		/// <returns>An option containing an equality comparer for type T, or None to use the default comparer</returns>
		public virtual Option<IEqualityComparer<T>> EqualityComparer<T>() {
			return Factory.EqualityComparer<T>();
		}

		/// <summary>
		/// Converts a 32-bit color to a 32-bit integer without loss of information
		/// </summary>
		public static int Color32ToInt(Color32 c) {
			return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
		}
	}
}
