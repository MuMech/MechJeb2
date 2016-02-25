using UnityEngine;
using System;
using System.Collections.Generic;
using Smooth.Collections;
using Smooth.Compare;
using Smooth.Compare.Comparers;


//
//
// Example CustomConfiguration for Smooth.Compare.
//
//


//namespace Smooth.Compare {
//	public class CustomConfiguration : Configuration {
//		private bool hasRegistered;
//
//		public override void RegisterComparers() {
//			if (NoJit) {
//				//
//				// If running without JIT, register the KeyValuePair<,> comparers that we use.
//				//
//				// These are handled automatically by the factory if JIT is enabled.
//				//
//				Finder.RegisterKeyValuePair<int, bool>();
//				Finder.RegisterKeyValuePair<int, int>();
//				Finder.RegisterKeyValuePair<int, KeyValuePair<int, int>>();
//
//				//
//				// If running without JIT, register the JITable comparers that we use.
//				//
//				Finder.RegisterIComparableIEquatable<ExampleStruct1>();
//				Finder.Register<ExampleStruct2>((a, b) => a.Equals(b));
//			}
//
//			//
//			// Register sort order comparers for types that don't implement the IComparable<> and equality comparers for value types that don't provide type specific equality checks.
//			// 
//			Finder.Register<ExampleStruct2>((a, b) => string.Compare(a.name, b.name));
//			Finder.Register<ExampleStruct3>((a, b) => string.Compare(a.name, b.name), (a, b) => a.id == b.id);
//			Finder.Register<ExampleClass>((a, b) => string.Compare(a.name, b.name));
//
//			//
//			// Register the standard comparers.
//			//
//			base.RegisterComparers();
//
//			hasRegistered = true;
//		}
//
//		public override void HandleFinderEvent(ComparerType comparerType, EventType eventType, Type type) {
//			//
//			// Log all registrations by other classes for debugging purposes.
//			//
//			if (hasRegistered && eventType == EventType.Registered) {
//				Debug.Log(comparerType.ToStringCached() + " registered for type " + type.FullName);
//			}
//
//			base.HandleFinderEvent(comparerType, eventType, type);
//		}
//
//		//
//		// Manually disable JIT when running iOS builds in the editor and we want to find potential JIT problems.
//		//
//		//public override bool UseJit { get { return false; } }
//
//		//
//		// Manually enable JIT when running iOS builds in a simulator and we want to test functionality and worry about comparers later.
//		//
//		//public override bool UseJit { get { return true; } }
//	}
//
//	public struct ExampleStruct1 : IEquatable<ExampleStruct1>, IComparable<ExampleStruct1> {
//		public uint id;
//		public string name;
//		
//		public override bool Equals(object o) {
//			return o is ExampleStruct1 && this.Equals((ExampleStruct1) o);
//		}
//		
//		public bool Equals(ExampleStruct1 other) {
//			// Note: Only compares ids, but the sort comparer uses names.
//			// To properly adhere to the contract of equality, a.id == b.id must imply a.name == b.name.
//			return this.id == other.id;
//		}
//		
//		public override int GetHashCode() {
//			return (int) id;
//		}
//		
//		public int CompareTo(ExampleStruct1 other) {
//			return string.Compare(this.name, other.name);
//		}
//		
//		public static bool operator == (ExampleStruct1 lhs, ExampleStruct1 rhs) {
//			return lhs.Equals(rhs);
//		}
//		
//		public static bool operator != (ExampleStruct1 lhs, ExampleStruct1 rhs) {
//			return !lhs.Equals(rhs);
//		}
//		
//		public static bool operator > (ExampleStruct1 lhs, ExampleStruct1 rhs) {
//			return lhs.CompareTo(rhs) > 0;
//		}
//		
//		public static bool operator < (ExampleStruct1 lhs, ExampleStruct1 rhs) {
//			return lhs.CompareTo(rhs) < 0;
//		}
//		
//		public static bool operator >= (ExampleStruct1 lhs, ExampleStruct1 rhs) {
//			return lhs.CompareTo(rhs) >= 0;
//		}
//		
//		public static bool operator <= (ExampleStruct1 lhs, ExampleStruct1 rhs) {
//			return lhs.CompareTo(rhs) <= 0;
//		}
//		
//		public override string ToString() {
//			return name;
//		}
//	}
//
//	public struct ExampleStruct2 {
//		//
//		// The same thing as ExampleStruct1, but doesn't implement the comparable interfaces or provide a type specific == operator.
//		//
//
//		public uint id;
//		public string name;
//		
//		public override bool Equals(object o) {
//			return o is ExampleStruct2 && this.Equals((ExampleStruct2) o);
//		}
//		
//		public bool Equals(ExampleStruct2 other) {
//			return this.id == other.id;
//		}
//
//		public override int GetHashCode() {
//			return (int) id;
//		}
//
//		public override string ToString() {
//			return name;
//		}
//	}
//
//	public struct ExampleStruct3 {
//		//
//		// The same thing as ExampleStruct1, but doesn't override Equals(object) or have a type specific equals method.
//		//
//		// Thus we use a priori knowledge about the type to make allocation free equality comparisons.
//		//
//		
//		public uint id;
//		public string name;
//		
//		public override int GetHashCode() {
//			return (int) id;
//		}
//		
//		public override string ToString() {
//			return name;
//		}
//	}
//	
//	public class ExampleClass {
//		//
//		// Class version of ExampleStruct, without a type specific equals method.
//		//
//		// Equality comparisons won't box / allocate, but we still want to register a sort order comparer.
//		//
//		
//		public uint id;
//		public string name;
//		
//		public override bool Equals(object o) {
//			return o is ExampleClass && this.id == ((ExampleClass) o).id;
//		}
//		
//		public override int GetHashCode() {
//			return (int) id;
//		}
//		
//		public override string ToString() {
//			return name;
//		}
//	}
//}
